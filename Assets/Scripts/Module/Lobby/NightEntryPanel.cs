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
    /// 망령의 밤 입장 패널(화면 N1) — 요일 줄 · 오늘 조건 · 오늘 호스트 · 최고 처치 · 한 판 흐름 · 입장 3번 · 시작.
    /// 화면 설계서 Projects/AVSR/wireframes/AVSR_Screen_Modes.md 4절 · 시안 _exchange/in/mock_mode_N1.png.
    /// </summary>
    public sealed class NightEntryPanel : MonoBehaviour
    {
        private const int Days = 7;
        private const int Faces = 7;

        [SerializeField] private Sprite _dayCell;
        [SerializeField] private Sprite _dayCellToday;
        [SerializeField] private Sprite _pipOn;
        [SerializeField] private Sprite _pipOff;

        private UIBinder _ui;
        private IModeProgress _progress;
        private SpriteAtlas _faceAtlas;
        private readonly List<IDisposable> _tokens = new();
        private int _shownDay;
        private bool _starting;

        public bool IsOpen => gameObject.activeSelf;

        private static readonly string[] DayKeys = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("NightBackButton", Close);
            _ui.OnClick("NightBuyButton", () => AskBuy(false));
            _ui.OnClick("NightStartButton", OnStart);
            for (int i = 0; i < Days; i++)
            {
                int day = i;   // 클로저가 루프 변수를 잡지 않게 복사한다
                _ui.OnClick($"NightDayCell{i}", () => { _shownDay = day; Refresh(); });
            }
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
            var top = transform.parent != null ? transform.parent.Find("FixedTopBar") : null;
            if (top == null) transform.SetAsLastSibling();
            else transform.SetSiblingIndex(Mathf.Max(0, top.GetSiblingIndex() - (transform.GetSiblingIndex() < top.GetSiblingIndex() ? 1 : 0)));
            _starting = false;
            CoreModule.TryGet(out _progress);
            _progress?.Refresh();
            _shownDay = _progress?.TodayIndex ?? 0;
            if (_faceAtlas == null) LoadFacesAsync().Forget();   // fire-and-forget: 얼굴은 늦게 와도 된다
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (!gameObject.activeSelf || _progress == null) return;
            var table = _progress.Table;
            int today = _progress.TodayIndex;
            _ui.SetText("NightTitleText", Localize.Get("ui.night.title"));

            for (int i = 0; i < Days; i++)
            {
                var cell = _ui.Get<Image>($"NightDayCell{i}");
                if (cell != null) cell.sprite = i == _shownDay ? _dayCellToday : _dayCell;
                _ui.SetText($"NightDayName{i}", Localize.Get($"ui.night.day.{DayKeys[i]}"));
                _ui.SetText($"NightDayRule{i}", Localize.Get($"ui.night.day_short.{DayKeys[i]}"));
            }

            string dayName = Localize.Get($"ui.night.day.{DayKeys[_shownDay]}");
            _ui.SetText("NightDayText", Localize.Format(_shownDay == today ? "ui.night.today" : "ui.night.other_day", dayName));
            _ui.SetText("NightRuleTitleText", Localize.Get($"ui.night.day_rule.{DayKeys[_shownDay]}"));
            _ui.SetText("NightRuleText", Localize.Get($"ui.night.day_desc.{DayKeys[_shownDay]}"));

            var hosts = DayHosts(table, _shownDay);
            hosts.Sort((a, b) => _progress.IsTrained(b).CompareTo(_progress.IsTrained(a)));
            int trained = 0;
            for (int i = 0; i < hosts.Count; i++) if (_progress.IsTrained(hosts[i])) trained++;
            _ui.SetText("NightHostFitText", Localize.Format("ui.night.fit", hosts.Count, trained));
            for (int i = 0; i < Faces; i++)
            {
                string key = i < hosts.Count ? hosts[i] : null;
                var img = _ui.Get<Image>($"NightHostFace{i}");
                if (img != null)
                {
                    var s = key != null && _faceAtlas != null ? _faceAtlas.GetSprite($"face_{key}") : null;
                    img.sprite = s;
                    img.enabled = s != null;
                    img.color = key != null && _progress.IsTrained(key) ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                }
                _ui.SetActive($"NightHostSlot{i}", key != null);
                _ui.SetActive($"NightHostStar{i}", key != null && _progress.IsTrained(key));
            }

            _ui.SetText("NightBestLabel", Localize.Format("ui.night.best", dayName));
            _ui.SetText("NightBestText", _progress.NightBest(_shownDay).ToString());
            _ui.SetText("NightRewardLabel", Localize.Get("ui.night.reward_label"));
            _ui.SetText("NightRewardText", Localize.Format("ui.night.reward", table != null ? table.KillsPerShard : 50,
                                                           table != null ? table.ClearShardBonus : 3));

            _ui.SetText("NightFlowTitle0", "0:00");
            _ui.SetText("NightFlowText0", Localize.Get("ui.night.flow.start"));
            _ui.SetText("NightFlowTitle1", Localize.Get("ui.night.flow.wave_title"));
            _ui.SetText("NightFlowText1", Localize.Get("ui.night.flow.wave"));
            _ui.SetText("NightFlowTitle2", Localize.Format("ui.night.flow.big_title", table != null ? (int)table.BigHostInterval : 45));
            _ui.SetText("NightFlowText2", Localize.Get("ui.night.flow.big"));
            int bossAt = table != null ? (int)table.BossTime : 300;
            _ui.SetText("NightFlowTitle3", $"{bossAt / 60}:{bossAt % 60:00}");
            _ui.SetText("NightFlowText3", Localize.Get("ui.night.flow.boss"));

            int entries = _progress.NightEntries;
            _ui.SetText("NightEntryLabel", Localize.Get("ui.tower.entries_label"));
            for (int i = 0; i < 3; i++)
            {
                var pip = _ui.Get<Image>($"NightEntryPip{i}");
                if (pip != null) pip.sprite = i < entries ? _pipOn : _pipOff;
            }
            _ui.SetText("NightEntryText", Localize.Format("ui.tower.entries", entries, table != null ? table.FreeEntriesPerDay : 3));
            int cost = _progress.NightBuyCost;
            _ui.SetText("NightBuyText", Localize.Get("ui.tower.buy"));
            var buy = _ui.Get<Button>("NightBuyButton");
            if (buy != null) buy.interactable = cost >= 0;

            bool isToday = _shownDay == today;
            var start = _ui.Get<Button>("NightStartButton");
            if (start != null) start.interactable = isToday;
            _ui.SetText("NightStartText", !isToday ? Localize.Get("ui.night.today_only")
                : entries > 0 ? Localize.Get("ui.night.start")
                : cost >= 0 ? Localize.Format("ui.tower.start_buy", cost) : Localize.Get("ui.tower.no_entries"));
            _ui.SetText("NightStartHintText", Localize.Get("ui.tower.start_hint"));
        }

        /// <summary>그 요일에 몸으로 나오는 호스트(숨긴 몸 뺌).</summary>
        internal static List<string> DayHosts(ModeTable table, int dayIndex)
        {
            if (!CoreModule.TryGet<IModeProgress>(out var progress)) return new List<string>();
            var day = table != null && table.Days != null && dayIndex < table.Days.Length ? table.Days[dayIndex] : null;
            return progress.NightDayHosts(day);
        }

        private void AskBuy(bool thenStart)
        {
            if (_progress == null) return;
            int cost = _progress.NightBuyCost;
            if (cost < 0) { SystemPopup.Show(Localize.Get("ui.tower.no_entries"), null, cancelText: null); return; }
            string msg = Localize.Format("ui.mode.buy_ask", cost, _progress.NightBoughtToday, _progress.Table?.BuyEntriesMax ?? 3);
            SystemPopup.Show(msg, () =>
            {
                if (!_progress.TryBuyNightEntry())
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
            if (_starting || _progress == null || _shownDay != _progress.TodayIndex) return;
            if (_progress.NightEntries <= 0) { AskBuy(true); return; }
            if (!_progress.TryUseNightEntry()) return;
            _starting = true;
            GameSound.Cue("ui.play");
            int week = CoreModule.TryGet<IResetClock>(out var clock) ? clock.WeekKey : 0;
            RunModeContext.BeginNight(_progress.TodayIndex, week);
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

        private async UniTaskVoid LoadFacesAsync()
        {
            try { _faceAtlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>("atlas/hostface"); }
            catch (Exception e) { Debug.LogWarning($"[밤] 얼굴 아틀라스 로드 실패 — {e.Message}"); }
            if (this != null && gameObject.activeSelf) Refresh();
        }
    }
}
