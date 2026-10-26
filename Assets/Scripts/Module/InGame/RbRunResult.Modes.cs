using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 결과 화면의 탑 · 밤 판 — 시안 Screens/result-tower · result-night · tower-fail.png.
    /// 스토리 결과와 같은 판에 머리글만 글자(「2F CLEAR」 · 「128 KILLS」)로 바꾸고, 기록 · 보상 · 버튼을 모드에 맞춘다.
    /// 「다음 층 도전 · 다시 도전」 은 입장 1번을 쓰고 바로 다시 판으로(옛 TowerResultPopup · NightResultPopup 과 같은 규칙).
    /// </summary>
    public sealed partial class RbRunResult
    {
        private enum ResultMode { Story, Tower, Night }

        private const float TilePitch = 524f * 2f / 3f, TileCenter = 360f;   // 재화 칸 500 · 간격 24(시안 1080)
        private static readonly Color FailRed = new(1f, 0.35f, 0.35f);

        private ResultMode _mode;
        private bool _canGo, _leaving;
        private int _goFloor, _nightDay;

        public void ShowTower(TowerFinishedEvent e, SpriteAtlas faces)
        {
            Bind();
            _mode = ResultMode.Tower;
            Open(e.IsFloorCleared);
            bool clear = e.IsFloorCleared;
            Headline(clear ? string.Format(Localize.FromTable("ui.rb.result.tower_clear", "{0}F CLEAR"), e.PlayedFloor)
                           : string.Format(Localize.FromTable("ui.rb.result.tower_fail", "{0}F FAILED"), e.PlayedFloor), clear);
            _ui.SetText("RbResultChapter", Localize.FromTable("ui.rb.src.tower", "망령의 탑"));
            int max = CoreModule.TryGet<IModeProgress>(out var m) && m.Table != null ? m.Table.FreeEntriesPerDay : 3;
            Chip(string.Format(Localize.FromTable("ui.rb.result.entries", "남은 입장 {0}/{1}"), e.EntriesLeft, max), false);

            Record(0, Localize.FromTable("ui.rb.result.floor", "층"), $"{e.PlayedFloor}F");
            Record(1, Localize.FromTable("ui.rb.result.possess", "빙의 · 잃은 몸"), $"{e.PossessCount} · {e.LostCount}");
            Record(2, null, Clock(e.ElapsedSeconds));

            // 클리어 = 첫 클리어 보상(주에 한 번) · 실패 = 키우면 좋은 몸
            bool reward = clear && (e.RewardGold > 0 || (e.ShardHostKeys != null && e.ShardHostKeys.Length > 0));
            _ui.SetText("RbResultRewardTitle", clear
                ? (reward ? Localize.FromTable("ui.rb.result.first_clear", "첫 클리어 보상") : Localize.FromTable("ui.tower.first_clear_done", "이번 주에 이미 받은 보상이에요"))
                : Localize.FromTable("ui.rb.result.suggest", "이 층에 맞는 몸"));
            _ui.SetActive("RbResultGoldTile", reward && e.RewardGold > 0);
            _ui.SetText("RbResultGoldValue", e.RewardGold.ToString("N0"));
            // 몸 카드 줄 — 클리어는 받은 조각(×n), 실패는 이 층에 맞는 몸(조각 줄 없이 얼굴 · 이름만)
            string[] keys = clear ? e.ShardHostKeys : e.SuggestHostKeys;
            bool show = keys != null && keys.Length > 0 && (reward || !clear);
            ShowHosts(show ? keys : null, clear ? e.ShardCounts : null, faces);
            _ui.SetActive("RbResultExpTile", false);
            CenterTiles();

            _canGo = e.EntriesLeft > 0;
            _goFloor = clear ? e.NextFloor : e.PlayedFloor;
            Buttons(Localize.FromTable("ui.rb.result.to_tower", "탑으로"),
                    !_canGo ? Localize.Get("ui.tower.no_entries")
                    : clear ? Localize.FromTable("ui.rb.result.next_floor", "다음 층 도전") : Localize.FromTable("ui.rb.result.again", "다시 도전"));
        }

        public void ShowNight(NightFinishedEvent e, SpriteAtlas faces)
        {
            Bind();
            _mode = ResultMode.Night;
            Open(e.IsCleared);
            _nightDay = e.NightDayIndex;
            Headline(string.Format(Localize.FromTable("ui.rb.result.night_kills", "{0} KILLS"), e.FinalKills.ToString("N0")), true);
            string day = DayKey(e.NightDayIndex);
            _ui.SetText("RbResultChapter", $"{Localize.Get($"ui.night.day.{day}")} · {Localize.Get($"ui.night.day_rule.{day}")}");
            Chip(e.IsNewBest ? string.Format(Localize.FromTable("ui.rb.result.best_new", "최고 처치 {0}  <color=#FFC93D>NEW</color>"), e.FinalKills.ToString("N0"))
                             : string.Format(Localize.FromTable("ui.rb.result.best", "최고 처치 {0}"), Mathf.Max(e.PreviousBest, e.FinalKills).ToString("N0")), false);

            Record(0, Localize.FromTable("ui.rb.result.survived", "버틴 시간"), Clock(e.SurvivedSeconds));
            Record(1, null, e.FinalKills.ToString("N0"));
            Record(2, Localize.FromTable("ui.rb.result.level", "도달 Lv"), e.ReachedLevel.ToString());

            _ui.SetText("RbResultRewardTitle", Localize.FromTable("ui.rb.result.rewards", "획득 보상"));
            // 처치 수만큼 골드(PD 2026-10-26 · 시안 result-night 「432 골드」)
            _ui.SetActive("RbResultGoldTile", e.EarnedGold > 0);
            _ui.SetText("RbResultGoldValue", e.EarnedGold.ToString("N0"));
            ShowHosts(e.ShardHostKeys, e.ShardCounts, faces);
            _ui.SetActive("RbResultExpTile", false);
            CenterTiles();

            // 「다시」는 오늘 요일 판만 — 날이 바뀌었으면 로비에서 오늘 요일로 다시 고른다(밤 3-13)
            bool today = CoreModule.TryGet<IModeProgress>(out var m) && m.TodayIndex == e.NightDayIndex;
            _canGo = e.EntriesLeft > 0 && today;
            Buttons(Localize.FromTable("ui.rb.result.leave", "나가기"),
                    _canGo ? Localize.FromTable("ui.rb.result.again", "다시 도전")
                    : !today ? Localize.Get("ui.night.today_only") : Localize.Get("ui.tower.no_entries"));
        }

        // ── 칠하기 ──────────────────────────────────────────

        private void Open(bool clear)
        {
            CoreModule.TryGet(out _player);   // 조각 칸의 몸 이름 — 스토리 판(Show)과 같이 여기서 받는다
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui — 열 때 맨 위
            _leaving = false;
            ApplyLayout(false);
            SetSprite("RbResultBg", clear ? _bgClear : _bgFail);
            SetSprite("RbResultGhost", clear ? _ghostCheer : _ghostSad);
            _ui.SetActive("RbResultWord", false);
        }

        private void Headline(string text, bool good)
        {
            _ui.SetActive("RbResultHeadline", true);
            _ui.SetText("RbResultHeadline", text);
            if (_ui.Get<TextMeshProUGUI>("RbResultHeadline") is TextMeshProUGUI t) t.color = good ? Color.white : FailRed;
        }

        /// <summary>기록 한 줄 — 이름이 null 이면 판에 박힌 이름(처치 · 플레이 시간) 그대로.</summary>
        private void Record(int row, string label, string value)
        {
            if (label != null) _ui.SetText($"RbResultRecLabel{row}", label);
            _ui.SetText(row == 0 ? "RbResultRoomValue" : row == 1 ? "RbResultKillValue" : "RbResultTimeValue", value);
        }

        /// <summary>켜진 재화 칸(골드 · 경험치)을 가운데로 모은다(탑 · 밤은 골드 하나뿐일 때가 많다).</summary>
        private void CenterTiles()
        {
            var on = new System.Collections.Generic.List<RectTransform>(2);
            foreach (var n in new[] { "RbResultGoldTile", "RbResultExpTile" })
                if (_ui.Find(n) is RectTransform r && r.gameObject.activeSelf) on.Add(r);
            float left = TileCenter - TilePitch * (on.Count - 1) * 0.5f;
            for (int i = 0; i < on.Count; i++) on[i].anchoredPosition = new Vector2(left + TilePitch * i, on[i].anchoredPosition.y);
        }

        private void Buttons(string leave, string go)
        {
            _ui.SetText("RbResultLobbyText", leave);
            _ui.SetText("RbResultNextText", go);
            if (_ui.Get<Button>("RbResultNext") is Button b) b.interactable = _canGo;
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }

        private static string DayKey(int day)
            => day switch { 0 => "mon", 1 => "tue", 2 => "wed", 3 => "thu", 4 => "fri", 5 => "sat", _ => "sun" };

        // ── 나가기 ──────────────────────────────────────────

        private void LeaveMode(bool go)
        {
            if (_leaving) return;
            _leaving = true;
            GameSound.Cue("ui.tap");
            LeaveModeAsync(go && _canGo).Forget();   // fire-and-forget: 씬 전환
        }

        private async UniTaskVoid LeaveModeAsync(bool go)
        {
            if (go && CoreModule.TryGet<IModeProgress>(out var modes))
            {
                bool entered = false;
                if (_mode == ResultMode.Tower && modes.TryUseTowerEntry()) { RunModeContext.SetTowerFloor(_goFloor); entered = true; }
                else if (_mode == ResultMode.Night && modes.TryUseNightEntry() && CoreModule.TryGet<IResetClock>(out var clock))
                {
                    RunModeContext.BeginNight(_nightDay, clock.WeekKey);
                    entered = true;
                }
                if (entered)
                {
                    await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest { SceneName = SceneNames.InGame, LoadingStyle = LoadingStyle.Overlay });
                    return;
                }
            }
            RunModeContext.Clear();
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest { SceneName = SceneNames.Lobby, LoadingStyle = LoadingStyle.Overlay });
        }
    }
}
