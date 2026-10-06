using System;
using System.IO;
using Game.Module.Common.UI;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 호스트 탭 v5 — 한 화면(몸 정보 + 성급 · 능력치 · 스킬 둘 · 가로 목록) + 「성급 올리기」 창 (2026-10-06).
    ///
    /// 시안: Projects/AVSR/_exchange/in/growth_starup_mock_v4.png (통과) → 720 폭으로 다시 그린 growth_host_v5.png.
    /// 그림 · 글자 칸은 `Projects/AVSR/Tools/growth_v5_build.py` 가 `Growth/v5/` 에 둔다. 이 파일은 꽂기만 한다.
    /// 노드 이름(H5…) = `GrowthPanel` 이 찾는 이름.
    ///
    /// 유령 탭은 그대로다 — 이 판은 유령 탭 판들 위(바탕 바로 뒤)에 따로 서고, 호스트 탭일 때만 켠다.
    /// </summary>
    public static partial class GrowthBuilder
    {
        private const string Dir5 = Dir + "/v5";

        private static Spec s_spec5;
        private static float s_rowPitch5, s_cardPitch5;

        [Serializable] private class Spec5 { public float rowPitch; public float cardPitch; public GText[] texts; public GRect[] rects; }

        private static GText T5(string name)
        {
            var t = Array.Find(s_spec5.texts, x => x.name == name);
            if (t == null) throw new Exception($"[육성 v5] 글자 스펙 없음: {name}");
            return t;
        }

        private static float[] R5(string name)
        {
            var r = Array.Find(s_spec5.rects, x => x.name == name);
            if (r == null) throw new Exception($"[육성 v5] 자리 스펙 없음: {name}");
            return r.box;
        }

        private static Sprite Spr5(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir5}/{file}.png");

        private static RectTransform BuildHostPage(RectTransform page)
        {
            var raw = JsonUtility.FromJson<Spec5>(File.ReadAllText(Path.Combine(Dir5, "growth_v5_spec.json")));
            s_spec5 = new Spec { texts = raw.texts, rects = raw.rects };
            s_rowPitch5 = raw.rowPitch;
            s_cardPitch5 = raw.cardPitch;
            SetBorder($"{Dir}/hostcard.png", new Vector4(28, 28, 28, 28));
            SetBorder($"{Dir}/hostcard_sel.png", new Vector4(28, 28, 28, 28));

            var hp = Node(page, "HostPage"); Stretch(hp);
            Img(Place(hp, "H5Bg", 0, 0, PageW, s_spec.navY), Spr5("bg_host_v5"));

            BuildCard5(hp);
            BuildStats5(hp);
            BuildSkills5(hp);
            BuildList5(hp);
            BuildStarUpWindow(hp);
            return hp;
        }

        // ── 몸 정보 + 성급 ───────────────────────────────────────

        private static void BuildCard5(RectTransform hp)
        {
            var pr = R5("PORTRAIT");
            Fit(Img(Place(hp, "H5Portrait", pr[0], pr[1], pr[2], pr[3]), null));
            Text5(hp, "H5NameEnText", "H5NameEn", "GANGSTER", null);
            Text5(hp, "H5NameText", "H5Name", "갱스터", null);
            var desc = Text5(hp, "H5DescText", "H5Desc", "뒷골목을 주름잡던 두목,\n권총 한 자루로 길을 연다.", null);
            Wrap(desc, 182f);

            Text5(hp, "H5StarLabel", "H5StarLabel", Localize("ui.starup.label"), "ui.starup.label");
            for (int i = 0; i < 5; i++) HalfStar(hp, $"H5Star{i}", R5($"STAR{i}"), Spr("star_big_on"), Spr("star_big_off"));
            var sm = R5("STAR_MARK");
            Fit(Img(Place(hp, "H5StarMark", sm[0], sm[1], sm[2], sm[3]), Spr("star_big_on")));
            Text5(hp, "H5StarValue", "H5StarValue", "1.5 / 5", null);

            var sb = R5("SHARD_BAR_IN");
            var fill = Img(Place(hp, "H5ShardFill", sb[0], sb[1], sb[2], sb[3]), Spr("bar_h_fill"));
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 1f;
            var sv = Text5(hp, "H5ShardValue", "H5ShardValue", "16 / 16", null);
            sv.color = Color.white;   // 막대 위라 잰 색이 막대 빛에 물들었다 — 시안 글자는 흰색

            var bb = R5("STARUP_BTN");
            var btn = HitButton(hp, "H5StarUpButton", bb[0], bb[1], bb[2], bb[3]);
            Img(Place(btn, "Art", 0, 0, bb[2] - bb[0], bb[3] - bb[1]), Spr5("h5_starup_button"));
            // 노란 판 위 검은 글자는 잴 때 판 빛에 잉크가 얇게 잡혀 작게 재진다 — 시안 크기(24 · 22)로 둔다
            Size(TextIn5(btn, "H5StarUpText", "H5StarUp", Localize("ui.starup.button"), bb[0], bb[1], "ui.starup.button"), 24f);
            Size(TextIn5(btn, "H5StarUpCost", "H5StarUpCost", "1,200", bb[0], bb[1], null), 22f);
        }

        // ── 능력치 — 8종을 5줄 창에서 넘긴다 ─────────────────────────

        private static void BuildStats5(RectTransform hp)
        {
            Text5(hp, "H5StatHeaderText", "H5StatHeader", Localize("ui.growth.stat_header"), "ui.growth.stat_header");
            var help = R5("HELP");
            var hb = HitButton(hp, "H5StatHelpButton", help[0] - 8, help[1] - 8, help[2] + 8, help[3] + 8);   // 손가락 칸은 44 이상
            Img(Place(hb, "Icon", 8, 8, 8 + help[2] - help[0], 8 + help[3] - help[1]), Spr("help"));
            var cm = R5("CAP_MARK");
            Fit(Img(Place(hp, "H5CapMark", cm[0], cm[1], cm[2], cm[3]), Spr("star_big_on")));
            Text5(hp, "H5CapText", "H5Cap", "강화 상한 Lv 30", null);

            var view = R5("STAT_VIEW");
            var vp = Place(hp, "H5StatViewport", view[0], view[1], view[2], view[3]);
            vp.gameObject.AddComponent<RectMask2D>();
            var hit = vp.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);
            var content = Node(vp, "H5StatContent");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, s_rowPitch5 * 8);
            var scroll = vp.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.viewport = vp;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var band = R5("ROW_BAND");
            float ox = view[0], oy = view[1];
            var row = Place(content, "H5Row", band[0] - ox, band[1] - oy, band[2] - ox, band[3] - oy);
            Img(row, Spr5("h5_row"));
            float rx = band[0], ry = band[1];
            var ic = R5("ROW_ICON");
            Fit(Img(Place(row, "H5RowIcon", ic[0] - rx, ic[1] - ry, ic[2] - rx, ic[3] - ry), Spr("icon_atk")));
            TextIn5(row, "H5RowName", "H5RowName", "공격력", rx, ry, null);
            var a = TextIn5(row, "H5RowLvLabel", "H5RowLvLabel", "Lv.", rx, ry, null);
            var b = TextIn5(row, "H5RowLvNum", "H5RowLvNum", "0", rx, ry, null);
            var c = TextIn5(row, "H5RowLvMax", "H5RowLvMax", "/ 30", rx, ry, null);
            Run5(row, "H5RowLvRun", new[] { a, b, c }, new[] { "H5RowLvLabel", "H5RowLvNum", "H5RowLvMax" });
            TextIn5(row, "H5RowPct", "H5RowPct", "+0.0%", rx, ry, null);
            var br = R5("ROW_BTN");
            var btn = HitButton(row, "H5RowButton", br[0] - rx, br[1] - ry, br[2] - rx, br[3] - ry);
            Img(Place(btn, "Art", 0, 0, br[2] - br[0], br[3] - br[1]), Spr5("h5_gold_button"));
            TextIn5(btn, "H5RowCost", "H5RowCost", "60", br[0], br[1], null);
        }

        // ── 스킬 둘 ─────────────────────────────────────────────

        private static void BuildSkills5(RectTransform hp)
        {
            Text5(hp, "H5SkillHeaderText", "H5SkillHeader", Localize("ui.growth.skill.header"), "ui.growth.skill.header");
            foreach (var (tag, side) in new[] { ("TAG_A", "A"), ("TAG_P", "P") })
            {
                var t = R5(tag);
                Img(Place(hp, $"H5Tag{side}", t[0], t[1], t[2], t[3]), Spr5(side == "A" ? "h5_tag_active" : "h5_tag_passive"));
                var st = R5($"STAR_{side}");
                Fit(Img(Place(hp, $"H5SkillStar{side}", st[0], st[1], st[2], st[3]), Spr("star_big_on")));
                var ic = R5($"ICON_{side}");
                Fit(Img(Place(hp, $"H5SkillIcon{side}", ic[0], ic[1], ic[2], ic[3]), null));
                var bd = R5($"BADGE_{side}");
                var badge = Place(hp, $"H5Badge{side}", bd[0], bd[1], bd[2], bd[3]);
                Img(badge, Spr5("h5_badge"));
                var bt = T5("H5Badge");
                int bw = (int)(bd[2] - bd[0]), bh = (int)(bd[3] - bd[1]);
                TextAt(badge, $"H5Badge{side}Text", new[] { 4, 2, bw - 4, bh - 2 }, bt, "Lv.3", true, 0, 0, false, new[] { 4, 2, bw - 4, bh - 2 });
            }
            Text5(hp, "H5SkillNameA", "H5SkillName", "일제 표식", null);
            Wrap(Text5(hp, "H5SkillDescA", "H5SkillDesc", "방 안 모든 적에게 3초간 표식을 새긴다.", null), 460f);
            // 패시브 줄은 액티브 줄과 같은 글자 크기 · 색 — 시안 두 줄 설명은 바탕에 물들어 재진다
            var nameP = T5("H5SkillName"); var descP = T5("H5SkillDesc");
            int dy = 104;
            TextAtBox(hp, "H5SkillNameP", new[] { nameP.box[0], nameP.box[1] + dy, nameP.box[2] + 120, nameP.box[3] + dy }, nameP, "처형 계약");
            Wrap(TextAtBox(hp, "H5SkillDescP", new[] { descP.box[0], descP.box[1] + dy, descP.box[2] + 140, descP.box[3] + dy }, descP,
                           "표식이 붙은 적을 20% 로 즉사."), 410f);   // 시안 두 줄 폭 — 판 오른쪽 테두리에 닿지 않게

            var lk = R5("LOCK");
            var lockTag = Place(hp, "H5LockA", lk[0], lk[1], lk[2], lk[3]);
            Img(lockTag, Spr5("h5_lock"));
            TextIn5(lockTag, "H5LockText", "H5Lock", "Lv.5 특수 효과", lk[0], lk[1], null);
        }

        // ── 호스트 목록 — 가로로 넘긴다 ─────────────────────────────

        private static void BuildList5(RectTransform hp)
        {
            Text5(hp, "H5ListHeaderText", "H5ListHeader", Localize("ui.growth.host_list"), "ui.growth.host_list");
            var so = R5("SORT");
            var sort = HitButton(hp, "H5SortButton", so[0], so[1], so[2], so[3]);
            Img(Place(sort, "Art", 0, 0, so[2] - so[0], so[3] - so[1]), Spr5("h5_sort"));
            TextIn5(sort, "H5SortText", "H5Sort", Localize("ui.growth.sort.default"), so[0], so[1], "ui.growth.sort.default");

            var list = R5("LIST");
            var vp = Place(hp, "H5ListViewport", list[0], list[1], list[2], list[3]);
            vp.gameObject.AddComponent<RectMask2D>();
            var hit = vp.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);
            var content = Node(vp, "H5ListContent");
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(1000, 0);
            var scroll = vp.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.horizontal = true; scroll.vertical = false; scroll.viewport = vp;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var c0 = R5("CARD0");
            float cw = c0[2] - c0[0], ch = c0[3] - c0[1];
            var card = Place(content, "H5Card", c0[0] - list[0], c0[1] - list[1], c0[2] - list[0], c0[3] - list[1]);
            // 가로 목록 칸은 세로가 꽉 찬다 — 위 기준으로 둔다
            card.anchorMin = card.anchorMax = new Vector2(0f, 1f);
            var hitImg = card.gameObject.AddComponent<Image>(); hitImg.color = new Color(1, 1, 1, 0);
            card.gameObject.AddComponent<Button>().targetGraphic = hitImg;
            var fr = Img(Place(card, "H5CardFrame", 0, 0, cw, ch), Spr("hostcard")); fr.type = Image.Type.Sliced;
            var fs = Img(Place(card, "H5CardFrameSel", -2, -2, cw + 2, ch + 2), Spr("hostcard_sel")); fs.type = Image.Type.Sliced;
            var thumb = Fit(Img(Place(card, "H5CardThumb", 0, 0, 1, 1), null));
            // 얼굴 칸 — 이름 줄 바로 위까지. 가운데 기준(preserveAspect 는 기준점 쪽으로 붙는다)
            var tr = thumb.rectTransform;
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = new Vector2(cw * 0.5f, -(4 + 38));
            tr.sizeDelta = new Vector2(cw - 16, 76);
            var nm = T5("H5CardName");
            TextAt(card, "H5CardName", new[] { (int)c0[0] + 6, nm.box[1], (int)c0[2] - 6, nm.box[3] }, nm, "갱스터", true, c0[0], c0[1],
                   false, new[] { (int)c0[0] + 6, nm.box[1], (int)c0[2] - 6, nm.box[3] });
            float sw = 21f, gap = 23f, sx = (cw - (gap * 4 + sw)) * 0.5f, sy = 103f;
            for (int i = 0; i < 5; i++)
                HalfStar(card, $"H5CardStar{i}", new[] { sx + gap * i, sy, sx + gap * i + sw, sy + 22 }, Spr("slot_star_on"), Spr("slot_star_off"));
        }

        // ── 「성급 올리기」 창 ───────────────────────────────────────
        //
        // 창 그림(h5_starup_window, 623×809)은 코덱스 빈 창이다. 자리는 그 그림에서 잰 값(창 왼쪽 위 기준).

        private static void BuildStarUpWindow(RectTransform hp)
        {
            var root = Node(hp, "H5StarUpWindow"); Stretch(root);
            // 뒤 화면 어둡게 — 그림이 아니라 덮개다(공통 팝업과 같은 55%). 누르면 닫힌다
            var dim = Node(root, "H5Dim"); Stretch(dim);
            dim.offsetMin = new Vector2(-400, -400); dim.offsetMax = new Vector2(400, 400);   // 태블릿 양옆 · 하단 바까지 덮는다
            var dimImg = dim.gameObject.AddComponent<Image>(); dimImg.color = new Color(0, 0, 0, 0.55f);
            dim.gameObject.AddComponent<Button>().targetGraphic = dimImg;

            const float W = 623f, H = 809f;
            float x0 = (PageW - W) * 0.5f, y0 = 200f;
            var win = Place(root, "H5Window", x0, y0, x0 + W, y0 + H);
            Img(win, Spr5("h5_starup_window"));

            var title = T5("H5StatHeader");
            Key(Size(TextAt(win, "H5WinTitle", new[] { 60, 48, 563, 104 }, title, Localize("ui.starup.title"), true, 0, 0, false,
                            new[] { 60, 48, 563, 104 }), 44f).gameObject, "ui.starup.title");
            Fit(Img(Place(win, "H5WinFace", 54, 120, 194, 244), null));

            // 별 비교 — 「★ 1.5 → ★ 2」 와 별 다섯 칸 두 줄
            var big = T5("H5NameEn");
            for (int side = 0; side < 2; side++)
            {
                float bx = side == 0 ? 214 : 404;
                Fit(Img(Place(win, $"H5WinStarIcon{side}", bx, 140, bx + 34, 172), Spr("star_big_on")));
                var nb = new[] { (int)bx + 40, 136, (int)bx + 130, 176 };
                TextAt(win, $"H5WinStarNum{side}", nb, big, side == 0 ? "1.5" : "2", false, 0, 0, false, nb);
                for (int i = 0; i < 5; i++)
                {
                    float sx = (side == 0 ? 210 : 398) + 30 * i;
                    HalfStar(win, $"H5WinStar{side}_{i}", new[] { sx, 194, sx + 28, 220 }, Spr("star_big_on"), Spr("star_big_off"));
                }
            }
            var ab = new[] { 352, 136, 392, 176 };
            TextAt(win, "H5WinArrowText", ab, big, "→", true, 0, 0, false, ab);

            var hdr = T5("H5RowName");
            var rb = new[] { 70, 262, 400, 290 };
            Key(Size(TextAt(win, "H5WinRises", rb, hdr, Localize("ui.starup.rises"), false, 0, 0, false, rb), 24f).gameObject, "ui.starup.rises");

            // 함께 오르는 줄 넷 — 스킬 Lv · 액티브 · 패시브 · 강화 상한. 줄 안 왼쪽 위 기준
            float[] rowY = { 296, 369, 443, 518 };
            var name = T5("H5RowName"); var val = T5("H5RowPct");
            for (int i = 0; i < 4; i++)
            {
                float y = rowY[i];
                var r = Place(win, $"H5WinRow{i}", 52, y, 570, y + 66);
                Fit(Img(Place(r, "Icon", 16, 10, 72, 56), null));
                var lb = new[] { 110, 6, 500, 34 }; var vb = new[] { 110, 34, 500, 62 };
                Size(TextAt(r, "Label", lb, name, "스킬 Lv", false, 0, 0, false, lb), 26f);
                Size(TextAt(r, "Value", vb, val, "3 → 4", false, 0, 0, false, vb), 26f);
            }

            var cost = T5("H5ShardValue");
            Fit(Img(Place(win, "H5WinShardIcon", 104, 614, 140, 648), Spr("shard_icon")));
            var sbx = new[] { 146, 610, 284, 652 };
            Size(TextAt(win, "H5WinShardText", sbx, cost, "16 / 16", true, 0, 0, false, sbx), 30f).color = Color.white;
            Fit(Img(Place(win, "H5WinGoldIcon", 324, 614, 358, 648), Spr("reward_coin")));
            var gbx = new[] { 364, 610, 524, 652 };
            Size(TextAt(win, "H5WinGoldText", gbx, cost, "1,200", true, 0, 0, false, gbx), 30f).color = Color.white;

            var cancel = HitButton(win, "H5WinCancel", 66, 677, 290, 748);
            var ok = HitButton(win, "H5WinConfirm", 305, 676, 561, 750);
            var lbl = T5("H5StatHeader");
            var cb = new[] { 0, 10, 224, 61 }; var ob = new[] { 0, 10, 256, 64 };
            Key(Size(TextAt(cancel, "Text", cb, lbl, Localize("ui.common.cancel"), true, 0, 0, false, cb), 34f).gameObject, "ui.common.cancel");
            var okText = Size(TextAt(ok, "Text", ob, lbl, Localize("ui.starup.button"), true, 0, 0, false, ob), 34f);
            okText.color = new Color(0.16f, 0.10f, 0.02f, 1f);   // 노란 버튼 위 — 공통 팝업 확인 글자색
            Key(okText.gameObject, "ui.starup.button");
            root.gameObject.SetActive(false);
        }

        private static void BindHostPage(GrowthPanel gp)
        {
            var so = new SerializedObject(gp);
            so.FindProperty("_h5RowPitch").floatValue = s_rowPitch5;
            so.FindProperty("_h5CardPitch").floatValue = s_cardPitch5;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 거들개 ─────────────────────────────────────────────

        /// <summary>
        /// 별 한 칸 = 꺼진 별 위에 켜진 별(가로 채우기). 채움 0.5 가 반 칸이다(성급 한 단계 = 별 반 칸).
        /// 그림은 기존 별 두 장 그대로 — 반 칸 그림을 따로 만들지 않는다.
        /// </summary>
        private static void HalfStar(RectTransform parent, string name, float[] r, Sprite on, Sprite off)
        {
            var holder = Place(parent, name, r[0], r[1], r[2], r[3]);
            Fit(Img(Place(holder, "Off", 0, 0, r[2] - r[0], r[3] - r[1]), off));
            var o = Fit(Img(Place(holder, "On", 0, 0, r[2] - r[0], r[3] - r[1]), on));
            o.type = Image.Type.Filled; o.fillMethod = Image.FillMethod.Horizontal; o.fillOrigin = 0; o.fillAmount = 1f;
        }

        private static TextMeshProUGUI Text5(RectTransform parent, string node, string spec, string text, string key)
        {
            var t = T5(spec);
            var tmp = TextAt(parent, node, t.align == "C" ? t.area : t.box, t, text, t.align == "C", 0, 0, t.align == "R");
            if (key != null) Key(tmp.gameObject, key);
            return tmp;
        }

        private static TextMeshProUGUI TextIn5(RectTransform parent, string node, string spec, string text, float ox, float oy, string key)
        {
            var t = T5(spec);
            var tmp = TextAt(parent, node, t.align == "C" ? t.area : t.box, t, text, t.align == "C", ox, oy, t.align == "R");
            if (key != null) Key(tmp.gameObject, key);
            return tmp;
        }

        private static void Run5(RectTransform parent, string name, TextMeshProUGUI[] parts, string[] names)
        {
            var gaps = new float[parts.Length - 1];
            for (int i = 0; i < gaps.Length; i++) gaps[i] = T5(names[i + 1]).box[0] - T5(names[i]).box[2];
            var holder = Node(parent, name);
            holder.gameObject.AddComponent<InkRun>().Set(parts, gaps);
        }

        /// <summary>
        /// 비율 지키기 + 기준점 가운데(칸은 그대로). preserveAspect 는 칸보다 좁게 그려질 때 그림을 기준점 쪽으로 붙인다 —
        /// 왼쪽 위 기준점이면 그림이 칸 왼쪽에 붙는다(목록 얼굴이 16px 쏠렸다, 2026-10-06).
        /// </summary>
        private static Image Fit(Image img)
        {
            img.preserveAspect = true;
            var rt = img.rectTransform;
            var size = rt.sizeDelta;
            rt.anchoredPosition += new Vector2(size.x * (0.5f - rt.pivot.x), size.y * (0.5f - rt.pivot.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        private static TextMeshProUGUI Size(TextMeshProUGUI t, float size)
        {
            t.fontSize = size;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.6f;
            return t;
        }

        private static void SetBorder(string path, Vector4 border)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti || ti.spriteBorder == border) return;
            ti.spriteBorder = border;
            ti.SaveAndReimport();
        }
    }
}
