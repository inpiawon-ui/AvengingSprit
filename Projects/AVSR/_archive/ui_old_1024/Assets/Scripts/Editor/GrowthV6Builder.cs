using System.Collections.Generic;
using Game.Module.Common.UI;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.LobbyShopBuilder;

namespace Game.Editor
{
    /// <summary>
    /// 호스트 육성 탭 v6 — 시안 `Projects/AVSR/_exchange/in/mock_growth_v6_page.png` · `mock_growth_v6_starup.png` (PD 2026-10-16).
    /// 합친 안 `plan_growth_v6.md`: 몸 카드(등급 · 컨셉 · 전투력 · 성급 N/10 · 별) → 다음 성급 요약 띠 → 필살기 · 액티브 · 6성 각성 옵션 · 패시브
    /// → 능력치 강화 → 호스트 목록. 성급 표기는 은별 1~4 · 5성마다 큰 금별(StarRank).
    ///
    /// 유령 탭 · 탭 단추 · 아래 바는 그대로 두고 **호스트 판(`HostPage`)과 성급 창만** 새로 세운다 — `GrowthBuilder` · `FixedBarsBinder` 뒤에 돌린다.
    /// 판은 `GrowthPanel/Page`(탭이 있는 판, 위로 58 내리고 세로 0.9745 배) **앞**에 둔다 — 탭이 판 위에 그려진다.
    /// 성급 창은 `Page` **뒤**(맨 위)에 둔다 — 탭까지 어둡게 덮는다.
    ///
    /// 좌표: 시안(941 × 1672) × 0.765. 위 막대 · 탭 자리가 시안과 달라(탭 화면 y 190~282) 세로는 탭 밑(280)부터
    /// 시안의 몸 카드 위(시안 y 209)를 맞추고 0.958 배로 줄여 아래 바(1170) 위에 넣는다.
    /// </summary>
    public static class GrowthV6Builder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string St = "Assets/BaseResource/Store";
        private const string Su = "Assets/BaseResource/Summon";
        private const string Gr = "Assets/BaseResource/Growth";
        private const string Icons = "Assets/BaseResource/HostSelectPanel";

        private const float K = 0.765f, Top = 280f, MockTop = 209f, SqY = 0.958f;
        public const int StatRows = 8;
        public const float RowPitchMock = 38f;
        public const float CardPitchMock = 175f;

        private static readonly Color Gold = new(1f, 0.85f, 0.32f, 1f);
        private static readonly Color Soft = new(0.78f, 0.86f, 1f, 1f);
        private static readonly Color Ink = new(0.16f, 0.08f, 0.03f, 1f);
        private static readonly Color UltRed = new(1f, 0.62f, 0.55f, 1f);
        private static readonly Color ActCyan = new(0.45f, 0.95f, 1f, 1f);

        // 시안 좌표 → 화면 좌표
        private static float X(float mx) => mx * K;
        private static float Y(float my) => Top + (my * K - MockTop) * SqY;

        private static RectTransform N(Transform p, string name, float x0, float y0, float x1, float y1)
            => Node(p, name, X(x0), Y(y0), X(x1), Y(y1));

        private static Image P(Transform p, string name, string sprite, float x0, float y0, float x1, float y1)
            => Pic(p, name, sprite, X(x0), Y(y0), X(x1), Y(y1));

        private static Image S(Transform p, string name, string sprite, float x0, float y0, float x1, float y1, float ppu)
            => Sliced(p, name, sprite, X(x0), Y(y0), X(x1), Y(y1), ppu);

        /// <summary>글자 — 시안 왼쪽 x0 · 오른쪽 x1 · 가운데 y.</summary>
        private static TextMeshProUGUI TT(Transform p, string name, float x0, float x1, float cy, float size, Color c,
                                          bool bold, TextAlignmentOptions align, float h = 0f)
        {
            float hh = h > 0f ? h : size * 1.35f;
            var t = bold ? Title(p, name, X(x0), Y(cy), X(x1) - X(x0), hh, size, c)
                         : Body(p, name, X(x0), Y(cy), X(x1) - X(x0), hh, size, c);
            t.alignment = align;
            t.richText = true;
            return t;
        }

