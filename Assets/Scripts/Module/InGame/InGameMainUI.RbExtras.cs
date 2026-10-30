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
        private static readonly string[] FirstRunGuide = { "possess", "stand_fire", "ultimate", "ghost_hp", "rooms" };

        private RbGuide _rbGuide;
        private int _rbChapterTotal;
        private int _rbStage;
        private int _rbChapter = 1;
        private int _rbBossWarnId;

        private void SubscribeRbExtras(IEventBus bus)
        {
            if (!_reborn) return;
            _rbGuide = GetComponentInChildren<RbGuide>(true);
            foreach (var n in new[] { "RbBossWarn", "RbNextRoom", "RbNightPick", "RbGuide", "RbDangerEdge", "RbNoBodyHint" }) _ui.SetActive(n, false);
            _tokens.Add(bus.Subscribe<CombatHpChangedEvent>(OnRbHp));
            _ui.OnClick("RbNextRoomGo", () => { _ui.SetActive("RbNextRoom", false); _battle?.TakeOnlyExit(); });
            _ui.OnClick("RbNextRoomClose", () => _ui.SetActive("RbNextRoom", false));
            _tokens.Add(bus.Subscribe<RoomEnteredEvent>(OnRbRoomEntered));
            _tokens.Add(bus.Subscribe<ExitOpenedEvent>(_ => ShowNextRoom()));
            _tokens.Add(bus.Subscribe<BossHpChangedEvent>(OnRbBoss));
            // 카드 · 상인 설명은 그 창을 열기 직전에 먼저 띄운다(InGameMainUI.GuideFirst) — 예전엔 창이 뜬 뒤 0.6~0.8초에 위로 덮었다
        }

        private void OnRbRoomEntered(RoomEnteredEvent e)
        {
            _rbChapterTotal = e.ChapterTotal;
            _rbStage = e.StageInChapter;
            _rbChapter = e.Chapter;
            _ui.SetActive("RbNextRoom", false);
            // 첫 판 첫 방 — 빙의 · 멈춰야 쏨 · 두 버튼 · 유령 체력 · 다음 방(판을 멈춘다). 자물쇠 · 해제는 뺐다(빙의 파티 규칙엔 없음, PD 2026-10-30)
            if (!RunModeContext.IsMode && e.RoomIndex == 0) ShowGuideLater("ingame", 1.6f, FirstRunGuide);
        }

        /// <summary>
        /// 안내는 판이 열린 뒤(가림막이 걷힌 뒤) — 처음 한 번만.
        /// 다른 창(레벨업 · 상점 · 이벤트 · 제단 · 빙의 파티 · 다른 설명)이 떠 있으면 **닫힐 때까지 기다렸다가** 띄운다
        /// (예전엔 그 창 위에 덮어 떴다 — PD 2026-10-30 「게임하는 데 방해」).
        /// </summary>
        private void ShowGuideLater(string id, float delay, params string[] topics)
        {
            if (_rbGuide == null || RbGuide.HasSeen(id)) return;
            ShowGuideLaterAsync(id, delay, topics).Forget();   // fire-and-forget: 정해진 시간 뒤 · 창이 비면 한 번 띄운다
        }

        private async UniTaskVoid ShowGuideLaterAsync(string id, float delay, string[] topics, System.Action then = null)
        {
            var token = this.GetCancellationTokenOnDestroy();
            if (await UniTask.Delay(System.TimeSpan.FromSeconds(delay), DelayType.UnscaledDeltaTime, cancellationToken: token).SuppressCancellationThrow()) return;
            while (!_finished && _rbGuide != null && AnyWindowOpen())
                if (await UniTask.Delay(200, DelayType.UnscaledDeltaTime, cancellationToken: token).SuppressCancellationThrow()) return;
            if (_finished || _rbGuide == null) return;
            _rbGuide.ShowOnce(id, true, then, topics);
        }

        /// <summary>
        /// 그 창을 열기 전에 처음 보는 설명을 먼저 띄운다. 띄웠으면 true — 부른 쪽은 멈추고, 설명을 닫으면 then 이 이어서 연다.
        /// 이미 봤거나 설명 창이 없으면 false — 부른 쪽이 그대로 창을 연다.
        /// </summary>
        private bool GuideFirst(string id, System.Action then, params string[] topics)
        {
            if (_rbGuide == null || _finished || RbGuide.HasSeen(id)) return false;
            if (_rbGuide.IsOpen) return false;   // 다른 설명이 이미 떠 있으면 이번 것은 다음 기회에(겹쳐 띄우지 않는다)
            return _rbGuide.ShowOnce(id, true, then, topics);
        }

        /// <summary>첫 판 그림 설명을 지금 — 유령 출전 셋 고르기 앞에서 전투가 부른다. 이미 봤거나 다른 설명이 떠 있으면 아무것도 안 한다.</summary>
        public void ShowFirstRunGuideNow()
        {
            if (_rbGuide == null || _finished || _rbGuide.IsOpen || RbGuide.HasSeen("ingame")) return;
            _rbGuide.ShowOnce("ingame", true, FirstRunGuide);
        }

        /// <summary>창이 비면 처음 한 번 그림 설명 — 전투(따라 하기 · 파티)가 부른다.</summary>
        public void GuideLater(string id, params string[] topics) => ShowGuideLater(id, 0.4f, topics);

        /// <summary>그림 설명 없이 — 겹치는 창이 다 닫히면 then 을 부른다(전투의 칸 짚기 안내).</summary>
        public void WhenClearThen(System.Action then) => WhenClearAsync(then).Forget();   // fire-and-forget: 창이 비면 한 번 부른다

        private async UniTaskVoid WhenClearAsync(System.Action then)
        {
            var token = this.GetCancellationTokenOnDestroy();
            if (await UniTask.Delay(400, DelayType.UnscaledDeltaTime, cancellationToken: token).SuppressCancellationThrow()) return;
            while (!_finished && AnyWindowOpen())
                if (await UniTask.Delay(200, DelayType.UnscaledDeltaTime, cancellationToken: token).SuppressCancellationThrow()) return;
            if (!_finished) then?.Invoke();
        }

        /// <summary>그 창을 열기 전에 처음 보는 그림 설명을 먼저 — 띄웠으면 true(닫으면 then).</summary>
        public bool GuideBefore(string id, System.Action then, params string[] topics) => GuideFirst(id, then, topics);

        private static readonly string[] BlockingWindows = { "BuffChoicePanel", "ShopPanel", "EventPanel", "ShrinePanel" };

        /// <summary>설명을 띄우면 겹치는 창이 떠 있나.</summary>
        private bool AnyWindowOpen()
        {
            if (_rbGuide != null && _rbGuide.IsOpen) return true;
            if (_battle != null && (_battle.PartyPopupOpen || _battle.CoachShowing)) return true;
            for (int i = 0; i < BlockingWindows.Length; i++)
                if (_ui.Find(BlockingWindows[i]) is Transform t && t.gameObject.activeInHierarchy) return true;
            return false;
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
                // 빙의 파티는 적을 직접 누른다 — 빙의 버튼은 파티가 꺼졌을 때만(PD 2026-10-30 「이제 빙의 버튼 필요 없네」 · 탑 · 밤도 파티 구조)
                bool possessButton = !e.HasHost && !PartyRule.Enabled;
                _ui.SetActive("RbPossessGroup", possessButton);
                if (possessButton) PossessBreathAsync(++_rbPossessBreathId).Forget();   // fire-and-forget: 몸을 입으면 스스로 멈춘다
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
        private static readonly bool s_nextRoomPopupOn = false;   // 고정값(const)이면 「닿지 않는 코드」 경고가 난다

        private void ShowNextRoom()
        {
            if (!s_nextRoomPopupOn) return;
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
    }
}
