using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Game.Editor.UiHarness;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.UiHarness
{
    /// <summary>
    /// 추가 모드 UI 하네스 프로필(2026-10-15) — 로비 모드 칸 · 망령의 탑 입장(T1) · 망령의 밤 입장(N1)을
    /// 한국어 · 일본어 · 영어로 세 해상도에서 찍고 잰다. 시안 `_exchange/in/mock_mode_{L,T1,N1}.png`.
    /// 로비 프로필(`AvsrLobbyUiHarnessProfile`)과 따로 둔다 — 그쪽은 아웃게임 세션이 고친다.
    /// </summary>
    public sealed class AvsrModesUiHarnessProfile : IUiHarnessProfile
    {
        private const string LangKey = "avsr.uiharness.modes.lang";

        [MenuItem("Tools/Game/Verify/UI Harness (Modes)")]
        public static void RunMenu()
        {
            AvsrLobbyUiHarnessProfile.EnsureSizes();
            SessionState.SetInt(LangKey, PlayerPrefs.GetInt("game.language", (int)Language.Korean));
            UiHarnessRunner.Run(new AvsrModesUiHarnessProfile());
        }

        public string Name => "AvsrModes";
        public string BootScene => "Assets/Scenes/BootScene.unity";
        public string MockFolder => "Projects/AVSR/_exchange/in";

        public IReadOnlyList<(string ViewName, string Tag)> Resolutions { get; } = new[]
        {
            ("AVSR 16x9", "169"), ("AVSR 20x9", "209"), ("AVSR 4x3", "43"),
        };

        public UiHarnessRules Rules { get; } = new UiHarnessRules()
            .Modal(@"/(TowerEntryPanel|NightEntryPanel)$")
            .Modal(@"^SystemPopup")
            .Contain(@"/TowerLadderCell\d+$", 0f, "층 사다리 칸 — 층 · 이름 · 도전 표식 · 체크는 칸 안")
            .Contain(@"/TowerFloorCard$", 0f, "지금 층 카드 — 글자는 카드 안")
            .Contain(@"/(TowerStartButton|NightStartButton|TowerSweepButton|TowerBuyButton|NightBuyButton)$", 0f, "버튼 글자는 버튼 안")
            .Contain(@"/NightDayCell\d+$", 0f, "요일 칸 — 요일 · 조건 글자는 칸 안")
            .Contain(@"/NightFlowCell\d+$", 0f, "흐름 칸 — 글자는 칸 안")
            .AllowRule("C3", @"/(TowerBg|NightBg)$|/LobbyV4(Top|Mid|Gap)(/Side(Left|Right))?$|/LobbyV4Mid/LobbyV4City(/CitySide[LR])?$|/FixedTopBarEdge[LR]$|/NavSide[LR]$|/NavBg$",
                       "바탕 판 · 가장자리 이음 띠 — 화면을 덮게(cover) 깔아 밖으로 뻗는다")
            .AllowRule("C1?", @"/LobbyV4(Top|Mid)/Side(Left|Right)$|/LobbyV4Mid/LobbyV4City(/CitySide[LR])?$|/FixedTopBarEdge[LR]$",
                       "가장자리 이음 띠 — 판 밖 좌우로 이어 그린다")
            .AllowRule("R1", @"/CitySide[LR]$|/NavSide[LR]$|/FixedTopBarEdge[LR]$", "가장자리 이음 띠")
            .AllowRule("R3", @"/(TowerBg|NightBg)$|/CitySide[LR]$|/NavSide[LR]$|/FixedTopBarEdge[LR]$|/LobbyV4\w+$|/NavBg$",
                       "바탕 · 이음 띠 — 키워 쓰는 것이 의도")
            .AllowRule("R1", @"/ProfileExpFill$", "경험치 막대 채움")
            .AllowRule("*", @"(^|/)(RandomFx|Fx\w*)(/|$)", "빛 연출");

        public IReadOnlyList<UiHarnessScenario> Scenarios { get; } = new[]
        {
            new UiHarnessScenario
            {
                Name = "lobby_modes", Mock = "mock_mode_L",
                Enter = d => UniTask.CompletedTask,
                Expect = () => Active("ModeDefenseCard") ? null : "로비 모드 칸이 없다",
            },
            Tower("tower", Language.Korean),
            Tower("tower_ja", Language.Japanese),
            Tower("tower_en", Language.English),
            Night("night", Language.Korean),
            Night("night_ja", Language.Japanese),
            Night("night_en", Language.English, last: true),
        };

        private static UiHarnessScenario Tower(string name, Language lang) => new()
        {
            Name = name, Mock = "mock_mode_T1",
            Enter = async d => { SetLang(lang); await UiHarnessDriver.Seconds(0.5f); await d.Click("ModeDefenseCard"); await UiHarnessDriver.Seconds(0.6f); },
            Expect = () => Active("TowerEntryPanel") ? null : "탑 입장 패널이 안 열렸다",
            Leave = async d => { await d.Click("TowerBackButton"); SetLang(Language.Korean); },
        };

        private static UiHarnessScenario Night(string name, Language lang, bool last = false) => new()
        {
            Name = name, Mock = "mock_mode_N1",
            Enter = async d => { SetLang(lang); await UiHarnessDriver.Seconds(0.5f); await d.Click("ModeSurvivalCard"); await UiHarnessDriver.Seconds(0.6f); },
            Expect = () => Active("NightEntryPanel") ? null : "밤 입장 패널이 안 열렸다",
            Leave = async d => { await d.Click("NightBackButton"); if (last) RestoreLang(); else SetLang(Language.Korean); },
        };

        public Transform Root()
        {
            var lobby = Object.FindAnyObjectByType<Game.Module.Lobby.LobbyMainUI>();
            if (lobby == null) return null;
            var canvas = lobby.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : lobby.transform;
        }

        public async UniTask GotoStart(UiHarnessDriver driver)
        {
            for (int i = 0; i < 600; i++)   // 최대 약 60초
            {
                bool ready = Active("BottomNav") && GameObject.Find("[LoadingView]") == null
                             && Localize.Get("ui.lobby.game_mode") != "ui.lobby.game_mode"
                             && CoreModule.TryGet<IPlayerDataService>(out var p) && p.IsReady;
                if (ready) break;
                if (Invoke("SkipButton") || Invoke("TouchArea")) await UiHarnessDriver.Seconds(0.3f);
                else await UiHarnessDriver.Seconds(0.1f);
            }
            SetLang(Language.Korean);
            if (Active("ChapterButton")) await driver.Click("ChapterButton");
            await UiHarnessDriver.Seconds(1f);
        }

        private static bool Invoke(string name)
        {
            var go = GameObject.Find(name);
            if (go == null || !go.activeInHierarchy) return false;
            var b = go.GetComponent<UnityEngine.UI.Button>();
            if (b == null || !b.isActiveAndEnabled) return false;
            b.onClick.Invoke();
            return true;
        }

        private static bool Active(string name)
        {
            var go = GameObject.Find(name);
            return go != null && go.activeInHierarchy;
        }

        private static void SetLang(Language lang)
        {
            if (CoreModule.TryGet<ILanguageService>(out var l)) l.SetLanguage(lang);
        }

        private static void RestoreLang()
        {
            int before = SessionState.GetInt(LangKey, (int)Language.Korean);
            SetLang((Language)before);
            PlayerPrefs.SetInt("game.language", before);
            PlayerPrefs.Save();
        }
    }
}