        [MenuItem("Tools/Game/육성 — 호스트 탭 v6 세우기 (mock_growth_v6)")]
        public static void Run()
        {
            Borders();
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[육성 v6] 글자 본이 없다 — 챕터 창부터 세운다"); return; }
                var growth = lobby.transform.Find("GrowthPanel");
                var page = growth != null ? growth.Find("Page") : null;
                if (page == null) { Debug.LogError("[육성 v6] GrowthPanel/Page 없음 — 육성 빌더부터"); return; }
                foreach (var t in new[] { page.Find("HostPage"), growth.Find("HostPage"), growth.Find("H6StarUpWindow") })
                    if (t != null) Object.DestroyImmediate(t.gameObject);

                var hp = new GameObject("HostPage", typeof(RectTransform)).GetComponent<RectTransform>();
                hp.gameObject.layer = lobby.layer;
                hp.SetParent(growth, false);
                hp.SetSiblingIndex(page.GetSiblingIndex());
                // 위에 붙인다 — 탭(Page)이 위에 붙어 있다. 가운데에 두면 긴 화면에서 판만 아래로 내려가 탭과 벌어진다
                hp.anchorMin = hp.anchorMax = hp.pivot = new Vector2(0.5f, 1f);
                hp.anchoredPosition = Vector2.zero;
                hp.sizeDelta = new Vector2(720, 1280);
                hp.gameObject.AddComponent<ScreenFitLock>();

                var bg = Pic(hp, "H6Bg", $"{Ch}/ch_screen_s22e.png", 0, 0, 0, 0);
                bg.preserveAspect = false;
                bg.raycastTarget = true;
                float k = 720f / 941f;
                C(bg.rectTransform, 360, 645, bg.sprite.rect.width * k, bg.sprite.rect.height * k);

                BodyCard(hp);
                Summary(hp);
                Skills(hp);
                Stats(hp);
                List(hp);
                var win = StarUpWindow(growth);
                win.SetAsLastSibling();
                win.gameObject.SetActive(false);

                Bind(growth.GetComponent<GrowthPanel>());
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[육성 v6] 세움");
        }

        // ── 몸 카드 ──────────────────────────────────────────

        private static void BodyCard(Transform hp)
        {
            S(hp, "H6CardPlate", $"{Ch}/ch_s22e_box.png", 22, 273, 918, 518, 2f);
            var portrait = P(hp, "H6Portrait", null, 34, 282, 308, 510);
            portrait.preserveAspect = true;
            P(hp, "H6Grade", $"{Ch}/ch_grade_b.png", 40, 290, 104, 342);
            TT(hp, "H6Name", 320, 620, 320, 34, Color.white, true, TextAlignmentOptions.Left);
            TT(hp, "H6Concept", 330, 620, 374, 18, Soft, false, TextAlignmentOptions.Left);
            P(hp, "H6PowerIcon", $"{Gr}/icon_atk.png", 332, 410, 370, 448);
            TT(hp, "H6PowerText", 378, 620, 429, 26, Color.white, true, TextAlignmentOptions.Left);

            // 성급 칸 — 「성급 3/10」 · 별 · 조각 막대 · 성급 UP
            TT(hp, "H6RankLabel", 640, 900, 305, 20, Color.white, true, TextAlignmentOptions.Center);
            var stars = N(hp, "H6Stars", 640, 322, 900, 368);
            for (int i = 0; i < 5; i++) Pic(stars, $"H6Star{i}", $"{Ch}/ch_star_silver.png", 0, 0, 0, 0);
            P(hp, "H6ShardIcon", $"{Gr}/shard_icon.png", 636, 374, 680, 414);
            var bar = S(hp, "H6ShardBar", $"{Su}/pity_bar_bg.png", 686, 378, 896, 410, 3f);
            var fill = Pic(bar.transform, "H6ShardFill", $"{Su}/pity_bar_fill.png", 0, 0, 0, 0);
            StretchIn(fill.rectTransform);
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var sv = Title(bar.transform, "H6ShardText", 0, 0, 0, 0, 18, Color.white);
            Local(sv.rectTransform, 0, 0, X(896) - X(686) - 10, 22);

            var btn = S(hp, "H6StarUpButton", $"{St}/btn_confirm.png", 632, 424, 902, 508, 3f);
            btn.raycastTarget = true;
            Button(btn.gameObject);
            float bw = X(902) - X(632);
            var bt = Title(btn.transform, "H6StarUpText", 0, 0, 0, 0, 24, Ink);
            Local(bt.rectTransform, 0, 13, bw - 20, 28);
            Plain(bt);
            var coin = Pic(btn.transform, "H6StarUpCoin", $"{Ch}/ch_icon_coin.png", 0, 0, 0, 0);
            Local(coin.rectTransform, -34, -14, 22, 22);
            var cost = Title(btn.transform, "H6StarUpCost", 0, 0, 0, 0, 22, Ink);
            Pivot(cost.rectTransform, 0f);
            Local(cost.rectTransform, -20, -14, 90, 26);
            cost.alignment = TextAlignmentOptions.Left;
            Plain(cost);
        }

