using System.Collections.Generic;
using System.IO;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비 상점 창(`LobbyShopPanel`)을 세운다 — 시안 S04(머리 · 탭 · 재화) · S03(상자) · S02(오늘) · S05(구매 확인) · S07(획득).
    ///
    /// 좌표는 **게임 화면 좌표**(720 × 1280, 왼쪽 위 기준)다. 941 폭 시안은 0.765 배, 720 폭 시안(S03)은 그대로 재서
    /// 머리 높이 차이만큼 위로 옮겼다(S03 −92 · S02 −73 — 머리를 S04 하나로 맞춘다).
    /// 그림은 전부 납품 부품(`Assets/BaseResource/Store` · `ChapterHost` · `LobbyMainUI/Nav` · `Pass`). 코드로 그리지 않는다.
    /// 순서: 로비 바인더(`LobbyS22eBinder`) 뒤. 글자 결은 챕터 창 글자를 복제해 쓴다.
    /// </summary>
    public static class LobbyShopBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string St = "Assets/BaseResource/Store";
        private const string Pr = "Assets/BaseResource/Store/Products";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string Nav = "Assets/BaseResource/LobbyMainUI/Nav";
        private const string Lobby = "Assets/BaseResource/LobbyMainUI";
        private const string Pass = "Assets/BaseResource/Pass";
        private const string Faces = "Assets/BaseResource/Growth/Faces";

        private static readonly Color Ink = new(0.16f, 0.08f, 0.03f, 1f);
        private static readonly Color Soft = new(0.8f, 0.86f, 0.96f, 1f);
        private static readonly Color Gold = new(1f, 0.85f, 0.32f, 1f);
        private static readonly Color Cyan = new(0.55f, 0.9f, 1f, 1f);

        private static TextMeshProUGUI s_title, s_body;

        /// <summary>다른 창 빌더(소환 · 패스 · 프로필)가 같은 도우미를 쓰도록 글자 본을 잡는다.</summary>
        internal static bool UseTemplates(Transform lobby)
        {
            s_title = Find(lobby, "CHHostTitle")?.GetComponent<TextMeshProUGUI>();
            s_body = Find(lobby, "CHInfoDesc")?.GetComponent<TextMeshProUGUI>();
            return s_title != null && s_body != null;
        }

        [MenuItem("Tools/Game/로비 — 상점 창 세우기 (S02~S07)")]
        public static void Run()
        {
            Borders();
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                s_title = Find(lobby.transform, "CHHostTitle")?.GetComponent<TextMeshProUGUI>();
                s_body = Find(lobby.transform, "CHInfoDesc")?.GetComponent<TextMeshProUGUI>();
                if (s_title == null || s_body == null) { Debug.LogError("[상점 창] 글자 본(CHHostTitle · CHInfoDesc)이 없다 — 챕터 창부터 세운다"); return; }

                var old = lobby.transform.Find("LobbyShopPanel");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var panel = new GameObject("LobbyShopPanel", typeof(RectTransform)).GetComponent<RectTransform>();
                panel.gameObject.layer = lobby.layer;
                panel.SetParent(lobby.transform, false);
                Stretch(panel);
                var nav = lobby.transform.Find("BottomNav");
                if (nav != null) panel.SetSiblingIndex(nav.GetSiblingIndex());

                var box = new GameObject("ShopContent", typeof(RectTransform)).GetComponent<RectTransform>();
                box.gameObject.layer = lobby.layer;
                box.SetParent(panel, false);
                box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
                box.sizeDelta = new Vector2(720, 1280);
                box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

                // 배경 — 로비 챕터 창과 같은 칸 없는 판(밤 배관 벽). 누르면 뒤 로비가 눌리지 않게 막는다
                var bg = Pic(box, "ShopBackdrop", $"{Ch}/ch_screen_s22e.png", 0, 0, 0, 0);
                bg.preserveAspect = false;
                bg.raycastTarget = true;
                float k = 720f / 941f;
                C(bg.rectTransform, 360, 645, bg.sprite.rect.width * k, bg.sprite.rect.height * k);

                Header(box);
                Today(Page(box, "ShopPageToday"));
                Chest(Page(box, "ShopPageChest"));
                Currency(Page(box, "ShopPageCurrency"));
                // 확인 · 획득 창은 ShopContent 옆(형제)에 둔다 — 하네스 모달 규칙이 안쪽 창을 못 잡고, 어두운 판이 4:3 양옆까지 덮게
                Confirm(panel);
                Reward(panel);

                var comp = panel.gameObject.AddComponent<LobbyShopPanel>();
                Bind(comp);
                var main = new SerializedObject(lobby.GetComponent<LobbyMainUI>());
                main.FindProperty("_shopPanel").objectReferenceValue = comp;
                main.ApplyModifiedPropertiesWithoutUndo();
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[상점 창] 세움");
        }

        // ── 머리 · 탭 (S04) ──────────────────────────────────

        private static void Header(Transform box)
        {
            Sliced(box, "ShopHeader", $"{Ch}/ch_s22e_box.png", 23, 122, 697, 248, 2f);
            Pic(box, "ShopHeaderIcon", $"{Nav}/nav_icon_shop.png", 40, 132, 112, 188);
            var t = Title(box, "ShopTitleText", 120, 161, 130, 52, 38, Color.white);
            t.alignment = TextAlignmentOptions.Left;
            Pill(box, "ShopGoldPill", "ShopGoldText", $"{Ch}/ch_icon_coin.png", 249, 141, 383, 182);
            Pill(box, "ShopGemPill", "ShopGemText", $"{Ch}/ch_icon_gem.png", 398, 141, 532, 182);
            Tab(box, "ShopTabToday", "TabLabelToday", $"{Pass}/micon_login.png", 32, 191, 246, 240);
            Tab(box, "ShopTabChest", "TabLabelChest", $"{Pass}/micon_chest.png", 252, 191, 467, 240);
            Tab(box, "ShopTabCurrency", "TabLabelCurrency", $"{Pr}/prod_gold_1.png", 473, 191, 687, 240);
        }

        private static void Pill(Transform box, string name, string textName, string icon, float x0, float y0, float x1, float y1)
        {
            var pill = Sliced(box, name, $"{Nav}/top_stamina_plate.png", x0, y0, x1, y1, 3f);
            var ic = Pic(pill.transform, "Icon", icon, 0, 0, 0, 0);
            Local(ic.rectTransform, -(x1 - x0) * 0.5f + 20, 0, 26, 26);
            var tx = Body(pill.transform, textName, 0, 0, 0, 0, 20, Color.white);
            Local(tx.rectTransform, 12, 0, x1 - x0 - 46, 30);
            tx.alignment = TextAlignmentOptions.Center;
            tx.fontStyle = FontStyles.Bold;
        }

        private static void Tab(Transform box, string name, string labelName, string icon, float x0, float y0, float x1, float y1)
        {
            var tab = Sliced(box, name, $"{St}/shop_tab.png", x0, y0, x1, y1, 2.2f);
            tab.raycastTarget = true;
            Button(tab.gameObject);
            var ic = Pic(tab.transform, "TabIcon", icon, 0, 0, 0, 0);
            Local(ic.rectTransform, -(x1 - x0) * 0.5f + 52, 0, 32, 30);
            var lb = Title(tab.transform, labelName, 0, 0, 0, 0, 22, Color.white);
            Local(lb.rectTransform, 18, 0, 120, 32);
            lb.alignment = TextAlignmentOptions.Center;
        }

        private static RectTransform Page(Transform box, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = box.gameObject.layer;
            var r = go.GetComponent<RectTransform>();
            r.SetParent(box, false);
            Stretch(r);
            return r;
        }

        // ── 오늘 (S02, −73) ──────────────────────────────────

        private static readonly float[] DailyX = { 60, 264, 469 };
        private static readonly float[] DailyY = { 367, 645 };
        private const float DailyW = 192, DailyH = 262;

        private static void Today(Transform page)
        {
            Sliced(page, "TodayFrame", $"{Ch}/ch_s22e_box.png", 42, 252, 678, 925, 2f);
            Title(page, "TodayTitle", 74, 288, 360, 50, 36, Cyan).alignment = TextAlignmentOptions.Left;
            Body(page, "TodayTimer", 78, 338, 340, 26, 18, Soft).alignment = TextAlignmentOptions.Left;
            var rf = Sliced(page, "TodayRefresh", $"{St}/btn_small.png", 452, 270, 656, 320, 3f);
            rf.raycastTarget = true;
            Button(rf.gameObject);
            var rl = Body(rf.transform, "TodayRefreshLabel", 0, 0, 0, 0, 19, Color.white);
            Local(rl.rectTransform, -32, 0, 100, 28);
            var gi = Pic(rf.transform, "Icon", $"{Ch}/ch_icon_gem.png", 0, 0, 0, 0);
            Local(gi.rectTransform, 38, 0, 26, 26);
            var rc = Body(rf.transform, "TodayRefreshCost", 0, 0, 0, 0, 20, Color.white);
            Local(rc.rectTransform, 74, 0, 40, 28);
            Body(page, "TodayRefreshLeft", 452, 341, 204, 24, 16, Soft).alignment = TextAlignmentOptions.Center;

            for (int i = 0; i < 6; i++)
            {
                float x0 = DailyX[i % 3], y0 = DailyY[i / 3];
                var slot = Node(page, $"DailySlot{i}", x0, y0, x0 + DailyW, y0 + DailyH);
                var frame = Sliced(slot, "SlotFrame", $"{St}/daily_slot.png", 0, 0, 0, 0, 2.2f);
                StretchIn(frame.rectTransform);
                var free = Sliced(slot, "SlotFrameFree", $"{St}/daily_slot_free.png", 0, 0, 0, 0, 2.2f);
                StretchIn(free.rectTransform);
                var art = Pic(slot, "SlotArt", null, 0, 0, 0, 0);
                Local(art.rectTransform, 0, 54, 168, 132);
                var lk = Pic(slot, "SlotLock", $"{Ch}/ch_lock.png", 0, 0, 0, 0);
                Local(lk.rectTransform, 0, 60, 40, 52);
                var nm = Body(slot, "SlotName", 0, 0, 0, 0, 17, Color.white);
                Local(nm.rectTransform, 0, -20, 176, 24);
                var ct = Title(slot, "SlotCount", 0, 0, 0, 0, 22, Color.white);
                Local(ct.rectTransform, 0, -46, 176, 28);
                for (int s = 0; s < 5; s++)
                {
                    var star = Pic(slot, $"SlotStar{s}", $"{Ch}/ch_star_on.png", 0, 0, 0, 0);
                    Local(star.rectTransform, -34 + s * 17, -71, 16, 16);
                }
                var buy = Sliced(slot, "DailySlot" + i + "Buy", $"{St}/btn_small.png", 0, 0, 0, 0, 3f);
                Local(buy.rectTransform, 0, -104, 174, 42);
                buy.raycastTarget = true;
                Button(buy.gameObject);
                var pg = Pic(buy.transform, "SlotPriceIconGold", $"{Ch}/ch_icon_coin.png", 0, 0, 0, 0);
                Local(pg.rectTransform, -58, 0, 24, 24);
                var pgm = Pic(buy.transform, "SlotPriceIconGem", $"{Ch}/ch_icon_gem.png", 0, 0, 0, 0);
                Local(pgm.rectTransform, -58, 0, 24, 24);
                var pt = Title(buy.transform, "SlotPriceText", 0, 0, 0, 0, 20, Color.white);
                Local(pt.rectTransform, 10, 0, 130, 30);
                var nw = Pic(slot, "SlotNew", $"{St}/ribbon_new.png", 0, 0, 0, 0);
                Local(nw.rectTransform, -DailyW * 0.5f + 36, DailyH * 0.5f - 34, 74, 66);
                var sold = Sliced(slot, "SlotSold", $"{St}/sold_overlay.png", 0, 0, 0, 0, 2.2f);
                StretchIn(sold.rectTransform);
                sold.color = new Color(1f, 1f, 1f, 0.85f);
                var sx = Title(sold.transform, "SlotSoldText", 0, 0, 0, 0, 20, Soft);
                Local(sx.rectTransform, 0, 0, 170, 30);
                free.gameObject.SetActive(false);
                lk.gameObject.SetActive(false);
                nw.gameObject.SetActive(false);
                sold.gameObject.SetActive(false);
            }
        }

        // ── 상자 (S03, −92) ──────────────────────────────────

        private static readonly Color[] ChestTitleColors =
        {
            new(0.95f, 0.97f, 1f), new(1f, 0.86f, 0.35f), new(0.96f, 0.72f, 1f),
        };

        private static readonly string[] ChestArt = { "chest_silver", "chest_gold", "chest_magic" };

        private static void Chest(Transform page)
        {
            float[] rows = { 250, 483, 716 };
            for (int i = 0; i < 3; i++)
            {
                float y = rows[i];
                var row = Node(page, $"ChestRow{i}", 23, y, 697, y + 220);
                var plate = Sliced(row, "RowPlate", $"{Ch}/ch_s22e_box.png", 0, 0, 0, 0, 2f);
                StretchIn(plate.rectTransform);
                var t = Title(row, "RowTitle", 0, 0, 0, 0, 40, ChestTitleColors[i]);
                Pivot(t.rectTransform, 0f);
                Local(t.rectTransform, -337 + 24, 70, 300, 52);
                t.alignment = TextAlignmentOptions.Left;
                var art = Pic(row, "RowArt", $"{Lobby}/{ChestArt[i]}.png", 0, 0, 0, 0);
                Local(art.rectTransform, -147, -22, 190, 150);
                var buy = Node(row, $"ChestRow{i}Buy", 0, 0, 0, 0);
                Local(buy, 52, 75, 170, 50);
                var hit = buy.gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                Button(buy.gameObject);
                var gi = Pic(buy, "Icon", $"{Ch}/ch_icon_gem.png", 0, 0, 0, 0);
                Local(gi.rectTransform, -55, 0, 46, 40);
                var pr = Title(buy, "RowPrice", 0, 0, 0, 0, 34, Color.white);
                Pivot(pr.rectTransform, 0f);
                Local(pr.rectTransform, -24, 0, 110, 44);
                pr.alignment = TextAlignmentOptions.Left;
                var on = Sliced(row, "RowOpenNowPlate", $"{St}/ribbon_open_now.png", 0, 0, 0, 0, 3f);
                Local(on.rectTransform, 245, 75, 150, 42);
                var ot = Title(on.transform, "RowOpenNow", 0, 0, 0, 0, 17, Color.white);
                Local(ot.rectTransform, 12, 0, 110, 30);
                var info = Sliced(row, "RowInfo", $"{St}/gem_slot.png", 0, 0, 0, 0, 3f);
                Local(info.rectTransform, 165, -4, 315, 88);   // 시안 S03 x 368~683
                var ic1 = Pic(info.transform, "GoldIcon", $"{Pr}/prod_gold_1.png", 0, 0, 0, 0);
                Local(ic1.rectTransform, -125, 18, 36, 30);
                var g = Body(info.transform, "RowGold", 0, 0, 0, 0, 18, Color.white);
                Pivot(g.rectTransform, 0f);
                Local(g.rectTransform, -98, 18, 240, 26);
                g.alignment = TextAlignmentOptions.Left;
                var ic2 = Pic(info.transform, "GhostIcon", $"{Nav}/nav_icon_play.png", 0, 0, 0, 0);
                Local(ic2.rectTransform, -125, -18, 34, 30);
                var s = Body(info.transform, "RowShard", 0, 0, 0, 0, 18, Color.white);
                Pivot(s.rectTransform, 0f);
                Local(s.rectTransform, -98, -18, 240, 26);
                s.alignment = TextAlignmentOptions.Left;
                var rate = Sliced(row, $"ChestRow{i}Rate", $"{St}/btn_rate.png", 0, 0, 0, 0, 3f);
                Local(rate.rectTransform, 165, -76, 315, 46);
                rate.raycastTarget = true;
                Button(rate.gameObject);
                var rt = Body(rate.transform, "RowRateText", 0, 0, 0, 0, 19, Color.white);
                Local(rt.rectTransform, 22, 0, 240, 30);
            }
            var note = Sliced(page, "ChestNoteBand", $"{Ch}/ch_s22e_box.png", 32, 946, 688, 1004, 2f);
            var n = Body(note.transform, "ChestNote", 0, 0, 0, 0, 18, Color.white);
            Local(n.rectTransform, 0, 0, 620, 30);
            n.alignment = TextAlignmentOptions.Center;
        }

        // ── 재화 (S04 × 0.765) ───────────────────────────────

        private static readonly float[] ColX = { 34, 256, 470 };
        private const float ColW = 216;

        private static void Currency(Transform page)
        {
            // 계약 둘
            float[][] cards = { new float[] { 27, 251, 356, 463 }, new float[] { 363, 251, 692, 463 } };
            for (int i = 0; i < 2; i++)
            {
                var b = cards[i];
                var card = Node(page, $"Card{i}", b[0], b[1], b[2], b[3]);
                var plate = Sliced(card, "CardPlate", $"{Ch}/ch_s22e_box.png", 0, 0, 0, 0, 2f);
                StretchIn(plate.rectTransform);
                float w = b[2] - b[0], h = b[3] - b[1];
                var art = Pic(card, "CardArt", $"{Pr}/{(i == 0 ? "prod_card_small" : "prod_card_big")}.png", 0, 0, 0, 0);
                Local(art.rectTransform, 10, 18, w - 40, 128);
                var t = Title(card, "CardTitle", 0, 0, 0, 0, 25, Color.white);
                Pivot(t.rectTransform, 0f);
                Local(t.rectTransform, -w * 0.5f + 18, h * 0.5f - 26, w - 30, 34);
                t.alignment = TextAlignmentOptions.Left;
                var d = Body(card, "CardDesc", 0, 0, 0, 0, 17, Color.white);
                Local(d.rectTransform, 0, -48, w - 20, 26);
                d.alignment = TextAlignmentOptions.Center;
                // 산 계약 — 남은 날 · 오늘 받기 / 안 산 계약 — 값 버튼
                var days = Sliced(card, "CardDays", $"{St}/btn_small.png", 0, 0, 0, 0, 3f);
                Local(days.rectTransform, -78, -82, 140, 36);
                var dt = Body(days.transform, "CardDaysText", 0, 0, 0, 0, 16, Color.white);
                Local(dt.rectTransform, 0, 0, 126, 26);
                var claim = Sliced(card, "CardClaim", $"{St}/btn_small.png", 0, 0, 0, 0, 3f);
                claim.gameObject.name = $"Card{i}Claim";
                Local(claim.rectTransform, 78, -82, 140, 40);
                claim.raycastTarget = true;
                Button(claim.gameObject);
                var ct = Title(claim.transform, "CardClaimText", 0, 0, 0, 0, 19, Cyan);
                Local(ct.rectTransform, 0, 0, 126, 30);
                PriceButton(card, $"Card{i}Buy", "CardPrice", 0, -82, w - 80, 44);
            }

            // 입문 패키지
            var sec = Node(page, "StarterSection", 27, 473, 692, 679);
            var sp = Sliced(sec, "StarterPlate", $"{Ch}/ch_s22e_box.png", 0, 0, 0, 0, 2f);
            StretchIn(sp.rectTransform);
            var clock = Pic(sec, "Clock", $"{Lobby}/chesttimeicon.png", 0, 0, 0, 0);
            Local(clock.rectTransform, -332 + 32, 79, 30, 30);
            var head = Title(sec, "StarterHead", 0, 0, 0, 0, 25, Color.white);
            Pivot(head.rectTransform, 0f);
            Local(head.rectTransform, -332 + 54, 79, 520, 34);
            head.alignment = TextAlignmentOptions.Left;
            for (int i = 0; i < 3; i++)
            {
                var s = Node(page, $"Starter{i}", ColX[i], 520, ColX[i] + ColW, 673);
                s.SetParent(sec, true);
                var plate = Sliced(s, "Plate", $"{St}/starter_slot.png", 0, 0, 0, 0, 2.5f);
                StretchIn(plate.rectTransform);
                var t = Title(s, "StarterTitle", 0, 0, 0, 0, 20, Color.white);
                Local(t.rectTransform, 0, 60, ColW - 20, 28);
                var art = Pic(s, "Art", $"{Pr}/prod_starter_{i + 1}.png", 0, 0, 0, 0);
                Local(art.rectTransform, 0, 20, ColW - 40, 52);
                // 구성이 넷(행동력 포함)이면 한 줄에 안 들어간다 — 두 줄까지 받는다
                var d = Body(s, "StarterDesc", 0, 0, 0, 0, 13, Color.white);
                Local(d.rectTransform, 0, -24, ColW - 14, 30);
                d.textWrappingMode = TextWrappingModes.Normal;
                d.fontSizeMin = 9f;
                var once = Sliced(s, "Once", $"{St}/ribbon_first2x.png", 0, 0, 0, 0, 3f);
                Local(once.rectTransform, -62, -55, 86, 32);
                var ot = Body(once.transform, "StarterOnce", 0, 0, 0, 0, 13, Color.white);
                Local(ot.rectTransform, 0, 0, 80, 22);
                PriceButton(s, $"Starter{i}Buy", "StarterPrice", 42, -55, 122, 34);
            }

            // 챕터 돌파
            var cp = Node(page, "ChapterPack", 27, 686, 692, 794);
            cp.gameObject.AddComponent<ShopProductTag>();
            var cpp = Sliced(cp, "Plate", $"{Ch}/ch_s22e_box.png", 0, 0, 0, 0, 2f);
            StretchIn(cpp.rectTransform);
            var gh = Pic(cp, "Ghost", "Assets/BaseResource/Summon/summon_ghost.png", 0, 0, 0, 0);
            Local(gh.rectTransform, -282, -4, 92, 86);
            var cpt = Title(cp, "ChapterPackTitle", 0, 0, 0, 0, 25, Color.white);
            Pivot(cpt.rectTransform, 0f);
            Local(cpt.rectTransform, -225, 30, 340, 34);
            cpt.alignment = TextAlignmentOptions.Left;
            var cpa = Pic(cp, "Art", $"{Pr}/prod_chapter.png", 0, 0, 0, 0);
            Local(cpa.rectTransform, -40, -9, 300, 48);
            cpt.transform.SetAsLastSibling();   // 긴 제목(영어)이 그림 위에 오게
            var cpd = Body(cp, "ChapterPackDesc", 0, 0, 0, 0, 14, Color.white);
            Local(cpd.rectTransform, -40, -42, 360, 20);
            var cpo = Sliced(cp, "Once", $"{St}/ribbon_first2x.png", 0, 0, 0, 0, 3f);
            Local(cpo.rectTransform, 254, 34, 112, 28);
            var cpot = Body(cpo.transform, "ChapterPackOnce", 0, 0, 0, 0, 14, Color.white);
            Local(cpot.rectTransform, 0, 0, 100, 22);
            PriceButton(cp, "ChapterPackBuy", "ChapterPackPrice", 250, -8, 142, 42);

            // 젬 · 골드 묶음 판(S04) — 칸보다 먼저 깐다
            Sliced(page, "GemSection", $"{Ch}/ch_s22e_box.png", 27, 800, 692, 1004, 2f);
            Sliced(page, "GoldSection", $"{Ch}/ch_s22e_box.png", 27, 1008, 692, 1113, 2f);

            // 젬 묶음
            Pic(page, "GemHeadIcon", $"{Ch}/ch_icon_gem.png", 40, 805, 70, 833);
            Title(page, "GemHead", 78, 818, 300, 34, 25, Color.white).alignment = TextAlignmentOptions.Left;
            for (int i = 0; i < 6; i++)
            {
                float y0 = i < 3 ? 838 : 922;
                var g = Node(page, $"Gem{i}", ColX[i % 3], y0, ColX[i % 3] + ColW, y0 + 78);
                var plate = Sliced(g, "Plate", $"{St}/gem_slot.png", 0, 0, 0, 0, 3f);
                StretchIn(plate.rectTransform);
                var art = Pic(g, "Art", $"{Pr}/prod_gem_{i + 1}.png", 0, 0, 0, 0);
                Local(art.rectTransform, -38, 14, 62, 38);
                var am = Title(g, "GemAmount", 0, 0, 0, 0, 20, Color.white);
                Local(am.rectTransform, 46, 14, 100, 28);
                PriceButton(g, $"Gem{i}Buy", "GemPrice", 0, -20, ColW - 20, 30);
                var first = Sliced(g, "GemFirst", $"{St}/ribbon_first2x.png", 0, 0, 0, 0, 3f);
                Local(first.rectTransform, -52, 27, 100, 22);   // 칸 안 왼쪽 위(시안)
                var ft = Body(first.transform, "GemFirstText", 0, 0, 0, 0, 12, Color.white);
                Local(ft.rectTransform, 0, 0, 94, 18);
            }

            // 골드
            Pic(page, "GoldHeadIcon", $"{Ch}/ch_icon_coin.png", 40, 1013, 70, 1041);
            Title(page, "GoldHead", 78, 1026, 200, 34, 25, Color.white).alignment = TextAlignmentOptions.Left;
            var legal = Body(page, "ShopLegalText", 330, 1026, 230, 22, 13, Soft);
            legal.alignment = TextAlignmentOptions.Right;
            Hit(page, "ShopLegal", 330, 1015, 560, 1037);
            var rest = Body(page, "ShopRestoreText", 572, 1026, 110, 22, 13, Soft);
            rest.alignment = TextAlignmentOptions.Right;
            Hit(page, "ShopRestore", 572, 1015, 682, 1037);
            for (int i = 0; i < 3; i++)
            {
                var g = Node(page, $"Gold{i}", ColX[i], 1048, ColX[i] + ColW, 1104);
                var plate = Sliced(g, "Plate", $"{St}/gold_slot.png", 0, 0, 0, 0, 3f);
                StretchIn(plate.rectTransform);
                plate.raycastTarget = true;
                plate.gameObject.name = $"Gold{i}Buy";
                Button(plate.gameObject);
                var art = Pic(g, "Art", $"{Pr}/prod_gold_{i + 1}.png", 0, 0, 0, 0);
                Local(art.rectTransform, -68, 0, 62, 44);
                var lb = Body(g, "GoldLabel", 0, 0, 0, 0, 16, Color.white);
                Pivot(lb.rectTransform, 0f);
                Local(lb.rectTransform, -30, 0, 130, 26);
                lb.alignment = TextAlignmentOptions.Left;
            }
        }

        private static Image PriceButton(Transform parent, string name, string textName, float x, float y, float w, float h)
        {
            var b = Sliced(parent, name, $"{St}/btn_price.png", 0, 0, 0, 0, 3f);
            Local(b.rectTransform, x, y, w, h);
            b.raycastTarget = true;
            Button(b.gameObject);
            var t = Title(b.transform, textName, 0, 0, 0, 0, Mathf.Clamp(h * 0.62f, 13f, 27f), Ink);
            Local(t.rectTransform, 0, 0, w - 16, h - 6);
            Plain(t);
            return b;
        }

        // ── 구매 확인 (S05 × 0.765) ──────────────────────────

        private static void Confirm(Transform box)
        {
            var root = Node(box, "StoreConfirm", 0, 0, 720, 1280);
            Stretch(root);
            root.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();   // 창은 가운데 그대로(ScreenFit 이 줄마다 위아래로 찢지 않게)
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0.03f, 0.62f);
            dim.raycastTarget = true;
            Sliced(root, "Body", $"{St}/popup_plate_wide.png", 126, 325, 597, 972, 2f);
            var bar = Sliced(root, "TitleBar", $"{St}/shop_tab.png", 187, 289, 535, 358, 2f);
            var t = Title(bar.transform, "StoreConfirmTitle", 0, 0, 0, 0, 34, Color.white);
            Local(t.rectTransform, 0, 0, 320, 48);
            var x = Pic(root, "StoreConfirmX", $"{St}/btn_close_x.png", 555, 306, 612, 362);
            x.raycastTarget = true;
            Button(x.gameObject);
            var art = Pic(root, "StoreConfirmArt", null, 140, 366, 584, 552);
            art.preserveAspect = true;
            Title(root, "StoreConfirmName", 140, 581, 444, 46, 34, new Color(0.96f, 0.82f, 1f));
            for (int i = 0; i < 4; i++)
            {
                float y = 616 + i * 57;
                var row = Sliced(root, $"StoreConfirmRow{i}", $"{St}/content_row.png", 157, y, 562, y + 47, 3f);
                var ic = Pic(row.transform, "RowIcon", null, 0, 0, 0, 0);
                Local(ic.rectTransform, -150, 0, 60, 38);
                var n = Body(row.transform, "RowName", 0, 0, 0, 0, 20, Color.white);
                Pivot(n.rectTransform, 0f);
                Local(n.rectTransform, -100, 0, 200, 30);
                n.alignment = TextAlignmentOptions.Left;
                var a = Title(row.transform, "RowAmount", 0, 0, 0, 0, 22, Color.white);
                Pivot(a.rectTransform, 1f);
                Local(a.rectTransform, 180, 0, 140, 30);
                a.alignment = TextAlignmentOptions.Right;
            }
            Body(root, "StoreConfirmLimit", 157, 803, 405, 30, 21, Color.white).alignment = TextAlignmentOptions.Center;
            var buy = Sliced(root, "StoreConfirmBuy", $"{St}/btn_confirm.png", 161, 834, 558, 897, 3f);
            buy.raycastTarget = true;
            Button(buy.gameObject);
            var bt = Title(buy.transform, "StoreConfirmBuyText", 0, 0, 0, 0, 32, Ink);
            Local(bt.rectTransform, 0, 0, 370, 50);
            Plain(bt);
            var close = Sliced(root, "StoreConfirmClose", $"{St}/btn_cancel.png", 207, 912, 513, 958, 3f);
            close.raycastTarget = true;
            Button(close.gameObject);
            var ctx = Title(close.transform, "StoreConfirmCloseText", 0, 0, 0, 0, 24, Color.white);
            Local(ctx.rectTransform, 0, 0, 280, 36);
            Body(root, "StoreConfirmNote", 157, 990, 405, 24, 14, Soft).alignment = TextAlignmentOptions.Center;
            root.gameObject.SetActive(false);
        }

        // ── 획득 (S07 × 0.765) ──────────────────────────────

        private static void Reward(Transform box)
        {
            var root = Node(box, "StoreReward", 0, 0, 720, 1280);
            Stretch(root);
            root.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();   // 창은 가운데 그대로(ScreenFit 이 줄마다 위아래로 찢지 않게)
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0.03f, 0.55f);
            dim.raycastTarget = true;
            Sliced(root, "Body", $"{St}/popup_plate_wide.png", 27, 252, 692, 1006, 2f);
            Pic(root, "Ghost", "Assets/BaseResource/Summon/summon_ghost.png", 55, 228, 235, 390);
            var title = Title(root, "StoreRewardTitle", 240, 290, 300, 80, 66, Gold);
            title.alignment = TextAlignmentOptions.Center;
            float[][] cells =
            {
                new float[] { 52, 405, 233, 593 }, new float[] { 264, 405, 444, 593 }, new float[] { 476, 405, 664, 593 },
                new float[] { 60, 650, 249, 822 }, new float[] { 266, 650, 452, 822 }, new float[] { 472, 650, 661, 822 },
            };
            for (int i = 0; i < 6; i++)
            {
                var c = cells[i];
                var cell = Sliced(root, $"StoreRewardCell{i}", $"{St}/reward_cell.png", c[0], c[1], c[2], c[3], 2.4f);
                float w = c[2] - c[0], h = c[3] - c[1];
                var ic = Pic(cell.transform, "CellIcon", null, 0, 0, 0, 0);
                Local(ic.rectTransform, 0, 16, w - 50, h - 80);
                var lb = Title(cell.transform, "CellLabel", 0, 0, 0, 0, 24, Color.white);
                Local(lb.rectTransform, 0, -h * 0.5f + 30, w - 18, 32);
            }
            var done = Title(root, "StoreRewardDone", 252, 866, 218, 44, 30, new Color(1f, 0.32f, 0.25f));
            done.alignment = TextAlignmentOptions.Center;
            var ok = Sliced(root, "StoreRewardOk", $"{St}/btn_confirm.png", 161, 910, 558, 987, 3f);
            ok.raycastTarget = true;
            Button(ok.gameObject);
            var ot = Title(ok.transform, "StoreRewardOkText", 0, 0, 0, 0, 38, Ink);
            Local(ot.rectTransform, 0, 0, 300, 54);
            Plain(ot);
            root.gameObject.SetActive(false);
        }

        // ── 연결 ────────────────────────────────────────────

        private static void Bind(LobbyShopPanel p)
        {
            var so = new SerializedObject(p);
            var faces = new List<(string, Sprite)>();
            if (Directory.Exists(Faces))
                foreach (var f in Directory.GetFiles(Faces, "face_*.png"))
                    faces.Add((Path.GetFileNameWithoutExtension(f).Substring(5), Spr(f.Replace('\\', '/'))));
            Keyed(so, "_faces", faces);
            Keyed(so, "_chests", new List<(string, Sprite)>
            {
                ("silver", Spr($"{Lobby}/chest_silver.png")), ("gold", Spr($"{Lobby}/chest_gold.png")),
                ("platinum", Spr($"{Lobby}/chest_magic.png")),
            });
            var prods = new List<(string, Sprite)>
            {
                ("card_small", Spr($"{Pr}/prod_card_small.png")), ("card_big", Spr($"{Pr}/prod_card_big.png")),
                ("starter_1", Spr($"{Pr}/prod_starter_1.png")), ("starter_2", Spr($"{Pr}/prod_starter_2.png")),
                ("starter_3", Spr($"{Pr}/prod_starter_3.png")),
                ("gold_s", Spr($"{Pr}/prod_gold_1.png")), ("gold_m", Spr($"{Pr}/prod_gold_2.png")), ("gold_l", Spr($"{Pr}/prod_gold_3.png")),
                ("stamina_refill", Spr($"{Pr}/prod_stamina.png")),
            };
            string[] gems = { "gem_60", "gem_320", "gem_680", "gem_2100", "gem_3600", "gem_7500" };
            for (int i = 0; i < gems.Length; i++) prods.Add((gems[i], Spr($"{Pr}/prod_gem_{i + 1}.png")));
            for (int c = 1; c <= 10; c++) prods.Add(($"chapter_{c:00}", Spr($"{Pr}/prod_chapter.png")));
            Keyed(so, "_products", prods);
            so.FindProperty("_gemIcon").objectReferenceValue = Spr($"{Pr}/prod_gem_1.png");
            so.FindProperty("_goldIcon").objectReferenceValue = Spr($"{Pr}/prod_gold_1.png");
            so.FindProperty("_goldPile").objectReferenceValue = Spr($"{Pr}/prod_gold_3.png");
            so.FindProperty("_ticketIcon").objectReferenceValue = Spr($"{Pr}/icon_ticket.png");
            so.FindProperty("_essenceIcon").objectReferenceValue = Spr($"{Pass}/micon_possess.png");
            so.FindProperty("_staminaIcon").objectReferenceValue = Spr($"{Pr}/prod_stamina.png");
            so.FindProperty("_tab").objectReferenceValue = Spr($"{St}/shop_tab.png");
            so.FindProperty("_starGold").objectReferenceValue = Spr($"{Ch}/ch_star_gold.png");
            so.FindProperty("_starSilver").objectReferenceValue = Spr($"{Ch}/ch_star_silver.png");
            so.FindProperty("_tabSelected").objectReferenceValue = Spr($"{St}/shop_tab_sel.png");
            so.FindProperty("_silhouette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UISilhouette.mat")
                ?? FindSilhouette();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material FindSilhouette()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material Silhouette"))
                return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            return null;
        }

        internal static void Keyed(SerializedObject so, string field, List<(string key, Sprite sprite)> list)
        {
            var p = so.FindProperty(field);
            p.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = list[i].key;
                e.FindPropertyRelative("Sprite").objectReferenceValue = list[i].sprite;
            }
        }

        // ── 9분할 테두리 ────────────────────────────────────

        private static void Borders()
        {
            B($"{St}/btn_price.png", 40); B($"{St}/btn_small.png", 40); B($"{St}/btn_confirm.png", 44);
            B($"{St}/btn_cancel.png", 40); B($"{St}/btn_rate.png", new Vector4(110, 34, 40, 34));
            B($"{St}/gem_slot.png", 44); B($"{St}/gold_slot.png", 44); B($"{St}/starter_slot.png", 44);
            B($"{St}/daily_slot.png", new Vector4(44, 80, 44, 44)); B($"{St}/daily_slot_free.png", new Vector4(44, 80, 44, 44));
            B($"{St}/sold_overlay.png", 44); B($"{St}/popup_plate_wide.png", 60); B($"{St}/shop_tab.png", 40);
            B($"{St}/shop_tab_sel.png", 40); B($"{St}/content_row.png", 34); B($"{St}/reward_cell.png", 50);
            B($"{St}/ribbon_open_now.png", new Vector4(90, 30, 30, 30)); B($"{St}/ribbon_first2x.png", new Vector4(36, 20, 36, 20));
            B($"{Nav}/top_stamina_plate.png", 40);
        }

        internal static void B(string path, float all) => B(path, new Vector4(all, all, all, all));

        internal static void B(string path, Vector4 b)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) { Debug.LogWarning($"[상점 창] 그림 없음: {path}"); return; }
            if (ti.spriteBorder == b) return;
            ti.spriteBorder = b;
            ti.SaveAndReimport();
        }

        // ── 도우미 ───────────────────────────────────────────

        internal static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        internal static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = Find(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        internal static RectTransform Node(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false);
            if (x1 > x0) Screen(r, x0, y0, x1, y1);
            return r;
        }

        internal static Image Pic(Transform parent, string name, string sprite, float x0, float y0, float x1, float y1)
        {
            var r = Node(parent, name, x0, y0, x1, y1);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? Spr(sprite) : null;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sprite != null && img.sprite == null) Debug.LogWarning($"[상점 창] 그림 없음: {sprite}");
            return img;
        }

        internal static Image Sliced(Transform parent, string name, string sprite, float x0, float y0, float x1, float y1, float ppu)
        {
            var img = Pic(parent, name, sprite, x0, y0, x1, y1);
            img.preserveAspect = false;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu;
            return img;
        }

        internal static TextMeshProUGUI Title(Transform parent, string name, float x, float cy, float w, float h, float size, Color color)
            => Text(s_title, parent, name, x, cy, w, h, size, color, true);

        internal static TextMeshProUGUI Body(Transform parent, string name, float x, float cy, float w, float h, float size, Color color)
            => Text(s_body, parent, name, x, cy, w, h, size, color, false);

        /// <summary>글자 칸 — (x, cy) 는 왼쪽 끝 · 가운데 y(화면 좌표). w = 0 이면 부모 기준 자리는 나중에 Local 로.</summary>
        internal static TextMeshProUGUI Text(TextMeshProUGUI template, Transform parent, string name, float x, float cy, float w, float h,
                                            float size, Color color, bool bold)
        {
            var go = Object.Instantiate(template.gameObject, parent, false);
            go.name = name;
            for (int i = go.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            var loc = go.GetComponent("LocalizedText");
            if (loc != null) Object.DestroyImmediate(loc);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = string.Empty;
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(8f, size * 0.5f);
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            if (bold) t.fontStyle = FontStyles.Bold;
            var r = t.rectTransform;
            if (w > 0f)
            {
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(x - 360f, 640f - cy);
                r.sizeDelta = new Vector2(w, h);
            }
            else
            {
                // 본(CHHostTitle)의 피벗이 왼쪽이라, 그대로 두면 Local 의 가운데 자리에서 오른쪽으로 삐져나간다
                r.pivot = new Vector2(0.5f, 0.5f);
            }
            return t;
        }

        /// <summary>노란 버튼 위 짙은 글자 — 외곽선을 빼야 회색으로 뭉개지지 않는다.</summary>
        internal static void Plain(TMP_Text t)
        {
            if (t.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText h) h.Set(0.1f, 0f, new Color32(0, 0, 0, 0));
            if (t.font != null && t.font.material != null) t.fontSharedMaterial = t.font.material;
        }

        internal static void Button(GameObject go)
        {
            if (go.GetComponent<Button>() == null) go.AddComponent<Button>().transition = Selectable.Transition.None;
        }

        internal static void Hit(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var r = Node(parent, name, x0, y0, x1, y1);
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            Button(r.gameObject);
        }

        // 화면 좌표(720 × 1280, 왼쪽 위) 상자 — 부모가 화면 크기(가운데 기준)일 때
        internal static void Screen(RectTransform r, float x0, float y0, float x1, float y1)
        {
            var parent = r.parent as RectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            var center = new Vector2((x0 + x1) * 0.5f - 360f, 640f - (y0 + y1) * 0.5f);
            // 부모가 화면 판이 아니면(행 · 칸 안) 부모의 화면 위치를 빼서 부모 기준으로 바꾼다
            if (parent != null && !IsScreen(parent)) center -= ScreenCenterOf(parent);
            r.anchoredPosition = center;
        }

        internal static Vector2 ScreenCenterOf(RectTransform r)
        {
            var sum = Vector2.zero;
            for (var t = r; t != null && !IsScreen(t); t = t.parent as RectTransform)
                sum += t.anchoredPosition;
            return sum;
        }

        /// <summary>화면 크기(720 × 1280, 가운데)인 판 — 그 안의 좌표는 화면 좌표 그대로다.</summary>
        internal static bool IsScreen(RectTransform r)
            => r.sizeDelta == new Vector2(720, 1280) && r.anchoredPosition == Vector2.zero
               || r.anchorMin == Vector2.zero && r.anchorMax == Vector2.one;

        internal static void C(RectTransform r, float cx, float cy, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(cx - 360f, 640f - cy);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>부모 가운데 기준 자리.</summary>
        internal static void Local(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        internal static void Pivot(RectTransform r, float x) => r.pivot = new Vector2(x, 0.5f);

        internal static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        internal static void StretchIn(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
