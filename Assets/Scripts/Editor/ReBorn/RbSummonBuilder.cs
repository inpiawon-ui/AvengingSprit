// RE:BORN UI 리뉴얼(2026-10-23) — 소환 탭(시안 Screens/summon · summon-result-10 · summon-rates · summon-records).
// 로비 프리팹(RbLobbyUI) 안의 쪽. 좌표는 시안 1080 픽셀 상자, 몸통은 쪽 머리 아래 ~ 아래 탭 위 세로 스크롤(16:9 에서 넘치면 민다).
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbSummonBuilder
    {
        private const float Top = 420f;   // 몸통 위 기준 줄(쪽 머리 바로 아래)
        private const float LowTop = 1270f;   // 아래 묶음(천장 판부터) 위 기준 줄

        public static void Build(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageSummon");
            RbHostBuilder.Head(page, "Sum", "ui.rb.tab.summon", "소환", 25, 280, 155, 400, 600, 922);
            BuildMain(page);
            BuildResult(page);
            BuildList(page, "SumRates", "ui.rb.summon.rates_title", "확률 보기", new[] { "SumRatesGrades", "SumRatesAmounts", "SumRatesHosts" }, new[] { 260f, 300f, 1500f });
            BuildList(page, "SumRecords", "ui.rb.summon.records_title", "뽑기 기록", new[] { "SumRecordsList" }, new[] { 6200f });

            var view = page.gameObject.AddComponent<RbSummonPage>();
            var so = new SerializedObject(view);
            void Many(string field, params string[] paths)
            {
                var p = so.FindProperty(field);
                p.arraySize = paths.Length;
                for (int i = 0; i < paths.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite(paths[i]);
            }
            Many("_cardFrames", "Components/rb_summon_card_r", "Components/rb_summon_card_sr", "Components/rb_summon_card_ssr");
            Many("_rarityTags", "Components/rb_rarity_r", "Components/rb_rarity_sr", "Components/rb_rarity_ssr");
            so.FindProperty("_iconGem").objectReferenceValue = KitSprite("Icons/rb_icon_currency_gem_48");
            so.FindProperty("_iconTicket").objectReferenceValue = KitSprite("Icons/rb_icon_summon_ticket_48");
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }

        private static void BuildMain(RectTransform page)
        {
            var main = Stretch(Node(page, "SumMain"));
            var scroll = VScroll(main, "SumBody", Top, 2040, 1960 - Top);
            // 20:9 처럼 길면 판을 늘려 아래 묶음(천장 · 보유 · 소환 버튼)을 바닥에 붙인다 — 위 묶음은 위에 그대로
            scroll.gameObject.AddComponent<Game.Module.Common.UI.RbFillHeight>().SetDesignHeight((1960 - Top) * S);
            var body = scroll;
            var low = Node(scroll, "SumLow");
            low.anchorMin = new Vector2(0f, 0f); low.anchorMax = new Vector2(1f, 0f); low.pivot = new Vector2(0.5f, 0f);
            low.sizeDelta = new Vector2(0f, (1960f - LowTop) * S); low.anchoredPosition = Vector2.zero;

            // 픽업 배너 — 키트 일러스트 + 글
            const float bx0 = 33, by0 = 439, bx1 = 1049, by1 = 1089;
            var banner = PlaceIn(Node(body, "SumBanner"), Top, bx0, by0, bx1, by1);
            // 그림(1.42)이 칸(1.56)보다 높다 — 늘리면 10% 옆으로 퍼졌다(하네스 R1). 칸을 덮게 깔고 칸이 잘라 낸다
            banner.gameObject.AddComponent<RectMask2D>();
            var bannerArt = Stretch(Node(banner, "SumBannerArt"));
            var bannerImg = Img(bannerArt, "Art/rb_art_banner_summon");
            bannerImg.preserveAspect = false;
            var bannerFit = bannerArt.gameObject.AddComponent<AspectRatioFitter>();
            bannerFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bannerFit.aspectRatio = bannerImg.sprite != null ? bannerImg.sprite.rect.width / bannerImg.sprite.rect.height : 1.42f;
            // 글이 놓이는 왼쪽을 가라앉힌다 — 그림 왼쪽 아래 불빛 위에서 안내 글이 묻혔다(첫 캡처). 아래 그늘을 -90도로 눕혀 왼쪽이 짙게
            var shade = Node(banner, "SumBannerShade");
            shade.anchorMin = shade.anchorMax = new Vector2(0.5f, 0.5f);
            shade.sizeDelta = new Vector2((by1 - by0) * S, 640f * S);
            shade.anchoredPosition = new Vector2((bx0 + 320f - (bx0 + bx1) * 0.5f) * S, 0f);
            shade.localRotation = Quaternion.Euler(0f, 0f, -90f);
            Img(shade, "Components/rb_fade_bottom").preserveAspect = false;
            Text(Local(Node(banner, "SumPickupLabel"), bx0, by0, bx1, by1, 66, 462, 700, 506), "ui.rb.summon.pickup", "이번 주 픽업:", 36, Cyan, TA.Left, heavy: true);
            Text(Local(Node(banner, "SumBannerTitle"), bx0, by0, bx1, by1, 66, 510, 800, 600), "ui.rb.summon.banner", "영혼 명부", 76, White, TA.Left, heavy: true);
            Text(Local(Node(banner, "SumPickupHost"), bx0, by0, bx1, by1, 66, 612, 800, 660), null, "SSR 흡혈귀", 42, White, TA.Left, heavy: true);
            Text(Local(Node(banner, "SumPickupRule"), bx0, by0, bx1, by1, 66, 668, 900, 710), null, "SSR 이 나오면 50% 확률로 픽업 호스트", 34, White, TA.Left);
            Text(Local(Node(banner, "SumPickupLeft"), bx0, by0, bx1, by1, 66, 714, 600, 754), null, "남은 2일 4시간", 32, Dim, TA.Left);

            Button2(body, "SumRatesButton", "ui.rb.summon.rates_title", "확률 보기", 33, 1122, 525, 1250, false);
            Button2(body, "SumRecordsButton", "ui.rb.summon.records_title", "뽑기 기록", 555, 1122, 1049, 1250, false);

            // 천장 — 「대박까지 49 /50」 · 막대 · 안내
            const float px0 = 33, py0 = 1287, px1 = 1049, py1 = 1450;
            var pity = PlaceIn(Node(low, "SumPityPanel"), LowTop, px0, py0, px1, py1);
            Img(pity, "Components/rb_panel_base");
            Text(Local(Node(pity, "SumPity"), px0, py0, px1, py1, 60, 1306, 900, 1360), null, "대박까지 49 /50", 46, White, TA.Left, heavy: true);
            Bar(Local(Node(pity, "SumPityBar"), px0, py0, px1, py1, 60, 1370, 1020, 1386), "SumPityFill", "Components/rb_bar_fill_cyan");
            Text(Local(Node(pity, "SumPityNote"), px0, py0, px1, py1, 60, 1396, 1020, 1436), "ui.rb.summon.pity_note", "천장 횟수는 이월되지 않습니다.", 32, Dim, TA.Left);

            // 보유 — 젬 · 소환권
            Img(PlaceIn(Node(low, "SumGemIcon"), LowTop, 40, 1500, 96, 1556), "Icons/rb_icon_currency_gem_48");
            Text(PlaceIn(Node(low, "SumGem"), LowTop, 106, 1500, 500, 1556), null, "14,550", 46, White, TA.Left, number: true);
            Text(PlaceIn(Node(low, "SumTickets"), LowTop, 600, 1504, 1049, 1552), null, "보유 소환권 1", 36, Dim, TA.Right);

            Button2(low, "SumPull1", null, "오늘 무료 1회", 33, 1600, 376, 1760, false, LowTop);
            Button2(low, "SumPull10", null, "10회 소환  젬 3,000", 400, 1600, 1049, 1760, true, LowTop);
            Text(PlaceIn(Node(low, "SumNote"), LowTop, 33, 1790, 1049, 1900), "ui.rb.summon.note",
                 "10회 소환은 SR 1회 보장 · 50회 안에 대박이 없으면\n50번째는 대박 확정", 32, Dim, TA.TopLeft).enableWordWrapping = true;
        }

        /// <summary>버튼(그림 + 가운데 글자) — 글자 이름은 `{name}Text`.</summary>
        private static Button Button2(RectTransform body, string name, string key, string korean, float x0, float y0, float x1, float y1, bool primary,
                                      float refTop = Top)
        {
            var b = PlaceIn(Node(body, name), refTop, x0, y0, x1, y1);
            var btn = Btn(b, primary ? "Components/rb_btn_primary" : "Components/rb_btn_secondary");
            Text(Stretch(Node(b, name + "Text")), key, korean, primary ? 46 : 40, primary ? Ink : White, TA.Center, heavy: true);
            return btn;
        }

        // ── 결과 — 1회는 큰 카드 한 장 · 10회는 5×2 ───────────────

        private static void BuildResult(RectTransform page)
        {
            var res = Stretch(Node(page, "SumResult"));
            Img(Stretch(Node(res, "SumResultBg")), "Art/rb_art_summon_result_bg").preserveAspect = false;
            Text(Place(Node(res, "SumResultCount"), Edge.Top, 300, 430, 780, 476), null, "10회 소환", 38, Cyan, TA.Center, heavy: true);
            Text(Place(Node(res, "SumResultTitle"), Edge.Top, 240, 480, 840, 560), null, "소환 결과", 64, White, TA.Center, heavy: true);

            // 카드 본보기(화면 스크립트가 10장으로 복제 · 자리를 잡는다) — 칸 190 × 300(시안)
            var grid = Place(Node(res, "SumGrid"), Edge.Top, 40, 600, 1040, 1250);
            var card = Node(grid, "SumCardTemplate");
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.sizeDelta = new Vector2(190f * S, 300f * S);
            Img(card, "Components/rb_summon_card_r");
            float x0 = 0, y0 = 0, x1 = 190, y1 = 300;
            Img(Local(Node(card, "SumCardRarity"), x0, y0, x1, y1, 6, 6, 62, 46), "Components/rb_rarity_r");
            var nw = Local(Node(card, "SumCardNew"), x0, y0, x1, y1, 110, 4, 186, 40);
            Img(nw, "Components/rb_badge_new");
            Text(Stretch(Node(nw, "SumCardNewText")), "ui.rb.summon.new", "새 몸!", 24, Ink, TA.Center, heavy: true);
            Img(Local(Node(card, "SumCardFace"), x0, y0, x1, y1, 45, 60, 145, 160), "Icons/rb_icon_mode_ghost_96");
            Text(Local(Node(card, "SumCardName"), x0, y0, x1, y1, 6, 176, 184, 214), null, "갱스터", 30, White, TA.Center, heavy: true);
            Img(Local(Node(card, "SumCardShardIcon"), x0, y0, x1, y1, 22, 226, 52, 256), "Icons/rb_icon_shard_48");
            Text(Local(Node(card, "SumCardShards"), x0, y0, x1, y1, 54, 222, 184, 260), null, "조각 ×2", 28, White, TA.Left);
            var jp = Local(Node(card, "SumCardJackpot"), x0, y0, x1, y1, 10, 264, 180, 296);
            Img(jp, "Components/rb_badge_best");
            Text(Stretch(Node(jp, "SumCardJackpotText")), "ui.rb.summon.jackpot_card", "대박 확정", 24, Ink, TA.Center, heavy: true);

            var ess = Place(Node(res, "SumResultEssence"), Edge.Bottom, 300, 1700, 780, 1760);
            Img(Local(Node(ess, "SumResultEssenceIcon"), 300, 1700, 780, 1760, 300, 1704, 352, 1756), "Icons/rb_icon_essence_48");
            Text(Local(Node(ess, "SumResultEssenceText"), 300, 1700, 780, 1760, 360, 1700, 780, 1760), null, "+30 영혼 정수", 40, White, TA.Left, heavy: true);

            var ok = Place(Node(res, "SumResultOk"), Edge.Bottom, 33, 1860, 360, 1990);
            Btn(ok, "Components/rb_btn_secondary");
            Text(Stretch(Node(ok, "SumResultOkText")), "ui.common.ok", "확인", 44, White, TA.Center, heavy: true);
            var again = Place(Node(res, "SumResultAgain"), Edge.Bottom, 384, 1850, 1049, 2000);
            Btn(again, "Components/rb_btn_primary");
            Text(Stretch(Node(again, "SumResultAgainText")), null, "10회 더  젬 3,000", 46, Ink, TA.Center, heavy: true);
            res.gameObject.SetActive(false);
        }

        // ── 확률 · 기록 — 머리 + 글 목록(세로 스크롤) ───────────────

        private static void BuildList(RectTransform page, string name, string titleKey, string korean, string[] texts, float[] heights)
        {
            var sub = Stretch(Node(page, name));
            Img(Stretch(Node(sub, name + "Bg")), "Art/rb_art_bg_outgame_blur").preserveAspect = false;
            RbHostBuilder.Head(sub, name, titleKey, korean, 25, 280, 155, 400, 800, 922);
            sub.Find(name + "Help")?.gameObject.SetActive(false);
            float total = 40f;
            foreach (var h in heights) total += h + 30f;
            var body = VScroll(sub, name + "Body", Top, 2040, total);
            float y = Top + 20f;
            for (int i = 0; i < texts.Length; i++)
            {
                string t = texts[i];
                float h = heights[i];
                var panel = PlaceIn(Node(body, t + "Panel"), Top, 33, y, 1049, y + h);
                Img(panel, "Components/rb_panel_base");
                var tmp = Text(Local(Node(panel, t), 33, y, 1049, y + h, 66, y + 26, 1016, y + h - 20), null, "—", 36, White, TA.TopLeft);
                tmp.enableWordWrapping = true;
                tmp.enableAutoSizing = false;
                tmp.fontSize = 36 * S;
                y += h + 30f;
            }
            sub.gameObject.SetActive(false);
        }
    }
}