        private static void Summary(Transform hp)
        {
            S(hp, "H6SummaryPlate", $"{Ch}/ch_s22e_box.png", 22, 524, 918, 568, 2f);
            P(hp, "H6SummaryMark", $"{Ch}/ch_s22e_tick.png", 40, 532, 52, 560);
            TT(hp, "H6SummaryText", 60, 900, 546, 19, Color.white, true, TextAlignmentOptions.Center);
        }

        // ── 스킬 — 필살기 · 액티브 · 6성 각성 옵션 · 패시브 ───────────

        private static void Skills(Transform hp)
        {
            // 필살기 — 가장 크게 · 금 · 붉은 테
            S(hp, "H6UltPlate", $"{Su}/pull_plate_10.png", 22, 576, 918, 712, 2.4f);
            P(hp, "H6UltIcon", null, 46, 588, 172, 702);
            Tag(hp, "H6UltTag", "H6UltTagText", 190, 586, 432, 616, UltRed);
            TT(hp, "H6UltName", 196, 488, 642, 32, Color.white, true, TextAlignmentOptions.Left);
            TT(hp, "H6UltCond", 198, 488, 682, 18, Soft, false, TextAlignmentOptions.Left);
            TT(hp, "H6UltPower", 492, 906, 644, 21, Color.white, true, TextAlignmentOptions.Left);
            TT(hp, "H6UltDesc", 492, 906, 683, 17, Soft, false, TextAlignmentOptions.Left);
            Hit(hp, "H6UltHit", X(22), Y(576), X(918), Y(712));

            // 액티브
            S(hp, "H6ActPlate", $"{Ch}/ch_s22e_box.png", 22, 716, 918, 822, 2f);
            P(hp, "H6ActIcon", null, 46, 726, 166, 814);
            Tag(hp, "H6ActTag", "H6ActTagText", 180, 722, 378, 750, ActCyan);
            TT(hp, "H6ActName", 196, 418, 769, 28, Color.white, true, TextAlignmentOptions.Left);
            TT(hp, "H6ActCond", 198, 418, 801, 16, Soft, false, TextAlignmentOptions.Left);
            TT(hp, "H6ActPower", 420, 716, 767, 19, Color.white, true, TextAlignmentOptions.Left);
            TT(hp, "H6ActDesc", 420, 716, 801, 16, Soft, false, TextAlignmentOptions.Left);
            var step = S(hp, "H6ActStep", $"{St}/btn_small.png", 728, 726, 904, 814, 3f);
            float sw = X(904) - X(728);
            var st = Title(step.transform, "H6ActStepTitle", 0, 0, 0, 0, 18, ActCyan);
            Local(st.rectTransform, 0, 15, sw - 14, 24);
            var sv = Body(step.transform, "H6ActStepValue", 0, 0, 0, 0, 16, Color.white);
            Local(sv.rectTransform, 0, -14, sw - 14, 22);
            sv.richText = true;
            Hit(hp, "H6ActHit", X(22), Y(716), X(718), Y(822));

            // 6성 각성 옵션
            S(hp, "H6OptPlate", $"{St}/content_row.png", 22, 828, 918, 913, 3f);
            P(hp, "H6OptLock", $"{Ch}/ch_lock.png", 74, 842, 126, 902);
            Tag(hp, "H6OptTag", "H6OptTagText", 184, 838, 470, 870, Gold);
            TT(hp, "H6OptText", 196, 906, 891, 18, Color.white, false, TextAlignmentOptions.Left);

            // 패시브
            S(hp, "H6PasPlate", $"{Ch}/ch_s22e_box.png", 22, 918, 918, 1013, 2f);
            P(hp, "H6PasIcon", null, 46, 928, 160, 1006);
            Tag(hp, "H6PasTag", "H6PasTagText", 178, 924, 378, 952, new Color(0.55f, 0.75f, 1f));
            TT(hp, "H6PasName", 196, 452, 972, 28, Color.white, true, TextAlignmentOptions.Left);
            TT(hp, "H6PasPower", 455, 906, 966, 20, Color.white, true, TextAlignmentOptions.Left);
            TT(hp, "H6PasDesc", 455, 906, 997, 16, Soft, false, TextAlignmentOptions.Left);
            Hit(hp, "H6PasHit", X(22), Y(918), X(918), Y(1013));
        }

