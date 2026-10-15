using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 망령의 밤 결과(화면 설계서 N7, 시안 `mock_mode_N7`) — 처치 수 · 최고 기록 · 처치 보상 조각 · 버틴 시간 · 오늘 입장.
    /// 기록 · 보상은 전투가 판 끝에 이미 저장했다. 「다시 ▶」는 입장 1번을 쓰고 같은 요일로 바로 다시.
    /// 노드는 `ModeInGameBuilder` 가 세운다.
    /// </summary>
    public sealed class NightResultPopup : MonoBehaviour
    {
        private UIBinder _ui;
        private SpriteAtlas _faces;
        private bool _leaving;
        private bool _canAgain;
        private int _day;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("NightResultLobbyButton", () => Leave(false));
            _ui.OnClick("NightResultAgainButton", () => Leave(true));
        }

        public void Show(NightFinishedEvent e)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowAsync(e).Forget();   // fire-and-forget: 얼굴 아틀라스가 오면 초상을 채운다
        }

        private async UniTaskVoid ShowAsync(NightFinishedEvent e)
        {
            Fill(e);
            var content = _ui.Find("NightResultContent");
            if (content != null) ModeResultPips.PopAsync(content, this).Forget();   // fire-and-forget: 0.38초 등장
            if (_faces != null) return;
            try { _faces = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>("atlas/hostface"); }
            catch (Exception ex) { Debug.LogWarning($"[밤 결과] 얼굴 아틀라스 로드 실패 — {ex.Message}"); }
            if (this != null) Fill(e);
        }

        private void Fill(NightFinishedEvent e)
        {
            _day = e.NightDayIndex;
            string day = DayKey(e.NightDayIndex);
            _ui.SetText("NightResultTitle", Localize.Get(e.IsCleared ? "ui.night.result.clear" : "ui.night.result.fail"));
            string sub = Localize.Get($"ui.night.day.{day}") + " · " + Localize.Get($"ui.night.day_rule.{day}");
            if (e.IsCleared) sub += " · " + Localize.Format("ui.night.result.boss", BossName(e.BossKey));
            _ui.SetText("NightResultSub", sub);

            _ui.SetText("NightResultKillLabel", Localize.Get("ui.night.result.kills"));
            _ui.SetText("NightResultKillText", e.FinalKills.ToString("N0"));
            _ui.SetActive("NightResultBestText", e.IsNewBest);
            _ui.SetText("NightResultBestText", Localize.Format("ui.night.result.best", e.PreviousBest));

            int per = CoreModule.TryGet<IModeProgress>(out var m) && m.Table != null ? m.Table.KillsPerShard : 50;
            int bonus = m != null && m.Table != null ? m.Table.ClearShardBonus : 3;
            _ui.SetText("NightResultRewardLabel", e.IsCleared ? Localize.Format("ui.night.result.reward_clear", per, bonus)
                                                              : Localize.Format("ui.night.result.reward", per));
            bool any = e.ShardHostKeys != null && e.ShardHostKeys.Length > 0;
            for (int i = 0; i < 3; i++)
            {
                bool on = any && i < e.ShardHostKeys.Length;
                _ui.SetActive($"NightResultFace{i}Frame", on);
                if (!on) continue;
                var img = _ui.Get<Image>($"NightResultFace{i}");
                var s = _faces != null ? _faces.GetSprite($"face_{e.ShardHostKeys[i]}") : null;
                if (img != null) { img.sprite = s; img.enabled = s != null; }
                _ui.SetText($"NightResultFace{i}Text", $"×{e.ShardCounts[i]}");
            }
            _ui.SetText("NightResultRewardEmpty", any ? string.Empty : Localize.Format("ui.night.result.reward_none", per));

            int sec = Mathf.FloorToInt(e.SurvivedSeconds);
            _ui.SetText("NightResultStatText", Localize.Format("ui.night.result.stat_full",
                $"{sec / 60}:{sec % 60:00}", e.ReachedLevel, e.PossessCount, e.LostCount));

            int max = m != null && m.Table != null ? m.Table.FreeEntriesPerDay : 3;
            _ui.SetText("NightResultEntryLabel", Localize.Get("ui.tower.entries_label"));
            _ui.SetText("NightResultEntryText", Localize.Format("ui.tower.entries", e.EntriesLeft, max));
            ModeResultPips.Set(_ui, "NightResultPip", e.EntriesLeft);

            // 「다시」는 오늘 요일 판만 — 날이 바뀌었으면 로비에서 오늘 요일로 다시 고른다(밤 3-13)
            bool today = m != null && m.TodayIndex == e.NightDayIndex;
            _canAgain = e.EntriesLeft > 0 && today;
            _ui.SetText("NightResultLobbyText", Localize.Get("ui.night.result.lobby"));
            // 못 다시 하면 버튼은 남기고 흐리게 막는다 — 왜 안 되는지를 버튼 글자로
            if (_ui.Get<Button>("NightResultAgainButton") is { } btn) btn.interactable = _canAgain;
            if (_ui.Get<Image>("NightResultAgainButton") is { } againImg)
                againImg.color = _canAgain ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            _ui.SetText("NightResultAgainText", _canAgain ? Localize.Get("ui.tower.retry")
                : !today ? Localize.Get("ui.night.today_only") : Localize.Get("ui.tower.no_entries"));
        }

        private static string DayKey(int day)
            => day switch { 0 => "mon", 1 => "tue", 2 => "wed", 3 => "thu", 4 => "fri", 5 => "sat", _ => "sun" };

        private static string BossName(string key)
        {
            string name = Localize.Get($"boss.{key}.name");
            return name == $"boss.{key}.name" ? key : name;
        }

        private void Leave(bool again)
        {
            if (_leaving) return;
            _leaving = true;
            LeaveAsync(again && _canAgain).Forget();   // fire-and-forget: 씬 전환
        }

        private async UniTaskVoid LeaveAsync(bool again)
        {
            if (again && CoreModule.TryGet<IModeProgress>(out var modes) && modes.TryUseNightEntry()
                && CoreModule.TryGet<IResetClock>(out var clock))
            {
                RunModeContext.BeginNight(_day, clock.WeekKey);
                await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
                {
                    SceneName = SceneNames.InGame,
                    LoadingStyle = LoadingStyle.Overlay,
                });
                return;
            }
            RunModeContext.Clear();
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.Lobby,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }
    }
}
