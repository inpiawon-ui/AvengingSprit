using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 도전 탭(시안 Screens/challenge · tower · night) — 허브(탑 · 밤 카드) · 망령의 탑(층 사다리 · 첫 클리어 보상 · 소탕 · 도전) · 망령의 밤(요일 · 보상 · 시작).
    /// 진행 · 입장 · 보상은 IModeProgress 그대로(옛 TowerEntryPanel · NightEntryPanel 과 같은 규칙). 화면은 보여 주고 버튼을 건다.
    /// </summary>
    public sealed class RbChallengePage : MonoBehaviour
    {
        [SerializeField] private Sprite _rowNow, _rowPast, _rowLocked;

        private static readonly string[] DayKeys = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };
        private const int Ladder = 5;

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private IModeProgress _progress;
        private SpriteAtlas _faces;
        private bool _starting;
        private int _day;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("ChlBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("ChlHelp", () => _shell.ShowGuide("tower", "night"));   // 시안 guide
            _ui.OnClick("ChlTowerEnter", () => Show("ChlTower"));
            _ui.OnClick("ChlNightEnter", () => Show("ChlNight"));
            _ui.OnClick("ChlTowerPageBack", () => Show(null));
            _ui.OnClick("ChlNightPageBack", () => Show(null));
            _ui.OnClick("ChlTowerStart", StartTower);
            _ui.OnClick("ChlTowerBuy", () => AskBuy(true, false));
            _ui.OnClick("ChlTowerSweep", Sweep);
            _ui.OnClick("ChlTowerFit", ShowFit);
            _ui.OnClick("ChlNightStart", StartNight);
            for (int i = 0; i < 7; i++)
            {
                int d = i;
                _ui.OnClick($"ChlDay{i}", () => { _day = d; GameSound.Cue("ui.tap"); FillNight(); });
            }
        }

        public void Open(SpriteAtlas faces)
        {
            _faces = faces;
            gameObject.SetActive(true);
            CoreModule.TryGet(out _progress);
            _progress?.Refresh();
            _starting = false;
            Show(null);
        }

        /// <summary>탑 · 밤 쪽으로 바로 — 조각 획득처의 「보상 확인」에서 온다.</summary>
        public void ShowSub(string sub) => Show(sub);

        public bool Back()
        {
            if (_ui == null) return false;
            foreach (var s in new[] { "ChlTower", "ChlNight" })
                if (_ui.Find(s) is Transform t && t.gameObject.activeSelf) { Show(null); return true; }
            return false;
        }

        private void Show(string sub)
        {
            _ui.SetActive("ChlHub", sub == null);
            _ui.SetActive("ChlTower", sub == "ChlTower");
            _ui.SetActive("ChlNight", sub == "ChlNight");
            if (_progress == null) return;
            if (sub == null) FillHub();
            else if (sub == "ChlTower") FillTower();
            else { _day = _progress.TodayIndex; FillNight(); }
        }

        private string EntryLine(int left, int max)
        {
            string reset = CoreModule.TryGet<IResetClock>(out var c) ? $"{(int)c.UntilNextDay.TotalHours:00}:{c.UntilNextDay.Minutes:00}:{c.UntilNextDay.Seconds:00}" : "--:--";
            return string.Format(Localize.FromTable("ui.rb.chl.entries", "남은 입장 <color=#FFFFFF>{0}/{1}</color> · 초기화까지 {2}"), left, max, reset);
        }

        // ── 허브 ────────────────────────────────────────────

        private void FillHub()
        {
            var table = _progress.Table;
            int max = table != null ? table.FreeEntriesPerDay : 3;
            _ui.SetText("ChlTowerTitle", Localize.Get("ui.tower.title"));
            _ui.SetText("ChlTowerInfo", string.Format(Localize.FromTable("ui.rb.chl.tower_info", "이번 주 <color=#FFFFFF>{0}층</color> · 지난주 최고 <color=#FFFFFF>{1}층</color>"),
                                                      _progress.TowerWeekCleared, _progress.TowerLastWeekBest));
            _ui.SetText("ChlTowerEntries", EntryLine(_progress.TowerEntries, max));
            int today = _progress.TodayIndex;
            _ui.SetText("ChlNightTitle", Localize.Get("ui.night.title"));
            _ui.SetText("ChlNightInfo", string.Format(Localize.FromTable("ui.rb.chl.night_info", "{0} 최고 처치 <color=#FFFFFF>{1}</color>"),
                                                      Localize.Get($"ui.night.day.{DayKeys[today]}"), _progress.NightBest(today)));
            _ui.SetText("ChlNightEntries", EntryLine(_progress.NightEntries, max));
        }

        // ── 탑 ─────────────────────────────────────────────

        private readonly List<string> _fit = new();

        private void FillTower()
        {
            var table = _progress.Table;
            if (table == null) return;
            int next = _progress.TowerNextFloor;
            for (int i = 0; i < Ladder; i++)
            {
                int floor = next + 3 - i;   // 위에서부터 next+3 … next-1
                var row = _ui.Find($"ChlFloor{i}");
                if (row == null) continue;
                row.gameObject.SetActive(floor >= 1);
                if (floor < 1) continue;
                bool now = floor == next, past = floor < next;
                if (row.GetComponent<Image>() is Image bg) bg.sprite = now ? _rowNow : past ? _rowPast : _rowLocked;
                TextIn(row, $"ChlFloor{i}No", $"{floor}F");
                TextIn(row, $"ChlFloor{i}State", now ? Localize.FromTable("ui.rb.chl.now", "현재 도전")
                                                : past ? "✓ " + Localize.FromTable("ui.rb.chl.cleared", "클리어")
                                                : Localize.FromTable("ui.rb.chl.locked", "잠김"));
                _ui.Find(row, $"ChlFloor{i}Lock")?.gameObject.SetActive(!now && !past);
            }
            var cur = table.FloorAt(next);
            _ui.SetText("ChlFloorTitle", $"{next}F  {TowerEntryPanel.FloorName(cur, next)}");
            _ui.SetText("ChlFloorDesc", cur != null ? Localize.Get(cur.DescKey) : string.Empty);
            var reward = cur != null && cur.IsBoss ? table.FirstClearBoss : table.FirstClear;
            _ui.SetText("ChlRewardGold", reward.Gold.ToString("N0"));
            _ui.SetText("ChlRewardShard", reward.Shards.ToString());
            _fit.Clear();
            _fit.AddRange(_progress.TowerFitHosts(cur));
            _fit.Sort((a, b) => _progress.IsTrained(b).CompareTo(_progress.IsTrained(a)));
            SetFace("ChlRewardShardFace", _fit.Count > 0 ? _fit[0] : null);

            var step = _progress.SweepNow;
            bool swept = _progress.TowerSweptToday;
            _ui.SetText("ChlSweepInfo", step != null ? Localize.Format("ui.tower.sweep_reward", step.Gold, step.Shards) : Localize.Get("ui.tower.sweep_locked"));
            _ui.SetText("ChlTowerSweepText", Localize.Get(swept ? "ui.tower.sweep_done" : "ui.tower.sweep_btn"));
            if (_ui.Get<Button>("ChlTowerSweep") is Button sb) sb.interactable = step != null && !swept;

            int entries = _progress.TowerEntries, cost = _progress.TowerBuyCost;
            _ui.SetText("ChlTowerEntryLine", EntryLine(entries, table.FreeEntriesPerDay));
            if (_ui.Get<Button>("ChlTowerBuy") is Button bb) bb.interactable = cost >= 0;
            _ui.SetText("ChlTowerStartText", entries > 0 ? Localize.Format("ui.tower.start", next)
                                           : cost >= 0 ? Localize.Format("ui.tower.start_buy", cost) : Localize.Get("ui.tower.no_entries"));
        }

        private void ShowFit()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _fit.Count; i++)
            {
                if (i > 0) sb.Append(i % 3 == 0 ? "\n" : " · ");
                sb.Append(TowerEntryPanel.HostName(_fit[i]));
                if (_progress.IsTrained(_fit[i])) sb.Append(" ★");
            }
            Toast($"{Localize.Format("ui.tower.fit_title", _progress.TowerNextFloor)}\n\n{sb}");
        }

        private void Sweep()
        {
            var keys = new List<string>();
            var counts = new List<int>();
            if (!_progress.TrySweep(keys, counts, out int gold)) return;
            GameSound.Cue("run.gold");
            Toast($"{Localize.Get("ui.tower.sweep_result")}\n\n{TowerEntryPanel.RewardLine(gold, keys, counts)}");
            _shell.RefreshHeader();
            FillTower();
        }

        private void AskBuy(bool tower, bool thenStart)
        {
            int cost = tower ? _progress.TowerBuyCost : _progress.NightBuyCost;
            if (cost < 0) { Toast(Localize.Get("ui.tower.no_entries")); return; }
            int bought = tower ? _progress.TowerBoughtToday : _progress.NightBoughtToday;
            // 시안 tower-entry — 공용 구매 확인 창(젬 아이콘 · 「추가 입장」 · 젬 값 · 오늘 n/m)
            int max = _progress.Table?.BuyEntriesMax ?? 3;
            _shell.Notice.ShowConfirm("rb_icon_currency_gem_96", Localize.FromTable("ui.rb.chl.extra_entry", "추가 입장"),
                string.Format(Localize.FromTable("ui.rb.chl.extra_price", "젬 {0}"), cost),
                string.Format(Localize.FromTable("ui.rb.chl.extra_today", "오늘 추가 입장 {0} / {1}"), bought, max), () =>
            {
                if (!(tower ? _progress.TryBuyTowerEntry() : _progress.TryBuyNightEntry())) { _shell.Notice.ShowGemShort(); return; }
                _shell.RefreshHeader();
                if (tower) FillTower(); else FillNight();
                if (thenStart) { if (tower) StartTower(); else StartNight(); }
            });
        }

        private void StartTower()
        {
            if (_starting) return;
            if (_progress.TowerEntries <= 0) { AskBuy(true, true); return; }
            if (!_progress.TryUseTowerEntry()) return;
            _starting = true;
            GameSound.Cue("ui.play");
            RunModeContext.BeginTower(_progress.TowerNextFloor, CoreModule.TryGet<IResetClock>(out var c) ? c.WeekKey : 0);
            EnterAsync().Forget();   // fire-and-forget: 씬 전환 — 화면은 씬과 함께 사라진다
        }

        // ── 밤 ─────────────────────────────────────────────

        private void FillNight()
        {
            var table = _progress.Table;
            int today = _progress.TodayIndex;
            for (int i = 0; i < 7; i++)
            {
                _ui.SetActive($"ChlDay{i}On", i == _day);
                _ui.SetText($"ChlDay{i}Text", Localize.Get($"ui.night.day.{DayKeys[i]}").Substring(0, 1));
            }
            string dayName = Localize.Get($"ui.night.day.{DayKeys[_day]}");
            _ui.SetText("ChlNightDayTitle", $"{dayName} · {Localize.Get($"ui.night.day_rule.{DayKeys[_day]}")}");
            _ui.SetText("ChlNightBest", string.Format(Localize.FromTable("ui.rb.chl.night_info", "{0} 최고 처치 <color=#FFFFFF>{1}</color>"), dayName, _progress.NightBest(_day)));
            _ui.SetText("ChlNightLeft", string.Format(Localize.FromTable("ui.rb.chl.left", "남은 입장 {0}/{1}"), _progress.NightEntries, table != null ? table.FreeEntriesPerDay : 3));
            _ui.SetText("ChlNightRule", Localize.Get($"ui.night.day_desc.{DayKeys[_day]}"));
            _ui.SetText("ChlNightReward", Localize.Format("ui.night.reward", table != null ? table.KillsPerShard : 50, table != null ? table.ClearShardBonus : 3));
            var hosts = NightEntryPanel.DayHosts(table, _day);
            SetFace("ChlNightShardFace", hosts.Count > 0 ? hosts[0] : null);
            bool isToday = _day == today;
            int entries = _progress.NightEntries, cost = _progress.NightBuyCost;
            if (_ui.Get<Button>("ChlNightStart") is Button b) b.interactable = isToday;
            _ui.SetText("ChlNightStartText", !isToday ? Localize.Get("ui.night.today_only")
                                           : entries > 0 ? Localize.Get("ui.night.start")
                                           : cost >= 0 ? Localize.Format("ui.tower.start_buy", cost) : Localize.Get("ui.tower.no_entries"));
        }

        private void StartNight()
        {
            if (_starting || _day != _progress.TodayIndex) return;
            if (_progress.NightEntries <= 0) { AskBuy(false, true); return; }
            if (!_progress.TryUseNightEntry()) return;
            _starting = true;
            GameSound.Cue("ui.play");
            RunModeContext.BeginNight(_progress.TodayIndex, CoreModule.TryGet<IResetClock>(out var c) ? c.WeekKey : 0);
            EnterAsync().Forget();   // fire-and-forget: 씬 전환 — 화면은 씬과 함께 사라진다
        }

        private static async UniTaskVoid EnterAsync()
        {
            if (CoreModule.TryGet<IPlayerDataService>(out var p)) await p.SaveAsync();
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest { SceneName = SceneNames.InGame, LoadingStyle = LoadingStyle.Overlay });
        }

        // ── 도구 ────────────────────────────────────────────

        private void SetFace(string node, string key)
        {
            if (_ui.Get<Image>(node) is not Image img) return;
            img.sprite = key != null && _faces != null ? _faces.GetSprite($"face_{key}") : null;
            img.enabled = img.sprite != null;
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