        private static void Tag(Transform hp, string name, string textName, float x0, float y0, float x1, float y1, Color c)
        {
            var tag = S(hp, name, $"{St}/btn_small.png", x0, y0, x1, y1, 4f);
            var t = Title(tag.transform, textName, 0, 0, 0, 0, 16, c);
            Local(t.rectTransform, 0, 0, X(x1) - X(x0) - 12, (Y(y1) - Y(y0)) - 2);
        }

        // ── 능력치 강화 ──────────────────────────────────────

        private static void Stats(Transform hp)
        {
            S(hp, "H6StatPlate", $"{Ch}/ch_s22e_box.png", 22, 1023, 918, 1265, 2f);
            P(hp, "H6StatMark", $"{Ch}/ch_s22e_tick.png", 38, 1032, 50, 1060);
            TT(hp, "H6StatHeader", 58, 300, 1046, 24, Color.white, true, TextAlignmentOptions.Left);
            var help = P(hp, "H6StatHelp", $"{Gr}/help.png", 252, 1032, 282, 1062);
            help.raycastTarget = true;
            Button(help.gameObject);
            TT(hp, "H6CapText", 560, 896, 1047, 18, Gold, true, TextAlignmentOptions.Right);

            var view = N(hp, "H6StatViewport", 30, 1064, 910, 1258);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            var content = new GameObject("H6StatContent", typeof(RectTransform)).GetComponent<RectTransform>();
            content.gameObject.layer = view.gameObject.layer;
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0, RowPitchMock * K * SqY * StatRows);
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // 줄 견본 하나 — 런타임이 8줄로 복제한다(GrowthPanel.HostPage)
            float vw = X(910) - X(30), rh = 35f * K * SqY;
            var row = new GameObject("H6Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.gameObject.layer = content.gameObject.layer;
            row.SetParent(content, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0, -2);
            row.sizeDelta = new Vector2(vw - 8, rh);
            var plate = Sliced(row, "Plate", $"{St}/content_row.png", 0, 0, 0, 0, 4f);
            StretchIn(plate.rectTransform);
            float l = -(vw - 8) * 0.5f;
            var icon = Pic(row, "H6RowIcon", $"{Gr}/icon_atk.png", 0, 0, 0, 0);
            Local(icon.rectTransform, l + 26, 0, rh - 4, rh - 4);
            var nm = Body(row, "H6RowName", 0, 0, 0, 0, 16, Color.white);
            Pivot(nm.rectTransform, 0f);
            Local(nm.rectTransform, l + 64, 0, 150, rh);
            nm.alignment = TextAlignmentOptions.Left;
            var lv = Body(row, "H6RowLv", 0, 0, 0, 0, 16, Color.white);
            Pivot(lv.rectTransform, 0f);
            Local(lv.rectTransform, l + 236, 0, 120, rh);
            lv.alignment = TextAlignmentOptions.Left;
            lv.richText = true;
            var pct = Title(row, "H6RowPct", 0, 0, 0, 0, 16, Color.white);
            Pivot(pct.rectTransform, 0f);
            Local(pct.rectTransform, l + 392, 0, 110, rh);
            pct.alignment = TextAlignmentOptions.Left;
            var b = Sliced(row, "H6RowButton", $"{Gr}/v5/h5_gold_button.png", 0, 0, 0, 0, 3f);
            b.type = Image.Type.Simple;
            b.preserveAspect = false;
            float bh = rh - 2, bwid = bh * 4.94f;   // 그림 비율(4.94) 그대로
            Local(b.rectTransform, (vw - 8) * 0.5f - bwid * 0.5f - 6, 0, bwid, bh);
            b.raycastTarget = true;
            Button(b.gameObject);
            var bc = Pic(b.transform, "H6RowCoin", $"{Ch}/ch_icon_coin.png", 0, 0, 0, 0);
            Local(bc.rectTransform, -bwid * 0.5f + 18, 0, rh - 8, rh - 8);
            var ct = Title(b.transform, "H6RowCost", 0, 0, 0, 0, 16, Ink);
            Local(ct.rectTransform, 12, 0, 80, rh - 4);
            Plain(ct);
        }

