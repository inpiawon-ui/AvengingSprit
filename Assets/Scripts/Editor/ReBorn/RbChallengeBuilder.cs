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
            // 시안 tower: 현재 · 깬 층 = 도톰한 남색 버튼 판(흰 글자), 잠긴 층 = 납작한 판(흐린 글자). 선택 판(rb_list_row_selected)은 왼쪽 청록 막대가 층 글자를 덮었다
            so.FindProperty("_rowNow").objectReferenceValue = KitSprite("Components/rb_btn_secondary");
            so.FindProperty("_rowPast").objectReferenceValue = KitSprite("Components/rb_btn_secondary");
            so.FindProperty("_rowLocked").objectReferenceValue = KitSprite("Components/rb_list_row");
            string[] days = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };
            var arts = so.FindProperty("_nightDayArts");
            arts.arraySize = days.Length;
            for (int i = 0; i < days.Length; i++) arts.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Art/rb_art_night_day_{days[i]}");
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
        private static void ArtCard(RectTransform card, string name, string art, float aspect, bool shade = true)
        {
            var clip = Stretch(Node(card, name + "Clip"));
            clip.offsetMin = new Vector2(4f, 4f); clip.offsetMax = new Vector2(-4f, -4f);
            clip.gameObject.AddComponent<RectMask2D>();
            var a = Stretch(Node(clip, name + "Art"));
            Img(a, art).preserveAspect = false;
            var fit = a.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = aspect;
            if (shade) Img(Stretch(Node(clip, name + "Shade")), "Components/rb_fade_bottom").preserveAspect = false;
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
            // 시안 tower(1080 기준) — 탑 그림 판 위에 다섯 층(이번 5층 묶음) · 아래 판에 보상 · 버튼 · 오늘의 소탕.
            // 16:9 한 화면(아래 탭 위 ≈ 1880)에 다 들게 — PD 2026-10-26 「층마다 벌려 놓지 말고 · 스크롤 될 필요 없다」.
            // 층 줄은 시안처럼 거의 붙여(높이 100 · 사이 10)
            var body = Group(page, "ChlTower", "ui.tower.title", "망령의 탑", 1620 - Top, true);
            // 해상도 맞춤 — 4:3 은 통째로 키우고, 20:9 는 남는 세로를 탑 판 | 아래 판 사이에 나눈다(경계 = 두 판 사이)
            body.gameObject.AddComponent<Game.Module.Common.UI.RbFitBands>().Setup((1620 - Top) * S, new[] { (1013 - Top) * S });
            var ladder = PlaceIn(Node(body, "ChlLadder"), Top, 29, 430, 1047, 1004);
            Img(ladder, "Components/rb_panel_base");
            ArtCard(ladder, "ChlLadder", "Art/rb_art_bg_tower_column", AspectOf("Art/rb_art_bg_tower_column"));
            for (int i = 0; i < 5; i++)
            {
                float y0 = 452 + i * 110f, y1 = y0 + 100;
                var row = PlaceIn(Node(body, $"ChlFloor{i}"), Top, 55, y0, 1024, y1);
                Img(row, "Components/rb_list_row");
                Text(Local(Node(row, $"ChlFloor{i}No"), 55, y0, 1024, y1, 82, y0 + 20, 300, y0 + 80), null, "5F", 52, White, TA.Left, number: true);
                Img(Local(Node(row, $"ChlFloor{i}Lock"), 55, y0, 1024, y1, 862, y0 + 30, 902, y0 + 70), "Icons/rb_icon_sys_lock_48");
                Text(Local(Node(row, $"ChlFloor{i}State"), 55, y0, 1024, y1, 600, y0 + 24, 994, y0 + 76), null, "잠김", 40, Dim, TA.Right, heavy: true);
            }
            Img(PlaceIn(Node(body, "ChlTowerPanel"), Top, 29, 1022, 1047, 1610), "Components/rb_panel_base");
            Text(PlaceIn(Node(body, "ChlFloorTitle"), Top, 55, 1040, 730, 1098), null, "2층", 54, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlFloorDesc"), Top, 55, 1104, 740, 1144), "ui.tower.first_clear", "첫 클리어 보상 (매주 다시)", 34, Dim, TA.Left);
            var buy = Button2(body, "ChlTowerBuy", "ui.rb.chl.buy", "추가 입장 ›", 750, 1042, 1020, 1142, false, 38);
            buy.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Cyan;   // 시안: 청록 글자
            Reward(body, "ChlReward", 1164, 1314);
            Button2(body, "ChlTowerFit", "ui.rb.chl.sweep_rule", "소탕 조건 확인", 55, 1334, 527, 1450, false, 40);
            StartButton(body, "ChlTowerStart", "도전", 552, 1334, 1024, 1450);
            // 버튼 줄과 오늘의 소탕 사이 가는 줄(시안 #1D2331 · 두께 2 — 코덱스 납품 rb_divider_line)
            if (KitSprite("Components/rb_divider_line") != null) Img(PlaceIn(Node(body, "ChlSweepLine"), Top, 55, 1466, 1024, 1474), "Components/rb_divider_line").preserveAspect = false;
            Text(PlaceIn(Node(body, "ChlSweepTitle"), Top, 55, 1490, 760, 1540), "ui.tower.sweep", "오늘의 소탕", 44, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlSweepInfo"), Top, 55, 1546, 770, 1590), null, "하루 1번 · 입장 횟수를 쓰지 않는다", 32, Dim, TA.Left);
            Button2(body, "ChlTowerSweep", null, "소탕", 790, 1490, 1024, 1592, false, 40);
            buy.transform.SetAsLastSibling();
        }

        /// <summary>보상 두 칸 — 골드 · 조각(얼굴).</summary>
        private static void Reward(RectTransform body, string p, float y0, float y1)
        {
            // 시안 tower: 얇은 청록 테 칸 둘 — 아이콘 왼쪽 · 숫자와 이름 가운데 · 조각은 「?」
            var g = PlaceIn(Node(body, p + "GoldTile"), Top, 55, y0, 527, y1);
            Img(g, CellTeal).pixelsPerUnitMultiplier = CellPpu;
            Img(Local(Node(g, p + "GoldIcon"), 55, y0, 527, y1, 85, y0 + 32, 185, y0 + 132), "Icons/rb_icon_currency_gold_96");
            Text(Local(Node(g, p + "Gold"), 55, y0, 527, y1, 200, y0 + 22, 505, y0 + 86), null, "500", 54, White, TA.Center, number: true);
            Text(Local(Node(g, p + "GoldLabel"), 55, y0, 527, y1, 200, y0 + 92, 505, y0 + 136), "ui.rb.result.gold", "골드", 34, White, TA.Center, heavy: true);
            var s = PlaceIn(Node(body, p + "ShardTile"), Top, 552, y0, 1024, y1);
            Img(s, CellTeal).pixelsPerUnitMultiplier = CellPpu;
            var si = Local(Node(s, p + "ShardIcon"), 552, y0, 1024, y1, 577, y0 + 32, 677, y0 + 132);
            Img(si, "Icons/rb_icon_shard_badge_socket_96");
            Question(si, 577, y0 + 32, 677, y0 + 132);
            Text(Local(Node(s, p + "Shard"), 552, y0, 1024, y1, 710, y0 + 22, 1004, y0 + 86), null, "3", 54, White, TA.Center, number: true);
            Text(Local(Node(s, p + "ShardLabel"), 552, y0, 1024, y1, 710, y0 + 92, 1004, y0 + 136), "ui.rb.chl.host_shards", "호스트 조각", 34, White, TA.Center, heavy: true);
        }

        // ── 밤 ─────────────────────────────────────────────

        private static void BuildNight(RectTransform page)
        {
            // 시안 night(1080 기준) — 16:9 한 화면(아래 탭 위 ≈ 1880)에 다 들게, 시작 버튼은 판 바로 아래
            // (PD 2026-10-26 「시작 버튼 좀 올리고 · 스크롤 될 필요 없다 · 여백에 맞춰 버튼 크기」)
            var body = Group(page, "ChlNight", "ui.night.title", "망령의 밤", 1740 - Top, true);
            // 해상도 맞춤 — 4:3 은 통째로 키우고, 20:9 는 남는 세로를 요일 | 배너 | 정보 판 | 시작 버튼 사이에 나눈다
            body.gameObject.AddComponent<Game.Module.Common.UI.RbFitBands>().Setup((1740 - Top) * S, new[] { (559 - Top) * S, (957 - Top) * S, (1571 - Top) * S });
            // 요일 7칸 — 폭을 꽉 채우는 네모 버튼(141 × 120, 사이 5) · 시안처럼 일곱 칸 다 흰 글자
            for (int i = 0; i < 7; i++)
            {
                float x0 = 29 + i * 146f, x1 = x0 + 141f;
                var d = PlaceIn(Node(body, $"ChlDay{i}"), Top, x0, 430, x1, 550);
                Btn(d, "Components/rb_btn_secondary");
                Text(Stretch(Node(d, $"ChlDay{i}Text")), null, "월", 44, White, TA.Center, heavy: true);
            }
            // 배너 — 요일 테마 그림(rb_art_night_day_mon~sun, 화면 스크립트가 요일마다 바꿔 낀다) · 없으면 판 폭 전체 밤거리 그림
            // (rb_art_key_night 는 왼쪽 절반이 빈 그림이었다)
            const float ax0 = 29, ay0 = 568, ax1 = 1047, ay1 = 948;   // 요일 그림 비율(약 2.66)에 맞춘 높이 380
            var art = PlaceIn(Node(body, "ChlNightArt"), Top, ax0, ay0, ax1, ay1);
            Img(art, "Components/rb_panel_base");
            ArtCard(art, "ChlNightArt", "Art/rb_art_key_challenge_night", AspectOf("Art/rb_art_key_challenge_night"), shade: false);   // 글자가 없는 배너 — 아래 그늘이 요일 그림 가운데 장면을 가렸다
            // 정보 판 — 요일 · 최고 처치 · 남은 입장 · ◆ 오늘의 보상(두 칸) · 규칙 · 보상 줄
            const float px0 = 29, py0 = 966, px1 = 1047, py1 = 1560;
            Img(PlaceIn(Node(body, "ChlNightPanel"), Top, px0, py0, px1, py1), "Components/rb_panel_base");
            Text(PlaceIn(Node(body, "ChlNightDayTitle"), Top, 55, 984, 1020, 1044), null, "목 · 파워의 날", 56, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlNightBest"), Top, 55, 1056, 640, 1098), null, "목요일 최고 처치 128", 36, White, TA.Left);
            Text(PlaceIn(Node(body, "ChlNightLeft"), Top, 640, 1056, 1020, 1098), null, "남은 입장 2/3", 36, White, TA.Right);
            Text(PlaceIn(Node(body, "ChlNightRewardTitle"), Top, 55, 1116, 700, 1164), "ui.rb.chl.today_reward", "◆ 오늘의 보상", 44, White, TA.Left, heavy: true);
            // 시안 night 그대로 — ① 금화 · 골드(처치 수만큼, PD 2026-10-26) ② 「?」 조각 · 호스트 조각. 값은 그 요일 최고 처치 기준 예상
            NightCell(body, "ChlNightGold", 55, 527, 1180, 1400, CellGold, "Icons/rb_icon_currency_gold_96", false, "432", "ui.rb.result.gold", "골드");
            NightCell(body, "ChlNightShard", 552, 1020, 1180, 1400, CellTeal, "Icons/rb_icon_shard_badge_socket_96", true, "3", "ui.rb.chl.host_shards", "호스트 조각");
            var rule = Text(PlaceIn(Node(body, "ChlNightRule"), Top, 55, 1418, 1020, 1460), null, "몸 셋 중 하나에 빙의", 34, Dim, TA.Left);
            rule.enableWordWrapping = true;
            Text(PlaceIn(Node(body, "ChlNightReward"), Top, 55, 1476, 1020, 1526), null, "처치 50마다 조각 1 · 보스 처치 +3", 36, White, TA.Left, heavy: true);
            // 시작 — 판과 같은 여백(29 ~ 1047)의 노란 큰 버튼 「시작 ▶ | 🎫1」 · 판 바로 아래
            StartButton(body, "ChlNightStart", "시작 ▶", 29, 1582, 1047, 1722);
        }

        /// <summary>오늘의 보상 칸 — 테두리 칸 · 아이콘(위) · 큰 숫자 · 이름(아래). 시안 night 의 「432 골드」 · 「3 호스트 조각」 칸.</summary>
        private static void NightCell(RectTransform body, string p, float x0, float x1, float y0, float y1, string frame, string icon, bool question,
                                      string value, string key, string label)
        {
            var c = PlaceIn(Node(body, p + "Tile"), Top, x0, y0, x1, y1);
            Img(c, frame).pixelsPerUnitMultiplier = CellPpu;
            float cx = (x0 + x1) * 0.5f;
            var ic = Local(Node(c, p + "Icon"), x0, y0, x1, y1, cx - 46, y0 + 12, cx + 46, y0 + 104);
            Img(ic, icon);
            if (question) Question(ic, cx - 46, y0 + 12, cx + 46, y0 + 104);
            Text(Local(Node(c, p + "Value"), x0, y0, x1, y1, x0 + 10, y0 + 106, x1 - 10, y0 + 160), null, value, 56, White, TA.Center, number: true);
            Text(Local(Node(c, p + "Label"), x0, y0, x1, y1, x0 + 10, y0 + 162, x1 - 10, y0 + 208), key, label, 38, White, TA.Center, heavy: true);
        }

        // 시안 칸 판 — 짙은 남색 · 얇은 금갈색 / 청록 테(코덱스 납품 rb_cell_line_*). 아직 없으면 남색 판
        // 칸 판 테 40px 를 화면 모서리 둥글기 약 12px(시안)로 — 9분할 테를 2.5 배 줄여 그린다
        private const float CellPpu = 2.5f;
        private static string CellGold => KitSprite("Components/rb_cell_line_gold") != null ? "Components/rb_cell_line_gold" : "Components/rb_panel_base";
        private static string CellTeal => KitSprite("Components/rb_cell_line_teal") != null ? "Components/rb_cell_line_teal" : "Components/rb_panel_base";

        /// <summary>「?」 조각 — 조각 아이콘 오른쪽 아래 홈(rb_icon_shard_badge_socket)에 「?」(시안 night · tower: 어느 몸 조각인지 모른다).</summary>
        private static void Question(RectTransform icon, float x0, float y0, float x1, float y1)
        {
            float w = x1 - x0, h = y1 - y0;
            Text(Local(Node(icon, icon.name + "Q"), x0, y0, x1, y1, x0 + w * 0.70f, y0 + h * 0.66f, x0 + w * 0.97f, y0 + h * 0.97f),
                 null, "?", h * 0.30f, White, TA.Center, heavy: true);
        }

        /// <summary>
        /// 도전 · 시작 노란 버튼 「글자 | 🎫1」 — 글자 · 가는 칸막이 · 입장표 아이콘(rb_icon_summon_ticket) · 「1」(입장 1번).
        /// 넷을 한 묶음으로 버튼 가운데에 — 글자 폭이 언어마다 달라 화면 스크립트(RbChallengePage.StartLabel)가 재서 놓는다.
        /// </summary>
        private static void StartButton(RectTransform body, string name, string korean, float x0, float y0, float x1, float y1)
        {
            var b = PlaceIn(Node(body, name), Top, x0, y0, x1, y1);
            Btn(b, "Components/rb_btn_primary");
            float cx = (x0 + x1) * 0.5f, h = y1 - y0;
            Text(Local(Node(b, name + "Text"), x0, y0, x1, y1, cx - 200, y0 + 10, cx + 40, y1 - 18), null, korean, 58, Ink, TA.Center, heavy: true);
            Img(Local(Node(b, name + "Bar"), x0, y0, x1, y1, cx + 50, y0 + h * 0.26f, cx + 53, y1 - h * 0.30f), "Components/rb_bar_fill_cyan").color = new Color(0.55f, 0.36f, 0.05f, 0.7f);
            Img(Local(Node(b, name + "Ticket"), x0, y0, x1, y1, cx + 72, y0 + h * 0.5f - 32, cx + 152, y0 + h * 0.5f + 32), "Icons/rb_icon_summon_ticket_96");
            Text(Local(Node(b, name + "Cost"), x0, y0, x1, y1, cx + 156, y0 + 10, cx + 230, y1 - 18), null, "1", 64, Ink, TA.Center, number: true);
        }
    }
}
