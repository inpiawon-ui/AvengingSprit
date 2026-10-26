// RE:BORN UI 리뉴얼(2026-10-23) — 도전 탭(시안 Screens/challenge · tower · night). 로비 프리팹 안의 쪽 하나에 허브 · 탑 · 밤 세 묶음.
// 좌표는 시안 1080 픽셀 상자, 묶음 몸통은 쪽 머리 아래(420) ~ 아래 탭 위(2040) 세로 스크롤.
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbChallengeBuilder
    {
        private const float Top = 420f;

        public static void Build(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageChallenge");
            BuildHub(page);
            BuildTower(page);
            BuildNight(page);
            var view = page.gameObject.AddComponent<RbChallengePage>();
            var so = new SerializedObject(view);
            so.FindProperty("_rowNow").objectReferenceValue = KitSprite("Components/rb_list_row");   // 선택 판은 왼쪽 청록 막대가 층 글자를 덮는다 — 현재 층은 청록 글자로
            so.FindProperty("_rowPast").objectReferenceValue = KitSprite("Components/rb_list_row");
            so.FindProperty("_rowLocked").objectReferenceValue = KitSprite("Components/rb_list_row");
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }

        private static RectTransform Group(RectTransform page, string name, string titleKey, string korean, float contentH, bool help)
        {
            var g = Stretch(Node(page, name));
            // 쪽 머리 이름 — 허브는 Chl, 탑 · 밤은 ChlTowerPage · ChlNightPage(허브 카드의 ChlTowerTitle 등과 겹치지 않게)
            string head = name == "ChlHub" ? "Chl" : name + "Page";
            RbHostBuilder.Head(g, head, titleKey, korean, 25, 280, 155, 400, 800, 922);
            if (!help) g.Find(head + "Help")?.gameObject.SetActive(false);
            return VScroll(g, name + "Body", Top, 2040, contentH);
        }

        private static Button Button2(RectTransform body, string name, string key, string korean, float x0, float y0, float x1, float y1, bool primary, float size = 0f)
        {
            var b = PlaceIn(Node(body, name), Top, x0, y0, x1, y1);
            var btn = Btn(b, primary ? "Components/rb_btn_primary" : "Components/rb_btn_secondary");
            Text(Stretch(Node(b, name + "Text")), key, korean, size > 0f ? size : primary ? 46 : 38, primary ? Ink : White, TA.Center, heavy: true);
            return btn;
        }

        /// <summary>그림을 판 안에 덮어 깐다(넘치는 쪽은 잘림) + 아래 그늘.</summary>
        private static void ArtCard(RectTransform card, string name, string art, float aspect)
        {
            var clip = Stretch(Node(card, name + "Clip"));
            clip.offsetMin = new Vector2(4f, 4f); clip.offsetMax = new Vector2(-4f, -4f);
            clip.gameObject.AddComponent<RectMask2D>();
            var a = Stretch(Node(clip, name + "Art"));
            Img(a, art).preserveAspect = false;
            var fit = a.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = aspect;
            Img(Stretch(Node(clip, name + "Shade")), "Components/rb_fade_bottom").preserveAspect = false;
        }

        private static float AspectOf(string path)
        {
            var s = KitSprite(path);
            return s != null ? s.rect.width / s.rect.height : 2f;
        }

        // ── 허브 ────────────────────────────────────────────

        private static void BuildHub(RectTransform page)
        {
            var body = Group(page, "ChlHub", "ui.rb.tab.challenge", "도전", 1880 - Top, true);
            Card(body, "ChlTower", 452, 1122, "Art/rb_art_key_challenge_tower");
            Card(body, "ChlNight", 1165, 1832, "Art/rb_art_key_challenge_night");
        }

        private static void Card(RectTransform body, string p, float y0, float y1, string art)
        {
            const float x0 = 33, x1 = 1049;
            var c = PlaceIn(Node(body, p + "Card"), Top, x0, y0, x1, y1);
            Img(c, "Components/rb_panel_hero");
            ArtCard(c, p + "Card", art, AspectOf(art));
            float ty = y1 - 412;
            Text(Local(Node(c, p + "Title"), x0, y0, x1, y1, 66, ty, 700, ty + 70), null, "망령의 탑", 62, White, TA.Left, heavy: true);
            Text(Local(Node(c, p + "Info"), x0, y0, x1, y1, 66, ty + 96, 900, ty + 140), null, "이번 주 2층 · 지난주 최고 5층", 38, Dim, TA.Left);
            Text(Local(Node(c, p + "Entries"), x0, y0, x1, y1, 66, ty + 172, 760, ty + 212), null, "남은 입장 2/3 · 초기화까지 03:24:00", 32, Dim, TA.Left);
            var e = Local(Node(c, p + "Enter"), x0, y0, x1, y1, 760, ty + 250, 1016, ty + 350);
            Btn(e, "Components/rb_btn_secondary");
            Text(Stretch(Node(e, p + "EnterText")), "ui.rb.chl.enter", "입장 ›", 40, White, TA.Center, heavy: true);
        }

        // ── 탑 ─────────────────────────────────────────────

        private static void BuildTower(RectTransform page)
        {
            // 시안 tower(1080 기준, 탑 판 위 변 = 430) — 탑 그림 판 위에 다섯 층(이번 5층 묶음) · 아래 판에 보상 · 버튼 · 오늘의 소탕
            var body = Group(page, "ChlTower", "ui.tower.title", "망령의 탑", 1990 - Top, true);
            var ladder = PlaceIn(Node(body, "ChlLadder"), Top, 29, 430, 1047, 1192);
            Img(ladder, "Components/rb_panel_base");
            ArtCard(ladder, "ChlLadder", "Art/rb_art_bg_tower_column", AspectOf("Art/rb_art_bg_tower_column"));
            float[] ys = { 454, 600, 746, 892, 1038 };
            for (int i = 0; i < 5; i++)
            {
                float y0 = ys[i], y1 = y0 + 124;
                // 줄 그림은 모두 같은 판(rb_list_row) — 「현재 도전」 판(rb_list_row_selected)은 왼쪽 청록 막대가 층 글자를 덮었다(PD 2026-10-26)
                var row = PlaceIn(Node(body, $"ChlFloor{i}"), Top, 55, y0, 1024, y1);
                Img(row, "Components/rb_list_row");
                Text(Local(Node(row, $"ChlFloor{i}No"), 55, y0, 1024, y1, 82, y0 + 30, 300, y0 + 94), null, "5F", 54, White, TA.Left, number: true);
                Img(Local(Node(row, $"ChlFloor{i}Lock"), 55, y0, 1024, y1, 862, y0 + 40, 902, y0 + 80), "Icons/rb_icon_sys_lock_48");
                Text(Local(Node(row, $"ChlFloor{i}State"), 55, y0, 1024, y1, 600, y0 + 34, 994, y0 + 90), null, "잠김", 40, Dim, TA.Right, heavy: true);
            }
            Img(PlaceIn(Node(body, "ChlTowerPanel"), Top, 29, 1216, 1047, 1957), "Components/rb_panel_base");
            Text(PlaceIn(Node(body, "ChlFloorTitle"), Top, 55, 1238, 730, 1298), null, "2층", 54, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlFloorDesc"), Top, 55, 1310, 740, 1352), "ui.tower.first_clear", "첫 클리어 보상 (매주 다시)", 34, Dim, TA.Left);
            var buy = Button2(body, "ChlTowerBuy", "ui.rb.chl.buy", "추가 입장 ›", 750, 1240, 1020, 1350, false, 38);
            Reward(body, "ChlReward", 1393, 1557);
            Button2(body, "ChlTowerFit", "ui.rb.chl.sweep_rule", "소탕 조건 확인", 55, 1584, 527, 1717, false, 40);
            StartButton(body, "ChlTowerStart", "도전", 552, 1584, 1024, 1717);
            Text(PlaceIn(Node(body, "ChlSweepTitle"), Top, 55, 1760, 760, 1812), "ui.tower.sweep", "오늘의 소탕", 44, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlSweepInfo"), Top, 55, 1822, 770, 1868), null, "하루 1번 · 입장 횟수를 쓰지 않는다", 32, Dim, TA.Left);
            Button2(body, "ChlTowerSweep", null, "소탕", 790, 1756, 1024, 1871, false, 40);
            Text(PlaceIn(Node(body, "ChlTowerEntryLine"), Top, 55, 1890, 1024, 1940), null, "남은 입장 2/3 · 초기화까지 03:24:00", 30, Dim, TA.Left);
            buy.transform.SetAsLastSibling();
        }

        /// <summary>보상 두 칸 — 골드 · 조각(얼굴).</summary>
        private static void Reward(RectTransform body, string p, float y0, float y1)
        {
            // 시안 tower: 얇은 테 칸(rb_slot_common) 둘 — 아이콘 왼쪽 · 숫자와 이름 가운데
            var g = PlaceIn(Node(body, p + "GoldTile"), Top, 55, y0, 527, y1);
            Img(g, "Components/rb_slot_common");
            Img(Local(Node(g, p + "GoldIcon"), 55, y0, 527, y1, 85, y0 + 32, 185, y0 + 132), "Icons/rb_icon_currency_gold_96");
            Text(Local(Node(g, p + "Gold"), 55, y0, 527, y1, 200, y0 + 22, 505, y0 + 86), null, "500", 54, White, TA.Center, number: true);
            Text(Local(Node(g, p + "GoldLabel"), 55, y0, 527, y1, 200, y0 + 92, 505, y0 + 136), "ui.rb.result.gold", "골드", 34, White, TA.Center, heavy: true);
            var s = PlaceIn(Node(body, p + "ShardTile"), Top, 552, y0, 1024, y1);
            Img(s, "Components/rb_slot_common");
            Img(Local(Node(s, p + "ShardIcon"), 552, y0, 1024, y1, 582, y0 + 40, 662, y0 + 120), "Icons/rb_icon_shard_96");
            Img(Local(Node(s, p + "ShardFace"), 552, y0, 1024, y1, 640, y0 + 24, 700, y0 + 84), "Icons/rb_icon_mode_ghost_96");
            Text(Local(Node(s, p + "Shard"), 552, y0, 1024, y1, 710, y0 + 22, 1004, y0 + 86), null, "3", 54, White, TA.Center, number: true);
            Text(Local(Node(s, p + "ShardLabel"), 552, y0, 1024, y1, 710, y0 + 92, 1004, y0 + 136), "ui.rb.chl.host_shards", "호스트 조각", 34, White, TA.Center, heavy: true);
        }

        // ── 밤 ─────────────────────────────────────────────

        private static void BuildNight(RectTransform page)
        {
            // 시안 night(1080 기준, 요일 줄 위 변 = 430) — PD 2026-10-26 「버튼 크기 · 오늘 보상 표현 · 시작 모두 시안과 똑같이」
            var body = Group(page, "ChlNight", "ui.night.title", "망령의 밤", 1990 - Top, true);
            // 요일 7칸 — 폭을 꽉 채우는 네모 버튼(141 × 137, 사이 5) · 고른 날은 청록 글자
            for (int i = 0; i < 7; i++)
            {
                float x0 = 29 + i * 146f, x1 = x0 + 141f;
                var d = PlaceIn(Node(body, $"ChlDay{i}"), Top, x0, 430, x1, 567);
                Btn(d, "Components/rb_btn_secondary");
                Text(Stretch(Node(d, $"ChlDay{i}Text")), null, "월", 44, Dim, TA.Center, heavy: true);
            }
            // 배너 — 판 폭 전체에 그림을 덮어 깐다
            var art = PlaceIn(Node(body, "ChlNightArt"), Top, 29, 604, 1047, 902);
            Img(art, "Components/rb_panel_base");
            ArtCard(art, "ChlNightArt", "Art/rb_art_key_night", AspectOf("Art/rb_art_key_night"));
            // 정보 판 — 요일 · 최고 처치 · 남은 입장 · ◆ 오늘의 보상(두 칸) · 규칙 · 보상 줄
            const float px0 = 29, py0 = 927, px1 = 1047, py1 = 1615;
            Img(PlaceIn(Node(body, "ChlNightPanel"), Top, px0, py0, px1, py1), "Components/rb_panel_base");
            Text(PlaceIn(Node(body, "ChlNightDayTitle"), Top, 55, 955, 1020, 1020), null, "목 · 파워의 날", 56, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlNightBest"), Top, 55, 1052, 640, 1096), null, "목요일 최고 처치 128", 36, White, TA.Left);
            Text(PlaceIn(Node(body, "ChlNightLeft"), Top, 640, 1052, 1020, 1096), null, "남은 입장 2/3", 36, White, TA.Right);
            Text(PlaceIn(Node(body, "ChlNightRewardTitle"), Top, 55, 1124, 700, 1174), "ui.rb.chl.today_reward", "◆ 오늘의 보상", 44, White, TA.Left, heavy: true);
            NightCell(body, "ChlNightKill", 55, 527, "Components/rb_slot_ssr", "Icons/rb_icon_shard_96", "1", "처치 50마다");
            NightCell(body, "ChlNightBoss", 552, 1020, "Components/rb_slot_rare", "Icons/rb_icon_shard_96", "+3", "보스 처치");
            Img(PlaceIn(Node(body, "ChlNightShardFace"), Top, 330, 1215, 400, 1285), "Icons/rb_icon_mode_ghost_96");   // 그날 몸 얼굴 — 조각 아이콘 옆
            var rule = Text(PlaceIn(Node(body, "ChlNightRule"), Top, 55, 1465, 1020, 1510), null, "몸 셋 중 하나에 빙의", 34, Dim, TA.Left);
            rule.enableWordWrapping = true;
            Text(PlaceIn(Node(body, "ChlNightReward"), Top, 55, 1540, 1020, 1590), null, "처치 50마다 조각 1 · 보스 처치 +3", 36, White, TA.Left, heavy: true);
            // 시작 — 노란 큰 버튼 「시작 ▶ | 입장 1」(시안 1825~1975)
            StartButton(body, "ChlNightStart", "시작 ▶", 29, 1825, 1047, 1975);
        }

        /// <summary>오늘의 보상 칸 — 테두리 칸 · 아이콘(위) · 큰 숫자 · 이름(아래). 시안 night 의 「432 골드」 · 「3 호스트 조각」 칸.</summary>
        private static void NightCell(RectTransform body, string p, float x0, float x1, string frame, string icon, string value, string label)
        {
            const float y0 = 1201, y1 = 1445;
            var c = PlaceIn(Node(body, p + "Tile"), Top, x0, y0, x1, y1);
            Img(c, frame);
            float cx = (x0 + x1) * 0.5f;
            Img(Local(Node(c, p + "Icon"), x0, y0, x1, y1, cx - 46, y0 + 18, cx + 46, y0 + 110), icon);
            Text(Local(Node(c, p + "Value"), x0, y0, x1, y1, x0 + 10, y0 + 114, x1 - 10, y0 + 172), null, value, 52, White, TA.Center, number: true);
            Text(Local(Node(c, p + "Label"), x0, y0, x1, y1, x0 + 10, y0 + 176, x1 - 10, y0 + 222), null, label, 34, White, TA.Center, heavy: true);
        }

        /// <summary>도전 · 시작 노란 버튼 — 글자 · 칸막이 · 「입장 1」(시안의 「▶ | 🎫1」). 「1」 은 입장 1번을 쓴다는 뜻.</summary>
        private static void StartButton(RectTransform body, string name, string korean, float x0, float y0, float x1, float y1)
        {
            var b = PlaceIn(Node(body, name), Top, x0, y0, x1, y1);
            Btn(b, "Components/rb_btn_primary");
            float cx = (x0 + x1) * 0.5f;
            Text(Local(Node(b, name + "Text"), x0, y0, x1, y1, x0 + 20, y0 + 10, cx + 60, y1 - 18), null, korean, 58, Ink, TA.Right, heavy: true);
            Img(Local(Node(b, name + "Bar"), x0, y0, x1, y1, cx + 84, y0 + 34, cx + 88, y1 - 42), "Components/rb_bar_fill_cyan").color = Ink;
            Text(Local(Node(b, name + "Cost"), x0, y0, x1, y1, cx + 106, y0 + 10, x1 - 20, y1 - 18), "ui.rb.chl.cost_entry", "입장 1", 46, Ink, TA.Left, heavy: true);
        }
    }
}
