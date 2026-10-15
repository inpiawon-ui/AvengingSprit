using System.Collections.Generic;
using System.IO;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.LobbyShopBuilder;

namespace Game.Editor
{
    /// <summary>
    /// 소환 창(`SummonPanel`) — 시안 `mock_summon_a`(× 0.765) · 결과 S13 · 연출 S09~S11.
    /// 그림은 납품 부품(`Assets/BaseResource/Summon` · `SummonFx` · `Store`). 순서: 로비 바인더 · 상점 창 뒤.
    /// </summary>
    public static class SummonBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Su = "Assets/BaseResource/Summon";
        private const string Fx = "Assets/BaseResource/SummonFx";
        private const string St = "Assets/BaseResource/Store";
        private const string Pr = "Assets/BaseResource/Store/Products";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string Nav = "Assets/BaseResource/LobbyMainUI/Nav";
        private const string Lobby = "Assets/BaseResource/LobbyMainUI";
        private const string Faces = "Assets/BaseResource/Growth/Faces";
        private const string Portraits = "Assets/BaseResource/Growth/Portraits";

        private static readonly Color Ink = new(0.16f, 0.08f, 0.03f, 1f);
        private static readonly Color Soft = new(0.85f, 0.82f, 0.98f, 1f);
        private static readonly Color Gold = new(1f, 0.85f, 0.32f, 1f);
        private static readonly Color Pink = new(0.98f, 0.62f, 1f, 1f);

        [MenuItem("Tools/Game/로비 — 소환 창 세우기 (mock_summon_a · S13)")]
        public static void Run()
        {
            Borders();
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[소환 창] 글자 본이 없다 — 챕터 창부터 세운다"); return; }
                var old = lobby.transform.Find("SummonPanel");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var panel = new GameObject("SummonPanel", typeof(RectTransform)).GetComponent<RectTransform>();
                panel.gameObject.layer = lobby.layer;
                panel.SetParent(lobby.transform, false);
                Stretch(panel);
                var nav = lobby.transform.Find("BottomNav");
                if (nav != null) panel.SetSiblingIndex(nav.GetSiblingIndex());

                var box = new GameObject("SummonContent", typeof(RectTransform)).GetComponent<RectTransform>();
                box.gameObject.layer = lobby.layer;
                box.SetParent(panel, false);
                box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
                box.sizeDelta = new Vector2(720, 1280);
                box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

                Main(box);
                // 연출 · 결과 · 확률 창은 SummonContent 옆(형제)에 둔다 — 하네스 모달 규칙이 위 창을 고르고, 어두운 판이 4:3 양옆까지 덮게
                FxLayer(panel);
                Result(panel);
                Info(panel);

                var comp = panel.gameObject.AddComponent<SummonPanel>();
                Bind(comp);
                var main = new SerializedObject(lobby.GetComponent<LobbyMainUI>());
                main.FindProperty("_summonPanel").objectReferenceValue = comp;
                main.ApplyModifiedPropertiesWithoutUndo();
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[소환 창] 세움");
        }

        // ── 본 화면 ──────────────────────────────────────────

