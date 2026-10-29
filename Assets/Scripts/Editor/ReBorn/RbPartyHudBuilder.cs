using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    /// <summary>
    /// 빙의 파티 위 HUD 꽂기 (PD 2026-10-29 확정 틀 S1b · 시안 `Projects/AVSR/_exchange/in/mock_partyui_S1b.png` · `F_states`).
    ///   · 위 한 줄 판(190 px 안): 유령 HP · 메인 칸(왕관 · 얼굴 · 이름 · 체력) · 보조 얼굴 둘(체력 없음 · 비면 「+ −10」) · 무대 · 골드 · 일시정지
    ///   · 그 아래 Lv 막대(크게 그대로)
    ///   · 판 바로 밑 왼쪽에 시너지 열 — **세로로 쌓는다**(맨 위 금빛 인연 칸, 그 아래 켜진 태그 칸 「무도가 2/3」), 작게(60 px · 글자는 읽히게 13)
    /// 옛 노드(GhostHpText · HostHpBarFill · LevelText · RunExpFill · GoldText · PauseButton …)는 **이름 그대로 자리만 옮긴다** — 화면 스크립트가 이름으로 채운다.
    /// 좌표는 시안 720 × 1280 게임 px 그대로(위 왼쪽 기준).
    /// ⚠ `인게임 세우기`(RbInGameBuilder) 끝에서도 부른다 — 빌더를 다시 돌려도 이 배치가 남게.
    /// </summary>
    public static class RbPartyHudBuilder
    {
        private const string Dir = "Assets/BaseResource/InGameMainUI/";
        private static readonly Color White = Color.white;
        private static readonly Color Pale = new(0.78f, 0.86f, 1f);   // 시안 「유령 HP」 · 「−10」 의 옅은 하늘빛
        private static readonly Color Gold = new(1f, 0.84f, 0.42f);

        [MenuItem("Tools/Game/RE:BORN 화면/빙의 파티 HUD 꽂기")]
        public static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(RbInGameBuilder.PrefabPath);
            try
            {
                Apply(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, RbInGameBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[빙의 파티 HUD] 꽂음 → " + RbInGameBuilder.PrefabPath);
        }

        public static void Apply(Transform root)
        {
            SliceBorders();
            var hud = F(root, "TopHudGroup");
            if (hud == null) return;
            var number = F(root, "GhostHpText")?.GetComponent<TextMeshProUGUI>();
            var heavy = F(root, "RbNoBodyText")?.GetComponent<TextMeshProUGUI>() ?? number;

            // 좌표 = 시안 S1b 를 2배로 키워 잰 상자(2026-10-29 실측 — 눈대중 금지)
            var bar = F(root, "RbTopBar");
            Px(bar, hud, 9, 7, 711, 151);
            Img(bar, "partyhud_panel");

            // ── 유령 ──
            Px(F(root, "GhostHudIcon"), hud, 18, 18, 88, 78);
            Txt(Px(Node(hud, "PartyGhostLabel"), hud, 96, 25, 200, 46), "유령 HP", 17, Pale, TA.Left, heavy, bold: true);
            var ghp = Px(F(root, "GhostHpText"), hud, 96, 47, 210, 77);
            Size(ghp, 26f);
            var gbg = Px(F(root, "GhostHpBarBg"), hud, 24, 80, 212, 94);
            Img(gbg, "partyhud_bar_bg");
            var gfill = F(root, "GhostHpBarFill");
            Img(gfill, "partyhud_bar_ghost");
            FillLeft(gfill);

            // ── 메인 칸(왕관 테 · 얼굴 · 이름 · 체력) ──
            var mainFrame = Px(Node(hud, "PartyMainFrame"), hud, 226, 12, 390, 112);
            Img(mainFrame, "partyhud_main_frame");
            mainFrame.SetSiblingIndex(bar.GetSiblingIndex() + 1);
            Px(F(root, "HostPortraitImage"), hud, 232, 30, 298, 92);
            // 이름은 왕관(오른쪽 위)과 겹치지 않게 그 아래에 · 긴 이름(「코만도(미사일)」)은 줄여서 칸 안에
            Txt(Px(Node(hud, "PartyMainName"), hud, 300, 40, 382, 58), "빙의할 호스트를 터치", 14, White, TA.Left, heavy, bold: true, min: 8f);
            var hp = Px(F(root, "HostHpText"), hud, 298, 60, 382, 90);
            Size(hp, 19f);
            var hbg = Px(F(root, "HostHpBarBg"), hud, 235, 93, 380, 106);
            Img(hbg, "partyhud_bar_bg");
            var hfill = F(root, "HostHpBarFill");
            Img(hfill, "partyhud_bar_hp");
            hfill.GetComponent<Image>().color = White;
            FillLeft(hfill);

            // ── 보조 둘 — 얼굴만 · 비면 「+ −10」 ──
            for (int i = 0; i < 2; i++)
            {
                float x0 = i == 0 ? 397 : 468;
                var frame = Px(Node(hud, $"PartySub{i}Frame"), hud, x0, 36, x0 + 64, 102);
                Img(frame, "partyhud_sub_frame");
                if (frame.GetComponent<Button>() == null) frame.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                var face = Px(Node(frame, $"PartySub{i}Face"), frame, 5, 5, 59, 61);
                var fi = Img(face, null);
                fi.preserveAspect = true;
                fi.raycastTarget = false;
                var empty = Px(Node(hud, $"PartySub{i}Empty"), hud, x0, 36, x0 + 72, 102);
                Img(empty, "partyhud_empty_frame").raycastTarget = false;
                // 「+」 는 그림 위쪽에 있다 — 「−10」 은 그 아래 · 점선 테 안쪽(시안 77~95)
                Txt(Px(Node(empty, $"PartySub{i}Cost"), empty, 6, 46, 66, 60), "−10", 12, Pale, TA.Center, number);
                frame.gameObject.SetActive(false);
            }

            // ── 무대 · 골드 · 일시정지 ──
            var stage = Px(F(root, "ChapterLabel"), hud, 580, 28, 640, 52);
            Size(stage, 22f);
            Px(F(root, "GoldIcon"), hud, 582, 64, 606, 90);
            var gold = Px(F(root, "GoldText"), hud, 610, 64, 642, 90);
            Size(gold, 20f);
            Px(F(root, "PauseButton"), hud, 647, 32, 701, 86);
            FitChild(F(root, "PauseButton"), "PauseIcon", 0.5f);

            // ── Lv 막대(판 아래 줄 · 크게) ──
            var row = F(root, "RunExpRow");
            if (row != null)
            {
                var lv = Px(F(root, "LevelText"), row, 34, 114, 94, 140);
                Size(lv, 24f);
                var xbg = Px(F(root, "RunExpBg"), row, 96, 119, 695, 137);
                Img(xbg, "partyhud_bar_bg");
                var xfill = F(root, "RunExpFill");
                Img(xfill, "partyhud_bar_ghost");
                FillLeft(xfill);
            }

            // ── 시너지 열(세로) — 판 바로 밑 왼쪽 ──
            BuildSynColumn(hud, heavy, number);

            // 옛 칸과 겹치는 것 — 그림만 끈다(화면 스크립트가 켜도 비어 있게)
            Mute(root, "AffinityTriangle", "RbNoBodyText");
        }

        // 시너지 칸 = 시안 그대로 「얇은 하늘색 테 상자 안에 빛나는 육각 문장 + 이름 + 모은 수」(시안 칸을 앵커로 받은 syn_cell_box · syn_icon_glow).
        // 크기는 시안(93 × 140)의 0.6배 — PD 「공간을 적게 차지하게 · UI 를 최대한 간소화해야 인게임에 방해가 안 된다」.
        private const float CellW = 56f, CellH = 84f;

        private static void BuildSynColumn(RectTransform hud, TextMeshProUGUI heavy, TextMeshProUGUI number)
        {
            var col = Node(hud, "PartySynColumn");
            col.SetParent(hud, false);
            col.anchorMin = col.anchorMax = new Vector2(0f, 1f);
            col.pivot = new Vector2(0f, 1f);
            col.anchoredPosition = new Vector2(7f, -155f);
            col.sizeDelta = new Vector2(CellW, CellH * 3 + 8f);
            var hit = Img(col, null);
            hit.color = new Color(0f, 0f, 0f, 0f);   // 누르는 자리만 — 그림 없음
            if (col.GetComponent<Button>() == null) col.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            var vl = col.GetComponent<VerticalLayoutGroup>() ?? col.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 4f;
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = vl.childControlHeight = false;
            vl.childForceExpandWidth = vl.childForceExpandHeight = false;

            // 인연 칸 — 맨 위 · 금빛 번짐 + 금빛 육각 테 안에 메인 얼굴 + 인연 이름(두 줄까지)
            var iy = Cell(col, "PartyInyeonCell");
            Glow(iy, "PartyInyeonGlow");
            var ring = Px(Node(iy, "PartyInyeonRing"), iy, 10, 6, 46, 46);
            Img(ring, "syn_cell_on").raycastTarget = false;
            var face = Px(Node(iy, "PartyInyeonFace"), iy, 15, 12, 41, 40);
            Img(face, null).preserveAspect = true;
            if (iy.Find("PartyInyeonTag") is Transform oldTag) Object.DestroyImmediate(oldTag.gameObject);
            Txt(Px(Node(iy, "PartyInyeonLabel"), iy, 5, 47, 51, 79), "인연", 12, Gold, TA.Center, heavy, bold: true, min: 8f);
            iy.Find("PartyInyeonLabel").GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
            Order(iy, "PartyInyeonGlow", "PartyInyeonRing", "PartyInyeonFace", "PartyInyeonLabel");
            iy.gameObject.SetActive(false);

            for (int i = 0; i < 4; i++)
            {
                var c = Cell(col, $"PartySynCell{i}");
                Glow(c, $"PartySynGlow{i}");
                var icon = Px(Node(c, $"PartySynIcon{i}"), c, 10, 6, 46, 46);
                Img(icon, "syn_icon_martial").preserveAspect = true;
                Txt(Px(Node(c, $"PartySynName{i}"), c, 5, 47, 51, 63), "무도가", 12, Gold, TA.Center, heavy, bold: true, min: 8f);
                Txt(Px(Node(c, $"PartySynLabel{i}"), c, 5, 63, 51, 79), "2/3", 12, White, TA.Center, number);
                Order(c, $"PartySynGlow{i}", $"PartySynIcon{i}", $"PartySynName{i}", $"PartySynLabel{i}");
                c.gameObject.SetActive(false);
            }
            col.gameObject.SetActive(false);
        }

        /// <summary>육각 문장 뒤 금빛 번짐(빛 — 그림에 굽지 않고 따로 깐다, 08_fx 2절).</summary>
        private static void Glow(RectTransform cell, string name)
        {
            var g = Px(Node(cell, name), cell, 2, -2, 54, 54);
            var img = Img(g, "syn_icon_glow");
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0.85f);
        }

        /// <summary>자식 그리는 순서를 정한다(앞 이름이 맨 아래).</summary>
        private static void Order(RectTransform parent, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
                if (parent.Find(names[i]) is Transform t) t.SetSiblingIndex(i);
        }

        private static RectTransform Cell(RectTransform col, string name)
        {
            var c = Node(col, name);
            c.anchorMin = c.anchorMax = new Vector2(0f, 1f);
            c.pivot = new Vector2(0f, 1f);
            c.sizeDelta = new Vector2(CellW, CellH);
            var img = Img(c, "syn_cell_box");
            // 원본(186 × 280)을 0.3배로 줄여 쓴다 — 9분할이면 테(16)가 줄지 않아 칸 안에서 겹친다(2026-10-29 검수).
            // 원본 비율(0.664)이 칸(0.667)과 같으니 통째로 줄인다
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            return c;
        }

        // ── 도구 ────────────────────────────────────────────

        /// <summary>늘려 쓰는 부품의 9분할 테 — 메인 칸은 왕관이 오른쪽 위 테 안에 들어가게 오른쪽을 넓게.</summary>
        private static void SliceBorders()
        {
            Border("partyhud_main_frame", new Vector4(24, 20, 36, 22));
        }

        private static void Border(string name, Vector4 b)
        {
            if (AssetImporter.GetAtPath(Dir + name + ".png") is not TextureImporter ti || ti.spriteBorder == b) return;
            ti.spriteBorder = b;
            ti.SaveAndReimport();
        }

        private static Sprite Sp(string name) => name == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(Dir + name + ".png");

        private static RectTransform F(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t as RectTransform;
            Debug.LogWarning($"[빙의 파티 HUD] 노드 없음: {name}");
            return null;
        }

        private static RectTransform Node(RectTransform parent, string name)
        {
            if (parent.Find(name) is RectTransform found) return found;
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>시안 px 상자(왼 · 위 · 오른 · 아래)로 놓는다 — 부모 위 왼쪽 기준.</summary>
        private static RectTransform Px(RectTransform rt, RectTransform parent, float x0, float y0, float x1, float y1)
        {
            if (rt == null || parent == null) return rt;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, -(y0 + y1) * 0.5f);
            return rt;
        }

        private static Image Img(RectTransform rt, string sprite)
        {
            if (rt == null) return null;
            var img = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            img.sprite = Sp(sprite);
            img.color = White;
            img.enabled = true;
            img.type = img.sprite != null && img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            return img;
        }

        private static void Txt(RectTransform rt, string text, float size, Color color, TA align, TextMeshProUGUI like,
                                bool bold = false, float min = 10f)
        {
            if (rt == null) return;
            var t = rt.GetComponent<TextMeshProUGUI>() ?? rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (like != null) { t.font = like.font; t.fontSharedMaterial = like.fontSharedMaterial; }
            t.text = text;
            t.color = color;
            t.alignment = align;
            // 칸을 넘으면 줄여서라도 칸 안에(06_ui 「글자는 삐져나가지 않는다」)
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Min(size, min);
            t.enableWordWrapping = false;
            // ⚠ 줄임표(Ellipsis)로 두면 글자 높이가 칸을 넘을 때 글자가 통째로 사라진다(2026-10-29 시너지 이름 · 인연 꼬리표).
            //   모든 문구가 칸에 들어가는 것은 재서 확인했다(GetPreferredValues) — 넘침 그대로 둔다
            t.overflowMode = TextOverflowModes.Overflow;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
        }

        private static void Size(RectTransform rt, float max)
        {
            if (rt == null || !rt.TryGetComponent<TextMeshProUGUI>(out var t)) return;
            t.fontSize = max;
            t.enableAutoSizing = true;
            t.fontSizeMax = max;
            t.fontSizeMin = Mathf.Min(max, 10f);
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
        }

        /// <summary>막대 채움 — 홈 왼쪽에 붙어 폭만 바뀐다(InGameMainUI SetFill 은 sizeDelta.x 만 쓴다).</summary>
        private static void FillLeft(RectTransform fill)
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(((RectTransform)fill.parent).sizeDelta.x, 0f);
        }

        private static void FitChild(RectTransform parent, string child, float ratio)
        {
            if (parent == null || parent.Find(child) is not RectTransform c) return;
            c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
            c.anchoredPosition = Vector2.zero;
            c.sizeDelta = parent.sizeDelta * ratio;
        }

        private static void Mute(Transform root, params string[] names)
        {
            foreach (var n in names)
            {
                var t = F(root, n);
                if (t == null) continue;
                foreach (var g in t.GetComponents<Graphic>()) g.enabled = false;
            }
        }
    }
}
