// RE:BORN UI 리뉴얼(2026-10-23) — 상점 탭(시안 Screens/shop · shop-items). 로비 프리팹 안의 쪽.
// 줄(패키지 · 계약 · 3칸 · 제목 · 새로고침)은 본보기만 세우고 화면 스크립트(RbShopPage)가 복제해 쌓는다. 본보기 크기는 게임 px.
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbShopBuilder
    {
        public static void Build(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageShop");
            RbHostBuilder.Head(page, "Shp", "ui.store.title", "상점", 25, 280, 155, 400, 600, 922);

            // 탭 4 — 추천 · 오늘 · 상자 · 재화. 시안 shop-items: 모서리만 둥근 네모 버튼 넷(y 453~566) · 고른 탭은 청록 글자 —
            // 호스트 도감 거르기 칩과 같은 버튼(PD 2026-10-26 「캐릭터 쪽 수정한 것처럼 버튼 크게」). 예전엔 얇은 띠(85)였다
            string[] keys = { "ui.rb.shop.tab.best", "ui.rb.shop.tab.today", "ui.rb.shop.tab.chest", "ui.rb.shop.tab.currency" };
            string[] ko = { "추천", "오늘", "상자", "재화" };
            for (int i = 0; i < 4; i++)
            {
                float x0 = 30 + i * 257.5f, x1 = x0 + 242.5f;
                var t = Place(Node(page, $"ShpTab{i}"), Edge.Top, x0, 453, x1, 566);
                Btn(t, "Components/rb_btn_secondary");
                Text(Stretch(Node(t, $"ShpTab{i}Text")), keys[i], ko[i], 42, Dim, TA.Center, heavy: true);
            }

            var content = VScroll(page, "ShpBody", 584, 2040, 1400);
            Section(content);
            Pack(content);
            Contract(content);
            Cell(content);
            Refresh(content);

            var view = page.gameObject.AddComponent<RbShopPage>();
            var so = new SerializedObject(view);
            // 값 버튼 — 시안은 패키지만 노란 큰 버튼, 나머지(젬 · 결제 · 구매 · 받기 · 확률 보기)는 남색 버튼(PD 2026-10-26 「시안이랑 달라」)
            so.FindProperty("_priceGem").objectReferenceValue = KitSprite("Components/rb_btn_secondary");
            so.FindProperty("_priceIap").objectReferenceValue = KitSprite("Components/rb_btn_secondary");
            so.FindProperty("_pricePack").objectReferenceValue = KitSprite("Components/rb_btn_primary");
            so.FindProperty("_priceSold").objectReferenceValue = KitSprite("Components/rb_btn_secondary_disabled");
            var arts = AssetDatabase.FindAssets("rb_art_shop_ t:Sprite", new[] { $"{Kit}/Art" });
            Fill(so.FindProperty("_packArts"), arts);
            Fill(so.FindProperty("_icons"), AssetDatabase.FindAssets("_96 t:Sprite", new[] { $"{Kit}/Icons" }));
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }

        private static void Fill(SerializedProperty p, string[] guids)
        {
            p.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        private static RectTransform Tpl(RectTransform content, string name, float w, float h)
        {
            var t = Node(content, name);
            t.anchorMin = t.anchorMax = new Vector2(0f, 1f);
            t.pivot = new Vector2(0f, 1f);
            t.sizeDelta = new Vector2(w, h);
            return t;
        }

        /// <summary>값 버튼 — 그림 · 아이콘(왼쪽) · 글자. 화면 스크립트가 그림 · 글 · 아이콘을 바꾼다.</summary>
        private static void PriceButton(RectTransform parent, string name, float x, float y, float w, float h, bool fromBottom = false, float size = 40f)
        {
            var b = fromBottom ? Bottom2(Node(parent, name), x, y, w, h) : Corner2(Node(parent, name), x, y, w, h);
            Btn(b, "Components/rb_btn_secondary");
            var icon = Node(b, name + "Icon");
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
            icon.sizeDelta = new Vector2(h * 0.62f, h * 0.62f);
            Img(icon, "Icons/rb_icon_currency_gem_96");
            var t = Node(b, name + "Text");
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
            t.sizeDelta = new Vector2(w - 16f, h);
            Text(t, null, "80", size, Yellow, TA.Center, number: true);
        }

        /// <summary>아래 변 기준 상자 — 칸 높이가 바뀌어도(상자 칸은 확률 보기만큼 높다) 버튼이 바닥에 붙는다. y = 아래 변에서 버튼 아래까지.</summary>
        private static RectTransform Bottom2(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, y + h * 0.5f);
            return rt;
        }

        private static RectTransform Corner2(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            return rt;
        }

        private static void Section(RectTransform content)
        {
            var s = Tpl(content, "ShpSectionTpl", 676f, 70f);
            Text(Corner2(Node(s, "ShpSectionTitle"), 0f, 0f, 676f, 40f), null, "◆ 상자 · 바로 열림", 40, White, TA.Left, heavy: true);
            Text(Corner2(Node(s, "ShpSectionSub"), 0f, 42f, 676f, 26f), null, "상자는 사는 즉시 열립니다", 28, Dim, TA.Left);
        }

        private static void Pack(RectTransform content)
        {
            // 시안 shop-purchased: 값 버튼이 노랗고 크다(높이 109 · 폭 634) — 예전 70
            var p = Tpl(content, "ShpPackTpl", 676f, 436f);
            Img(p, "Components/rb_panel_hero");
            var clip = Corner2(Node(p, "ShpPackClip"), 6f, 6f, 664f, 270f);
            clip.gameObject.AddComponent<RectMask2D>();
            var art = Stretch(Node(clip, "ShpPackArt"));
            Img(art, "Art/rb_art_shop_starter_1").preserveAspect = false;
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1032f / 420f;
            Img(Stretch(Node(clip, "ShpPackShade")), "Components/rb_fade_bottom").preserveAspect = false;
            var badge = Corner2(Node(p, "ShpPackBadge"), 16f, 14f, 92f, 40f);
            Img(badge, "Components/rb_badge_best");
            Text(Stretch(Node(badge, "ShpPackBadgeText")), "ui.rb.shop.best", "BEST", 26, Ink, TA.Center, number: true);
            Text(Corner2(Node(p, "ShpPackName"), 20f, 58f, 520f, 48f), null, "입문 패키지 Ⅰ", 50, White, TA.Left, heavy: true);
            Text(Corner2(Node(p, "ShpPackTime"), 20f, 106f, 520f, 30f), null, "7일 남음 · 1회 한정", 30, Yellow, TA.Left);
            for (int i = 0; i < 4; i++)
            {
                var r = Corner2(Node(p, $"ShpPackR{i}"), 22f + i * 160f, 150f, 150f, 118f);
                Img(Corner2(Node(r, $"ShpPackR{i}Icon"), 47f, 0f, 56f, 56f), "Icons/rb_icon_currency_gem_96");
                Text(Corner2(Node(r, $"ShpPackR{i}Value"), 0f, 58f, 150f, 34f), null, "300", 40, White, TA.Center, number: true);
                Text(Corner2(Node(r, $"ShpPackR{i}Label"), 0f, 92f, 150f, 24f), null, "젬", 26, Dim, TA.Center);
            }
            PriceButton(p, "ShpPackBuy", 20f, 306f, 636f, 110f, size: 64f);
        }

        private static void Contract(RectTransform content)
        {
            // 시안 shop: 「구매」 버튼이 칸 폭 가득(293 × 91) — 예전 60 높이라 「너무 작다」(PD 2026-10-26)
            var c = Tpl(content, "ShpContractTpl", 332f, 284f);
            Img(c, "Components/rb_panel_base");
            Text(Corner2(Node(c, "ShpContractName"), 16f, 14f, 300f, 40f), null, "영혼 계약", 40, White, TA.Left, heavy: true);
            var d = Text(Corner2(Node(c, "ShpContractDesc"), 16f, 60f, 300f, 100f), null, "즉시 젬 300 + 30일 매일 90", 30, Yellow, TA.TopLeft);
            d.enableWordWrapping = true;
            PriceButton(c, "ShpContractBuy", 20f, 16f, 292f, 92f, fromBottom: true, size: 46f);
        }

        private static void Cell(RectTransform content)
        {
            // 시안 shop-items: 값 버튼 189 × 77 · 상자 칸은 그 위에 「확률 보기」 189 × 86(칸이 그만큼 높다 — 화면 스크립트가 늘린다).
            // 버튼은 아래 변 기준이라 칸 높이가 바뀌어도 바닥에 붙는다
            var c = Tpl(content, "ShpCellTpl", 217f, 312f);
            Img(c, "Components/rb_panel_base");
            Img(Corner2(Node(c, "ShpCellIcon"), 58f, 14f, 100f, 100f), "Icons/rb_icon_chest_silver_96");
            Text(Corner2(Node(c, "ShpCellName"), 6f, 118f, 205f, 36f), null, "은 상자", 34, White, TA.Center, heavy: true);
            Text(Corner2(Node(c, "ShpCellSub"), 6f, 154f, 205f, 28f), null, "첫 구매 2배", 26, Dim, TA.Center);
            PriceButton(c, "ShpCellBuy", 14f, 12f, 189f, 77f, fromBottom: true, size: 42f);
            var odds = Bottom2(Node(c, "ShpCellOdds"), 14f, 99f, 189f, 86f);
            Btn(odds, "Components/rb_btn_secondary");
            Text(Stretch(Node(odds, "ShpCellOddsText")), "ui.store.chest.rate", "확률 보기", 36, White, TA.Center, heavy: true);
        }

        private static void Refresh(RectTransform content)
        {
            var r = Tpl(content, "ShpRefreshTpl", 676f, 92f);
            PriceButton(r, "ShpRefreshBuy", 168f, 6f, 340f, 80f, size: 42f);
        }
    }
}