        private static void Main(Transform box)
        {
            // 배경 — 밤 배관 판을 보랏빛으로 + 제단 그림(시안의 보라 · 검붉은 제단 판)
            var bg = Pic(box, "SummonBackdrop", $"{Ch}/ch_screen_s22e.png", 0, 0, 0, 0);
            bg.preserveAspect = false;
            bg.raycastTarget = true;
            bg.color = new Color(0.72f, 0.52f, 0.95f, 1f);
            float k = 720f / 941f;
            C(bg.rectTransform, 360, 645, bg.sprite.rect.width * k, bg.sprite.rect.height * k);
            var altar = Pic(box, "SummonAltar", $"{Su}/summon_bg.png", 0, 0, 0, 0);
            altar.preserveAspect = false;
            C(altar.rectTransform, 360, 700, 1054 * 0.86f, 677 * 0.86f);

            Pic(box, "SummonTitleIcon", $"{Nav}/nav_icon_summon.png", 34, 126, 104, 196);
            var t = Title(box, "SummonTitleText", 112, 163, 200, 54, 40, Color.white);
            t.alignment = TextAlignmentOptions.Left;

            // 픽업 띠
            Sliced(box, "GachaPickupBanner", $"{Su}/pickup_plate.png", 23, 197, 698, 444, 1.8f);
            Pic(box, "PickupArt", null, 300, 214, 600, 440);
            Title(box, "PickupLabelText", 46, 240, 320, 40, 30, Color.white).alignment = TextAlignmentOptions.Left;
            Title(box, "PickupNameText", 44, 296, 340, 70, 52, Pink).alignment = TextAlignmentOptions.Left;
            Body(box, "PickupNoteText", 46, 360, 330, 26, 18, Color.white).alignment = TextAlignmentOptions.Left;
            var tp = Sliced(box, "PickupTimePlate", $"{St}/btn_small.png", 42, 383, 272, 426, 3f);
            var ck = Pic(tp.transform, "Clock", $"{Lobby}/chesttimeicon.png", 0, 0, 0, 0);
            Local(ck.rectTransform, -88, 0, 28, 28);
            var tt = Body(tp.transform, "PickupTimeText", 0, 0, 0, 0, 19, Color.white);
            Local(tt.rectTransform, 14, 0, 180, 30);

            // 소환권
            var tk = Sliced(box, "GachaTicketPlate", $"{Nav}/top_stamina_plate.png", 438, 461, 679, 514, 3f);
            var ti = Pic(tk.transform, "TicketIcon", $"{Pr}/icon_ticket.png", 0, 0, 0, 0);
            Local(ti.rectTransform, -86, 0, 50, 40);
            var tx = Body(tk.transform, "GachaTicketText", 0, 0, 0, 0, 20, Color.white);
            Local(tx.rectTransform, 20, 0, 170, 30);

            // 제단 위 유령 · 마법진
            Pic(box, "SummonCircle", $"{Su}/summon_circle.png", 200, 686, 520, 778);
            Pic(box, "SummonGhost", $"{Su}/summon_ghost.png", 236, 520, 486, 690);

            // 뽑기 버튼 셋
            PullButton(box, "GachaFreeButton", 27, 784, 237, 930, false, "FreeTitleText", null, "FreeTagText");
            PullButton(box, "GachaPull1Button", 252, 784, 463, 930, false, "Pull1TitleText", "Pull1PriceText", null);
            PullButton(box, "GachaPull10Button", 474, 784, 692, 930, true, "Pull10TitleText", "Pull10PriceText", "Pull10NoteText");

            // 천장
            Sliced(box, "GachaPityPanel", $"{Ch}/ch_s22e_box.png", 27, 942, 692, 1021, 2f);
            Body(box, "GachaPityText", 53, 966, 400, 30, 24, Color.white).alignment = TextAlignmentOptions.Left;
            var bar = Sliced(box, "GachaPityBar", $"{Su}/pity_bar_bg.png", 53, 983, 581, 1006, 3f);
            var fill = Pic(bar.transform, "GachaPityFill", $"{Su}/pity_bar_fill.png", 0, 0, 0, 0);
            StretchIn(fill.rectTransform);
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0.5f;
            Pic(box, "GachaPityIcon", $"{Su}/card_front_jackpot.png", 600, 950, 662, 1016);

            // 확률 · 기록 — 또렷하게(확률 공개 의무)
            var rate = Sliced(box, "GachaRateButton", $"{St}/btn_rate.png", 53, 1033, 353, 1082, 3f);
            rate.raycastTarget = true;
            Button(rate.gameObject);
            var rt = Title(rate.transform, "GachaRateText", 0, 0, 0, 0, 22, Color.white);
            Local(rt.rectTransform, 24, 0, 220, 34);
            var hist = Sliced(box, "GachaHistoryButton", $"{St}/btn_small.png", 366, 1033, 664, 1082, 3f);
            hist.raycastTarget = true;
            Button(hist.gameObject);
            var ht = Title(hist.transform, "GachaHistoryText", 0, 0, 0, 0, 22, Color.white);
            Local(ht.rectTransform, 0, 0, 240, 34);
        }

