using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Module.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 인게임 남은 화면(2026-10-24) — 보스 경고 띠(시안 boss) · 다음 방 창(exit) · 밤 몸 고르기(night-pick) · 안내(guide).
    /// 노드는 빌더(RbExtraScreensBuilder.BuildInGame)가 세운다. 옛 화면(_reborn 꺼짐)에서는 아무것도 하지 않는다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float BossWarnSeconds = 3.2f;
        private static readonly string[] FirstRunGuide = { "possess", "stand_fire", "ghost_hp", "lock", "release", "rooms" };

        private RbGuide _rbGuide;
        private int _rbChapterTotal;
        private int _rbStage;
        private int _rbChapter = 1;
        private int _rbBossWarnId;
        private int _rbNightPick = -1;
        private string[] _rbNightKeys;

        private void SubscribeRbExtras(IEventBus bus)
        {
            if (!_reborn) return;
            _rbGuide = GetComponentInChildren<RbGuide>(true);
            foreach (var n in new[] { "RbBossWarn", "RbNextRoom", "RbNightPick", "RbGuide", "RbDangerEdge", "RbNoBodyHint" }) _ui.SetActive(n, false);
            _tokens.Add(bus.Subscribe<CombatHpChangedEvent>(OnRbHp));
            _ui.OnClick("RbNextRoomGo", () => { _ui.SetActive("RbNextRoom", false); _battle?.TakeOnlyExit(); });
            _ui.OnClick("RbNextRoomClose", () => _ui.SetActive("RbNextRoom", false));
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                _ui.OnClick($"RbNightPickCard{i}", () => { _rbNightPick = k; GameSound.Cue("ui.tap"); FillNightPick(); });
            }
            _ui.OnClick("RbNightPickGo", ConfirmNightPick);
            _tokens.Add(bus.Subscribe<RoomEnteredEvent>(OnRbRoomEntered));
            _tokens.Add(bus.Subscribe<ExitOpenedEvent>(_ => ShowNextRoom()));
            _tokens.Add(bus.Subscribe<BossHpChangedEvent>(OnRbBoss));
            _tokens.Add(bus.Subscribe<BuffOfferEvent>(_ => ShowGuideLater("cards", 0.8f, "cards")));
            _tokens.Add(bus.Subscribe<ShopOpenedEvent>(_ => ShowGuideLater("merchant", 0.6f, "merchant")));
            _tokens.Add(bus.Subscribe<NightBannerEvent>(OnRbNightBanner));
        }

        private void OnRbRoomEntered(RoomEnteredEvent e)
        {
            _rbChapterTotal = e.ChapterTotal;
            _rbStage = e.StageInChapter;
            _rbChapter = e.Chapter;
            _ui.SetActive("RbNextRoom", false);
            // 첫 판 첫 방 — 빙의 · 멈춰야 쏨 · 유령 에너지 · 자물쇠 · 놓아주기 · 다음 방(시안 guide, 판을 멈춘다)
            if (!RunModeContext.IsMode && e.RoomIndex == 0) ShowGuideLater("ingame", 1.6f, FirstRunGuide);
        }

        /// <summary>안내는 판이 열린 뒤(가림막이 걷힌 뒤) — 처음 한 번만. 다른 창이 떠 있으면 그 창 위에 뜬다.</summary>
        private void ShowGuideLater(string id, float delay, params string[] topics)
        {
            if (_rbGuide == null || RbGuide.HasSeen(id)) return;
            ShowGuideLaterAsync(id, delay, topics).Forget();   // fire-and-forget: 정해진 시간 뒤 한 번 띄운다
        }

        private async UniTaskVoid ShowGuideLaterAsync(string id, float delay, string[] topics)
        {
            var token = this.GetCancellationTokenOnDestroy();
            if (await UniTask.Delay(System.TimeSpan.FromSeconds(delay), DelayType.UnscaledDeltaTime, cancellationToken: token).SuppressCancellationThrow()) return;
            if (_finished || _rbGuide == null) return;
            _rbGuide.ShowOnce(id, true, topics);
        }

        // ── 몸 없음 · 위험 경고(시안 ghost-hud · fight-danger, PD 2026-10-25 「일단 추가해 보자」) ──
        // 몸이 없으면 몸 칸 · 스킬 단추에 「몸 없음」, 판에서 처음 몸이 없을 때 안내 줄 3초.
        // 몸 없이 유령 체력이 30% 이하면 화면 가장자리가 붉게 숨쉰다 — 빨리 빙의하라는 뜻(몸을 입고 있으면 호스트 막대가 따로 있다)

        private const float DangerRatio = 0.3f;
        private const float NoBodyHintSeconds = 3f;
        private int _rbBodyState = -1;   // -1 모름 · 0 몸 없음 · 1 몸 있음
        private bool _rbNoBodyHintShown, _rbDanger;

        private void OnRbHp(CombatHpChangedEvent e)
        {
            int body = e.HasHost ? 1 : 0;
            if (body != _rbBodyState)
            {
                _rbBodyState = body;
                _ui.SetActive("RbNoBodyText", !e.HasHost);
                _ui.SetActive("SkillNoBodyText", !e.HasHost);
                _ui.SetActive("UltNoBodyText", !e.HasHost);
                // 몸이 있으면 스킬 묶음(액티브 · 필살기)만, 몸을 잃으면 스킬을 걷고 빙의 버튼만 크게 숨쉬며(PD 2026-10-27)
                _ui.SetActive("RbSkillGroup", e.HasHost);
                _ui.SetActive("RbPossessGroup", !e.HasHost);
                if (!e.HasHost) PossessBreathAsync(++_rbPossessBreathId).Forget();   // fire-and-forget: 몸을 입으면 스스로 멈춘다
                else _rbPossessBreathId++;
                if (e.HasHost) _ui.SetActive("RbNoBodyHint", false);
                else if (!_rbNoBodyHintShown && !_finished)
                {
                    _rbNoBodyHintShown = true;
                    NoBodyHintAsync().Forget();   // fire-and-forget: 3초 뒤 걷는다
                }
            }
            bool danger = !e.HasHost && !_finished && e.GhostHpMax > 0 && e.GhostHp > 0 && e.GhostHp <= e.GhostHpMax * DangerRatio;
            if (danger == _rbDanger) return;
            _rbDanger = danger;
            if (danger) DangerPulseAsync().Forget();   // fire-and-forget: 위험이 풀리면 스스로 끈다
        }

        private int _rbPossessBreathId;
        private Vector3 _rbPossessHomeScale;

        // 빙의 버튼 숨쉬기 — 몸이 없는 동안 「지금 이것만 누르면 된다」가 읽히게 천천히 커졌다 작아진다(1 → 1.06)
        private async UniTaskVoid PossessBreathAsync(int id)
        {
            if (_ui.Find("PossessButton") is not RectTransform btn) return;
            if (_rbPossessHomeScale == Vector3.zero) _rbPossessHomeScale = btn.localScale;
            var token = this.GetCancellationTokenOnDestroy();
            float t = 0f;
            while (id == _rbPossessBreathId)
            {
                float k = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f / 1.4f);
                btn.localScale = _rbPossessHomeScale * Mathf.Lerp(1f, 1.06f, k);
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
                t += Time.unscaledDeltaTime;
            }
            btn.localScale = _rbPossessHomeScale;
        }

        private async UniTaskVoid NoBodyHintAsync()
        {
            _ui.SetActive("RbNoBodyHint", true);
            var token = this.GetCancellationTokenOnDestroy();
            if (await UniTask.Delay(System.TimeSpan.FromSeconds(NoBodyHintSeconds), DelayType.UnscaledDeltaTime, cancellationToken: token).SuppressCancellationThrow()) return;
            _ui.SetActive("RbNoBodyHint", false);
        }

        private async UniTaskVoid DangerPulseAsync()
        {
            var edge = _ui.Get<Image>("RbDangerEdge");
            if (edge == null) return;
            edge.gameObject.SetActive(true);
            var token = this.GetCancellationTokenOnDestroy();
            // 1초에 한 번 숨쉰다 — 그림이 시안보다 훨씬 진해(0.42 로 찍으니 가장자리가 통째로 빨갰다) 0.08 ~ 0.26 으로 옅게
            for (float t = 0f; _rbDanger && !_finished; t += Time.unscaledDeltaTime)
            {
                edge.color = new Color(1f, 1f, 1f, 0.17f + 0.09f * Mathf.Sin(t * Mathf.PI * 2f));
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            edge.gameObject.SetActive(false);
        }

        // ── 보스 경고 ─────────────────────────────────────────

        private void OnRbBoss(BossHpChangedEvent e)
        {
            if (string.IsNullOrEmpty(e.AppearedBossKey)) return;
            string weak = Localize.Get($"boss.{e.AppearedBossKey}.weak");
            if (weak == $"boss.{e.AppearedBossKey}.weak") weak = string.IsNullOrEmpty(e.BossName) ? "BOSS" : e.BossName;
            _ui.SetText("RbBossWarnWeak", weak);
            if (_ui.Get<Image>("RbBossWarnFace") is Image face && _battle != null && _battle.BossSprite is Sprite s)
            {
                face.sprite = s;
                face.preserveAspect = true;
            }
            _ui.SetActive("RbBossWarn", true);
            if (_ui.Find("RbBossWarn") is Transform t) t.SetAsLastSibling();
            HideBossWarnAsync(++_rbBossWarnId).Forget();   // fire-and-forget: 정해진 시간 뒤 걷는다
            ShowGuideLater("boss", BossWarnSeconds + 0.2f, "boss");
        }

        private async UniTaskVoid HideBossWarnAsync(int id)
        {
            var band = _ui.Find("RbBossWarn");
            if (band == null) return;
            var group = band.GetComponent<CanvasGroup>();
            var token = this.GetCancellationTokenOnDestroy();
            const float fade = 0.3f;
            for (float t = 0f; t < BossWarnSeconds; t += Time.unscaledDeltaTime)
            {
                if (id != _rbBossWarnId) return;
                if (group != null) group.alpha = t < fade ? t / fade : t > BossWarnSeconds - fade ? (BossWarnSeconds - t) / fade : 1f;
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (id == _rbBossWarnId) _ui.SetActive("RbBossWarn", false);
        }

        // ── 다음 방 ───────────────────────────────────────────

        // 적을 다 잡으면 뜨던 「다음 방」 창 — PD 2026-10-27 「일단 안 뜨게」. 출구로 걸어 나가면 그대로 다음 방이다(창은 안내일 뿐)
        private const bool NextRoomPopupOn = false;

        private void ShowNextRoom()
        {
            if (!NextRoomPopupOn) return;
            if (_battle == null || _finished || !_battle.TryNextRoomPreview(out var kind, out bool midBoss)) return;
            OpenNextRoom(kind, midBoss);
        }

        // 캡처 도구(RbShotTool)도 이 길로 연다
        private void OpenNextRoom(RoomKind kind, bool midBoss)
        {
            string key = midBoss ? "midboss" : kind switch
            {
                RoomKind.Elite => "elite",
                RoomKind.Boss => "boss",
                RoomKind.Shop => "shop",
                RoomKind.Rest => "rest",
                RoomKind.Event => "event",
                _ => "normal",
            };
            string icon = key switch
            {
                "elite" => "elite", "boss" => "boss", "midboss" => "midboss", "shop" => "merchant",
                "rest" => "rest", "event" => "event", _ => "battle",
            };
            int total = Mathf.Max(1, _rbChapterTotal);
            _ui.SetText("RbNextRoomStage", $"{_rbChapter}-{_rbStage + 1:00} /{total}");
            _ui.SetText("RbNextRoomName", Localize.Get($"ui.rb.room.{key}"));
            _ui.SetText("RbNextRoomDesc", Localize.Get($"ui.rb.room.{key}_desc"));
            if (_ui.Get<Image>("RbNextRoomIcon") is Image img && FindKitIcon($"rb_icon_room_{icon}_96") is Sprite k) img.sprite = k;
            _ui.SetActive("RbNextRoom", true);
            if (_ui.Find("RbNextRoom") is Transform t) t.SetAsLastSibling();
        }

        [SerializeField] private Sprite[] _rbRoomIcons = System.Array.Empty<Sprite>();

        private Sprite FindKitIcon(string name)
        {
            for (int i = 0; i < _rbRoomIcons.Length; i++)
                if (_rbRoomIcons[i] != null && _rbRoomIcons[i].name == name) return _rbRoomIcons[i];
            return null;
        }

        // ── 밤 몸 고르기 ──────────────────────────────────────

        private void OnRbNightBanner(NightBannerEvent e)
        {
            if (e.BannerKey != "ui.night.start_hint" || _battle == null) return;
            var keys = _battle.NightStartKeys();
            if (keys.Count == 0) return;
            OpenNightPick(keys.ToArray());
        }

        // 캡처 도구(RbShotTool)도 이 길로 연다
        private void OpenNightPick(string[] keys)
        {
            _rbNightKeys = keys;
            _rbNightPick = -1;
            _ui.SetActive("RbNightPick", true);
            if (_ui.Find("RbNightPick") is Transform t) t.SetAsLastSibling();
            Time.timeScale = 0f;   // 고르는 동안 무리가 다가오지 않게(고르면 되돌린다)
            FillNightPick();
        }

        private void FillNightPick()
        {
            if (_rbNightKeys == null) return;
            for (int i = 0; i < 3; i++)
            {
                bool on = i < _rbNightKeys.Length;
                _ui.SetActive($"RbNightPickCard{i}", on);
                if (!on) continue;
                string key = _rbNightKeys[i];
                var e = _battle != null ? CoreModule_GetHost(key) : null;
                SetRbText($"RbNightPickCard{i}Name", e != null ? e.DisplayName : key);
                var kind = AffinityRule.KindOf(key);
                _ui.SetActive($"RbNightPickCard{i}Chip", kind != Affinity.None);
                SetRbText($"RbNightPickCard{i}ChipText", AffinityName(kind));
                _ui.SetActive($"RbNightPickCard{i}Sel", i == _rbNightPick);
                if (_ui.Get<Image>($"RbNightPickCard{i}Art") is Image art)
                {
                    if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
                    show.Show(key, Color.white);
                }
            }
            bool picked = _rbNightPick >= 0;
            if (_ui.Get<Button>("RbNightPickGo") is Button b) b.interactable = picked;
            string name = picked ? (CoreModule_GetHost(_rbNightKeys[_rbNightPick])?.DisplayName ?? _rbNightKeys[_rbNightPick]) : null;
            SetRbText("RbNightPickGoText", picked ? string.Format(Localize.FromTable("ui.rb.nightpick.go", "{0}에 빙의"), name)
                                              : Localize.FromTable("ui.rb.nightpick.pick", "몸을 선택하세요"));
        }

        private void ConfirmNightPick()
        {
            if (_rbNightPick < 0) return;
            _ui.SetActive("RbNightPick", false);
            Time.timeScale = 1f;
            _battle?.PickNightStart(_rbNightPick);
            _rbNightKeys = null;
        }

        private static Game.Character.HostEntry CoreModule_GetHost(string key)
            => GameFramework.Core.Base.CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) ? p.GetHost(key) : null;

        private void SetRbText(string node, string value)
        {
            if (_ui.Get<TextMeshProUGUI>(node) is TextMeshProUGUI t) t.text = value;
        }
    }
}
