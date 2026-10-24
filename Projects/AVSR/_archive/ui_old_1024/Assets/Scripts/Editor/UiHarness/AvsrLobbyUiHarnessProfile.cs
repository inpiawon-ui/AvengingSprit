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
    /// AVSR 로비 UI 하네스 프로필(2026-10-15, 키트 `Tools/ui-mockup-kit/APPLY.md`).
    /// 로비 · 챕터 창(출전 방식 셋 — 시안 S22e · S22b · S22c) · 일본어 · 영어를 세 해상도로 찍고 잰다.
    /// 상태를 더하면 세 해상도로 자동으로 찍는다 — 지적이 나온 상태는 다 시나리오로 남긴다.
    /// </summary>
    public sealed class AvsrLobbyUiHarnessProfile : IUiHarnessProfile
    {
        private const string LangKey = "avsr.uiharness.lang";
        private const string RebornKey = "avsr.uiharness.reborn";

        // 세로 게임 — 16:9 휴대폰 · 20:9 긴 휴대폰 · 4:3 태블릿. Game 뷰에 이 이름으로 등록한다(EnsureSizes)
        private static readonly (string Name, int W, int H, string Tag)[] Sizes =
        {
            ("AVSR 16x9", 720, 1280, "169"), ("AVSR 20x9", 1080, 2400, "209"), ("AVSR 4x3", 1536, 2048, "43"),
        };

        [MenuItem("Tools/Game/Verify/UI Harness (Lobby)")]
        public static void RunMenu()
        {
            EnsureSizes();
            // 찍는 동안 언어를 바꾼다 — 끝(마지막 상태의 Leave)에 이 값으로 되돌린다(2026-10-12 녹화 뒤 일본어로 남은 일)
            SessionState.SetInt(LangKey, PlayerPrefs.GetInt("game.language", (int)Language.Korean));
            // 2026-10-24 부터 기본은 RE:BORN 화면 — 이 프로필은 옛 로비를 잰다. 찍는 동안만 끄고 끝에 되돌린다
            SessionState.SetInt(RebornKey, RebornUiSwitch.Enabled ? 1 : 0);
            RebornUiSwitch.Enabled = false;
            UiHarnessRunner.Run(new AvsrLobbyUiHarnessProfile());
        }

        [MenuItem("Tools/Game/Verify/게임 뷰 해상도 등록 (AVSR)")]
        public static void EnsureSizes()
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetProperty("currentGroup").GetValue(instance);
            var gt = group.GetType();
            var texts = (string[])gt.GetMethod("GetDisplayTexts").Invoke(group, null);
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var kindType = asm.GetType("UnityEditor.GameViewSizeType");
            var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
            foreach (var s in Sizes)
            {
                bool have = false;
                foreach (var t in texts)
                    if (t == s.Name || t.StartsWith(s.Name + " ", System.StringComparison.Ordinal)) { have = true; break; }
                if (have) continue;
                var made = ctor.Invoke(new object[] { System.Enum.Parse(kindType, "FixedResolution"), s.W, s.H, s.Name });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { made });
            }
            sizesType.GetMethod("SaveToHDD")?.Invoke(instance, null);
        }

        public string Name => "AvsrLobby";
        public string BootScene => "Assets/Scenes/BootScene.unity";
        public string MockFolder => "Projects/AVSR/_exchange/in";

        public IReadOnlyList<(string ViewName, string Tag)> Resolutions { get; } = new[]
        {
            (Sizes[0].Name, Sizes[0].Tag), (Sizes[1].Name, Sizes[1].Tag), (Sizes[2].Name, Sizes[2].Tag),
        };

        public UiHarnessRules Rules { get; } = new UiHarnessRules()
            // 챕터 창은 로비 판을 통째로 덮는 창이다 — 열려 있으면 창 안만 잰다(뒤 로비는 lobby 상태가 잰다)
            .Modal(@"/ChapterHostPanel$")
            // 상점은 내용판(ShopContent)에 건다 — 확인 · 획득 창이 그 형제로 떠야 모달 규칙이 위 창을 고른다
            .Modal(@"/LobbyShopPanel/ShopContent$")
            .Modal(@"/SummonPanel/SummonContent$")
            .Modal(@"/SeasonPassPanel$")
            .Modal(@"/TowerEntryPanel$")
            .Modal(@"/NightEntryPanel$")
            .Modal(@"/ProfilePopup$")
            .Modal(@"/GrowthPanel/H6StarUpWindow$")
            .Modal(@"/GachaResultPopup$")
            .Modal(@"/GachaInfoPopup$")
            .Modal(@"/StoreConfirm$")
            .Modal(@"/StoreReward$")
            .Modal(@"^SystemPopup")
            .Contain(@"/DailySlot\d$|/ChestRow\d$|/Card\d$|/Starter\d$|/Gem\d$|/Gold\d$|/ChapterPack$", 0f,
                     "상점 칸 — 그림 · 이름 · 값 버튼 · 띠는 칸 안")
            .Contain(@"/StoreRewardCell\d$|/StoreConfirmRow\d$", 0f, "팝업 칸 — 아이콘 · 글자는 칸 안")
            .Contain(@"/CHMode(Host|Random|Ghost)$", 0f, "출전 카드 — 제목 · 그림 · ⚡ · 설명 · 고른 테는 판 안")
            .Contain(@"/CHHostCard\d+$", 0f, "호스트 카드 — 배지 · 별 · 이름 · 전투력 · 오늘의 몸 띠 · 고른 테는 카드 안")
            .Contain(@"/CHStartButton$", 0f, "도전하기 — 값 · 글자는 버튼 안")
            .Contain(@"/(HostButton|ChapterButton|SummonButton|ShopButton)$", 0f, "아래 바 칸 — 아이콘 · 제목 · 고른 테는 칸 안")
            // ── 의도된 것 ───────────────────────────────────────
            .AllowRule("C3", @"/CHScreen$|/LobbyV4(Top|Mid|Gap)(/Side(Left|Right))?$|/LobbyV4Mid/LobbyV4City(/CitySide[LR])?$|/FixedTopBarEdge[LR]$|/NavSide[LR]$|/NavBg$|/CHDim$",
                       "배경 판 · 가장자리 이음 띠 — 화면을 덮게(cover) 깔아 태블릿 · 긴 폰에서 밖으로 뻗는다")
            .AllowRule("C1?", @"/LobbyV4(Top|Mid)/Side(Left|Right)$|/LobbyV4Mid/LobbyV4City(/CitySide[LR])?$|/FixedTopBarEdge[LR]$",
                       "가장자리 이음 띠 — 판 밖 좌우로 이어 그린다")
            .AllowRule("R1", @"/CitySide[LR]$|/NavSide[LR]$|/FixedTopBarEdge[LR]$|/BottomNav/NavBg$",
                       "가장자리 이음 띠 — 1~6px 폭 그림을 옆으로 늘려 화면 끝까지 잇는다")
            .AllowRule("C2", @"/UnitShowcaseBody$", "진열 큰 몸 그림 — 128 칸 규격이라 몸 둘레 투명 여백까지 칸 상자다(보이는 몸은 판 안 · 캡처 확인 2026-10-19)")
            .AllowRule("C3", @"/UnitShowcaseBody$", "진열 큰 몸 그림 — 투명 여백 칸 상자만 화면 끝을 넘는다(보이는 몸은 화면 안 · 캡처 확인 2026-10-19)")
            .AllowRule("C6", @"/UnitShowcaseBody$", "진열 큰 몸 그림 — 투명 여백 칸 상자가 제목과 겹칠 뿐 보이는 몸은 글자를 가리지 않는다(캡처 확인 2026-10-19)")
            .AllowRule("R1", @"/GhostPath/PathLine(Done|Now|Lock)$",
                       "유령 성장 경로 줄 — 8×9 단색 띠를 런타임이 칸 사이 폭으로 늘린다(늘려도 모양이 안 변한다)")
            .AllowRule("R3", @"/CitySide[LR]$|/NavSide[LR]$|/FixedTopBarEdge[LR]$|/CHScreen$|/CHChapterArt$|/LobbyV4\w+$|/NavBg$",
                       "이음 띠 · 배경 · 챕터 그림 — 키워 쓰는 것이 의도")
            .AllowRule("C1", @"/BottomNav/\w+Button/NavBadge(/|$)", "새 소식 「!」 — 시안처럼 칸 모서리에 걸쳐 밖으로 나온다")
            .AllowRule("C3", @"/(SummonContent/SummonAltar|GachaResultPopup/ResultAltar)$", "소환 제단 배경 그림 — 긴 화면에서도 덮게 키워 깐다")
            .AllowRule("C3", @"/GrowthPanel/Page/(GrowthSide[LR]|GrowthFiller)$", "육성 판 가장자리 이음 띠 — 화면 밖까지 이어 그린다")
            .AllowRule("R1", @"/GrowthPanel/Page/(GrowthSide[LR]|GrowthFiller)$", "육성 판 가장자리 이음 띠 — 1px 그림을 늘려 쓴다")
            .AllowRule("C2", @"/H6StatContent/H6Row\d+(/|$)", "능력치 8줄 스크롤 — 창 가장자리 줄이 잘리는 것은 정상")
            .AllowRule("R1", @"/ProfileExpFill$", "경험치 막대 채움 — 폭이 값에 따라 줄어든다")
            .AllowRule("R1", @"/CHChapterArtBox/CHChapterDim$", "챕터 그림 위 어둡게 덮는 판 — 칸 크기로 늘려 쓴다")
            .AllowRule("C2", @"/CHBossMask/CHBossPortrait$",
                       "보스 그림 — 여백이 있는 256 칸이라 크게 그리고 칸(마스크)으로 자른다")
            .AllowRule("C2", @"/(MissionScrollContent/Mission\d+_\w+|PassScrollContent/PassRow\d+)(/|$)",
                       "패스 · 미션 줄 스크롤 — 창 가장자리 줄이 잘리는 것은 정상")
            .AllowRule("C2", @"/(ProfilePreviewMask/ProfilePreviewBg|PassBannerMask/PassBannerArt)$",
                       "프로필 미리보기 배경 — 칸을 덮게 키워 깔고 칸이 잘라 낸다")
            .AllowRule("C3", @"/(ProfilePreviewMask/ProfilePreviewBg|PassBannerMask/PassBannerArt)$", "프로필 미리보기 배경 — 칸 밖은 잘려 안 보인다")
            .AllowRule("C2", @"/LobbyV4Mid/Mode\w+Card/Mode\w+Art/Art$",
                       "로비 모드 카드 그림 — 칸(마스크)으로 잘라 쓴다(모드 칸, 기획 세션)")
            .AllowRule("C2", @"/HomeV5/HomeBody/.*/(HomeShowcaseArt|HomeScenarioArt|HomeTowerArt|HomeNightArt)$",
                       "홈 v5 칸 그림 — 칸을 덮게 키워 깔고 칸(RectMask2D)이 잘라 낸다(Envelope)")
            .AllowRule("C3", @"/HomeV5/(HomePageBg|HomeBody/.*/(HomeShowcaseArt|HomeScenarioArt|HomeTowerArt|HomeNightArt))$",
                       "홈 v5 바탕 · 칸 그림 — 화면 · 칸 밖은 잘려 안 보인다")
            .AllowRule("C3", @"/FixedTopBar/TopV5Plate$", "UI v5 위 막대 금테 판 — 옆 테가 안 보이게 화면 양끝 너머까지 늘려 깐다")
            .AllowRule("C2", @"/CHV5Head/CHV5HeadArt$", "UI v5 출전 머리 그림 — 칸을 덮게 키워 깔고 칸이 잘라 낸다(Envelope)")
            .AllowRule("C3", @"/CHV5Head/CHV5HeadArt$", "UI v5 출전 머리 그림 — 칸 밖은 잘려 안 보인다")
            .AllowRule("C3", @"/CHHostContent/CHHostCard\d+/", "카드 줄 스크롤 — 화면 밖 카드는 창이 잘라 낸다")
            .AllowRule("C2", @"/H6V5ShowView/H6V5ShowArt$", "UI v5 육성 진열 그림 — 칸을 덮게 키워 깔고 칸이 잘라 낸다(Envelope)")
            .AllowRule("C2", @"/H6ListContent/H6HostCard_\w+/", "호스트 목록 가로 스크롤 — 창 가장자리 카드가 잘리는 것은 정상")
            .AllowRule("C2", @"/SummonV5AltarView/SummonAltar$", "UI v5 소환 영혼 명부 그림 — 칸을 덮게 키워 깔고 칸이 잘라 낸다(Envelope)")
            .AllowRule("C3", @"/SummonV5AltarView(/SummonAltar)?$", "UI v5 소환 위 장면 — 화면 밖은 잘려 안 보인다")
            .AllowRule("C2", @"/PassScrollContent/PassV5Track$", "UI v5 패스 가운데 줄 — 30단계 길이라 스크롤 창이 잘라 낸다")
            .AllowRule("C3", @"/PassScrollContent/PassV5Track$", "UI v5 패스 가운데 줄 — 스크롤 밖은 안 보인다")
            .AllowRule("C3", @"/NightV5View/NightV5Art$", "UI v5 밤 오늘 카드 그림 — 긴 화면에서 칸이 길어져 덮기 그림이 화면 밖까지 커진다(칸이 잘라 낸다)")
            .AllowRule("C2", @"/NightV5View/NightV5Art$", "UI v5 밤 오늘 카드 그림 — 칸을 덮게 키워 깔고 칸이 잘라 낸다(Envelope)")
            .AllowRule("C2", @"/ShopV5Head/ShopV5HeadArt$", "UI v5 상점 머리 그림 — 칸을 덮게 키워 깔고 칸이 잘라 낸다(Envelope)")
            .AllowRule("C3", @"/ShopV5Head(/ShopV5HeadArt)?$", "UI v5 상점 머리 그림 — 화면 밖은 잘려 안 보인다")
            .AllowRule("C2", @"/GhostV5View/GhostV5Art$", "UI v5 유령 진열 그림 — 칸을 덮게 키워 깔고 칸이 잘라 낸다(Envelope)")
            .AllowRule("C2", @"/StatContent/StatRow\d*(/|$)", "유령 능력치 2열 스크롤 — 창 가장자리 카드가 잘리는 것은 정상")
            .AllowRule("C2", @"/CHChapterArtBox/CHChapterArt$", "챕터 그림 — 칸을 덮게 키워 깔고 칸이 잘라 낸다(FillBox)")
            .AllowRule("C3", @"/CHChapterArtBox/CHChapterArt$", "챕터 그림 — 칸 밖은 잘려 안 보인다(FillBox)")
            .AllowRule("C6", @"/CHHostContent/CHHostCard\d+/CardPowerText$",
                       "고른 금 테 안쪽의 거의 투명한 빛 — 캡처 확대로 글자가 안 가린 것을 확인함(2026-10-15)")
            .AllowRule("C2", @"/CHHostContent/CHHostCard\d+/", "카드 줄 스크롤 — 가장자리 카드가 창에 잘리는 것은 정상")
            .AllowRule("*", @"(^|/)(RandomFx|CHInfoRandomFx|Fx\w*)(/|$)", "빛 연출 — 번지는 것이 정상");

        public IReadOnlyList<UiHarnessScenario> Scenarios { get; } = new[]
        {
            new UiHarnessScenario
            {
                Name = "lobby", Mock = "scene_s01_lobby",
                Enter = d => UniTask.CompletedTask,
                Expect = () => Active("BottomNav") ? null : "로비 아래 바가 없다",
            },
            new UiHarnessScenario
            {
                Name = "ch_host", Mock = "scene_s22e_host",
                Enter = async d => { await d.Click("HomeContinueButton"); await d.Click("CHModeHost"); },
                Expect = () => !Active("CHModeHost") ? "챕터 창이 안 열렸다"
                    : !Interactable("CHStartButton") ? "도전하기가 꺼져 있다(반투명)" : null,
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "ch_random", Mock = "scene_s22b_random",
                Enter = async d => { await d.Click("HomeContinueButton"); await d.Click("CHModeRandom"); },
                Expect = () => Active("CHHostDim") ? null : "랜덤 — 호스트 줄 덮개가 없다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "ch_ghost", Mock = "scene_s22c_ghost",
                Enter = async d => { await d.Click("HomeContinueButton"); await d.Click("CHModeGhost"); },
                Expect = () => Active("CHHostDim") ? null : "유령 — 호스트 줄 덮개가 없다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "shop_currency", Mock = "scene_s04_currency",
                Enter = async d => { await d.Click("ShopButton"); await d.Click("ShopTabCurrency"); },
                Expect = () => Active("ShopPageCurrency") ? null : "상점 재화 탭이 안 열렸다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "shop_chest", Mock = "scene_s03_chest",
                Enter = async d => { await d.Click("ShopButton"); await d.Click("ShopTabChest"); },
                Expect = () => Active("ShopPageChest") ? null : "상점 상자 탭이 안 열렸다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "shop_today", Mock = "scene_s02_daily",
                Enter = async d => { await d.Click("ShopButton"); await d.Click("ShopTabToday"); },
                Expect = () => Active("ShopPageToday") ? null : "상점 오늘 탭이 안 열렸다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "shop_confirm", Mock = "scene_s05_confirm",
                Enter = async d => { await d.Click("ShopButton"); await d.Click("ShopTabCurrency"); await d.Click("Starter1Buy"); },
                Expect = () => Active("StoreConfirm") ? null : "구매 확인 창이 안 열렸다",
                Leave = async d => { await d.Click("StoreConfirmClose"); await d.Click("ChapterButton"); },
            },
            new UiHarnessScenario
            {
                Name = "summon", Mock = "mock_uiv5_summon",
                Enter = d => d.Click("SummonButton"),
                Expect = () => Active("GachaPull10Button") ? null : "소환 창이 안 열렸다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "summon_rates", Mock = "scene_s14_rates",
                Enter = async d => { await d.Click("SummonButton"); await d.Click("GachaRateButton"); },
                Expect = () => Active("RatesGroup") ? null : "확률 창이 안 열렸다",
                Leave = async d => { await d.Click("GachaInfoClose"); await d.Click("ChapterButton"); },
            },
            new UiHarnessScenario
            {
                // 오늘 무료 1회 — 세이브가 바뀐다(하네스 뒤 세이브를 되돌린다). 기둥 연출(약 1초)이 끝난 뒤 찍는다
                Name = "summon_result1", Mock = "scene_s12_result1",
                Enter = async d =>
                {
                    await d.Click("SummonButton");
                    await d.Click("GachaPull1Button");   // v5 — 오늘 무료가 남아 있으면 1회 단추가 무료로 뽑는다
                    await UiHarnessDriver.Seconds(3f);
                },
                Expect = () => Active("GachaResultPopup") ? null : "결과 창이 안 열렸다(무료 소환을 이미 썼을 수 있다)",
                Leave = async d => { await d.Click("ResultCloseButton"); await d.Click("ChapterButton"); },
            },
            new UiHarnessScenario
            {
                Name = "summon_history", Mock = "scene_s15_history",
                Enter = async d => { await d.Click("SummonButton"); await d.Click("GachaHistoryButton"); },
                Expect = () => Active("HistoryGroup") ? null : "기록 창이 안 열렸다",
                Leave = async d => { await d.Click("GachaHistoryClose"); await d.Click("ChapterButton"); },
            },
            new UiHarnessScenario
            {
                Name = "pass_rewards", Mock = "scene_s16_pass",
                Enter = d => d.Click("HomePassButton"),
                Expect = () => Active("PassRewardsPage") ? null : "패스 창이 안 열렸다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "pass_missions", Mock = "scene_s17_mission",
                Enter = async d => { await d.Click("HomePassButton"); await d.Click("PassTabMissions"); },
                Expect = () => Active("PassMissionsPage") ? null : "미션 탭이 안 열렸다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "growth_host", Mock = "mock_uiv5_growth_skill",
                Enter = async d => { await d.Click("HostButton"); await d.Click("TabHostButton"); await d.Click("H6V5Tab1"); },
                Expect = () => Active("H6GroupSkill") ? null : "호스트 육성 스킬 탭이 안 열렸다",
                Leave = d => d.Click("ChapterButton"),
            },
            new UiHarnessScenario
            {
                Name = "growth_ghost", Mock = "mock_uiv2_d_ghost",
                Enter = async d => { await d.Click("HostButton"); await d.Click("TabGhostButton"); },
                Expect = () => Active("GhostV5Deco") ? null : "유령 육성 화면이 안 열렸다",
                Leave = async d => { await d.Click("TabHostButton"); await d.Click("ChapterButton"); },
            },
            new UiHarnessScenario
            {
                Name = "growth_stats", Mock = "mock_uiv4_growth_stats",
                Enter = async d => { await d.Click("HostButton"); await d.Click("TabHostButton"); await d.Click("H6V5Tab0"); },
                Expect = () => Active("H6GroupStats") ? null : "호스트 육성 기본 능력 탭이 안 열렸다",
                Leave = async d => { await d.Click("H6V5Tab1"); await d.Click("ChapterButton"); },
            },
            new UiHarnessScenario
            {
                // 성급 창은 최대 성급이면 안 열린다 — 아직 덜 자란 몸을 골라 연다(없으면 실패로 남는다)
                Name = "growth_starup", Mock = "mock_uiv3_growth_star",
                Enter = async d =>
                {
                    // 「모든 호스트 열기」(GameConfig._openAllHosts)면 모든 몸이 최대 성급으로 읽혀 창이 안 열린다 — 이 상태 동안만 끈다
                    SetOpenAll(false);
                    await d.Click("HostButton");
                    await d.Click("TabHostButton");
                    string key = NotMaxedHost();
                    // 목록 카드는 가로 스크롤 밖일 수 있다 — 단추를 직접 누른다
                    if (key != null && Invoke($"H6HostCard_{key}")) await UiHarnessDriver.Seconds(0.3f);
                    await d.Click("H6StarUpButton");
                },
                // v5 — 성급 UP 은 창 대신 성급 탭을 연다
                Expect = () => Active("H6GroupStar") && Active("H6WinRow0") ? null : "성급 탭이 안 열렸다(덜 자란 몸이 없을 수 있다)",
                Leave = async d => { await d.Click("H6V5Tab1"); SetOpenAll(true); await d.Click("ChapterButton"); },
            },
            new UiHarnessScenario
            {
                Name = "tower", Mock = "mock_uiv3_tower",
                Enter = d => d.Click("HomeTowerCard"),
                Expect = () => Active("TowerContent") ? null : "망령의 탑 창이 안 열렸다",
                Leave = d => d.Click("TowerBackButton"),
            },
            new UiHarnessScenario
            {
                Name = "night", Mock = "mock_uiv3_night",
                Enter = d => d.Click("HomeNightCard"),
                Expect = () => Active("NightContent") ? null : "망령의 밤 창이 안 열렸다",
                Leave = d => d.Click("NightBackButton"),
            },
            new UiHarnessScenario
            {
                Name = "profile", Mock = "mock_store_profile",
                Enter = d => d.Click("LobbyProfile"),
                Expect = () => Active("ProfileGrid") ? null : "프로필 창이 안 열렸다",
                Leave = d => d.Click("ProfileClose"),
            },
            new UiHarnessScenario
            {
                Name = "shop_currency_en", Mock = "scene_s04_currency",
                Enter = async d => { SetLang(Language.English); await UiHarnessDriver.Seconds(0.5f); await d.Click("ShopButton"); await d.Click("ShopTabCurrency"); },
                Expect = () => Active("ShopPageCurrency") ? null : "상점 재화 탭이 안 열렸다",
                Leave = async d => { await d.Click("ChapterButton"); SetLang(Language.Korean); },
            },
            new UiHarnessScenario
            {
                Name = "ch_host_ja", Mock = "scene_s22e_host",
                Enter = async d => { SetLang(Language.Japanese); await UiHarnessDriver.Seconds(0.5f); await d.Click("HomeContinueButton"); },
                Expect = () => Active("CHModeHost") ? null : "챕터 창이 안 열렸다",
                Leave = async d => { await d.Click("ChapterButton"); SetLang(Language.Korean); },
            },
            new UiHarnessScenario
            {
                Name = "ch_host_en", Mock = "scene_s22e_host",
                Enter = async d => { SetLang(Language.English); await UiHarnessDriver.Seconds(0.5f); await d.Click("HomeContinueButton"); },
                Expect = () => Active("CHModeHost") ? null : "챕터 창이 안 열렸다",
                Leave = async d => { await d.Click("ChapterButton"); RestoreLang(); },
            },
            new UiHarnessScenario
            {
                Name = "growth_host_ja", Mock = "mock_uiv5_growth_skill",
                Enter = async d =>
                {
                    SetLang(Language.Japanese);
                    await UiHarnessDriver.Seconds(0.5f);
                    await d.Click("HostButton");
                    await d.Click("TabHostButton");
                },
                Expect = () => Active("H6GroupSkill") ? null : "호스트 육성 스킬 탭이 안 열렸다",
                Leave = async d => { await d.Click("ChapterButton"); SetLang(Language.Korean); },
            },
            new UiHarnessScenario
            {
                Name = "growth_host_en", Mock = "mock_uiv5_growth_skill",
                Enter = async d =>
                {
                    SetLang(Language.English);
                    await UiHarnessDriver.Seconds(0.5f);
                    await d.Click("HostButton");
                    await d.Click("TabHostButton");
                },
                Expect = () => Active("H6GroupSkill") ? null : "호스트 육성 스킬 탭이 안 열렸다",
                Leave = async d => { await d.Click("ChapterButton"); RestoreLang(); },
            },
        };

        public Transform Root()
        {
            var lobby = Object.FindAnyObjectByType<Game.Module.Lobby.LobbyMainUI>();
            if (lobby == null) return null;
            var canvas = lobby.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : lobby.transform;
        }

        /// <summary>
        /// 로비 PLAY 칸까지 — 타이틀 · 오프닝은 눌러 넘긴다. 언어팩 · 유저 데이터가 온 뒤에 연다
        /// (오기 전에 찍으면 글자가 키 그대로 나온다 — LobbyShotTool 2026-09-16).
        /// </summary>
        public async UniTask GotoStart(UiHarnessDriver driver)
        {
            for (int i = 0; i < 600; i++)   // 최대 약 60초
            {
                bool ready = Active("BottomNav") && GameObject.Find("[LoadingView]") == null
                             && Localize.Get("ui.lobby.game_mode") != "ui.lobby.game_mode"
                             && CoreModule.TryGet<IPlayerDataService>(out var p) && p.IsReady;
                if (ready) break;
                // 타이틀 · 오프닝은 검사 대상이 아니다 — 「터치」 칸을 배경 그림이 덮고 있어 진짜 클릭이 막힌다. 버튼을 직접 누른다
                if (Invoke("SkipButton") || Invoke("TouchArea")) await UiHarnessDriver.Seconds(0.3f);
                else await UiHarnessDriver.Seconds(0.1f);
            }
            SetLang(Language.Korean);
            if (Active("ChapterButton")) await driver.Click("ChapterButton");   // 열린 창을 닫고 PLAY 칸으로
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

        private static bool s_openAllWas = true;

        private static void SetOpenAll(bool on)
        {
            var f = typeof(PlayerDataService).GetField("s_openAllHosts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (f == null) return;
            if (!on) s_openAllWas = (bool)f.GetValue(null);
            f.SetValue(null, on && s_openAllWas);
        }

        private static string NotMaxedHost()
        {
            if (!CoreModule.TryGet<IPlayerDataService>(out var p) || p.PlayableHosts == null) return null;
            foreach (var h in p.PlayableHosts)
                if (h != null && !h.IsGhost && !PlayerDataService.IsHiddenHost(h.HostKey)
                    && p.GetMastery(h.HostKey) < p.MasteryMax && GameObject.Find($"H6HostCard_{h.HostKey}") != null)
                    return h.HostKey;
            return null;
        }

        private static bool Interactable(string name)
        {
            var go = GameObject.Find(name);
            return go != null && go.TryGetComponent<UnityEngine.UI.Selectable>(out var s) && s.IsInteractable();
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
            RebornUiSwitch.Enabled = SessionState.GetInt(RebornKey, 1) == 1;
        }
    }
}