        private static void PullButton(Transform box, string name, float x0, float y0, float x1, float y1, bool ten,
                                       string titleName, string priceName, string tagName)
        {
            var b = Sliced(box, name, $"{Su}/{(ten ? "pull_plate_10" : "pull_plate")}.png", x0, y0, x1, y1, 2f);
            b.raycastTarget = true;
            Button(b.gameObject);
            float w = x1 - x0;
            var t = Title(b.transform, titleName, 0, 0, 0, 0, 34, ten ? Gold : Color.white);
            Local(t.rectTransform, 0, 30, w - 20, 44);
            if (priceName != null)
            {
                var gi = Pic(b.transform, "GemIcon", $"{Ch}/ch_icon_gem.png", 0, 0, 0, 0);
                Local(gi.rectTransform, -38, -16, 40, 36);
                var ti = Pic(b.transform, "TicketIcon", $"{Pr}/icon_ticket.png", 0, 0, 0, 0);
                Local(ti.rectTransform, 40, -16, 46, 34);
                var p = Body(b.transform, priceName, 0, 0, 0, 0, 18, Color.white);
                Local(p.rectTransform, 0, -52, w - 16, 26);
            }
            else
            {
                var ti = Pic(b.transform, "TicketIcon", $"{Pr}/icon_ticket.png", 0, 0, 0, 0);
                Local(ti.rectTransform, 0, -26, 74, 52);
            }
            if (tagName != null)
            {
                var tag = Sliced(b.transform, "Tag", ten ? $"{St}/ribbon_first2x.png" : $"{St}/btn_price.png", 0, 0, 0, 0, 3f);
                Local(tag.rectTransform, 0, (y1 - y0) * 0.5f, ten ? 150 : 104, 28);
                var tg = Title(tag.transform, tagName, 0, 0, 0, 0, 17, ten ? Color.white : Ink);
                Local(tg.rectTransform, 0, 0, ten ? 140 : 96, 24);
                if (!ten) Plain(tg);
            }
        }

        // ── 연출 층 ──────────────────────────────────────────

        private static void FxLayer(Transform box)
        {
            var fx = Node(box, "SummonFx", 0, 0, 720, 1280);
            Stretch(fx);
            fx.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
            var hit = fx.gameObject.AddComponent<Image>();
            hit.color = new Color(0.02f, 0f, 0.05f, 0.45f);
            hit.raycastTarget = true;
            var skip = Node(fx, "SummonFxSkip", 0, 0, 720, 1280);
            Stretch(skip);
            var si = skip.gameObject.AddComponent<Image>();
            si.color = new Color(1f, 1f, 1f, 0f);
            Button(skip.gameObject);
            // 기둥은 제단 마법진에서 솟는다 — 가산(검정 = 투명)
            var pillar = Pic(fx, "SummonFxPillar", $"{Fx}/fx_pillar_cyan_01.png", 210, 250, 510, 800);
            pillar.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UIAdditiveSprite.mat");
            pillar.enabled = false;
            fx.gameObject.SetActive(false);
        }

        // ── 결과(S13 × 0.765) ───────────────────────────────

        private static readonly float[] CellX = { 27, 162, 297, 432, 568 };

