using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.Module.InGame;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 망령의 탑 입장 패널(화면 T1) — 이번 주 층 · 층 사다리 · 지금 층 카드 · 소탕 · 입장 3번 · 도전.
    /// 화면 설계서 Projects/AVSR/wireframes/AVSR_Screen_Modes.md 3절 · 시안 _exchange/in/mock_mode_T1.png.
    /// 노드는 빌더(`ModeEntryPanelBuilder`)가 세운다 — 이름 = 바인딩 키.
    /// </summary>
    public sealed class TowerEntryPanel : MonoBehaviour
    {
        private const int LadderCells = 5;   // 위 2 · 지금 · 아래 2
        private const int FitFaces = 3;

        [SerializeField] private Sprite _cellPast;
        [SerializeField] private Sprite _cellNext;
        [SerializeField] private Sprite _cellNow;
        [SerializeField] private Sprite _cellBoss;
        [SerializeField] private Sprite _pipOn;
        [SerializeField] private Sprite _pipOff;
        [SerializeField] private Sprite _kindBlade;
        [SerializeField] private Sprite _kindForce;
        [SerializeField] private Sprite _kindMagic;

        private UIBinder _ui;
        private IModeProgress _progress;
        private IPlayerDataService _player;
        private SpriteAtlas _faceAtlas;
        private readonly List<IDisposable> _tokens = new();
        private readonly List<string> _fit = new();
        private bool _starting;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("TowerBackButton", Close);
            _ui.OnClick("TowerFloorFitButton", ShowFit);
            _ui.OnClick("TowerSweepButton", OnSweep);
            _ui.OnClick("TowerBuyButton", () => AskBuy(false));
            _ui.OnClick("TowerStartButton", OnStart);
            // ⚠ 여기서 SetActive(false) 를 하지 않는다 — 처음 닫는 것은 주인인 LobbyMainUI 다
        }

        private void OnEnable()
        {
            if (!CoreModule.TryGet<IEventBus>(out var bus)) return;
            _tokens.Add(bus.Subscribe<ModeProgressChangedEvent>(_ => Refresh()));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            // 위 재화 줄(FixedTopBar)만 위에 남기고 아래 바는 덮는다(시안 T1 — 아래에 도전 버튼이 있다)
            var top = transform.parent != null ? transform.parent.Find("FixedTopBar") : null;
            if (top == null) transform.SetAsLastSibling();
            else transform.SetSiblingIndex(Mathf.Max(0, top.GetSiblingIndex() - (transform.GetSiblingIndex() < top.GetSiblingIndex() ? 1 : 0)));
            _starting = false;
            CoreModule.TryGet(out _progress);
            CoreModule.TryGet(out _player);
            _progress?.Refresh();
            if (_faceAtlas == null) LoadFacesAsync().Forget();   // fire-and-forget: 얼굴은 늦게 와도 된다
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        // ── 그리기 ──────────────────────────────────────────

        private void Refresh()
        {
            if (!gameObject.activeSelf || _progress == null) return;
            var table = _progress.Table;
            int cleared = _progress.TowerWeekCleared;
            int next = _progress.TowerNextFloor;

            _ui.SetText("TowerTitleText", Localize.Get("ui.tower.title"));
            if (CoreModule.TryGet<IResetClock>(out var clock))
                _ui.SetText("TowerResetText", Localize.Format("ui.tower.reset_in", Span(clock.UntilNextWeek)));
            _ui.SetText("TowerWeekText", Localize.Format("ui.tower.week", cleared));
            int last = _progress.TowerLastWeekBest;
            _ui.SetActive("TowerLastWeekText", last > 0);
            _ui.SetText("TowerLastWeekText", Localize.Format("ui.tower.last_week", last));

            if (table == null) return;

            // 층 사다리 — 위에서부터 next+2, next+1, next(지금), next−1, next−2
            for (int i = 0; i < LadderCells; i++)
            {
                int floor = next + 2 - i;
                var cell = _ui.Find($"TowerLadderCell{i}");
                if (cell == null) continue;
                bool show = floor >= 1;
                cell.gameObject.SetActive(show);
                if (!show) continue;
                var def = table.FloorAt(floor);
                bool now = floor == next, past = floor < next, boss = def != null && def.IsBoss && !past;
                var bg = cell.GetComponent<Image>();
                if (bg != null) bg.sprite = now ? _cellNow : past ? _cellPast : boss ? _cellBoss : _cellNext;
                SetCellText(cell, "TowerLadderFloorText", Localize.Format("ui.tower.floor", floor));
                SetCellText(cell, "TowerLadderNameText", FloorName(def, floor));
                SetCellActive(cell, "TowerLadderBadge", now);
                SetCellActive(cell, "TowerLadderCheck", past);
                SetCellActive(cell, "TowerLadderBossIcon", boss);
                var cg = cell.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = past ? 0.6f : 1f;   // 지난 층은 흐리게(시안 T1)
            }

            // 지금 층 카드
            var cur = table.FloorAt(next);
            _ui.SetText("TowerFloorNumText", Localize.Format("ui.tower.floor", next));
            _ui.SetText("TowerFloorNameText", FloorName(cur, next));
            _ui.SetText("TowerFloorDescText", cur != null ? Localize.Get(cur.DescKey) : "");
            ShowAdvantage(cur);

            _fit.Clear();
            _fit.AddRange(FitHosts(cur));
            _fit.Sort((a, b) => _progress.IsTrained(b).CompareTo(_progress.IsTrained(a)));
            int trained = 0;
            for (int i = 0; i < _fit.Count; i++) if (_progress.IsTrained(_fit[i])) trained++;
            _ui.SetText("TowerFloorFitText", Localize.Format("ui.tower.fit", _fit.Count, trained));
            var fitText = _ui.Get<TMPro.TextMeshProUGUI>("TowerFloorFitText");
            if (fitText != null) fitText.color = trained == 0 ? new Color(1f, 0.42f, 0.42f) : new Color(0.62f, 0.71f, 0.9f);
            for (int i = 0; i < FitFaces; i++)
            {
                string key = i < _fit.Count ? _fit[i] : null;
                SetFace($"TowerFitFace{i}", key);
                _ui.SetActive($"TowerFitFace{i}Frame", key != null);
                _ui.SetActive($"TowerFitStar{i}", key != null && _progress.IsTrained(key));
            }

            var reward = cur != null && cur.IsBoss ? table.FirstClearBoss : table.FirstClear;
            _ui.SetText("TowerFirstClearText", Localize.Get("ui.tower.first_clear"));
            _ui.SetText("TowerFirstClearGoldText", reward.Gold.ToString());
            _ui.SetText("TowerFirstClearShardText", Localize.Format("ui.tower.shard_count", reward.Shards));
            SetFace("TowerFirstClearFace", _fit.Count > 0 ? _fit[0] : null);

            // 소탕
            var step = _progress.SweepNow;
            bool swept = _progress.TowerSweptToday;
            _ui.SetText("TowerSweepTitleText", Localize.Get("ui.tower.sweep"));
            _ui.SetText("TowerSweepSubText", Localize.Format("ui.tower.sweep_basis", cleared));
            _ui.SetText("TowerSweepGoldText", step != null
                ? Localize.Format("ui.tower.sweep_reward", step.Gold, step.Shards)
                : Localize.Get("ui.tower.sweep_locked"));
            _ui.SetText("TowerSweepButtonText", Localize.Get(swept ? "ui.tower.sweep_done" : "ui.tower.sweep_btn"));
            var sweepBtn = _ui.Get<Button>("TowerSweepButton");
            if (sweepBtn != null) sweepBtn.interactable = step != null && !swept;
            _ui.SetText("TowerSweepHintText", Localize.Get("ui.tower.sweep_hint"));

            // 입장
            int entries = _progress.TowerEntries;
            _ui.SetText("TowerEntryLabel", Localize.Get("ui.tower.entries_label"));
            for (int i = 0; i < 3; i++)
            {
                var pip = _ui.Get<Image>($"TowerEntryPip{i}");
                if (pip != null) pip.sprite = i < entries ? _pipOn : _pipOff;
            }
            _ui.SetText("TowerEntryText", Localize.Format("ui.tower.entries", entries, table.FreeEntriesPerDay));
            int cost = _progress.TowerBuyCost;
            _ui.SetText("TowerBuyText", Localize.Get("ui.tower.buy"));
            var buy = _ui.Get<Button>("TowerBuyButton");
            if (buy != null) buy.interactable = cost >= 0;
            _ui.SetText("TowerStartText", entries > 0
                ? Localize.Format("ui.tower.start", next)
                : cost >= 0 ? Localize.Format("ui.tower.start_buy", cost) : Localize.Get("ui.tower.no_entries"));
            _ui.SetText("TowerStartHintText", Localize.Get("ui.tower.start_hint"));
        }

        private void ShowAdvantage(TowerFloor def)
        {
            // 「적 전부 마법 → 무기 몸 유리」 — 속성이 걸린 층만
            Affinity enemy = Affinity.None, adv = Affinity.None;
            if (def != null && def.Rule == TowerRule.EnemyAffinity) { enemy = def.Affinity; adv = Counter(enemy); }
            else if (def != null && def.Rule is TowerRule.BodyAffinity or TowerRule.Boss) adv = def.Affinity;
            bool show = adv != Affinity.None;
            _ui.SetActive("TowerFloorAdvRow", show);
            if (!show) return;
            var enemyIcon = _ui.Get<Image>("TowerFloorAdvEnemyIcon");
            if (enemyIcon != null) { enemyIcon.sprite = KindSprite(enemy); enemyIcon.enabled = enemy != Affinity.None; }
            _ui.SetActive("TowerFloorAdvEnemyText", enemy != Affinity.None);
            _ui.SetText("TowerFloorAdvEnemyText", Localize.Get("ui.tower.adv_enemy"));
            var advIcon = _ui.Get<Image>("TowerFloorAdvIcon");
            if (advIcon != null) advIcon.sprite = KindSprite(adv);
            _ui.SetText("TowerFloorAdvText", Localize.Format("ui.tower.adv", AffinityName(adv)));
        }

        /// <summary>그 층 조건에 맞는 호스트(조건 밖이면 빈 목록이 아니라 전부 — 기획서 3-9 예외).</summary>
        private List<string> FitHosts(TowerFloor def)
        {
            if (_progress == null) return new List<string>();
            var table = _progress.Table;
            Func<HostEntry, bool> match = def == null ? null : def.Rule switch
            {
                TowerRule.BodyRange => h => table.RangeOf(h.HostKey) == def.Range,
                TowerRule.BodyAffinity or TowerRule.Boss => h => AffinityRule.KindOf(h.HostKey) == def.Affinity,
                _ => null,
            };
            var list = _progress.HostsWhere(match);
            return list.Count > 0 ? list : _progress.HostsWhere(null);
        }

        // ── 버튼 ────────────────────────────────────────────

        private void ShowFit()
        {
            // 맞는 호스트 전체 — 키운 몸(★) 먼저. 화면(T2)은 그림이 오면 따로 세운다(지금은 시스템 창)
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _fit.Count; i++)
            {
                if (i > 0) sb.Append(i % 3 == 0 ? "\n" : " · ");
                sb.Append(HostName(_fit[i]));
                if (_progress.IsTrained(_fit[i])) sb.Append(" ★");
            }
            SystemPopup.Show($"{Localize.Format("ui.tower.fit_title", _progress.TowerNextFloor)}\n\n{sb}", null,
                             cancelText: null);
        }

        private void OnSweep()
        {
            var keys = new List<string>();
            var counts = new List<int>();
            if (_progress == null || !_progress.TrySweep(keys, counts, out int gold)) return;
            GameSound.Cue("run.gold");
            SystemPopup.Show($"{Localize.Get("ui.tower.sweep_result")}\n\n{RewardLine(gold, keys, counts)}", null,
                             cancelText: null);
            Refresh();
        }

        private void AskBuy(bool thenStart)
        {
            if (_progress == null) return;
            int cost = _progress.TowerBuyCost;
            if (cost < 0) { SystemPopup.Show(Localize.Get("ui.tower.no_entries"), null, cancelText: null); return; }
            string msg = Localize.Format("ui.mode.buy_ask", cost, _progress.TowerBoughtToday, _progress.Table?.BuyEntriesMax ?? 3);
            SystemPopup.Show(msg, () =>
            {
                if (!_progress.TryBuyTowerEntry())
                {
                    SystemPopup.Show(Localize.Get("ui.mode.buy_no_gem"), null, cancelText: null);
                    return;
                }
                Refresh();
                if (thenStart) OnStart();
            });
        }

        private void OnStart()
        {
            if (_starting || _progress == null) return;
            if (_progress.TowerEntries <= 0) { AskBuy(true); return; }
            if (!_progress.TryUseTowerEntry()) return;
            _starting = true;
            GameSound.Cue("ui.play");
            int week = CoreModule.TryGet<IResetClock>(out var clock) ? clock.WeekKey : 0;
            RunModeContext.BeginTower(_progress.TowerNextFloor, week);
            EnterAsync().Forget();   // fire-and-forget: 씬 전환 — 패널은 씬과 함께 사라진다
        }

        private static async UniTaskVoid EnterAsync()
        {
            if (CoreModule.TryGet<IPlayerDataService>(out var p)) await p.SaveAsync();
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.InGame,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }

        // ── 도우미 ──────────────────────────────────────────

        private async UniTaskVoid LoadFacesAsync()
        {
            try { _faceAtlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>("atlas/hostface"); }
            catch (Exception e) { Debug.LogWarning($"[탑] 얼굴 아틀라스 로드 실패 — {e.Message}"); }
            if (this != null && gameObject.activeSelf) Refresh();
        }

        private void SetFace(string node, string hostKey)
        {
            var img = _ui.Get<Image>(node);
            if (img == null) return;
            var s = hostKey != null && _faceAtlas != null ? _faceAtlas.GetSprite($"face_{hostKey}") : null;
            img.sprite = s;
            img.enabled = s != null;
        }

        private void SetCellText(Transform cell, string name, string text)
        {
            var t = _ui.Find(cell, name);
            var tmp = t != null ? t.GetComponent<TMPro.TextMeshProUGUI>() : null;
            if (tmp != null) tmp.text = text;
        }

        private void SetCellActive(Transform cell, string name, bool on)
        {
            var t = _ui.Find(cell, name);
            if (t != null) t.gameObject.SetActive(on);
        }

        private Sprite KindSprite(Affinity a) => a switch
        {
            Affinity.Blade => _kindBlade,
            Affinity.Force => _kindForce,
            Affinity.Magic => _kindMagic,
            _ => null,
        };

        private static Affinity Counter(Affinity enemy) => enemy switch
        {
            Affinity.Magic => Affinity.Blade,   // 무기 → 마법
            Affinity.Blade => Affinity.Force,   // 파워 → 무기
            Affinity.Force => Affinity.Magic,   // 마법 → 파워
            _ => Affinity.None,
        };

        internal static string AffinityName(Affinity a) => Localize.Get(a switch
        {
            Affinity.Blade => "ui.affinity.blade",
            Affinity.Force => "ui.affinity.force",
            Affinity.Magic => "ui.affinity.magic",
            _ => "ui.affinity.none",
        });

        internal static string FloorName(TowerFloor def, int floor)
        {
            if (def == null) return "";
            string own = Localize.Get($"tower.floor.{floor}.name");
            return own != $"tower.floor.{floor}.name" ? own : Localize.Get(def.NameKey);
        }

        internal static string HostName(string key)
            => CoreModule.TryGet<IPlayerDataService>(out var p) && p.GetHost(key) is { } h
                ? h.DisplayName : key;

        internal static string RewardLine(int gold, List<string> keys, List<int> counts)
        {
            var sb = new System.Text.StringBuilder();
            if (gold > 0) sb.Append(Localize.Format("ui.mode.reward_gold", gold));
            for (int i = 0; i < keys.Count; i++)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append($"{HostName(keys[i])} ×{counts[i]}");
            }
            return sb.ToString();
        }

        private static string Span(TimeSpan t)
            => t.TotalDays >= 1 ? Localize.Format("ui.mode.span_dh", (int)t.TotalDays, t.Hours)
                                : Localize.Format("ui.mode.span_hm", t.Hours, t.Minutes);
    }
}
