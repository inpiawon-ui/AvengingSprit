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

            // 탭 4 — 추천 · 오늘 · 상자 · 재화(시안 y 420~505)
            string[] keys = { "ui.rb.shop.tab.best", "ui.rb.shop.tab.today", "ui.rb.shop.tab.chest", "ui.rb.shop.tab.currency" };
            string[] ko = { "추천", "오늘", "상자", "재화" };
            for (int i = 0; i < 4; i++)
            {
                float x0 = 30 + i * 257.5f, x1 = x0 + 247f;
                var t = Place(Node(page, $"ShpTab{i}"), Edge.Top, x0, 420, x1, 505);
                Btn(t, "Components/rb_seg_track");
                var on = Stretch(Node(t, $"ShpTab{i}On"));
                Img(on, "Components/rb_seg_on");
                Text(Stretch(Node(t, $"ShpTab{i}Text")), keys[i], ko[i], 38, White, TA.Center, heavy: true);
            }

            var content = VScroll(page, "ShpBody", 520, 2040, 1400);
            Section(content);
            Pack(content);
            Contract(content);
            Cell(content);
            Refresh(content);

            var view = page.gameObject.AddComponent<RbShopPage>();
            var so = new SerializedObject(view);
            so.FindProperty("_priceGem").objectReferenceValue = KitSprite("Components/rb_btn_price");
            so.FindProperty("_priceIap").objectReferenceValue = KitSprite("Components/rb_btn_secondary");
            so.FindProperty("_priceSold").objectReferenceValue = KitSprite("Components/rb_btn_price_disabled");
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
        private static void PriceButton(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var b = Corner2(Node(parent, name), x, y, w, h);
            Btn(b, "Components/rb_btn_price");
            var icon = Node(b, name + "Icon");
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
            icon.sizeDelta = new Vector2(h * 0.62f, h * 0.62f);
            Img(icon, "Icons/rb_icon_currency_gem_96");
            var t = Node(b, name + "Text");
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
            t.sizeDelta = new Vector2(w - 16f, h);
            Text(t, null, "80", 40, White, TA.Center, number: true);
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
            var p = Tpl(content, "ShpPackTpl", 676f, 372f);
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
            PriceButton(p, "ShpPackBuy", 20f, 288f, 636f, 70f);
        }

        private static void Contract(RectTransform content)
        {
            var c = Tpl(content, "ShpContractTpl", 332f, 220f);
            Img(c, "Components/rb_panel_base");
            Text(Corner2(Node(c, "ShpContractName"), 16f, 14f, 300f, 40f), null, "영혼 계약", 40, White, TA.Left, heavy: true);
            var d = Text(Corner2(Node(c, "ShpContractDesc"), 16f, 60f, 300f, 76f), null, "즉시 젬 300 + 30일 매일 90", 30, Yellow, TA.TopLeft);
            d.enableWordWrapping = true;
            PriceButton(c, "ShpContractBuy", 16f, 146f, 300f, 60f);
        }

        private static void Cell(RectTransform content)
        {
            var c = Tpl(content, "ShpCellTpl", 217f, 260f);
            Img(c, "Components/rb_panel_base");
            Img(Corner2(Node(c, "ShpCellIcon"), 58f, 14f, 100f, 100f), "Icons/rb_icon_chest_silver_96");
            Text(Corner2(Node(c, "ShpCellName"), 6f, 118f, 205f, 36f), null, "은 상자", 34, White, TA.Center, heavy: true);
            Text(Corner2(Node(c, "ShpCellSub"), 6f, 154f, 205f, 28f), null, "첫 구매 2배", 26, Dim, TA.Center);
            PriceButton(c, "ShpCellBuy", 12f, 190f, 193f, 58f);
        }

        private static void Refresh(RectTransform content)
        {
            var r = Tpl(content, "ShpRefreshTpl", 676f, 76f);
            PriceButton(r, "ShpRefreshBuy", 188f, 4f, 300f, 68f);
        }
    }
}