        // ── 호스트 목록 ──────────────────────────────────────

        private static void List(Transform hp)
        {
            S(hp, "H6ListPlate", $"{Ch}/ch_s22e_box.png", 22, 1274, 918, 1483, 2f);
            P(hp, "H6ListMark", $"{Ch}/ch_s22e_tick.png", 38, 1284, 50, 1312);
            TT(hp, "H6ListHeader", 58, 360, 1298, 24, Color.white, true, TextAlignmentOptions.Left);
            var sort = S(hp, "H6SortButton", $"{St}/btn_small.png", 742, 1284, 900, 1316, 3f);
            sort.raycastTarget = true;
            Button(sort.gameObject);
            var stx = Body(sort.transform, "H6SortText", 0, 0, 0, 0, 16, Color.white);
            Local(stx.rectTransform, 0, 0, X(900) - X(742) - 12, 22);

            var view = N(hp, "H6ListViewport", 32, 1318, 908, 1480);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            var content = new GameObject("H6ListContent", typeof(RectTransform)).GetComponent<RectTransform>();
            content.gameObject.layer = view.gameObject.layer;
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(1000, 0);
            scroll.content = content;
            scroll.viewport = view;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            float cw = 170f * K, chh = 154f * K * SqY;
            var card = new GameObject("H6HostCard", typeof(RectTransform)).GetComponent<RectTransform>();
            card.gameObject.layer = content.gameObject.layer;
            card.SetParent(content, false);
            card.anchorMin = card.anchorMax = new Vector2(0f, 0.5f);
            card.pivot = new Vector2(0f, 0.5f);
            card.anchoredPosition = new Vector2(4f, 0f);
            card.sizeDelta = new Vector2(cw, chh);
            var hitImg = card.gameObject.AddComponent<Image>();
            hitImg.color = new Color(1f, 1f, 1f, 0f);
            Button(card.gameObject);
            var frame = Sliced(card, "CardFrame", $"{Ch}/ch_card_frame_b.png", 0, 0, 0, 0, 2.2f);
            StretchIn(frame.rectTransform);
            var sel = Sliced(card, "CardFrameSel", $"{Ch}/ch_card_sel_overlay.png", 0, 0, 0, 0, 2f);
            StretchIn(sel.rectTransform);
            var thumb = Pic(card, "CardThumb", null, 0, 0, 0, 0);
            Local(thumb.rectTransform, 0, 14, cw - 16, chh - 52);
            var grade = Pic(card, "CardGrade", $"{Ch}/ch_grade_b.png", 0, 0, 0, 0);
            Local(grade.rectTransform, -cw * 0.5f + 20, chh * 0.5f - 14, 30, 24);
            var name = Body(card, "CardName", 0, 0, 0, 0, 14, Color.white);
            Local(name.rectTransform, 0, -chh * 0.5f + 32, cw - 10, 18);
            var rank = Title(card, "CardRank", 0, 0, 0, 0, 14, Gold);
            Pivot(rank.rectTransform, 0f);
            Local(rank.rectTransform, -cw * 0.5f + 8, -chh * 0.5f + 13, 34, 18);
            rank.alignment = TextAlignmentOptions.Left;
            // 이름은 H6CardStar — 「CardStar0」 은 유령 탭 카드 별 이름이라 GrowthPanel 이 이름으로 끄고 켤 때 겹친다
            for (int i = 0; i < 5; i++) Pic(card, $"H6CardStar{i}", $"{Ch}/ch_star_silver.png", 0, 0, 0, 0);
            sel.gameObject.SetActive(false);
        }

