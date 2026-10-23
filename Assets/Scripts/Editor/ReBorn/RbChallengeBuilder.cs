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
            so.FindProperty("_rowNow").objectReferenceValue = KitSprite("Components/rb_list_row_selected");
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
            var body = Group(page, "ChlTower", "ui.tower.title", "망령의 탑", 1980 - Top, false);
            float[] ys = { 478, 625, 772, 919, 1066 };
            for (int i = 0; i < 5; i++)
            {
                float y0 = ys[i], y1 = y0 + 112;
                var row = PlaceIn(Node(body, $"ChlFloor{i}"), Top, 33, y0, 1040, y1);
                Img(row, "Components/rb_list_row");
                Text(Local(Node(row, $"ChlFloor{i}No"), 33, y0, 1040, y1, 66, y0 + 26, 260, y0 + 86), null, "5F", 50, White, TA.Left, number: true);
                Img(Local(Node(row, $"ChlFloor{i}Lock"), 33, y0, 1040, y1, 784, y0 + 36, 824, y0 + 76), "Icons/rb_icon_sys_lock_48");
                Text(Local(Node(row, $"ChlFloor{i}State"), 33, y0, 1040, y1, 600, y0 + 30, 1010, y0 + 82), null, "잠김", 38, Dim, TA.Right, heavy: true);
            }
            Text(PlaceIn(Node(body, "ChlFloorTitle"), Top, 40, 1240, 730, 1310), null, "2F  두 갈래 길", 54, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlFloorDesc"), Top, 40, 1318, 1040, 1360), null, "첫 클리어 보상 (매주 다시)", 32, Dim, TA.Left);
            var buy = Button2(body, "ChlTowerBuy", "ui.rb.chl.buy", "추가 입장 ›", 744, 1244, 1010, 1316, false, 34);
            Reward(body, "ChlReward", 1390, 1560);
            Button2(body, "ChlTowerFit", "ui.rb.chl.fit", "맞는 호스트 보기", 50, 1590, 521, 1722, false);
            Button2(body, "ChlTowerStart", null, "2층 도전", 545, 1590, 1017, 1722, true);
            Text(PlaceIn(Node(body, "ChlSweepTitle"), Top, 50, 1770, 700, 1822), "ui.tower.sweep", "오늘의 소탕", 42, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlSweepInfo"), Top, 50, 1830, 760, 1880), null, "하루 1번 · 입장 횟수를 쓰지 않는다", 32, Dim, TA.Left);
            Button2(body, "ChlTowerSweep", null, "소탕", 785, 1770, 1017, 1880, false);
            Text(PlaceIn(Node(body, "ChlTowerEntryLine"), Top, 50, 1910, 1017, 1960), null, "남은 입장 2/3 · 초기화까지 03:24:00", 30, Dim, TA.Left);
            buy.transform.SetAsLastSibling();
        }

        /// <summary>보상 두 칸 — 골드 · 조각(얼굴).</summary>
        private static void Reward(RectTransform body, string p, float y0, float y1)
        {
            var g = PlaceIn(Node(body, p + "GoldTile"), Top, 50, y0, 521, y1);
            Img(g, "Components/rb_panel_base");
            Img(Local(Node(g, p + "GoldIcon"), 50, y0, 521, y1, 80, y0 + 34, 180, y0 + 134), "Icons/rb_icon_currency_gold_96");
            Text(Local(Node(g, p + "Gold"), 50, y0, 521, y1, 200, y0 + 30, 500, y0 + 90), null, "500", 52, White, TA.Left, number: true);
            Text(Local(Node(g, p + "GoldLabel"), 50, y0, 521, y1, 200, y0 + 96, 500, y0 + 136), "ui.rb.result.gold", "골드", 32, Dim, TA.Left);
            var s = PlaceIn(Node(body, p + "ShardTile"), Top, 545, y0, 1017, y1);
            Img(s, "Components/rb_panel_base");
            Img(Local(Node(s, p + "ShardIcon"), 545, y0, 1017, y1, 575, y0 + 40, 655, y0 + 120), "Icons/rb_icon_shard_96");
            Img(Local(Node(s, p + "ShardFace"), 545, y0, 1017, y1, 640, y0 + 24, 700, y0 + 84), "Icons/rb_icon_mode_ghost_96");
            Text(Local(Node(s, p + "Shard"), 545, y0, 1017, y1, 720, y0 + 30, 1000, y0 + 90), null, "3", 52, White, TA.Left, number: true);
            Text(Local(Node(s, p + "ShardLabel"), 545, y0, 1017, y1, 720, y0 + 96, 1000, y0 + 136), "ui.rb.chl.host_shards", "호스트 조각", 32, Dim, TA.Left);
        }

        // ── 밤 ─────────────────────────────────────────────

        private static void BuildNight(RectTransform page)
        {
            var body = Group(page, "ChlNight", "ui.night.title", "망령의 밤", 1860 - Top, false);   // 16:9 보기 칸(966)에 시작 버튼까지 들게
            for (int i = 0; i < 7; i++)
            {
                float x0 = 26 + i * 145f, x1 = x0 + 135f;
                var d = PlaceIn(Node(body, $"ChlDay{i}"), Top, x0, 430, x1, 545);
                Btn(d, "Components/rb_seg_track");
                Img(Stretch(Node(d, $"ChlDay{i}On")), "Components/rb_seg_on");
                Text(Stretch(Node(d, $"ChlDay{i}Text")), null, "월", 40, White, TA.Center, heavy: true);
            }
            var art = PlaceIn(Node(body, "ChlNightArt"), Top, 26, 570, 1054, 820);
            Img(art, "Components/rb_panel_base");
            ArtCard(art, "ChlNightArt", "Art/rb_art_key_night", AspectOf("Art/rb_art_key_night"));
            Text(PlaceIn(Node(body, "ChlNightDayTitle"), Top, 40, 845, 1040, 915), null, "목 · 파워의 날", 58, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlNightBest"), Top, 40, 935, 700, 975), null, "목요일 최고 처치 128", 36, Dim, TA.Left);
            Text(PlaceIn(Node(body, "ChlNightLeft"), Top, 700, 935, 1040, 975), null, "남은 입장 2/3", 36, Dim, TA.Right);
            Text(PlaceIn(Node(body, "ChlNightRewardTitle"), Top, 40, 1000, 700, 1045), "ui.rb.chl.today_reward", "◆ 오늘의 보상", 42, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "ChlNightReward"), Top, 40, 1050, 1040, 1130), null, "처치 50마다 조각 1\n보스 처치 +3", 32, Yellow, TA.TopLeft).enableWordWrapping = true;   // 표 글이 두 줄이다
            var r = Reward2(body);
            var rule = Text(PlaceIn(Node(body, "ChlNightRule"), Top, 40, 1325, 1040, 1440), null, "몸 셋 중 하나에 빙의", 34, Dim, TA.TopLeft);
            rule.enableWordWrapping = true;
            Button2(body, "ChlNightStart", null, "시작 ▶", 30, 1700, 1050, 1840, true);
        }

        private static RectTransform Reward2(RectTransform body)
        {
            // 밤 보상은 「처치 50마다 1」 처럼 규칙이라 칸에 얼굴만(그 요일 몸) — 골드 칸은 두지 않는다(규칙에 골드가 없다)
            const float ty0 = 1145, ty1 = 1305;
            var s = PlaceIn(Node(body, "ChlNightShardTile"), Top, 40, ty0, 1040, ty1);
            Img(s, "Components/rb_panel_base");
            Img(Local(Node(s, "ChlNightShardIcon"), 40, ty0, 1040, ty1, 80, ty0 + 30, 180, ty0 + 130), "Icons/rb_icon_shard_96");
            Img(Local(Node(s, "ChlNightShardFace"), 40, ty0, 1040, ty1, 160, ty0 + 14, 230, ty0 + 84), "Icons/rb_icon_mode_ghost_96");
            Text(Local(Node(s, "ChlNightShardLabel"), 40, ty0, 1040, ty1, 260, ty0 + 40, 1010, ty0 + 120), "ui.rb.chl.night_shards", "그날 몸의 조각 · 판이 끝나면 처치 수만큼", 34, White, TA.Left);
            return s;
        }
    }
}
