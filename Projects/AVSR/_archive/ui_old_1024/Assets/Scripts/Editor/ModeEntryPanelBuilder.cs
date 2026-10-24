using Game.Module.Common.UI;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 추가 모드 입장 패널(탑 T1 · 밤 N1)과 로비 모드 칸 그림을 로비 프리팹에 세운다 (PD 2026-10-15).
    ///
    /// 좌표는 통과한 시안에서 잰 상자다 — 탑 `_exchange/in/mock_mode_T1.png`(720×1280) ·
    /// 밤 `mock_mode_N1.png`(941×1672 → ×0.7651 로 720 기준). `Box(x0, y0, x1, y1)` 하나로만 배치한다.
    /// 부품: `Assets/BaseResource/{TowerEntryPanel, NightEntryPanel, ModeCommon, ModeBackdrop}`(ModeUiImport).
    /// 화면 설계서 Projects/AVSR/wireframes/AVSR_Screen_Modes.md 3 · 4절 — 노드 이름 = 바인딩 키.
    /// 여러 번 돌려도 같다(두 패널 노드만 지우고 다시 세운다 · 모드 칸 그림은 있으면 다시 맞춘다).
    /// </summary>
    public static class ModeEntryPanelBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Font = "Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset";

        private static readonly Color Gold = new(1f, 0.82f, 0.25f);
        private static readonly Color Cyan = new(0.55f, 0.83f, 1f);
        private static readonly Color Dim = new(0.66f, 0.74f, 0.9f);
        private static readonly Color Green = new(0.37f, 0.94f, 0.63f);
        private static readonly Color Dark = new(0.16f, 0.09f, 0.0f);
        private static readonly Color Red = new(1f, 0.42f, 0.42f);

        private static TMP_FontAsset s_font;
        private static Material s_outline;

        [MenuItem("Tools/Game/모드 UI/입장 패널 세우기")]
        public static void Run()
        {
            s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);
            var root = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                PickOutline(root);
                var tower = BuildTower(root.transform);
                var night = BuildNight(root.transform);
                PlaceBelowTopBar(root.transform, tower.transform);
                PlaceBelowTopBar(root.transform, night.transform);
                ModeCards(root.transform);

                var so = new SerializedObject(root.GetComponent<LobbyMainUI>());
                so.FindProperty("_towerEntryPanel").objectReferenceValue = tower;
                so.FindProperty("_nightEntryPanel").objectReferenceValue = night;
                so.ApplyModifiedPropertiesWithoutUndo();
                tower.gameObject.SetActive(false);
                night.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[모드 UI] 입장 패널 세움 — TowerEntryPanel · NightEntryPanel · 모드 칸 그림");
        }

        // ── 탑 (시안 T1, 720 기준) ─────────────────────────────

        private static TowerEntryPanel BuildTower(Transform root)
        {
            var panel = Fresh(root, "TowerEntryPanel");
            Backdrop(panel, "TowerBg", Sp("ModeBackdrop/tower_panel_bg"));
            var box = Content(panel, "TowerContent");

            Frame(box, "TowerTitleBar", 22, 258, 698, 318);
            var back = Btn(Frame(box, "TowerBackButton", 32, 266, 148, 312));
            Txt(back, "TowerBackText", "◀ 뒤로", 24, TextAlignmentOptions.Center, Color.white, Fill);
            Txt(box, "TowerTitleText", "망령의 탑", 40, TextAlignmentOptions.Center, Color.white, 220, 262, 500, 316);
            Txt(box, "TowerResetText", "초기화까지 3일 4시간", 16, TextAlignmentOptions.MidlineRight, Dim, 480, 276, 684, 306);

            Frame(box, "TowerWeekBar", 22, 322, 698, 382);
            Txt(box, "TowerWeekText", "이번 주 12층", 24, TextAlignmentOptions.MidlineLeft, Color.white, 84, 332, 330, 372);
            Txt(box, "TowerLastWeekText", "지난주 최고 18층", 22, TextAlignmentOptions.MidlineRight, Dim, 400, 332, 646, 372);

            // 층 사다리 — 위에서부터 next+2 … next−2. 지금 · 보스 칸은 넓고 나머지는 좁다(시안 계단 모양)
            int[][] cells =
            {
                new[] { 46, 418, 298, 492 }, new[] { 46, 512, 186, 588 }, new[] { 42, 598, 300, 692 },
                new[] { 46, 706, 186, 782 }, new[] { 46, 786, 190, 852 },
            };
            for (int i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                var cell = Img(Node(box, $"TowerLadderCell{i}"), Sp("TowerEntryPanel/tower_ladder_cell_next"));
                Box(cell.rectTransform, c[0], c[1], c[2], c[3]);
                cell.gameObject.AddComponent<CanvasGroup>();
                var r = cell.rectTransform;
                float h = c[3] - c[1];
                bool wide = c[2] - c[0] > 200;
                var floorT = Txt(r, "TowerLadderFloorText", "13층", wide ? 32 : 26, TextAlignmentOptions.MidlineLeft,
                                 Color.white, 0, 0, 0, 0);
                LeftIn(floorT.rectTransform, 14, -h * 0.18f, 140, 40);
                var nameT = Txt(r, "TowerLadderNameText", "마법의 무리", 15, TextAlignmentOptions.MidlineLeft,
                                Dim, 0, 0, 0, 0);
                LeftIn(nameT.rectTransform, 14, h * 0.25f, c[2] - c[0] - 24, 24);
                LeftIn(floorT.rectTransform, 14, -h * 0.18f, Mathf.Min(140, c[2] - c[0] - 44), 40);
                var check = Img(Node(r, "TowerLadderCheck"), Sp("ChapterHost/ch_check"));
                check.preserveAspect = true;
                LeftIn(check.rectTransform, 92, -h * 0.18f, 30, 30);
                var badge = Img(Node(r, "TowerLadderBadge"), Sp("TowerEntryPanel/tower_badge_go"));
                RightIn(badge.rectTransform, -12, 0, 78, 40);
                Txt(badge.rectTransform, "TowerLadderBadgeText", "도전", 20, TextAlignmentOptions.Center, Dark, Fill, plain: true);
                var boss = Img(Node(r, "TowerLadderBossIcon"), Sp("ChapterHost/ch_icon_skull"));
                boss.preserveAspect = true;
                RightIn(boss.rectTransform, -14, 0, 44, 44);
            }

            // 지금 층 카드
            var card = Img(Node(box, "TowerFloorCard"), Sp("TowerEntryPanel/tower_floor_card"));
            Box(card.rectTransform, 314, 393, 700, 848);
            Txt(box, "TowerFloorNumText", "13층", 38, TextAlignmentOptions.MidlineLeft, Gold, 344, 416, 680, 464);
            Txt(box, "TowerFloorNameText", "마법의 무리", 26, TextAlignmentOptions.MidlineLeft, Color.white, 344, 472, 680, 506);
            var desc = Txt(box, "TowerFloorDescText", "적이 전부 마법. 몸은 세 속성이 섞여 나온다.", 17,
                           TextAlignmentOptions.MidlineLeft, Color.white, 344, 514, 684, 546);
            desc.enableAutoSizing = true;
            desc.fontSizeMin = 12;
            desc.fontSizeMax = 17;
            var adv = Node(box, "TowerFloorAdvRow");
            Box(adv, 344, 548, 684, 584);
            var enemyIcon = Img(Node(adv, "TowerFloorAdvEnemyIcon"), Sp("ChapterHost/ch_kind_magic"));
            enemyIcon.preserveAspect = true;
            LeftIn(enemyIcon.rectTransform, 0, 0, 32, 32);
            var enemyT = Txt(adv, "TowerFloorAdvEnemyText", "적", 18, TextAlignmentOptions.MidlineLeft, Color.white, 0, 0, 0, 0);
            LeftIn(enemyT.rectTransform, 38, 0, 30, 30);
            var arrow = Txt(adv, "TowerFloorAdvArrow", "→", 18, TextAlignmentOptions.MidlineLeft, Dim, 0, 0, 0, 0);
            LeftIn(arrow.rectTransform, 64, 0, 26, 30);
            var advIcon = Img(Node(adv, "TowerFloorAdvIcon"), Sp("ChapterHost/ch_kind_blade"));
            advIcon.preserveAspect = true;
            LeftIn(advIcon.rectTransform, 94, 0, 32, 32);
            var advT = Txt(adv, "TowerFloorAdvText", "무기 몸 유리 ▲", 19, TextAlignmentOptions.MidlineLeft, Green, 0, 0, 0, 0);
            LeftIn(advT.rectTransform, 132, 0, 200, 30);
            Txt(box, "TowerFloorFitText", "맞는 호스트 22 · 내가 키운 ★5", 16, TextAlignmentOptions.MidlineLeft, Dim,
                344, 590, 684, 618);
            for (int i = 0; i < 3; i++)
            {
                int x0 = 340 + i * 81;
                var frame = Img(Node(box, $"TowerFitFace{i}Frame"), Sp("ChapterHost/ch_thumbframe"));
                frame.preserveAspect = true;
                Box(frame.rectTransform, x0, 622, x0 + 74, 696);
                var face = Img(Node(frame.rectTransform, $"TowerFitFace{i}"), null);
                InsetFull(face.rectTransform, 4);
                var star = Txt(frame.rectTransform, $"TowerFitStar{i}", "★", 24, TextAlignmentOptions.Center, Gold, 0, 0, 0, 0);
                RightIn(star.rectTransform, 8, -30, 28, 28);
            }
            var fitBtn = Btn(Frame(box, "TowerFloorFitButton", 584, 622, 670, 696));
            Txt(fitBtn, "TowerFloorFitButtonText", "전체\n보기 ›", 17, TextAlignmentOptions.Center, Color.white, Fill);
            Txt(box, "TowerFirstClearText", "첫 클리어 보상 (매주 다시)", 16, TextAlignmentOptions.MidlineLeft, Dim,
                344, 726, 684, 754);
            Icon(box, "TowerFirstClearGoldIcon", Sp("ChapterHost/ch_icon_coin"), 342, 760, 380, 798);
            Txt(box, "TowerFirstClearGoldText", "50", 26, TextAlignmentOptions.MidlineLeft, Color.white, 392, 760, 446, 798);
            var fcFace = Img(Node(box, "TowerFirstClearFace"), null);
            Box(fcFace.rectTransform, 452, 758, 492, 798);
            Txt(box, "TowerFirstClearShardText", "조각 ×1", 24, TextAlignmentOptions.MidlineLeft, Color.white, 504, 760, 680, 798);

            // 소탕
            Frame(box, "TowerSweepBox", 24, 878, 696, 986);
            Txt(box, "TowerSweepTitleText", "오늘의 소탕", 24, TextAlignmentOptions.MidlineLeft, Color.white, 52, 892, 300, 924);
            Txt(box, "TowerSweepSubText", "12층 기준", 15, TextAlignmentOptions.MidlineLeft, Dim, 192, 898, 330, 922);
            Icon(box, "TowerSweepGoldIcon", Sp("ChapterHost/ch_icon_coin"), 50, 932, 86, 968);
            Txt(box, "TowerSweepGoldText", "200 · 조각 ×2", 22, TextAlignmentOptions.MidlineLeft, Color.white, 98, 932, 470, 968);
            var sweep = Btn(Img(Node(box, "TowerSweepButton"), Sp("LobbyMainUI/buttonblue")).rectTransform);
            Box(sweep, 500, 900, 674, 966);
            Txt(sweep, "TowerSweepButtonText", "소탕", 26, TextAlignmentOptions.Center, new Color(0.02f, 0.1f, 0.18f), Fill, plain: true);
            Txt(box, "TowerSweepHintText", "하루 1번 · 입장 횟수를 쓰지 않는다 · 싸우지 않고 받는다", 15,
                TextAlignmentOptions.Center, Dim, 24, 992, 696, 1018);

            // 입장 · 도전
            Frame(box, "TowerEntryBox", 78, 1032, 352, 1080);
            Txt(box, "TowerEntryLabel", "오늘 입장", 20, TextAlignmentOptions.MidlineLeft, Color.white, 88, 1040, 166, 1072);
            for (int i = 0; i < 3; i++)
                Icon(box, $"TowerEntryPip{i}", Sp("ModeCommon/mode_entry_pip_on"), 173 + i * 31, 1043, 199 + i * 31, 1069);
            Txt(box, "TowerEntryText", "2 / 3", 26, TextAlignmentOptions.MidlineLeft, Color.white, 272, 1038, 350, 1074);
            var buy = Btn(Frame(box, "TowerBuyButton", 458, 1032, 658, 1080));
            BuyContents(buy, "Tower");
            var start = Btn(GoldButton(box, "TowerStartButton"));
            Box(start, 148, 1104, 568, 1212);
            Txt(start, "TowerStartText", "13층 도전 ▶", 44, TextAlignmentOptions.Center, Dark, Fill, plain: true);
            Txt(box, "TowerStartHintText", "입장 1번 사용 · 이기든 지든 줄어든다", 15, TextAlignmentOptions.Center, Dim,
                148, 1222, 568, 1248);

            var comp = panel.gameObject.AddComponent<TowerEntryPanel>();
            var so = new SerializedObject(comp);
            so.FindProperty("_cellPast").objectReferenceValue = Sp("TowerEntryPanel/tower_ladder_cell_past");
            so.FindProperty("_cellNext").objectReferenceValue = Sp("TowerEntryPanel/tower_ladder_cell_next");
            so.FindProperty("_cellNow").objectReferenceValue = Sp("TowerEntryPanel/tower_ladder_cell_now");
            so.FindProperty("_cellBoss").objectReferenceValue = Sp("TowerEntryPanel/tower_ladder_cell_boss");
            so.FindProperty("_pipOn").objectReferenceValue = Sp("ModeCommon/mode_entry_pip_on");
            so.FindProperty("_pipOff").objectReferenceValue = Sp("ModeCommon/mode_entry_pip_off");
            so.FindProperty("_kindBlade").objectReferenceValue = Sp("ChapterHost/ch_kind_blade");
            so.FindProperty("_kindForce").objectReferenceValue = Sp("ChapterHost/ch_kind_force");
            so.FindProperty("_kindMagic").objectReferenceValue = Sp("ChapterHost/ch_kind_magic");
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
        }

        // ── 밤 (시안 N1, 941×1672 → 720 기준으로 잰 값) ─────────

        private static NightEntryPanel BuildNight(Transform root)
        {
            var panel = Fresh(root, "NightEntryPanel");
            Backdrop(panel, "NightBg", Sp("ModeBackdrop/night_panel_bg"));
            var box = Content(panel, "NightContent");

            var back = Btn(Frame(box, "NightBackButton", 22, 182, 102, 240));
            Txt(back, "NightBackText", "◀", 30, TextAlignmentOptions.Center, Cyan, Fill);
            Frame(box, "NightTitlePlate", 232, 178, 490, 242);
            Txt(box, "NightTitleText", "망령의 밤", 34, TextAlignmentOptions.Center, Color.white, 246, 182, 476, 238);

            for (int i = 0; i < 7; i++)
            {
                float cx = 18 + i * 98.4f + 46;
                var cell = Btn(Img(Node(box, $"NightDayCell{i}"), Sp("NightEntryPanel/night_day_cell")).rectTransform);
                Box(cell, cx - 43, 252, cx + 43, 330);
                var d = Txt(cell, $"NightDayName{i}", "화", 22, TextAlignmentOptions.Center, Color.white, 0, 0, 0, 0);
                CenterIn(d.rectTransform, 0, 11, 80, 30);
                var r = Txt(cell, $"NightDayRule{i}", "원거리", 15, TextAlignmentOptions.Center, Dim, 0, 0, 0, 0);
                CenterIn(r.rectTransform, 0, -20, 86, 24);
            }

            var card = Img(Node(box, "NightTodayCard"), Sp("NightEntryPanel/night_today_card"));
            Box(card.rectTransform, 14, 350, 706, 800);
            var art = Img(Node(box, "NightTodayArt"), Sp("NightEntryPanel/night_today_art"));
            art.preserveAspect = false;
            Box(art.rectTransform, 351, 368, 698, 562);
            Txt(box, "NightDayText", "오늘 · 화요일", 17, TextAlignmentOptions.MidlineLeft, Cyan, 50, 380, 330, 404);
            Txt(box, "NightRuleTitleText", "원거리의 날", 44, TextAlignmentOptions.MidlineLeft, Gold, 48, 404, 400, 458);
            Txt(box, "NightRuleText", "원거리 호스트만 몸으로 나온다", 21, TextAlignmentOptions.MidlineLeft, Color.white, 50, 470, 440, 500);
            Txt(box, "NightHostFitText", "오늘 나오는 호스트 7 · 내가 키운 ★2", 17, TextAlignmentOptions.MidlineLeft, Cyan, 50, 518, 440, 542);
            for (int i = 0; i < 7; i++)
            {
                int x0 = 48 + i * 92;
                var slot = Img(Node(box, $"NightHostSlot{i}"), Sp("ChapterHost/ch_thumbframe"));
                slot.preserveAspect = true;
                Box(slot.rectTransform, x0, 553, x0 + 80, 633);
                var face = Img(Node(slot.rectTransform, $"NightHostFace{i}"), null);
                InsetFull(face.rectTransform, 4);
                var star = Txt(slot.rectTransform, $"NightHostStar{i}", "★", 24, TextAlignmentOptions.Center, Gold, 0, 0, 0, 0);
                RightIn(star.rectTransform, 8, -32, 28, 28);
            }
            Frame(box, "NightBestBox", 40, 652, 352, 782);
            Txt(box, "NightBestLabel", "화요일 최고 처치", 17, TextAlignmentOptions.MidlineLeft, Cyan, 74, 674, 340, 698);
            Txt(box, "NightBestText", "238", 36, TextAlignmentOptions.MidlineLeft, Color.white, 72, 700, 340, 748);
            Frame(box, "NightRewardBox", 364, 652, 682, 782);
            Txt(box, "NightRewardLabel", "보상", 17, TextAlignmentOptions.MidlineLeft, Cyan, 398, 674, 670, 698);
            var rw = Txt(box, "NightRewardText", "처치 50마다 조각 1\n보스 처치 +3", 19, TextAlignmentOptions.TopLeft, Color.white,
                         398, 700, 676, 770);
            rw.textWrappingMode = TextWrappingModes.Normal;

            int[][] flow = { new[] { 20, 182 }, new[] { 192, 356 }, new[] { 364, 528 }, new[] { 538, 702 } };
            for (int i = 0; i < flow.Length; i++)
            {
                var f = Frame(box, $"NightFlowCell{i}", flow[i][0], 812, flow[i][1], 926);
                var t = Txt(f, $"NightFlowTitle{i}", "0:00", 22, TextAlignmentOptions.Center, Color.white, 0, 0, 0, 0);
                CenterIn(t.rectTransform, 0, 20, flow[i][1] - flow[i][0] - 16, 32);
                var s = Txt(f, $"NightFlowText{i}", "몸 셋 중 하나에 빙의", 15, TextAlignmentOptions.Center, Dim, 0, 0, 0, 0);
                CenterIn(s.rectTransform, 0, -18, flow[i][1] - flow[i][0] - 16, 26);
                s.enableAutoSizing = true;
                s.fontSizeMin = 10;
                s.fontSizeMax = 15;
            }

            Frame(box, "NightEntryBox", 22, 958, 494, 1018);
            Txt(box, "NightEntryLabel", "오늘 입장", 20, TextAlignmentOptions.MidlineLeft, Color.white, 38, 972, 126, 1004);
            for (int i = 0; i < 3; i++)
                Icon(box, $"NightEntryPip{i}", Sp("ModeCommon/mode_entry_pip_on"), 135 + i * 32, 975, 161 + i * 32, 1001);
            Txt(box, "NightEntryText", "3 / 3", 26, TextAlignmentOptions.MidlineLeft, Color.white, 238, 970, 320, 1006);
            var buy = Btn(Frame(box, "NightBuyButton", 512, 958, 700, 1018));
            BuyContents(buy, "Night");
            var start = Btn(GoldButton(box, "NightStartButton"));
            Box(start, 140, 1040, 580, 1140);
            Txt(start, "NightStartText", "시작 ▶", 48, TextAlignmentOptions.Center, Dark, Fill, plain: true);
            Txt(box, "NightStartHintText", "입장 1번 사용 · 이기든 지든 줄어든다", 16, TextAlignmentOptions.Center, Dim,
                170, 1150, 550, 1184);

            var comp = panel.gameObject.AddComponent<NightEntryPanel>();
            var so = new SerializedObject(comp);
            so.FindProperty("_dayCell").objectReferenceValue = Sp("NightEntryPanel/night_day_cell");
            so.FindProperty("_dayCellToday").objectReferenceValue = Sp("NightEntryPanel/night_day_cell_today");
            so.FindProperty("_pipOn").objectReferenceValue = Sp("ModeCommon/mode_entry_pip_on");
            so.FindProperty("_pipOff").objectReferenceValue = Sp("ModeCommon/mode_entry_pip_off");
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
        }

        /// <summary>큰 금 버튼(시안 T1 · N1 「도전 ▶」 · 「시작 ▶」) — 비율 그대로.</summary>
        private static RectTransform GoldButton(Transform parent, string name)
        {
            var img = Img(Node(parent, name), Sp("ChapterScreens/button_yellow_wide"));
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            return img.rectTransform;
        }

        /// <summary>「＋ 💎 추가 입장」 — 버튼 안 가운데 덩어리.</summary>
        private static void BuyContents(RectTransform buy, string prefix)
        {
            var plus = Txt(buy, $"{prefix}BuyPlus", "+", 24, TextAlignmentOptions.Center, Color.white, 0, 0, 0, 0);
            CenterIn(plus.rectTransform, -70, 0, 24, 30);
            var gem = Img(Node(buy, $"{prefix}BuyGem"), Sp("ChapterHost/ch_icon_gem"));
            gem.preserveAspect = true;
            CenterIn(gem.rectTransform, -38, 0, 30, 28);
            var t = Txt(buy, $"{prefix}BuyText", "추가 입장", 20, TextAlignmentOptions.MidlineLeft, Color.white, 0, 0, 0, 0);
            LeftInCenter(t.rectTransform, -16, 0, 110, 30);
        }

        // ── 로비 모드 칸 ────────────────────────────────────────

        /// <summary>
        /// 모드 칸 그림 · 자물쇠는 로비 바탕(LobbyV4Mid)에 구워져 있다 — 칸 위에 그림을 덮어 자물쇠를 가리고
        /// 디펜스 칸은 새 「유령 탑」 그림으로 바꾼다(시안 L). 빨간 점도 칸마다 하나.
        /// </summary>
        private static void ModeCards(Transform root)
        {
            Cover(root, "ModeSurvivalCard", "ModeSurvivalArt", Sp("LobbyMainUI/modeart_survival"), "ModeSurvivalBadge", "ModeSurvival");
            Cover(root, "ModeDefenseCard", "ModeDefenseArt", Sp("LobbyMainUI/modeart_tower"), "ModeDefenseBadge", "ModeDefense");
        }

        private static void Cover(Transform root, string cardName, string artName, Sprite art, string badgeName, string prefix)
        {
            var card = FindDeep(root, cardName);
            if (card == null) { Debug.LogWarning($"[모드 UI] {cardName} 없음"); return; }
            var old = card.Find(artName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var oldBadge = card.Find(badgeName);
            if (oldBadge != null) Object.DestroyImmediate(oldBadge.gameObject);
            var mask = Node(card, artName);
            InsetFull(mask, 6);
            mask.gameObject.AddComponent<RectMask2D>();
            mask.SetAsFirstSibling();
            var img = Img(Node(mask, "Art"), art);
            var ir = img.rectTransform;
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(320, 140);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art != null ? art.rect.width / art.rect.height : 2f;
            // 제목 · 칩 — 칸 왼쪽 아래(시안 L). 글자는 코드가 채운다(LobbyMainUI.RefreshModeCards) — 고정 번역은 뗀다
            var title = FindDeep(root, $"{prefix}TitleText");
            var sub = FindDeep(root, $"{prefix}SubText");
            var oldChip = card.Find($"{prefix}SubChip");
            if (oldChip != null) Object.DestroyImmediate(oldChip.gameObject);
            var chip = Img(Node(card, $"{prefix}SubChip"), Sp("ChapterHost/ch_pill"));
            var cr = chip.rectTransform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0f, 0f);
            cr.anchoredPosition = new Vector2(14, 10);
            cr.sizeDelta = new Vector2(250, 34);
            foreach (var t in new[] { title, sub })
            {
                if (t == null) continue;
                foreach (var c in t.GetComponents<Component>())
                    if (c != null && c.GetType().Name == "LocalizedText") Object.DestroyImmediate(c);
            }
            if (title != null)
            {
                title.SetParent(card, false);
                var tr = (RectTransform)title;
                tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0f, 0f);
                tr.anchoredPosition = new Vector2(16, 44);
                tr.sizeDelta = new Vector2(290, 46);
                var tmp = title.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMax = 34;
                    tmp.fontSizeMin = 20;
                    if (s_outline != null) tmp.fontSharedMaterial = s_outline;
                }
            }
            if (sub != null)
            {
                sub.SetParent(chip.rectTransform, false);
                var sr = (RectTransform)sub;
                sr.anchorMin = Vector2.zero;
                sr.anchorMax = Vector2.one;
                sr.offsetMin = new Vector2(12, 2);
                sr.offsetMax = new Vector2(-12, -2);
                var tmp = sub.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMax = 18;
                    tmp.fontSizeMin = 11;
                    tmp.color = Color.white;
                }
            }

            var badge = Img(Node(card, badgeName), Sp("LobbyMainUI/notifybadge"));
            badge.preserveAspect = true;
            var b = badge.rectTransform;
            b.anchorMin = b.anchorMax = b.pivot = new Vector2(1f, 1f);
            b.anchoredPosition = new Vector2(8, 8);
            b.sizeDelta = new Vector2(34, 34);
        }

        // ── 도우미 ──────────────────────────────────────────────

        private static Sprite Sp(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/BaseResource/{path}.png");

        private static void PickOutline(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.font == null || !t.fontSharedMaterial.name.Contains("Outline")) continue;
                if (s_font == null) s_font = t.font;
                if (t.font == s_font) { s_outline = t.fontSharedMaterial; return; }
            }
        }

        private static void PlaceBelowTopBar(Transform root, Transform panel)
        {
            var top = root.Find("FixedTopBar");
            if (top == null) { panel.SetAsLastSibling(); return; }
            int ti = top.GetSiblingIndex(), pi = panel.GetSiblingIndex();
            panel.SetSiblingIndex(pi < ti ? ti - 1 : ti);
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private static RectTransform Fresh(Transform root, string name)
        {
            var old = root.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var node = Node(root, name);
            node.anchorMin = Vector2.zero;
            node.anchorMax = Vector2.one;
            node.offsetMin = node.offsetMax = Vector2.zero;
            return node;
        }

        /// <summary>화면을 덮는 바탕 — 비율이 달라도 빈 곳 없이(cover), 뒤 로비를 막는다.</summary>
        private static void Backdrop(Transform panel, string name, Sprite sprite)
        {
            var img = Img(Node(panel, name), sprite);
            img.raycastTarget = true;   // 뒤 로비가 눌리지 않게
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(720, 1280);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 720f / 1280f;
        }

        /// <summary>시안 한 장(720×1280) 판을 화면 가운데에 — `ScreenFitLock` 으로 가장자리로 안 끌려가게.</summary>
        private static RectTransform Content(Transform parent, string name)
        {
            var box = Node(parent, name);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = new Vector2(720, 1280);
            box.gameObject.AddComponent<ScreenFitLock>();
            return box;
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(Transform node, Sprite sprite)
        {
            var img = node.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            if (sprite == null) img.enabled = false;
            return img;
        }

        private static RectTransform Frame(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var img = Img(Node(parent, name), Sp("ModeCommon/mode_frame"));
            Box(img.rectTransform, x0, y0, x1, y1);
            return img.rectTransform;
        }

        private static void Icon(Transform parent, string name, Sprite s, float x0, float y0, float x1, float y1)
        {
            var img = Img(Node(parent, name), s);
            img.preserveAspect = true;
            Box(img.rectTransform, x0, y0, x1, y1);
        }

        private static RectTransform Btn(RectTransform node)
        {
            var g = node.GetComponent<Graphic>();
            if (g == null) g = node.gameObject.AddComponent<Image>();
            g.raycastTarget = true;
            var b = node.gameObject.AddComponent<Button>();
            b.targetGraphic = g;
            return node;
        }

        private const int Fill = int.MinValue;

        private static TextMeshProUGUI Txt(Transform parent, string name, string text, float size,
                                           TextAlignmentOptions align, Color color, int fill, bool plain = false)
        {
            var t = Txt(parent, name, text, size, align, color, 0, 0, 0, 0, plain);
            var r = t.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(6, 2);
            r.offsetMax = new Vector2(-6, -2);
            return t;
        }

        private static TextMeshProUGUI Txt(Transform parent, string name, string text, float size,
                                           TextAlignmentOptions align, Color color, float x0, float y0, float x1, float y1,
                                           bool plain = false)
        {
            var node = Node(parent, name);
            var t = node.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = s_font;
            if (!plain && s_outline != null) t.fontSharedMaterial = s_outline;
            t.fontSize = size;
            t.text = text;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            // 일본어 · 영어가 더 길다 — 칸을 넘으면 줄인다(06_ui 「글자는 삐져나가지 않는다」)
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(10, size * 0.6f);
            if (x1 > x0) Box(node, x0, y0, x1, y1);
            return t;
        }

        /// <summary>시안 좌표(720×1280, 왼쪽 위 기준) 상자 → 판 가운데 기준 자리.</summary>
        private static void Box(RectTransform r, float x0, float y0, float x1, float y1)
        {
            var p = r.parent as RectTransform;
            // 판(720×1280) 바로 아래가 아니면 부모 상자의 시안 좌표를 빼서 넣는다
            float px0 = 0, py0 = 0, pw = 720, ph = 1280;
            if (p != null && p.GetComponent<ScreenFitLock>() == null && p.sizeDelta.x > 0)
            {
                pw = p.sizeDelta.x;
                ph = p.sizeDelta.y;
                px0 = 360 + p.anchoredPosition.x - pw / 2;
                py0 = 640 - p.anchoredPosition.y - ph / 2;
            }
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2((x0 + x1) / 2 - px0 - pw / 2, ph / 2 - ((y0 + y1) / 2 - py0));
            r.sizeDelta = new Vector2(x1 - x0, y1 - y0);
        }

        private static void InsetFull(RectTransform r, float inset)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>부모 왼쪽 가운데 기준 — x 는 왼쪽 끝에서, y 는 위로 +.</summary>
        private static void LeftIn(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>부모 오른쪽 가운데 기준 — x 는 오른쪽 끝에서 바깥으로 +.</summary>
        private static void RightIn(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot = new Vector2(1f, 0.5f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void CenterIn(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void LeftInCenter(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }
    }
}