        // ── 성급 올리기 창 ───────────────────────────────────

        public const int WinRows = 9;
        public const float WinRowH = 50f, WinRowGap = 4f;

        private static RectTransform StarUpWindow(Transform growth)
        {
            var root = new GameObject("H6StarUpWindow", typeof(RectTransform)).GetComponent<RectTransform>();
            root.gameObject.layer = growth.gameObject.layer;
            root.SetParent(growth, false);
            Stretch(root);
            root.gameObject.AddComponent<ScreenFitLock>();
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0.03f, 0.62f);
            dim.raycastTarget = true;
            var dimHit = Node(root, "H6Dim", 0, 0, 720, 1280);
            Stretch(dimHit);
            var dh = dimHit.gameObject.AddComponent<Image>();
            dh.color = new Color(1f, 1f, 1f, 0f);
            Button(dimHit.gameObject);

            // 창 — 가운데 기준, 높이는 줄 수에 따라 런타임이 맞춘다(H6WinBody)
            var win = new GameObject("H6Window", typeof(RectTransform)).GetComponent<RectTransform>();
            win.gameObject.layer = root.gameObject.layer;
            win.SetParent(root, false);
            win.anchorMin = win.anchorMax = new Vector2(0.5f, 0.5f);
            win.pivot = new Vector2(0.5f, 1f);
            win.anchoredPosition = new Vector2(0f, 470f);
            win.sizeDelta = new Vector2(560f, 900f);
            var body = Sliced(win, "H6WinBody", $"{St}/popup_plate_wide.png", 0, 0, 0, 0, 2f);
            StretchIn(body.rectTransform);
            body.raycastTarget = true;

            T(win, "H6WinTitle", 0, -48, 440, 50, 38, Color.white, true);
            var face = Pic(win, "H6WinFace", null, 0, 0, 0, 0);
            TL(face.rectTransform, 40, 92, 116, 116);
            T(win, "H6WinRank", 92, -122, 330, 56, 44, Gold, true);
            // 「1성 → 2성」 아래에 지금 별 → 다음 별(가운데 372 기준 좌우)
            var now = N0(win, "H6WinStarsNow", 200, 158, 150, 36);
            var next = N0(win, "H6WinStarsNext", 394, 158, 150, 36);
            for (int i = 0; i < 5; i++)
            {
                Pic(now, $"WStar{i}", $"{Ch}/ch_star_silver.png", 0, 0, 0, 0);
                Pic(next, $"WStar{i}", $"{Ch}/ch_star_silver.png", 0, 0, 0, 0);
            }
            var arrow = Title(win, "H6WinArrow", 0, 0, 0, 0, 24, Color.white);
            TL(arrow.rectTransform, 357, 160, 30, 32);
            arrow.text = "→";
            var rises = Title(win, "H6WinRises", 0, 0, 0, 0, 24, Color.white);
            TL(rises.rectTransform, 40, 222, 480, 32);
            rises.alignment = TextAlignmentOptions.Left;