        private static void Result(Transform box)
        {
            var root = Node(box, "GachaResultPopup", 0, 0, 720, 1280);
            Stretch(root);
            root.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
            // 시안(S12 · S13)은 소환 화면 UI 없이 제단 배경 위에 결과만 — 불투명 바탕 + 같은 제단 그림을 깐다
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.01f, 0.08f, 1f);
            dim.raycastTarget = true;
            var rbg = Pic(root, "ResultBackdrop", $"{Ch}/ch_screen_s22e.png", 0, 0, 0, 0);
            rbg.preserveAspect = false;
            rbg.color = new Color(0.72f, 0.52f, 0.95f, 1f);
            float rk = 720f / 941f;
            C(rbg.rectTransform, 360, 645, rbg.sprite.rect.width * rk, rbg.sprite.rect.height * rk);
            var raltar = Pic(root, "ResultAltar", $"{Su}/summon_bg.png", 0, 0, 0, 0);
            raltar.preserveAspect = false;
            C(raltar.rectTransform, 360, 700, 1054 * 0.86f, 677 * 0.86f);
            Pic(root, "ResultGhost", $"{Su}/summon_ghost.png", 150, 160, 290, 255);
            Title(root, "ResultTitleText", 300, 210, 300, 60, 46, Color.white).alignment = TextAlignmentOptions.Left;
            var cp = Sliced(root, "ResultCountPlate", $"{St}/btn_small.png", 245, 243, 474, 289, 3f);
            var ct = Title(cp.transform, "ResultCountText", 0, 0, 0, 0, 24, Color.white);
            Local(ct.rectTransform, 0, 0, 200, 34);
            for (int i = 0; i < 10; i++)
            {
                float x0 = CellX[i % 5], y0 = i < 5 ? 327 : 558;
                var cell = Node(root, $"ResultCell{i}", x0, y0, x0 + 126, y0 + 220);
                var frame = Sliced(cell, "CellFrame", $"{Su}/card_front_b.png", 0, 0, 0, 0, 2.2f);
                StretchIn(frame.rectTransform);
                var g = Title(cell, "CellGradeText", 0, 0, 0, 0, 22, Color.white);
                Local(g.rectTransform, 0, 88, 110, 30);
                var face = Pic(cell, "CellFace", null, 0, 0, 0, 0);
                Local(face.rectTransform, 0, 28, 108, 90);
                var n = Body(cell, "CellNameText", 0, 0, 0, 0, 15, Color.white);
                Local(n.rectTransform, 0, -36, 116, 22);
                var si = Pic(cell, "CellShardIcon", $"{Ch}/ch_icon_gem.png", 0, 0, 0, 0);
                Local(si.rectTransform, -24, -68, 24, 24);
                var a = Title(cell, "CellAmountText", 0, 0, 0, 0, 22, Color.white);
                Local(a.rectTransform, 12, -68, 80, 30);
                var pick = Pic(cell, "CellPickupMark", $"{Su}/ribbon_pickup.png", 0, 0, 0, 0);
                Local(pick.rectTransform, 36, 86, 64, 40);
                var nw = Pic(cell, "CellNewMark", $"{Su}/stamp_new.png", 0, 0, 0, 0);
                Local(nw.rectTransform, 30, 52, 56, 38);
                var jm = Node(cell, "CellJackpotMark", 0, 0, 0, 0);
                Local(jm, 0, -102, 110, 34);
                var jt = Title(jm, "CellJackpotText", 0, 0, 0, 0, 26, Gold);
                Local(jt.rectTransform, 0, 0, 110, 34);
                pick.gameObject.SetActive(false);
                nw.gameObject.SetActive(false);
                jm.gameObject.SetActive(false);
            }
            Single(root);
            var sum = Sliced(root, "ResultSummary", $"{Su}/summary_plate.png", 99, 906, 623, 962, 2f);
            var st = Title(sum.transform, "ResultSummaryText", 0, 0, 0, 0, 26, Color.white);
            Local(st.rectTransform, 0, 0, 500, 40);
            var again = Sliced(root, "ResultAgainButton", $"{Su}/pull_plate_10.png", 103, 987, 344, 1075, 2f);
            again.raycastTarget = true;
            Button(again.gameObject);
            var at = Title(again.transform, "ResultAgainTitle", 0, 0, 0, 0, 28, Gold);
            Local(at.rectTransform, 0, 16, 220, 36);
            var gi = Pic(again.transform, "GemIcon", $"{Ch}/ch_icon_gem.png", 0, 0, 0, 0);
            Local(gi.rectTransform, -40, -20, 30, 28);
            var ap = Title(again.transform, "ResultAgainPrice", 0, 0, 0, 0, 24, Color.white);
            Local(ap.rectTransform, 22, -20, 120, 32);
            var close = Sliced(root, "ResultCloseButton", $"{St}/btn_small.png", 375, 987, 612, 1075, 3f);
            close.raycastTarget = true;
            Button(close.gameObject);
            var cl = Title(close.transform, "ResultCloseText", 0, 0, 0, 0, 32, Color.white);
            Local(cl.rectTransform, 0, 0, 200, 44);
            root.gameObject.SetActive(false);
        }

        /// <summary>1회 결과(S12 × 0.765) — 가운데 큰 카드 한 장 + 다음 성급까지 조각 막대. 버튼 자리는 빈 칸(Slot)으로 둔다.</summary>
        private static void Single(Transform root)
        {
            var single = Node(root, "ResultSingle", 0, 0, 720, 1280);
            var frame = Sliced(single, "SingleFrame", $"{Su}/card_front_b.png", 220, 230, 486, 666, 1.2f);
            var face = Pic(single, "SingleFace", null, 0, 0, 0, 0);
            Local(face.rectTransform, -6, 263, 226, 264);   // 시안 S12 얼굴 칸 x 241~467 · y 245~509
            var grade = Title(single, "SingleGradeText", 224, 262, 72, 50, 34, Color.white);   // 얼굴 위(왼쪽 위 등급 칸)
            grade.alignment = TextAlignmentOptions.Center;
            var name = Title(single, "SingleNameText", 245, 541, 226, 46, 38, Color.white);
            name.alignment = TextAlignmentOptions.Center;
            Pic(single, "SingleShardIcon", $"{Ch}/ch_icon_gem.png", 262, 596, 318, 638);
            var amount = Title(single, "SingleAmountText", 330, 616, 150, 44, 34, Color.white);
            amount.alignment = TextAlignmentOptions.Left;
            Sliced(single, "SingleProgressPlate", $"{Su}/summary_plate.png", 77, 803, 643, 891, 2f);
            var pt = Body(single, "SingleProgressText", 100, 832, 520, 30, 24, Color.white);
            pt.alignment = TextAlignmentOptions.Center;
            var bar = Sliced(single, "SingleProgressBar", $"{Su}/pity_bar_bg.png", 103, 853, 616, 876, 3f);
            var fill = Pic(bar.transform, "SingleProgressFill", $"{Su}/pity_bar_fill.png", 0, 0, 0, 0);
            StretchIn(fill.rectTransform);
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            Node(single, "SingleAgainSlot", 70, 910, 353, 1018);
            Node(single, "SingleCloseSlot", 363, 910, 650, 1018);
            single.gameObject.SetActive(false);
        }

        // ── 확률(S14) · 기록(S15) 창 ─────────────────────────
        //
        // 한 창(GachaInfoPopup) 안에 두 묶음 — 확률은 표(구간 머리 + 줄 · 가운데 칸막이), 기록은 날짜 · 얼굴 · 이름 · 조각 · 사용 줄.
        // 기록 줄은 견본(HistoryRowTemplate) 하나만 두고 런타임에 필요한 만큼 복제한다(최근 100회).

        private const float HistoryRowStep = 60f;

        private static void Info(Transform box)
        {
            var root = Node(box, "GachaInfoPopup", 0, 0, 720, 1280);
            Stretch(root);
            root.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0.03f, 0.62f);
            dim.raycastTarget = true;
            Rates(root);
            History(root);
            root.gameObject.SetActive(false);
        }

        private static void Rates(Transform root)
        {
            var g = Node(root, "RatesGroup", 0, 0, 720, 1280);
            Sliced(g, "RatesBody", $"{St}/popup_plate_wide.png", 60, 157, 662, 1071, 2f);
            Sliced(g, "RatesHead", $"{Su}/summary_plate.png", 73, 172, 650, 285, 2f);   // 픽업 판은 유령이 그려져 있어 겹친다
            Pic(g, "RatesGhost", $"{Su}/summon_ghost.png", 84, 178, 232, 282);
            Title(g, "RatesTitle", 252, 212, 320, 52, 42, new Color(0.96f, 0.82f, 1f)).alignment = TextAlignmentOptions.Left;
            Body(g, "RatesSubTitle", 254, 257, 300, 30, 22, Color.white).alignment = TextAlignmentOptions.Left;
            var x = Pic(g, "GachaRatesX", $"{St}/btn_close_x.png", 589, 190, 643, 243);
            x.raycastTarget = true;
            Button(x.gameObject);

            // 구간 머리 셋 · 줄 여덟(등급 2 · 호스트 3 · 조각 수 3) — 시안 S14 자리
            RateHead(g, "RateHeadGrade", 295);
            RateRow(g, "RateRowGradeB", 340, false);
            RateRow(g, "RateRowGradeA", 382, false);
            RateHead(g, "RateHeadHost", 432);
            RateRow(g, "RateRowHostB", 477, false);
            RateRow(g, "RateRowPickup", 520, true);
            RateRow(g, "RateRowHostA", 576, false);
            RateHead(g, "RateHeadAmount", 627);
            RateRow(g, "RateRowAmount0", 673, false);
            RateRow(g, "RateRowAmount1", 716, false);
            RateRow(g, "RateRowAmount2", 759, false);

            var guard = Sliced(g, "RatesGuarantee", $"{Su}/pull_plate_10.png", 73, 815, 646, 876, 2.4f);
            var gi = Pic(guard.transform, "TicketIcon", $"{Pr}/icon_ticket.png", 0, 0, 0, 0);
            Local(gi.rectTransform, -222, 0, 74, 44);
            var gt = Title(guard.transform, "RatesGuaranteeText", 0, 0, 0, 0, 26, Color.white);
            Local(gt.rectTransform, 40, 0, 440, 40);
            var pity = Sliced(g, "RatesPity", $"{Nav}/nav_plate_summon.png", 73, 887, 646, 968, 2.6f);
            var pt = Title(pity.transform, "RatesPityText", 0, 0, 0, 0, 32, Color.white);
            Local(pt.rectTransform, 0, 14, 520, 40);
            var pn = Body(pity.transform, "RatesPityNote", 0, 0, 0, 0, 19, new Color(1f, 0.85f, 0.32f));
            Local(pn.rectTransform, 0, -20, 520, 26);
            var ok = Sliced(g, "GachaInfoClose", $"{St}/btn_cancel.png", 164, 987, 555, 1052, 3f);
            ok.raycastTarget = true;
            Button(ok.gameObject);
            var ot = Title(ok.transform, "GachaInfoCloseText", 0, 0, 0, 0, 34, Color.white);
            Local(ot.rectTransform, 0, 0, 300, 46);
            g.gameObject.SetActive(false);
        }

        private static void RateHead(Transform g, string name, float y0)
        {
            var h = Sliced(g, name, $"{Su}/summary_plate.png", 77, y0, 618, y0 + 42, 2.4f);
            var t = Title(h.transform, name + "Text", 0, 0, 0, 0, 26, Color.white);
            Pivot(t.rectTransform, 0f);
            Local(t.rectTransform, -240, 0, 360, 34);
            t.alignment = TextAlignmentOptions.Left;
        }

        private static void RateRow(Transform g, string name, float y0, bool pickup)
        {
            float h = pickup ? 52 : 42;
            var row = Sliced(g, name, pickup ? $"{Su}/pull_plate_10.png" : $"{Su}/rate_row.png", 81, y0, 614, y0 + h, pickup ? 2.6f : 3f);
            var div = Pic(row.transform, "RowDivider", $"{Ch}/ch_s22e_vline.png", 0, 0, 0, 0);
            Local(div.rectTransform, 2, 0, 10, h - 14);   // 비율 유지(얇은 선 그림)
            var label = Body(row.transform, "RowLabel", 0, 0, 0, 0, pickup ? 26 : 23, Color.white);
            Pivot(label.rectTransform, 0f);
            Local(label.rectTransform, -240, 0, 230, h - 8);
            label.alignment = TextAlignmentOptions.Left;
            var value = Title(row.transform, "RowValue", 0, 0, 0, 0, pickup ? 30 : 24, Color.white);
            Pivot(value.rectTransform, 1f);
            Local(value.rectTransform, 240, 0, 220, h - 8);
            value.alignment = TextAlignmentOptions.Right;
        }

        private static void History(Transform root)
        {
            var g = Node(root, "HistoryGroup", 0, 0, 720, 1280);
            Sliced(g, "HistoryBody", $"{St}/popup_plate_wide.png", 23, 195, 696, 1071, 2f);
            var title = Title(g, "HistoryTitle", 160, 245, 400, 70, 56, new Color(0.96f, 0.82f, 1f));
            title.alignment = TextAlignmentOptions.Center;
            Pic(g, "HistoryFlameL", $"{Nav}/nav_icon_summon.png", 140, 216, 196, 274);
            Pic(g, "HistoryFlameR", $"{Nav}/nav_icon_summon.png", 524, 216, 580, 274);
            var sub = Sliced(g, "HistorySubPlate", $"{St}/btn_small.png", 164, 291, 551, 337, 3f);
            var st = Title(sub.transform, "HistorySubText", 0, 0, 0, 0, 26, Color.white);
            Local(st.rectTransform, 0, 0, 340, 36);
            var x = Pic(g, "GachaHistoryX", $"{St}/btn_close_x.png", 608, 207, 681, 272);
            x.raycastTarget = true;
            Button(x.gameObject);

            // 표 머리
            Sliced(g, "HistoryHeadRow", $"{Su}/rate_row.png", 46, 344, 673, 386, 3f);
            HeadCell(g, "HistoryHeadDate", 103);
            HeadCell(g, "HistoryHeadHost", 291);
            HeadCell(g, "HistoryHeadShard", 467);
            HeadCell(g, "HistoryHeadPay", 593);

            // 줄 — 스크롤 · 견본 하나
            var view = Node(g, "HistoryScroll", 50, 392, 646, 990);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            var content = new GameObject("HistoryContent", typeof(RectTransform)).GetComponent<RectTransform>();
            content.gameObject.layer = view.gameObject.layer;
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0, HistoryRowStep * 10);
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            HistoryRow(content);
            var empty = Body(g, "HistoryEmptyText", 60, 600, 600, 40, 24, Color.white);
            empty.alignment = TextAlignmentOptions.Center;

            var close = Sliced(g, "GachaHistoryClose", $"{St}/btn_cancel.png", 222, 1010, 497, 1063, 3f);
            close.raycastTarget = true;
            Button(close.gameObject);
            var ct = Title(close.transform, "GachaHistoryCloseText", 0, 0, 0, 0, 32, Color.white);
            Local(ct.rectTransform, 0, 0, 240, 44);
            g.gameObject.SetActive(false);
        }

        private static void HeadCell(Transform g, string name, float cx)
        {
            var t = Body(g, name, cx - 70, 365, 140, 32, 22, Color.white);
            t.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>기록 한 줄(596 × 56) — 날짜 두 줄 · 얼굴 · 이름 · ×조각 · 사용(아이콘 + 글자). 대박 줄은 금 테.</summary>
        private static void HistoryRow(RectTransform content)
        {
            var row = new GameObject("HistoryRowTemplate", typeof(RectTransform)).GetComponent<RectTransform>();
            row.gameObject.layer = content.gameObject.layer;
            row.SetParent(content, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0, -2);
            row.sizeDelta = new Vector2(588, HistoryRowStep - 4);
            var plate = Sliced(row, "RowPlate", $"{Su}/rate_row.png", 0, 0, 0, 0, 3f);
            StretchIn(plate.rectTransform);
            var gold = Sliced(row, "RowJackpot", $"{Su}/pull_plate_10.png", 0, 0, 0, 0, 3f);
            StretchIn(gold.rectTransform);
            gold.gameObject.SetActive(false);
            var date = Body(row, "RowDate", 0, 0, 0, 0, 17, Color.white);
            Local(date.rectTransform, -238, 0, 100, 48);
            date.textWrappingMode = TextWrappingModes.Normal;
            date.lineSpacing = -12f;
            var face = Pic(row, "RowFace", null, 0, 0, 0, 0);
            Local(face.rectTransform, -137, 0, 54, 50);
            var name = Title(row, "RowName", 0, 0, 0, 0, 24, Color.white);
            Local(name.rectTransform, -36, 0, 130, 40);
            var shard = Title(row, "RowShard", 0, 0, 0, 0, 30, Color.white);
            Local(shard.rectTransform, 100, 0, 110, 44);
            var payIcon = Pic(row, "RowPayIcon", null, 0, 0, 0, 0);
            Local(payIcon.rectTransform, 200, 0, 40, 30);
            var pay = Body(row, "RowPay", 0, 0, 0, 0, 19, Color.white);
            Pivot(pay.rectTransform, 0f);
            Local(pay.rectTransform, 224, 0, 66, 30);
            pay.alignment = TextAlignmentOptions.Left;
            row.gameObject.SetActive(false);
        }

        // ── 연결 ────────────────────────────────────────────

        private static void Bind(SummonPanel p)
        {
            var so = new SerializedObject(p);
            Keys(so, "_faces", Faces, "face_");
            Keys(so, "_portraits", Portraits, "portrait_");
            so.FindProperty("_frontB").objectReferenceValue = Spr($"{Su}/card_front_b.png");
            so.FindProperty("_frontA").objectReferenceValue = Spr($"{Su}/card_front_a.png");
            so.FindProperty("_frontJackpot").objectReferenceValue = Spr($"{Su}/card_front_jackpot.png");
            so.FindProperty("_back").objectReferenceValue = Spr($"{Su}/card_back.png");
            so.FindProperty("_gemIcon").objectReferenceValue = Spr($"{Ch}/ch_icon_gem.png");
            so.FindProperty("_ticketIcon").objectReferenceValue = Spr($"{Pr}/icon_ticket.png");
            Frames(so, "_pillarCyan", "fx_pillar_cyan");
            Frames(so, "_pillarPurple", "fx_pillar_purple");
            Frames(so, "_pillarGold", "fx_pillar_gold");
            Frames(so, "_jackpot", "fx_jackpot");
            Frames(so, "_flip", "fx_flip");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Keys(SerializedObject so, string field, string dir, string prefix)
        {
            var list = new List<(string, Sprite)>();
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir, prefix + "*.png"))
                    list.Add((Path.GetFileNameWithoutExtension(f).Substring(prefix.Length), Spr(f.Replace('\\', '/'))));
            var prop = so.FindProperty(field);
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                var e = prop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = list[i].Item1;
                e.FindPropertyRelative("Sprite").objectReferenceValue = list[i].Item2;
            }
        }

        private static void Frames(SerializedObject so, string field, string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 1; i <= 12; i++)
            {
                var s = Spr($"{Fx}/{prefix}_{i:00}.png");
                if (s == null) break;
                list.Add(s);
            }
            var prop = so.FindProperty(field);
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        private static void Borders()
        {
            B($"{Su}/pickup_plate.png", 60); B($"{Su}/pull_plate.png", 50); B($"{Su}/pull_plate_10.png", 56);
            B($"{Su}/pity_bar_bg.png", new Vector4(30, 20, 30, 20)); B($"{Su}/summary_plate.png", 50);
            B($"{Su}/rate_row.png", new Vector4(40, 22, 40, 22));
            B($"{Su}/card_front_b.png", 44); B($"{Su}/card_front_a.png", 44); B($"{Su}/card_front_jackpot.png", 56);
        }
    }
}
