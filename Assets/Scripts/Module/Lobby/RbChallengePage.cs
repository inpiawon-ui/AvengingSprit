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
        /// <summary>망령의 밤 배너 — 요일(월~일) 테마 그림 7장(코덱스 납품 rb_art_night_day_*). 비어 있으면 대표 그림 그대로.</summary>
        [SerializeField] private Sprite[] _nightDayArts = Array.Empty<Sprite>();

        private static readonly string[] DayKeys = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };
        private const int Ladder = 5;
        private static readonly Color Cyan = new(0.24f, 0.90f, 1f), Dim = new(0.70f, 0.74f, 0.84f);

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private IModeProgress _progress;
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
            _ui.OnClick("ChlTowerFit", ShowSweepRule);   // 시안: 「소탕 조건 확인」(예전 「맞는 호스트 보기」)
            _ui.OnClick("ChlTowerPageHelp", () => _shell.ShowGuide("tower"));
            _ui.OnClick("ChlNightPageHelp", () => _shell.ShowGuide("night"));
            _ui.OnClick("ChlNightStart", StartNight);
            for (int i = 0; i < 7; i++)
            {
                int d = i;
                _ui.OnClick($"ChlDay{i}", () => { _day = d; GameSound.Cue("ui.tap"); FillNight(); });
            }
        }

        public void Open(SpriteAtlas faces)
        {
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

        private void FillTower()
        {
            var table = _progress.Table;
            if (table == null) return;
            int next = _progress.TowerNextFloor;
            // 다섯 층 묶음(1~5 · 6~10 …) — 시안처럼 1층부터 5층까지 한 판에. 예전엔 지금 층 둘레(next-1 ~ next+3)라 1층 묶음이 안 보였다
            int block = (next - 1) / Ladder * Ladder + 1;
            for (int i = 0; i < Ladder; i++)
            {
                int floor = block + Ladder - 1 - i;   // 위에서부터 5층 … 1층
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
                // 시안: 현재 · 깬 층 흰 글자, 잠긴 층 흐린 글자
                if (_ui.Find(row, $"ChlFloor{i}State")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI st) st.color = now || past ? Color.white : Dim;
                if (_ui.Find(row, $"ChlFloor{i}No")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI no) no.color = now || past ? Color.white : Dim;
                _ui.Find(row, $"ChlFloor{i}Lock")?.gameObject.SetActive(!now && !past);
            }
            var cur = table.FloorAt(next);
            // 시안: 「2층」 + 「첫 클리어 보상 (매주 다시)」(고정 글자)
            _ui.SetText("ChlFloorTitle", string.Format(Localize.FromTable("ui.rb.chl.floor_n", "{0}층"), next));
            var reward = cur != null && cur.IsBoss ? table.FirstClearBoss : table.FirstClear;
            _ui.SetText("ChlRewardGold", reward.Gold.ToString("N0"));
            _ui.SetText("ChlRewardShard", reward.Shards.ToString());

            var step = _progress.SweepNow;
            bool swept = _progress.TowerSweptToday;
            _ui.SetText("ChlSweepInfo", step != null ? Localize.FromTable("ui.rb.chl.sweep_short", "하루 1회 · 입장 횟수 소모 없음") : Localize.Get("ui.tower.sweep_locked"));
            _ui.SetText("ChlTowerSweepText", Localize.Get(swept ? "ui.tower.sweep_done" : "ui.tower.sweep_btn"));
            if (_ui.Get<Button>("ChlTowerSweep") is Button sb) sb.interactable = step != null && !swept;

            int entries = _progress.TowerEntries, cost = _progress.TowerBuyCost;
            if (_ui.Get<Button>("ChlTowerBuy") is Button bb) bb.interactable = cost >= 0;
            StartLabel("ChlTowerStart", entries > 0, Localize.FromTable("ui.rb.chl.challenge", "도전"),
                       cost >= 0 ? Localize.Format("ui.tower.start_buy", cost) : Localize.Get("ui.tower.no_entries"));
        }

        /// <summary>노란 시작 버튼 — 입장이 남았으면 「도전 | 입장 1」, 없으면 「젬 N 추가 입장」 한 줄(칸막이 · 입장 글자 숨김).</summary>
        private void StartLabel(string node, bool hasEntry, string label, string noEntry)
        {
            _ui.SetText(node + "Text", hasEntry ? label : noEntry);
            _ui.SetActive(node + "Bar", hasEntry);
            _ui.SetActive(node + "Cost", hasEntry);
            _ui.SetActive(node + "Ticket", hasEntry);
            if (_ui.Get<TextMeshProUGUI>(node + "Text") is not TextMeshProUGUI t) return;
            var tr = t.rectTransform;
            t.alignment = TextAlignmentOptions.Center;
            if (!hasEntry)
            {
                tr.anchoredPosition = new Vector2(0f, tr.anchoredPosition.y);
                tr.sizeDelta = new Vector2(((RectTransform)tr.parent).rect.width - 40f, tr.sizeDelta.y);
                return;
            }
            // 「글자 | 🎫1」 넷을 한 묶음으로 버튼 가운데에(PD 2026-10-26 「내용이 왜 오른쪽에 쏠려 있어」) — 글자 폭을 재서 놓는다
            var bar = _ui.Find(node + "Bar") as RectTransform;
            var ticket = _ui.Find(node + "Ticket") as RectTransform;
            var cost = _ui.Get<TextMeshProUGUI>(node + "Cost");
            if (bar == null || ticket == null || cost == null) return;
            const float gap = 18f;
            float tw = t.GetPreferredValues(t.text).x, cw = cost.GetPreferredValues(cost.text).x;
            float bw = bar.sizeDelta.x, iw = ticket.sizeDelta.x;
            float total = tw + gap + bw + gap + iw + 4f + cw;
            float x = -total * 0.5f;
            tr.sizeDelta = new Vector2(tw + 4f, tr.sizeDelta.y);
            tr.anchoredPosition = new Vector2(x + tw * 0.5f, tr.anchoredPosition.y); x += tw + gap;
            bar.anchoredPosition = new Vector2(x + bw * 0.5f, bar.anchoredPosition.y); x += bw + gap;
            ticket.anchoredPosition = new Vector2(x + iw * 0.5f, ticket.anchoredPosition.y); x += iw + 4f;
            var cr = cost.rectTransform;
            cr.sizeDelta = new Vector2(cw + 4f, cr.sizeDelta.y);
            cr.anchoredPosition = new Vector2(x + cw * 0.5f, cr.anchoredPosition.y);
        }

        /// <summary>「소탕 조건 확인」 — 이번 주 깬 층에 따라 받는 소탕 보상 단계(표) · 지금 단계.</summary>
        private void ShowSweepRule()
        {
            var table = _progress.Table;
            if (table == null) return;
            var sb = new System.Text.StringBuilder();
            sb.Append(Localize.Get("ui.tower.sweep_hint")).Append("\n\n");
            var now = _progress.SweepNow;
            foreach (var st in table.SweepSteps)
            {
                bool cur = now != null && st.FromFloor == now.FromFloor;
                if (cur) sb.Append("<color=#3EE6FF>");
                sb.Append(Localize.Format("ui.tower.sweep_preview", st.FromFloor, st.Gold.ToString("N0"), st.Shards));
                if (cur) sb.Append("</color>");
                sb.Append("\n");
            }
            _shell.Notice.ShowInfo("rb_icon_currency_gold_96", Localize.FromTable("ui.rb.chl.sweep_rule", "소탕 조건 확인"), sb.ToString().TrimEnd());
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
                _ui.SetText($"ChlDay{i}Text", Localize.Get($"ui.night.day.{DayKeys[i]}").Substring(0, 1));
                if (_ui.Get<TextMeshProUGUI>($"ChlDay{i}Text") is TextMeshProUGUI dt) dt.color = Color.white;   // 시안: 일곱 칸 다 흰 글자
            }
            string dayName = Localize.Get($"ui.night.day.{DayKeys[_day]}");
            _ui.SetText("ChlNightDayTitle", $"{dayName} · {Localize.Get($"ui.night.day_rule.{DayKeys[_day]}")}");
            _ui.SetText("ChlNightBest", string.Format(Localize.FromTable("ui.rb.chl.night_info", "{0} 최고 처치 <color=#FFFFFF>{1}</color>"), dayName, _progress.NightBest(_day)));
            _ui.SetText("ChlNightLeft", string.Format(Localize.FromTable("ui.rb.chl.left", "남은 입장 {0}/{1}"), _progress.NightEntries, table != null ? table.FreeEntriesPerDay : 3));
            _ui.SetText("ChlNightRule", Localize.Get($"ui.night.day_desc.{DayKeys[_day]}"));
            int perKills = table != null ? table.KillsPerShard : 50, bonus = table != null ? table.ClearShardBonus : 3;
            _ui.SetText("ChlNightReward", Localize.Format("ui.night.reward", perKills, bonus).Replace("\n", " · "));
            // 오늘의 보상 두 칸(시안 night) — 골드(처치 수만큼, PD 2026-10-26) · 호스트 조각. 그 요일 최고 처치로 받을 양
            // 「◆」 는 작은 청록 마름모(시안)
            if (_ui.Get<TextMeshProUGUI>("ChlNightRewardTitle") is TextMeshProUGUI rt)
                rt.text = Localize.Get("ui.rb.chl.today_reward").Replace("◆", "<size=60%><color=#3EE6FF>◆</color></size>");
            // 배너 — 요일 테마 그림(PD 2026-10-26 「캐릭터만 올려 놓지 말고 테마식으로 이미지를 만들어야지」)
            if (_day >= 0 && _day < _nightDayArts.Length && _nightDayArts[_day] != null && _ui.Get<Image>("ChlNightArtArt") is Image art)
                art.sprite = _nightDayArts[_day];
            int best = _progress.NightBest(_day);
            _ui.SetText("ChlNightGoldValue", _progress.NightGold(best).ToString("N0"));
            _ui.SetText("ChlNightShardValue", (best / Mathf.Max(1, perKills)).ToString("N0"));
            bool isToday = _day == today;
            int entries = _progress.NightEntries, cost = _progress.NightBuyCost;
            if (_ui.Get<Button>("ChlNightStart") is Button b) b.interactable = isToday;
            StartLabel("ChlNightStart", isToday && entries > 0, Localize.Get("ui.night.start"),
                       !isToday ? Localize.Get("ui.night.today_only")
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

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
