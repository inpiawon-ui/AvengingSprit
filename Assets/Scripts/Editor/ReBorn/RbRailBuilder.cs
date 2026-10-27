// RE:BORN UI 리뉴얼(2026-10-23) — 로비 양옆 아이콘(레일) · 프로필이 여는 화면: 유령 성장 · 미션 · 패스(쪽) + 우편 · 오늘 무료 · 설정 · 언어(맨 위 덧판).
// 시안 Screens/ghost-growth · missions · pass · mail-empty · daily-free · settings · language. 좌표는 시안 1080 픽셀 상자(쪽) · 게임 px(창).
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbRailBuilder
    {
        private const float Top = 420f;

        /// <summary>쪽 셋 — 쪽들 사이(위 막대 · 아래 탭보다 아래).</summary>
        public static void BuildPages(RectTransform rt)
        {
            BuildGhost(rt);
            BuildMissions(rt);
            BuildPass(rt);
            BuildMail(rt);   // 우편함은 쪽이다 — 위 막대 · 아래 탭이 보여야 한다(덮개 층에 두면 막대를 가렸다, 하네스 C6)
        }

        /// <summary>덧판 — 위 막대 · 아래 탭보다 위(모든 것 위에 뜨는 창).</summary>
        public static void BuildOverlay(RectTransform rt)
        {
            var root = Stretch(Node(rt, "RbMisc"));
            BuildDaily(root);
            BuildSettings(root);
            BuildLanguage(root);
            BuildProfile(root);
            foreach (var n in new[] { "RbMail", "RbDaily", "RbSettings", "RbLanguage", "RbProfile" }) root.Find(n)?.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<RbMiscPages>();
            var so = new SerializedObject(view);
            so.FindProperty("_toggleOn").objectReferenceValue = KitSprite("Components/rb_toggle_on");
            so.FindProperty("_toggleOff").objectReferenceValue = KitSprite("Components/rb_toggle_off");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Icons(SerializedObject so)
        {
            var guids = AssetDatabase.FindAssets("_96 t:Sprite", new[] { $"{Kit}/Icons" });
            var p = so.FindProperty("_icons");
            p.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        private static Button Button2(RectTransform body, string name, string key, string korean, float x0, float y0, float x1, float y1, bool primary, float size = 0f)
        {
            var b = PlaceIn(Node(body, name), Top, x0, y0, x1, y1);
            var btn = Btn(b, primary ? "Components/rb_btn_primary" : "Components/rb_btn_secondary");
            Text(Stretch(Node(b, name + "Text")), key, korean, size > 0f ? size : primary ? 46 : 38, primary ? Ink : White, TA.Center, heavy: true);
            return btn;
        }

        // ── 유령 성장 ─────────────────────────────────────────

        private static void BuildGhost(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageGhost");
            RbHostBuilder.Head(page, "Gho", "ui.rb.ghost.title", "유령", 25, 280, 155, 400, 800, 922);
            var body = VScroll(page, "GhoBody", Top, 2040, 2000 - Top);

            const float cx0 = 30, cy0 = 443, cx1 = 1050, cy1 = 790;
            var card = PlaceIn(Node(body, "GhoCard"), Top, cx0, cy0, cx1, cy1);
            Img(card, "Components/rb_panel_hero");
            Img(Local(Node(card, "GhoMascot"), cx0, cy0, cx1, cy1, 80, 470, 380, 770), "Art/rb_art_mascot_ghost_happy");
            Text(Local(Node(card, "GhoApplies"), cx0, cy0, cx1, cy1, 480, 480, 1020, 520), "ui.rb.ghost.applies", "모든 호스트에 적용", 34, Cyan, TA.Left, heavy: true);
            Text(Local(Node(card, "GhoLevelLabel"), cx0, cy0, cx1, cy1, 480, 528, 1020, 570), "ui.rb.ghost.level", "유령 레벨", 34, Dim, TA.Left);
            Text(Local(Node(card, "GhoLevel"), cx0, cy0, cx1, cy1, 480, 580, 1020, 720), null, "Lv 12", 120, White, TA.Left, number: true);

            float[][] px = { new[] { 31f, 358f }, new[] { 377f, 705f }, new[] { 724f, 1051f } };
            for (int i = 0; i < 3; i++)
            {
                float x0 = px[i][0], x1 = px[i][1], y0 = 828, y1 = 1090;
                var t = PlaceIn(Node(body, $"GhoPath{i}"), Top, x0, y0, x1, y1);
                Img(t, "Components/rb_panel_base");
                Img(Local(Node(t, $"GhoPath{i}Icon"), x0, y0, x1, y1, x0 + 18, y0 + 24, x0 + 108, y0 + 114), "Icons/rb_icon_currency_gold_96");
                Text(Local(Node(t, $"GhoPath{i}Lv"), x0, y0, x1, y1, x0 + 118, y0 + 22, x1 - 14, y0 + 66), null, "Lv 10", 40, White, TA.Left, number: true);
                Text(Local(Node(t, $"GhoPath{i}Reward"), x0, y0, x1, y1, x0 + 118, y0 + 70, x1 - 14, y0 + 112), null, "골드 500", 32, Yellow, TA.Left);
                var b = Local(Node(t, $"GhoPath{i}Claim"), x0, y0, x1, y1, x0 + 18, y0 + 140, x1 - 18, y0 + 238);
                Btn(b, "Components/rb_btn_primary");
                Text(Stretch(Node(b, $"GhoPath{i}ClaimText")), "ui.pass.claim", "받기", 42, Ink, TA.Center, heavy: true);
                Text(Local(Node(t, $"GhoPath{i}State"), x0, y0, x1, y1, x0 + 18, y0 + 150, x1 - 18, y0 + 228), null, "Lv 15", 36, Dim, TA.Center, heavy: true);
            }

            Text(PlaceIn(Node(body, "GhoStatHeader"), Top, 30, 1116, 520, 1168), "ui.rb.growth.stat_header", "◆ 능력치", 50, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "GhoStatHint"), Top, 560, 1122, 1050, 1164), "ui.rb.growth.hold_hint", "길게 누르면 연속 강화", 34, Cyan, TA.Right);
            const float sx0 = 25, sy0 = 1180, sx1 = 530, sy1 = 1312;
            var tile = PlaceIn(Node(body, "GhoStatTemplate"), Top, sx0, sy0, sx1, sy1);
            Img(tile, "Components/rb_stat_tile");
            Img(Local(Node(tile, "GhoStatIcon"), sx0, sy0, sx1, sy1, 42, sy0 + 26, 122, sy0 + 106), "Icons/rb_icon_stat_attack_96");
            Text(Local(Node(tile, "GhoStatName"), sx0, sy0, sx1, sy1, 132, sy0 + 12, 386, sy0 + 60), null, "공격력", 34, Dim, TA.Left).fontSizeMin = 22f * S;
            Text(Local(Node(tile, "GhoStatValue"), sx0, sy0, sx1, sy1, 132, sy0 + 66, 386, sy0 + 120), null, "20% › 24%", 40, White, TA.Left, number: true);
            var buy = Local(Node(tile, "GhoStatBuy"), sx0, sy0, sx1, sy1, 392, sy0 + 30, 522, sy0 + 102);
            Btn(buy, "Components/rb_btn_price");
            buy.gameObject.AddComponent<Game.Module.Common.UI.RbHoldRepeat>();
            Img(Local(Node(buy, "GhoStatBuyIcon"), 392, sy0 + 30, 522, sy0 + 102, 400, sy0 + 46, 440, sy0 + 86), "Icons/rb_icon_currency_gold_48");
            Text(Local(Node(buy, "GhoStatBuyText"), 392, sy0 + 30, 522, sy0 + 102, 442, sy0 + 34, 516, sy0 + 98), null, "90", 38, White, TA.Left, number: true);
            Text(Local(Node(tile, "GhoStatMax"), sx0, sy0, sx1, sy1, 392, sy0 + 30, 522, sy0 + 102), null, "MAX", 32, Yellow, TA.Center, heavy: true);

            const float ex0 = 30, ey0 = 1770, ex1 = 1050, ey1 = 1980;
            var exp = PlaceIn(Node(body, "GhoExp"), Top, ex0, ey0, ex1, ey1);
            Img(exp, "Components/rb_panel_base");
            Text(Local(Node(exp, "GhoExpTitle"), ex0, ey0, ex1, ey1, 60, 1792, 600, 1848), "ui.rb.ghost.exp", "유령 경험치", 44, White, TA.Left, heavy: true);
            Text(Local(Node(exp, "GhoExpValue"), ex0, ey0, ex1, ey1, 600, 1792, 1020, 1848), null, "30 /100", 44, White, TA.Right, number: true);
            Bar(Local(Node(exp, "GhoExpBar"), ex0, ey0, ex1, ey1, 60, 1866, 1020, 1884), "GhoExpFill", "Components/rb_bar_fill_cyan");
            Text(Local(Node(exp, "GhoExpNote"), ex0, ey0, ex1, ey1, 60, 1902, 1020, 1950), null, "챕터 클리어로 획득 · 다음 보상 Lv 15", 32, Dim, TA.Left);

            var view = page.gameObject.AddComponent<RbGhostPage>();
            var so = new SerializedObject(view);
            string[] stat = { "hp", "attack", "crit", "attackspeed", "range", "movespeed", "critdamage", "defense" };
            var icons = so.FindProperty("_statIcons");
            icons.arraySize = stat.Length;
            for (int i = 0; i < stat.Length; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Icons/rb_icon_stat_{stat[i]}_96");
            so.FindProperty("_claimOn").objectReferenceValue = KitSprite("Components/rb_panel_hero");
            so.FindProperty("_claimOff").objectReferenceValue = KitSprite("Components/rb_panel_base");
            Icons(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }

        // ── 미션 ───────────────────────────────────────────

        private static void BuildMissions(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageMissions");
            RbHostBuilder.Head(page, "Mis", "ui.rb.rail.mission", "미션", 25, 280, 155, 400, 800, 922);
            // 시안 missions(1080 기준, 탭 위 변 = 440) — PD 2026-10-26 「일일 · 주간 · 간격 · 완료 받기 등 시안과 똑같이」
            // 탭은 둘(일일 · 주간) — 한 띠(rb_seg_track) 위에 고른 탭 판(rb_seg_on) · 청록 글자. 업적 탭은 시안에 없어 뺐다(준비 중이었다)
            string[] keys = { "ui.rb.mis.daily", "ui.rb.mis.weekly" };
            string[] ko = { "일일", "주간" };
            Img(Place(Node(page, "MisTabTrack"), Edge.Top, 29, 440, 1047, 557), "Components/rb_seg_track");
            for (int i = 0; i < 2; i++)
            {
                float x0 = 29 + i * 509f, x1 = x0 + 509f;
                var t = Place(Node(page, $"MisTab{i}"), Edge.Top, x0, 440, x1, 557);
                var hit = Img(t, "Components/rb_seg_on");
                hit.color = new Color(1f, 1f, 1f, 0f);   // 누르는 칸 — 그림은 아래 「On」 이 맡는다
                hit.raycastTarget = true;
                t.gameObject.AddComponent<Button>().targetGraphic = hit;
                Img(Stretch(Node(t, $"MisTab{i}On")), "Components/rb_seg_on");
                Text(Stretch(Node(t, $"MisTab{i}Text")), keys[i], ko[i], 42, White, TA.Center, heavy: true);
            }
            // 「전부 완료」 판 — 어두운 판 · 제목 · n/m개 달성 · 막대 · 상자 아이콘 · 「골드 상자 1 / 전체 완료 시」
            const float ax0 = 29, ay0 = 572, ax1 = 1047, ay1 = 806;
            var all = Place(Node(page, "MisAll"), Edge.Top, ax0, ay0, ax1, ay1);
            Img(all, "Components/rb_panel_base");
            Text(Local(Node(all, "MisAllTitle"), ax0, ay0, ax1, ay1, 55, 595, 680, 643), null, "◆ 오늘 미션 전부 완료", 44, White, TA.Left, heavy: true);
            Text(Local(Node(all, "MisAllCount"), ax0, ay0, ax1, ay1, 55, 685, 600, 735), null, "3/6개 달성", 44, White, TA.Left, heavy: true);
            Bar(Local(Node(all, "MisAllBar"), ax0, ay0, ax1, ay1, 55, 757, 614, 771), "MisAllFill", "Components/rb_bar_fill_cyan");
            Img(Local(Node(all, "MisAllIcon"), ax0, ay0, ax1, ay1, 691, 688, 775, 772), "Icons/rb_icon_reward_chest_96");
            Text(Local(Node(all, "MisAllReward"), ax0, ay0, ax1, ay1, 800, 682, 1030, 726), null, "골드 상자 1", 36, White, TA.Left, heavy: true);
            Text(Local(Node(all, "MisAllHint"), ax0, ay0, ax1, ay1, 800, 728, 1030, 770), "ui.rb.mis.all_hint", "전체 완료 시", 32, Dim, TA.Left);
            var ac = Local(Node(all, "MisAllClaim"), ax0, ay0, ax1, ay1, 800, 678, 1020, 774);
            Btn(ac, "Components/rb_btn_primary");
            Text(Stretch(Node(ac, "MisAllClaimText")), "ui.pass.claim", "받기", 38, Ink, TA.Center, heavy: true);

            // 미션 줄(본보기 · 화면 스크립트가 8줄로 복제) — 어두운 판 174 · 사이 24(시안) · 세로 스크롤
            var body = VScroll(page, "MisBody", 830, 1860, 8 * 198f + 20f);
            const float rx0 = 29, ry0 = 846, rx1 = 1047, ry1 = 1020;
            var row = PlaceIn(Node(body, "MisRowTemplate"), 830, rx0, ry0, rx1, ry1);
            Img(row, "Components/rb_panel_base");
            Text(Local(Node(row, "MisRowName"), rx0, ry0, rx1, ry1, 48, ry0 + 26, 700, ry0 + 78), null, "챕터 클리어", 42, White, TA.Left, heavy: true);
            Bar(Local(Node(row, "MisRowBar"), rx0, ry0, rx1, ry1, 48, ry0 + 118, 517, ry0 + 132), "MisRowFill", "Components/rb_bar_fill_cyan");
            Text(Local(Node(row, "MisRowCount"), rx0, ry0, rx1, ry1, 528, ry0 + 100, 690, ry0 + 150), null, "1/1", 40, White, TA.Left, number: true);
            Img(Local(Node(row, "MisRowRewardIcon"), rx0, ry0, rx1, ry1, 700, ry0 + 70, 744, ry0 + 114), "Icons/rb_icon_currency_gem_96");
            Text(Local(Node(row, "MisRowReward"), rx0, ry0, rx1, ry1, 750, ry0 + 66, 845, ry0 + 118), null, "30", 42, White, TA.Left, number: true);
            var rb = Local(Node(row, "MisRowClaim"), rx0, ry0, rx1, ry1, 852, ry0 + 29, 1030, ry0 + 148);
            Btn(rb, "Components/rb_btn_secondary");
            Text(Stretch(Node(rb, "MisRowClaimText")), null, "받기", 40, Yellow, TA.Center, heavy: true);

            var soon = Place(Node(page, "MisSoon"), Edge.Top, 100, 700, 980, 900);
            Text(soon, "ui.rb.soon", "준비 중이에요 · 업데이트로 열려요", 40, Dim, TA.Center);
            var claimAll = Place(Node(page, "MisClaimAll"), Edge.Bottom, 35, 1880, 1045, 2020);
            Btn(claimAll, "Components/rb_btn_primary");
            Text(Stretch(Node(claimAll, "MisClaimAllText")), "ui.pass.claim_all", "모두 받기", 50, Ink, TA.Center, heavy: true);

            var view = page.gameObject.AddComponent<RbMissionsPage>();
            var so = new SerializedObject(view);
            Icons(so);
            so.FindProperty("_btnIdle").objectReferenceValue = KitSprite("Components/rb_btn_secondary");
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }

        // ── 패스 ───────────────────────────────────────────

        private static void BuildPass(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PagePass");
            RbHostBuilder.Head(page, "Pas", "ui.rb.pass.title", "시즌 패스", 25, 280, 155, 400, 800, 922);
            // ── 시안 Screens/pass.png 그대로(PD 2026-10-28 「시안이랑 아예 다르다 · 똑같이」) — 좌표는 시안 1080 px
            // 시즌 판 — 챕터 칸과 같은 짙은 판 + 위쪽 하늘색 한 줄 · 오른쪽에 보상 호스트 얼굴
            const float bx0 = 30, by0 = 440, bx1 = 1050, by1 = 795;
            var banner = Place(Node(page, "PasBanner"), Edge.Top, bx0, by0, bx1, by1);
            var bannerImg = Img(banner, "Components/rb_lobby_chapter_panel");
            bannerImg.preserveAspect = false;
            bannerImg.pixelsPerUnitMultiplier = 1.5f;
            Img(Local(Node(banner, "PasBannerLine"), bx0, by0, bx1, by1, 362, 437, 714, 445), "Components/rb_bar_fill_cyan").preserveAspect = false;
            Text(Local(Node(banner, "PasSeason"), bx0, by0, bx1, by1, 62, 470, 600, 514), null, "SEASON 1", 40, Cyan, TA.Left, number: true);
            Text(Local(Node(banner, "PasName"), bx0, by0, bx1, by1, 62, 520, 860, 594), "ui.rb.pass.name", "복수자", 62, White, TA.Left, heavy: true);
            Text(Local(Node(banner, "PasPerk"), bx0, by0, bx1, by1, 62, 606, 860, 654), "ui.rb.pass.reward_host", "SSR 흡혈귀", 40, White, TA.Left, heavy: true);
            Text(Local(Node(banner, "PasPerkLv"), bx0, by0, bx1, by1, 62, 662, 860, 712), "ui.rb.pass.reward_lv", "프리미엄 Lv 30 보상", 36, White, TA.Left);
            Text(Local(Node(banner, "PasLeft"), bx0, by0, bx1, by1, 62, 718, 600, 766), null, "남은 기간 17일", 36, Dim, TA.Left);
            Img(Local(Node(banner, "PasFace"), bx0, by0, bx1, by1, 916, 514, 1000, 598), "Icons/rb_icon_sys_info_48").preserveAspect = true;   // 화면 스크립트가 보상 호스트 얼굴로 바꾼다

            // 레벨 판
            const float lx0 = 30, ly0 = 822, lx1 = 1050, ly1 = 992;
            var lvp = Place(Node(page, "PasLvPanel"), Edge.Top, lx0, ly0, lx1, ly1);
            Img(lvp, "Components/rb_panel_base");
            Text(Local(Node(lvp, "PasLevel"), lx0, ly0, lx1, ly1, 56, 860, 780, 914), null, "Lv 3 · 40/100", 48, White, TA.Left, number: true);
            Bar(Local(Node(lvp, "PasXpBar"), lx0, ly0, lx1, ly1, 56, 944, 782, 960), "PasXpFill", "Components/rb_bar_fill_cyan");
            var mis = Local(Node(lvp, "PasMissions"), lx0, ly0, lx1, ly1, 808, 838, 1022, 990);
            Btn(mis, "Components/rb_btn_secondary");
            Text(Stretch(Node(mis, "PasMissionsText")), "ui.rb.pass.missions", "미션 ›", 40, White, TA.Center, heavy: true);
            Img(Local(Node(mis, "PasMissionsDot"), 808, 838, 1022, 990, 992, 830, 1028, 866), "Components/rb_dot_red");

            // 머리 글자 — 무료 · 레벨 · 프리미엄
            Text(Place(Node(page, "PasHeadFree"), Edge.Top, 30, 1022, 468, 1070), "ui.rb.pass.head_free", "무료", 40, Dim, TA.Center, heavy: true);
            Text(Place(Node(page, "PasHeadLv"), Edge.Top, 468, 1022, 612, 1070), "ui.rb.pass.head_lv", "레벨", 40, Dim, TA.Center, heavy: true);
            Text(Place(Node(page, "PasHeadPrem"), Edge.Top, 612, 1022, 1050, 1070), "ui.rb.pass.head_prem", "프리미엄", 40, Dim, TA.Center, heavy: true);

            // 단계 줄 — 칸 높이 240 · 줄 간격 256(본보기 + 화면 스크립트 간격 10 게임 px). 아래 탭 없이 버튼 위까지
            var body = VScroll(page, "PasBody", 1080, 2000, 30 * 256f);
            const float ry0 = 1090, ry1 = 1331;
            var row = PlaceIn(Node(body, "PasRowTemplate"), 1080, 30, ry0, 1052, ry1);
            // 가운데 세로 줄 — 다음 줄까지 이어지게 틈만큼 길게
            var line = Img(Local(Node(row, "PasRowLine"), 30, ry0, 1052, ry1, 538, ry0, 542, ry1 + 16), "Components/rb_bar_fill_cyan");
            line.preserveAspect = false;
            line.color = new Color(1f, 1f, 1f, 0.45f);
            Cell(row, "PasRowFree", 30, ry0, 1052, ry1, 30, 468, false);
            Cell(row, "PasRowPrem", 30, ry0, 1052, ry1, 612, 1052, true);
            Text(Local(Node(row, "PasRowLv"), 30, ry0, 1052, ry1, 480, ry0 + 115, 600, ry0 + 185), null, "3", 56, Cyan, TA.Center, number: true);   // 「현재」 줄은 화면 스크립트가 노랗게 · 위로
            Img(Local(Node(row, "PasRowDot"), 30, ry0, 1052, ry1, 492, ry0 + 160, 522, ry0 + 190), "Components/rb_toggle_knob").color = Yellow;   // 흰 원에 노랑(빨간 점에 곱하면 빨갛게 남는다)
            Text(Local(Node(row, "PasRowNow"), 30, ry0, 1052, ry1, 468, ry0 + 196, 612, ry0 + 246), "ui.rb.pass.now", "현재", 38, Yellow, TA.Center, heavy: true);

            // 아래 — 모두 받기 · 프리미엄 열기 | 값 (아래 탭은 시즌 패스를 여는 동안 숨긴다)
            var ca = Place(Node(page, "PasClaimAll"), Edge.Bottom, 30, 2022, 306, 2192);
            Btn(ca, "Components/rb_btn_secondary");
            Text(Stretch(Node(ca, "PasClaimAllText")), "ui.pass.claim_all", "모두 받기", 44, White, TA.Center, heavy: true);
            const float px0 = 332, py0 = 2022, px1 = 1050, py1 = 2192;
            var pr = Place(Node(page, "PasPremium"), Edge.Bottom, px0, py0, px1, py1);
            Btn(pr, "Components/rb_btn_primary");
            Text(Local(Node(pr, "PasPremiumText"), px0, py0, px1, py1, 350, 2050, 712, 2160), "ui.pass.premium", "프리미엄 열기", 50, Ink, TA.Center, heavy: true);
            var div = Img(Local(Node(pr, "PasPremiumDiv"), px0, py0, px1, py1, 713, 2058, 717, 2152), "Components/rb_divider_line");
            div.preserveAspect = false;
            div.color = new Color(0.45f, 0.3f, 0.05f, 0.6f);
            Text(Local(Node(pr, "PasPremiumPrice"), px0, py0, px1, py1, 720, 2050, 1040, 2160), null, "₩12,000", 64, Ink, TA.Center, number: true);

            var view = page.gameObject.AddComponent<RbPassPage>();
            var so = new SerializedObject(view);
            Icons(so);
            // 칸 판 — 시안 크롭 앵커로 발주(rb_pass_cell · rb_pass_cell_claim). 들어오기 전엔 비슷한 키트 판
            so.FindProperty("_cellClaim").objectReferenceValue = KitSprite(KitHas("Components/rb_pass_cell_claim") ? "Components/rb_pass_cell_claim" : "Components/rb_btn_secondary");
            so.FindProperty("_cellIdle").objectReferenceValue = KitSprite(KitHas("Components/rb_pass_cell") ? "Components/rb_pass_cell" : "Components/rb_panel_base");
            so.FindProperty("_cellDone").objectReferenceValue = KitSprite(KitHas("Components/rb_pass_cell") ? "Components/rb_pass_cell" : "Components/rb_panel_base");
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }

        // 보상 칸 — 시안: 아이콘(왼쪽) · 수량(위) · 상태 또는 보상 이름(아래) · 프리미엄은 오른쪽 위 자물쇠
        //   시안 좌표 — 무료 칸 아이콘 가운데 x 178 · 글자 305 / 프리미엄 790 · 888
        private static void Cell(RectTransform row, string p, float rx0, float ry0, float rx1, float ry1, float x0, float x1, bool premium)
        {
            var c = Local(Node(row, p), rx0, ry0, rx1, ry1, x0, ry0, x1, ry1);
            Btn(c, "Components/rb_panel_base");
            float ic = premium ? 790f : 178f, tx = premium ? 888f : 305f;
            Img(Local(Node(c, p + "Icon"), x0, ry0, x1, ry1, ic - 44, ry0 + 114, ic + 44, ry0 + 202), "Icons/rb_icon_currency_gem_96");
            Text(Local(Node(c, p + "Amount"), x0, ry0, x1, ry1, tx - 80, ry0 + 100, tx + 80, ry0 + 158), null, "30", 56, White, TA.Center, number: true);
            Text(Local(Node(c, p + "State"), x0, ry0, x1, ry1, tx - 90, ry0 + 164, tx + 90, ry0 + 214), null, "받기", 40, Yellow, TA.Center, heavy: true);
            Img(Local(Node(c, p + "Lock"), x0, ry0, x1, ry1, x1 - 62, ry0 + 58, x1 - 20, ry0 + 100), "Icons/rb_icon_sys_lock_48");
        }

        // ── 우편 · 오늘 무료 · 설정 · 언어 ─────────────────────────

        private static void BuildMail(RectTransform root)
        {
            var page = RbHostBuilder.Page(root, "RbMail");
            RbHostBuilder.Head(page, "Mail", "ui.rb.mail.title", "우편함", 25, 280, 155, 400, 800, 922);
            page.Find("MailHelp")?.gameObject.SetActive(false);
            Img(Place(Node(page, "MailEmptyArt"), Edge.Center, 290, 860, 790, 1360), "Art/rb_art_mail_empty");
            Text(Place(Node(page, "MailEmptyText"), Edge.Center, 140, 1400, 940, 1470), "ui.rb.mail.empty", "받은 우편이 없어요", 52, White, TA.Center, heavy: true);
            page.gameObject.SetActive(false);
        }

        /// <summary>가운데 창 — 딤 + 720×1280 판 + 모달 판 + 제목판 + 닫기.</summary>
        internal static RectTransform Modal(RectTransform root, string name, string titleKey, string korean, float x0, float y0, float x1, float y1, out RectTransform c)
        {
            var page = Stretch(Node(root, name));
            var dim = page.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);   // 0.8 은 선형 색 공간에서 절반쯤만 어두워졌다(흰 글자 255 → 124)
            dim.raycastTarget = true;
            c = Node(page, name + "Content");
            c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
            c.sizeDelta = new Vector2(720f, 1280f);
            var box = Corner(Node(c, name + "Box"), x0, y0, x1, y1);
            Img(box, "Components/rb_modal");
            float cx = (x0 + x1) * 0.5f;
            var plate = Corner(Node(c, name + "TitlePlate"), cx - 120f, y0 - 24f, cx + 120f, y0 + 26f);
            Img(plate, "Components/rb_popup_title_plate");
            Text(Corner(Node(c, name + "Title"), cx - 95f, y0 - 20f, cx + 95f, y0 + 22f), titleKey, korean, 44, White, TA.Center, heavy: true);   // 판 양 끝 막대(각 15)를 비켜
            var x = Corner(Node(c, name.Replace("Rb", string.Empty) + "Close"), x1 - 54f, y0 - 22f, x1 + 2f, y0 + 34f);
            Btn(x, "Components/rb_btn_icon_round");
            Img(Inside(Node(x, x.name + "Icon"), 28f, 28f, 30f, 30f), "Icons/rb_icon_sys_close_48");
            // ⚠ 여기서 끄지 않는다 — 꺼진 채 붙인 TMP 는 실행 때 처음 켜지며 기본값(최대 72)으로 돌아가 글자가 칸만큼 커졌다(2026-10-23 설정 캡처).
            //   다 붙인 뒤 BuildOverlay 가 끈다
            return page;
        }

        /// <summary>판(720×1280) 안 위 왼쪽 기준 상자(게임 px).</summary>
        internal static RectTransform Corner(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, -(y0 + y1) * 0.5f);
            return rt;
        }

        internal static RectTransform Inside(RectTransform rt, float cx, float cy, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, -cy);
            return rt;
        }

        private static void Row(RectTransform c, string name, string key, string korean, float y, string buttonKey, string buttonKo, bool primary)
        {
            var row = Corner(Node(c, name + "Row"), 100f, y, 620f, y + 96f);
            Img(row, "Components/rb_list_row");
            Text(Corner(Node(c, name + "Name"), 120f, y + 10f, 400f, y + 50f), key, korean, 36, White, TA.Left, heavy: true);
            Text(Corner(Node(c, name + "Sub"), 120f, y + 52f, 400f, y + 86f), null, "오늘의 상점", 28, Dim, TA.Left);
            var b = Corner(Node(c, name + (primary ? "Claim" : "Go")), 410f, y + 16f, 606f, y + 80f);
            Btn(b, primary ? "Components/rb_btn_primary" : "Components/rb_btn_secondary");
            Text(Stretch(Node(b, b.name + "Text")), buttonKey, buttonKo, 32, primary ? Ink : White, TA.Center, heavy: true);
        }

        private static void BuildDaily(RectTransform root)
        {
            Modal(root, "RbDaily", "ui.rb.rail.daily", "오늘 무료", 80f, 300f, 640f, 900f, out var c);
            Img(Corner(Node(c, "DailyArt"), 200f, 350f, 520f, 560f), "Art/rb_art_daily_free_header");
            Row(c, "DailyGold", "ui.rb.daily.gold", "무료 골드", 590f, "ui.pass.claim", "받기", true);
            Row(c, "DailySummon", "ui.rb.summon.free", "오늘 무료 1회", 700f, "ui.rb.daily.go_summon", "소환하러 가기 ›", false);
            Text(Corner(Node(c, "DailyReset"), 100f, 820f, 620f, 860f), null, "02:14:32 후 새 상품", 30, Dim, TA.Center);
        }

        private static void Toggle(RectTransform c, string name, string key, string korean, float y)
        {
            Text(Corner(Node(c, name + "Label"), 100f, y, 420f, y + 60f), key, korean, 38, White, TA.Left, heavy: true);
            // 시안 settings.png 토글 — 트랙 88 × 48 · 손잡이 지름 38(게임 px = 시안의 2/3, 시안 크롭 앵커로 발주한 부품). 예전엔 140 × 52 칸에 늘려 붙여 찌그러졌다(PD 2026-10-28)
            var b = Corner(Node(c, name), 522f, y + 6f, 610f, y + 54f);   // 게임 px — 시안 140 × 76 비율(1.84) · 높이 48
            HitArea(b);
            Img(Stretch(Node(b, name + "Track")), "Components/rb_toggle_on").preserveAspect = false;
            var k = Node(b, name + "Knob");
            k.anchorMin = k.anchorMax = k.pivot = new Vector2(0.5f, 0.5f);
            k.sizeDelta = new Vector2(38f, 38f);   // 시안 — 트랙 높이의 79% · 끝에서 높이의 11%(좌우 ±20 은 화면 스크립트 RbMiscPages.Toggle)
            Img(k, "Components/rb_toggle_knob");
        }

        private static void BuildSettings(RectTransform root)
        {
            Modal(root, "RbSettings", "ui.rb.rail.settings", "설정", 60f, 330f, 660f, 930f, out var c);
            Toggle(c, "SetMusic", "ui.rb.set.music", "배경음", 380f);
            Toggle(c, "SetSfx", "ui.rb.set.sfx", "효과음", 460f);
            Toggle(c, "SetVibrate", "ui.rb.set.vibrate", "진동", 540f);
            var lang = Corner(Node(c, "SetLanguage"), 90f, 630f, 630f, 710f);
            Btn(lang, "Components/rb_list_row");
            Text(Corner(Node(lang, "SetLanguageLabel"), 30f, 0f, 260f, 80f), "ui.rb.set.language", "언어", 38, White, TA.Left, heavy: true);
            var lv = Text(Corner(Node(lang, "SetLanguageValue"), 260f, 0f, 520f, 80f), null, "한국어 ›", 36, White, TA.Right, heavy: true);
            lv.rectTransform.anchorMin = lv.rectTransform.anchorMax = new Vector2(0f, 1f);
            string[] names = { "SetAccount", "SetSupport", "SetTerms" };
            string[] keys = { "ui.rb.set.account", "ui.rb.set.support", "ui.rb.set.terms" };
            string[] ko = { "계정", "고객센터", "약관" };
            for (int i = 0; i < 3; i++)
            {
                var b = Corner(Node(c, names[i]), 90f + i * 184f, 760f, 262f + i * 184f, 850f);
                Btn(b, "Components/rb_btn_secondary");
                Text(Stretch(Node(b, names[i] + "Text")), keys[i], ko[i], 34, White, TA.Center, heavy: true);
            }
        }

        /// <summary>프로필 — 대표 얼굴 · 이름 · 유령 Lv · 칭호 / 얼굴 · 칭호 탭 / 칸 30(세로 스크롤) / 유령 성장 ›. 시안이 없어 키트 부품으로 세웠다.</summary>
        private static void BuildProfile(RectTransform root)
        {
            Modal(root, "RbProfile", "ui.rb.profile.title", "프로필", 50f, 220f, 670f, 1110f, out var c);
            Img(Corner(Node(c, "ProfRing"), 86f, 268f, 234f, 416f), "Components/rb_profile_frame");
            Img(Corner(Node(c, "ProfFace"), 100f, 282f, 220f, 402f), "Icons/rb_icon_mode_ghost_144");
            Text(Corner(Node(c, "ProfName"), 252f, 282f, 640f, 330f), null, "유령", 44, White, TA.Left, heavy: true);
            Text(Corner(Node(c, "ProfLevel"), 252f, 334f, 640f, 372f), null, "Lv 10", 38, Cyan, TA.Left, number: true);
            Text(Corner(Node(c, "ProfTitleName"), 252f, 376f, 640f, 412f), null, "—", 32, Yellow, TA.Left);
            string[] tabs = { "ProfTabFace", "ProfTabTitle" };
            string[] keys = { "ui.profile.tab.face", "ui.profile.tab.title" };
            string[] ko = { "대표 얼굴", "칭호" };
            for (int i = 0; i < 2; i++)
            {
                var t = Corner(Node(c, tabs[i]), 80f + i * 284f, 440f, 354f + i * 284f, 506f);
                Btn(t, "Components/rb_seg_track");
                Img(Stretch(Node(t, tabs[i] + "On")), "Components/rb_seg_on");
                Text(Stretch(Node(t, tabs[i] + "Text")), keys[i], ko[i], 36, White, TA.Center, heavy: true);
            }
            // 칸 — 세로 스크롤(보기 칸은 RectMask2D 만)
            var view = Corner(Node(c, "ProfGrid"), 76f, 522f, 644f, 1000f);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);
            var content = Node(view, "ProfGridContent");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 6 * 112f + 8f); content.anchoredPosition = Vector2.zero;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.content = content; sr.viewport = view; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < 30; i++)
            {
                float x = 8f + (i % 5) * 112f, y = 4f + (i / 5) * 112f;
                var cell = Node(content, $"ProfCell{i}");
                cell.anchorMin = cell.anchorMax = new Vector2(0f, 1f); cell.pivot = new Vector2(0f, 1f);
                cell.sizeDelta = new Vector2(104f, 104f); cell.anchoredPosition = new Vector2(x, -y);
                Btn(cell, "Components/rb_panel_base");   // 로비 칸 그림(rb_slot) 대신 남색 판 — PD 2026-10-27
                Img(Inside(Node(cell, $"ProfCell{i}Face"), 52f, 52f, 80f, 80f), "Icons/rb_icon_mode_ghost_96");
                var tt = Text(Inside(Node(cell, $"ProfCell{i}Text"), 52f, 52f, 96f, 90f), null, "", 26, White, TA.Center, heavy: true);
                tt.enableWordWrapping = true;
                tt.fontSizeMin = 16f;
                Img(Inside(Node(cell, $"ProfCell{i}Check"), 86f, 18f, 30f, 30f), "Icons/rb_icon_sys_check_48");
                Img(Inside(Node(cell, $"ProfCell{i}Lock"), 20f, 20f, 30f, 30f), "Icons/rb_icon_sys_lock_48");   // 구석 — 칭호 글자를 가리지 않게
            }
            var grow = Corner(Node(c, "ProfGrow"), 90f, 1016f, 630f, 1090f);
            Btn(grow, "Components/rb_btn_primary");
            Text(Stretch(Node(grow, "ProfGrowText")), "ui.rb.profile.grow", "유령 성장 ›", 42, Ink, TA.Center, heavy: true);
        }

        private static void BuildLanguage(RectTransform root)
        {
            var page = Modal(root, "RbLanguage", "ui.rb.set.language_title", "언어 선택", 200f, 300f, 520f, 920f, out var c);
            for (int i = 0; i < 5; i++)
            {
                var b = Corner(Node(c, $"Lang{i}"), 230f, 360f + i * 108f, 490f, 448f + i * 108f);
                Btn(b, "Components/rb_btn_secondary");
                Text(Stretch(Node(b, $"Lang{i}Text")), null, "한국어", 40, White, TA.Center, heavy: true);
            }
        }
    }
}