            // 함께 오르는 줄 — 9칸(해당 없는 줄은 런타임이 숨기고 당긴다)
            for (int i = 0; i < WinRows; i++)
            {
                var r = Sliced(win, $"H6WinRow{i}", $"{St}/content_row.png", 0, 0, 0, 0, 3f);
                TL(r.rectTransform, 30, 262 + i * (WinRowH + WinRowGap), 500, WinRowH);
                var ic = Pic(r.transform, "Icon", null, 0, 0, 0, 0);
                Local(ic.rectTransform, -222, 0, 40, 40);
                var lb = Title(r.transform, "Label", 0, 0, 0, 0, 19, Color.white);
                Pivot(lb.rectTransform, 0f);
                Local(lb.rectTransform, -192, 0, 190, 40);
                lb.alignment = TextAlignmentOptions.Left;
                var v = Title(r.transform, "Value", 0, 0, 0, 0, 20, Color.white);
                Pivot(v.rectTransform, 1f);
                Local(v.rectTransform, 238, 0, 240, 40);
                v.alignment = TextAlignmentOptions.Right;
                v.richText = true;
            }
            // 줄 왼쪽을 잇는 노란 세로선(시안) — 런타임이 길이를 맞춘다
            var line = Pic(win, "H6WinLine", $"{Ch}/ch_s22e_vline.png", 0, 0, 0, 0);
            line.preserveAspect = false;
            line.type = Image.Type.Sliced;   // 세로로 늘려 쓰는 선 — 위아래 끝만 남기고 가운데를 늘린다
            line.color = Color.white;   // 선 그림 색 그대로(금으로 물들이면 청록 선이 초록으로 탁해진다)
            TL(line.rectTransform, 84, 270, 4, 400);

