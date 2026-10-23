using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using GameFramework.Core.Base;
using GameFramework.Game.Editor.UiHarness;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.UiHarness
{
    /// <summary>
    /// AVSR RE:BORN 로비 UI 하네스 프로필(2026-10-24) — 새 화면(RbLobbyUI)을 세 해상도로 찍고 잰다.
    /// 옛 로비는 `AvsrLobbyUiHarnessProfile`(찍는 동안 RE:BORN 을 끈다). 시안은 묶음 Screens 폴더(1080 × 2340).
    /// 상태는 시작 화면(전투 탭)에서 실제로 눌러 들어간다 — 가려진 버튼이면 도달 실패로 남는다.
    /// </summary>
    public sealed class AvsrRebornUiHarnessProfile : IUiHarnessProfile
    {
        private const string LangKey = "avsr.uiharness.reborn.lang";

        [MenuItem("Tools/Game/Verify/UI Harness (RE:BORN)")]
        public static void RunMenu()
        {
            AvsrLobbyUiHarnessProfile.EnsureSizes();
            SessionState.SetInt(LangKey, PlayerPrefs.GetInt("game.language", (int)Language.Korean));
            RebornUiSwitch.Enabled = true;
            UiHarnessRunner.Run(new AvsrRebornUiHarnessProfile());
        }

        public string Name => "AvsrReborn";
        public string BootScene => "Assets/Scenes/BootScene.unity";
        public string MockFolder => "Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens";

        public IReadOnlyList<(string ViewName, string Tag)> Resolutions { get; } = new[]
        {
            ("AVSR 16x9", "169"), ("AVSR 20x9", "209"), ("AVSR 4x3", "43"),
        };

        public UiHarnessRules Rules { get; } = new UiHarnessRules()
            // 덮는 창 — 열려 있으면 창 안만 잰다(뒤 쪽은 그 쪽 상태가 잰다)
            .Modal(@"/RbMisc/(RbDaily|RbSettings|RbLanguage|RbProfile)$")
            .Modal(@"/(GrwStarAsk|GrwStarDone|GrwSkillInfo|GrwSources|GrwEssence)$")
            .Modal(@"/RbNotice/RbNot\w+$")
            .Modal(@"^SystemPopup")
            .Contain(@"/ColCard\d+$", 0f, "도감 카드 — 테 · 얼굴 · 이름 · 별 · 전투력은 카드 안")
            .Contain(@"/ChsRow\d*$", 0f, "챕터 줄 — 섬네일 · 이름 · 상태는 줄 안")
            .Contain(@"/(TabShop|TabHost|TabBattle|TabSummon|TabChallenge)$", 0f, "아래 탭 칸 — 아이콘 · 이름은 칸 안")
            // ── 의도된 것 ───────────────────────────────────────
            .AllowRule("C3", @"/(Page\w+Bg|StageBg|StageFade|RbMail|RndBg|RndFade)$", "배경 — 화면을 덮게(Envelope) 깔아 긴 화면 · 태블릿에서 밖으로 뻗는다")
            .AllowRule("C2", @"/(Page\w+Bg|StageBg|RndBg|RbMailBg)$", "배경 — 쪽(RectMask2D)이 잘라 낸다")
            .AllowRule("C2", @"/ChsRowThumbArt$", "챕터 섬네일 — 무대 그림을 칸(RectMask2D)으로 잘라 쓴다")
            .AllowRule("R1", @"Fill$", "막대 채움 — 폭이 값에 따라 줄어든다")
            .AllowRule("C2", ScrollContent, "스크롤 판 — 보기 칸 가장자리의 줄 · 카드가 잘리는 것은 정상")
            .AllowRule("C3", ScrollContent, "스크롤 판 — 화면 밖 줄 · 카드는 보기 칸이 잘라 안 보인다")
            .AllowRule("C6", ScrollContent, "스크롤 판 — 아래 탭 뒤로 지나가는 줄 · 카드(탭이 덮는다)")
            .AllowRule("C1", @"/ChsRowThumb/ChsRowThumbArt$", "챕터 섬네일 — 무대 그림을 칸(RectMask2D)으로 잘라 쓴다")
            .AllowRule("R1", @"(Shade|Fade|ColCardGlow|GrwHeroEdge)$", "그늘 · 빛 판 — 칸에 맞춰 늘려 쓴다(모양 없는 번짐이라 늘려도 티가 안 난다)")
            .AllowRule("C2", @"Clip/\w+Art$", "칸 그림 — 칸(RectMask2D)을 덮게 깔고 칸이 잘라 낸다(Envelope)")
            .AllowRule("R2", @"/CardBadge$", "BEST 띠 — 글자 폭에 맞춰 한 장을 쓴다(모서리 장식이 없는 그림)");

        // 스크롤 판 내용 — 이 아래 노드는 보기 칸(RectMask2D)이 잘라 낸다
        private const string ScrollContent = @"/(SortieContent|ColGridContent|ChsBodyContent|ProfGridContent|SumBodyContent|ChlHubBodyContent|ShpBodyContent|GrwBodyContent|LckBodyContent|MisBodyContent|PasBodyContent|GhoBodyContent|\w+BodyContent)/";

        public IReadOnlyList<UiHarnessScenario> Scenarios { get; } = new[]
        {
            Tab("battle", "lobby", null, "PageBattle"),
            Tab("host", "collection", "TabHost", "PageHost"),
            Tab("summon", "summon", "TabSummon", "PageSummon"),
            Tab("shop", "shop", "TabShop", "PageShop"),
            Tab("challenge", "challenge", "TabChallenge", "PageChallenge"),
            new UiHarnessScenario
            {
                Name = "chapters", Mock = "chapter-select",
                Enter = d => d.Click("ChapterOpen"),
                Expect = () => Active("PageChapters") ? null : "챕터 선택이 안 열렸다",
                Leave = d => d.Click("ChsBack"),   // 챕터 선택은 아래 탭이 없다
            },
            Tab("missions", "missions", "RailMission", "PageMissions"),
            Tab("pass", "pass", "RailPass", "PagePass"),
            Tab("mail", "mail-empty", "RailMail", "RbMail"),
            new UiHarnessScenario
            {
                Name = "growth", Mock = "growth",
                Enter = async d => { await d.Click("TabHost"); await d.Click("ColCard0"); },
                Expect = () => Active("PageGrowth") || Active("PageLocked") ? null : "호스트 성장 쪽이 안 열렸다",
                Leave = d => d.Click("TabBattle"),
            },
            Overlay("profile", null, "HeaderProfile", "RbProfile", "ProfileClose"),
            Overlay("settings", "settings", "RailSettings", "RbSettings", "SettingsClose"),
            Overlay("daily", "daily-free", "RailDaily", "RbDaily", "DailyClose"),
            new UiHarnessScenario
            {
                Name = "battle_ja", Mock = "lobby",
                Enter = d => { SetLang(Language.Japanese); return UniTask.CompletedTask; },
                Expect = () => Active("PageBattle") ? null : "전투 탭이 아니다",
                Leave = d => { SetLang(Language.Korean); return UniTask.CompletedTask; },
            },
            new UiHarnessScenario
            {
                Name = "host_en", Mock = "collection",
                Enter = async d => { SetLang(Language.English); await d.Click("TabHost"); },
                Expect = () => Active("PageHost") ? null : "도감이 안 열렸다",
                Leave = async d => { await d.Click("TabBattle"); RestoreLang(); },
            },
        };

        public Transform Root()
        {
            var lobby = Object.FindAnyObjectByType<Game.Module.Lobby.RbLobbyUI>();
            if (lobby == null) return null;
            var canvas = lobby.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : lobby.transform;
        }

        /// <summary>전투 탭까지 — 타이틀 「시작하기」 는 눌러 넘긴다. 언어팩 · 유저 데이터가 온 뒤에 연다.</summary>
        public async UniTask GotoStart(UiHarnessDriver driver)
        {
            for (int i = 0; i < 600; i++)   // 최대 약 60초
            {
                bool ready = Object.FindAnyObjectByType<Game.Module.Lobby.RbLobbyUI>() != null && GameObject.Find("[LoadingView]") == null
                             && Localize.Get("ui.rb.lobby.sortie_title") != "ui.rb.lobby.sortie_title"
                             && CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) && p.IsReady;
                if (ready) break;
                if (Invoke("StartButton") || Invoke("SkipButton") || Invoke("TouchArea")) await UiHarnessDriver.Seconds(0.3f);
                else await UiHarnessDriver.Seconds(0.1f);
            }
            SetLang(Language.Korean);
            // 열린 쪽 · 창을 닫고 전투 탭으로 — 아래 탭이 없는 쪽(챕터 선택 · 미보유 · 보여주기)도 있어 화면 스크립트로 직접 간다
            var lobby = Object.FindAnyObjectByType<Game.Module.Lobby.RbLobbyUI>();
            if (lobby != null) lobby.SelectTab("Battle");
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

        private static UiHarnessScenario Tab(string name, string mock, string button, string page) => new()
        {
            Name = name, Mock = mock,
            Enter = button == null ? d => UniTask.CompletedTask : d => d.Click(button),
            Expect = () => Active(page) ? null : $"{page} 가 안 열렸다",
            Leave = d => button == null ? UniTask.CompletedTask : d.Click("TabBattle"),
        };

        private static UiHarnessScenario Overlay(string name, string mock, string button, string panel, string close) => new()
        {
            Name = name, Mock = mock,
            Enter = d => d.Click(button),
            Expect = () => Active(panel) ? null : $"{panel} 창이 안 열렸다",
            Leave = d => d.Click(close),
        };

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