            // 아래 묶음 — 줄 수에 따라 런타임이 올린다
            var foot = new GameObject("H6WinFoot", typeof(RectTransform)).GetComponent<RectTransform>();
            foot.gameObject.layer = win.gameObject.layer;
            foot.SetParent(win, false);
            TL(foot, 0, 760, 560, 140);
            var shard = Sliced(foot, "H6WinShardPlate", $"{St}/btn_small.png", 0, 0, 0, 0, 3f);
            TL(shard.rectTransform, 40, 6, 230, 44);
            var si = Pic(shard.transform, "Icon", $"{Gr}/shard_icon.png", 0, 0, 0, 0);
            Local(si.rectTransform, -80, 0, 32, 32);
            var stx = Title(shard.transform, "H6WinShardText", 0, 0, 0, 0, 22, Color.white);
            Local(stx.rectTransform, 18, 0, 170, 32);
            stx.richText = true;
            var gold = Sliced(foot, "H6WinGoldPlate", $"{St}/btn_small.png", 0, 0, 0, 0, 3f);
            TL(gold.rectTransform, 290, 6, 230, 44);
            var gi = Pic(gold.transform, "Icon", $"{Ch}/ch_icon_coin.png", 0, 0, 0, 0);
            Local(gi.rectTransform, -80, 0, 30, 30);
            var gtx = Title(gold.transform, "H6WinGoldText", 0, 0, 0, 0, 22, Color.white);
            Local(gtx.rectTransform, 18, 0, 170, 32);
            gtx.richText = true;
            var cancel = Sliced(foot, "H6WinCancel", $"{St}/btn_cancel.png", 0, 0, 0, 0, 3f);
            TL(cancel.rectTransform, 40, 60, 210, 58);
            cancel.raycastTarget = true;
            Button(cancel.gameObject);
            var ctx = Title(cancel.transform, "H6WinCancelText", 0, 0, 0, 0, 26, Color.white);
            Local(ctx.rectTransform, 0, 0, 190, 40);
            var ok = Sliced(foot, "H6WinConfirm", $"{St}/btn_confirm.png", 0, 0, 0, 0, 3f);
            TL(ok.rectTransform, 266, 58, 254, 62);
            ok.raycastTarget = true;
            Button(ok.gameObject);
            var otx = Title(ok.transform, "H6WinConfirmText", 0, 0, 0, 0, 28, Ink);
            Local(otx.rectTransform, 0, 0, 230, 42);
            Plain(otx);
            var nextMile = Body(foot, "H6WinNext", 0, 0, 0, 0, 16, Soft);
            TL(nextMile.rectTransform, 30, 124, 500, 24);
            nextMile.alignment = TextAlignmentOptions.Center;
            nextMile.richText = true;
            return root;
        }

        /// <summary>창 안 왼쪽 위 기준 자리(창 피벗이 위 가운데).</summary>
        private static void TL(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            r.sizeDelta = new Vector2(w, h);
        }

        private static RectTransform N0(Transform p, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer = p.gameObject.layer;
            r.SetParent(p, false);
            TL(r, x, y, w, h);
            return r;
        }

        private static TextMeshProUGUI T(Transform p, string name, float x, float y, float w, float h, float size, Color c, bool bold)
        {
            var t = bold ? Title(p, name, 0, 0, 0, 0, size, c) : Body(p, name, 0, 0, 0, 0, size, c);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            t.richText = true;
            return t;
        }

        // ── 연결 ────────────────────────────────────────────

        private static void Bind(GrowthPanel gp)
        {
            var so = new SerializedObject(gp);
            so.FindProperty("_starGold").objectReferenceValue = Spr($"{Ch}/ch_star_gold.png");
            so.FindProperty("_starSilver").objectReferenceValue = Spr($"{Ch}/ch_star_silver.png");
            Arr(so, "_gradeBadges", $"{Ch}/ch_grade_b.png", $"{Ch}/ch_grade_a.png", $"{Ch}/ch_grade_s.png");
            Arr(so, "_gradeFrames", $"{Ch}/ch_card_frame_b.png", $"{Ch}/ch_card_frame_a.png", $"{Ch}/ch_card_frame_s.png");
            so.FindProperty("_h6RowPitch").floatValue = RowPitchMock * K * SqY;
            so.FindProperty("_h6CardPitch").floatValue = CardPitchMock * K;
            // 몸마다 필살기 아이콘 — 액티브(Skill) 칸은 GrowthBuilder 가 채운 ultimateicon_ 그대로
            var arts = so.FindProperty("_hostArts");
            for (int i = 0; i < arts.arraySize; i++)
            {
                var e = arts.GetArrayElementAtIndex(i);
                string key = e.FindPropertyRelative("Key").stringValue;
                e.FindPropertyRelative("Ult").objectReferenceValue = Spr($"{Icons}/ulticon_{key}.png");
                e.FindPropertyRelative("Skill").objectReferenceValue = Spr($"{Icons}/ultimateicon_{key}.png");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Arr(SerializedObject so, string field, params string[] paths)
        {
            var p = so.FindProperty(field);
            p.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = Spr(paths[i]);
        }

        private static void Borders()
        {
            B($"{Su}/pull_plate_10.png", 60);
            B($"{St}/content_row.png", new Vector4(40, 30, 40, 30));
            B($"{St}/btn_small.png", 40);
            B($"{Ch}/ch_s22e_box.png", 40);
            B($"{Ch}/ch_s22e_vline.png", new Vector4(0, 20, 0, 20));
        }
    }
}
