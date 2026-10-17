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
    /// 로비 홈 v5 — 혼합 컨셉 「픽셀 연대기 × 쇼케이스」(PD 2026-10-17, 시안 `Projects/AVSR/_exchange/in/mock_uiv5_home.png`).
    /// 상자 · 무료 소환 · 시즌 패스 한 줄을 걷고, 진열 칸 위에 PASS · 미션 · 업적을 띄운다. 모드 셋은 큰 카드로 각각 들어간다.
    ///
    /// 예전 판(LobbyV4Gap · Top · Mid)은 끄기만 한다 — 다른 빌더가 그 안 노드를 찾는다. 로비는 `HomeV5` 가 있으면 그것만 켜고 끈다.
    /// 부품: 키트 `Assets/BaseResource/UiKit/`(atlas/uikit) · 그림 `Assets/BaseResource/HomeV5Art/`.
    /// 좌표는 9:16 화면(720 × 1280) 기준으로 시안(941 × 1672)을 0.765 배 한 값에서 잡았다. 진열 칸 높이만 화면 길이를 따라간다(HomeV5View.Layout).
    /// </summary>
    public static class LobbyHomeV5Builder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Kit = "Assets/BaseResource/UiKit";
        private const string Art = "Assets/BaseResource/HomeV5Art";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string Gr = "Assets/BaseResource/Growth";
        private const string UnitDir = "Assets/BaseResource/Unit";

        private const float TopBar = 120f, NavBar = 112f;   // 위 막대 판(120)은 투명 칸까지 눌림을 막는다 — 그 아래부터
        private const float ScenarioH = 284f, ModeH = 326f, Gap = 10f;

        private static readonly Color Ink = new(0.16f, 0.08f, 0.03f, 1f);
        private static readonly Color Gold = new(1f, 0.85f, 0.32f, 1f);
        private static readonly Color Soft = new(0.86f, 0.9f, 1f, 1f);

        [MenuItem("Tools/Game/로비 — 홈 v5 세우기 (mock_uiv5_home)")]
        public static void Run()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[홈 v5] 글자 본이 없다 — 챕터 창부터 세운다"); return; }
                var old = lobby.transform.Find("HomeV5");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var mid = lobby.transform.Find("LobbyV4Mid");
                foreach (var n in new[] { "LobbyV4Gap", "LobbyV4Top", "LobbyV4Mid" })
                    if (lobby.transform.Find(n) is Transform t) t.gameObject.SetActive(false);
                // 시즌 패스 한 줄은 진열 칸 PASS 단추로 옮겼다(PD 「칸을 차지하지 않게」)
                if (Find(lobby.transform, "PassBadge") is Transform badge) badge.gameObject.SetActive(false);

                var root = new GameObject("HomeV5", typeof(RectTransform)).GetComponent<RectTransform>();
                root.gameObject.layer = lobby.layer;
                root.SetParent(lobby.transform, false);
                root.SetSiblingIndex(mid != null ? mid.GetSiblingIndex() + 1 : 0);
                Stretch(root);
                // 로비 루트의 ScreenFit 이 자식 앵커를 다시 쓴다 — 홈은 제 배치(HomeV5View.Layout)를 갖으니 잠근다
                root.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

                var bg = Pic(root, "HomePageBg", $"{Art}/page_bg.png", 0, 0, 0, 0);
                bg.preserveAspect = false;
                Envelope(bg, 1080f / 1920f);

                // 화면을 꽉 채운다(PD 2026-10-17 「위치만 말고 스케일링으로 꽉 차게」) — 판은 가로로 늘고,
                // 세로는 HomeV5View.Layout 이 9:16 비율 그대로 키운다(태블릿 = 넓게 · 긴 폰 = 높게)
                var body = Node(root, "HomeBody", 0, 0, 0, 0);
                body.anchorMin = Vector2.zero;
                body.anchorMax = Vector2.one;
                body.pivot = new Vector2(0.5f, 0f);
                body.offsetMin = new Vector2(0f, NavBar);
                body.offsetMax = new Vector2(0f, -TopBar);

                var cards = Node(body, "HomeCards", 0, 0, 0, 0);
                HStretch(cards, 0f, 0f, 0f, ScenarioH + Gap + ModeH);
                Scenario(cards);
                Mode(cards, "Tower", 0f, 0.5f, 12f, 4f, $"{Art}/mode_tower.png", true);
                Mode(cards, "Night", 0.5f, 1f, 4f, 12f, $"{Art}/mode_night.png", false);

                var show = Node(body, "HomeShowcase", 0, 0, 0, 0);
                HStretch(show, 12f, 12f, ScenarioH + Gap + ModeH + Gap, 418f);
                Showcase(show);

                Bind(root.gameObject.AddComponent<HomeV5View>());
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[홈 v5] 세움");
        }

        // ── 진열 칸 ──────────────────────────────────────────

        private static void Showcase(RectTransform show)
        {
            var frame = Sliced(show, "HomeShowcaseFrame", $"{Kit}/panel_navy_big.png", 0, 0, 0, 0, 1f);
            StretchIn(frame.rectTransform);
            var view = Node(show, "HomeShowcaseView", 0, 0, 0, 0);
            Inset(view, 7f);
            view.gameObject.AddComponent<RectMask2D>();
            var art = Pic(view, "HomeShowcaseArt", $"{Art}/showcase_bg.png", 0, 0, 0, 0);
            art.preserveAspect = false;
            Envelope(art, 1600f / 830f);

            // 유령은 왼쪽 위에 떠 있고, 고른 몸은 그 오른쪽 아래 바닥에 선다(시안)
            var ghost = Pic(view, "HomeGhost", $"{Gr}/Portraits/portrait_ghost.png", 0, 0, 0, 0);
            Box(ghost.rectTransform, 0.5f, 0.55f, -158f, 0f, 170f, 170f, 0.5f, 0.5f);
            var host = Pic(view, "HomeHost", null, 0, 0, 0, 0);
            Box(host.rectTransform, 0.5f, 0f, -8f, 18f, 310f, 310f, 0.5f, 0f);

            var grade = Pic(view, "HomeGrade", $"{Ch}/ch_grade_b.png", 0, 0, 0, 0);
            Box(grade.rectTransform, 1f, 0f, -28f, 84f, 84f, 50f);
            var kind = Pic(view, "HomeKind", $"{Ch}/ch_kind_blade.png", 0, 0, 0, 0);
            Box(kind.rectTransform, 1f, 0f, -36f, 16f, 66f, 66f);

            // 떠 있는 단추 셋 — 오른쪽 위. 미션 · 업적은 아직 화면이 없어 준비 중 안내(자리만 먼저)
            var pass = IconButton(view, "HomePassButton", -176f, $"{Kit}/icon_pass.png", null);
            var tag = Sliced(pass, "HomePassTag", $"{Kit}/tag_gold.png", 0, 0, 0, 0, 2f);
            Box(tag.rectTransform, 0.5f, 0f, 0f, 6f, 62f, 26f, 0.5f, 0f);
            var tagText = T(tag.transform, "HomePassTagText", 17f, Ink, true, TextAlignmentOptions.Center);
            StretchIn(tagText.rectTransform);
            Plain(tagText);
            var lv = T(pass, "HomePassLv", 18f, Gold, true, TextAlignmentOptions.Center);
            Box(lv.rectTransform, 0.5f, 0f, 0f, 6f, 70f, 24f, 0.5f, 0f);
            lv.gameObject.SetActive(false);
            Dot(pass, "HomePassDot");
            IconButton(view, "HomeMissionButton", -96f, $"{Kit}/icon_mission.png", "ui.home.mission");
            IconButton(view, "HomeAchieveButton", -16f, $"{Kit}/icon_achieve.png", "ui.home.achieve");
        }

        /// <summary>정사각 판 + 아이콘 + 아래 글자(있으면). 오른쪽 위 기준 x.</summary>
        private static RectTransform IconButton(Transform view, string name, float x, string icon, string labelKey)
        {
            var plate = Sliced(view, name, $"{Kit}/icon_plate.png", 0, 0, 0, 0, 1.4f);
            plate.raycastTarget = true;
            Button(plate.gameObject);
            var r = plate.rectTransform;
            Box(r, 1f, 1f, x, -14f, 74f, 88f);
            var img = Pic(r, name.Replace("Button", "Icon"), icon, 0, 0, 0, 0);
            Box(img.rectTransform, 0.5f, 1f, 0f, -8f, 58f, 48f, 0.5f, 1f);
            if (labelKey != null)
            {
                var t = T(r, name.Replace("Button", "Label"), 17f, Color.white, true, TextAlignmentOptions.Center);
                Box(t.rectTransform, 0.5f, 0f, 0f, 6f, 70f, 24f, 0.5f, 0f);
                Localized(t, labelKey);
            }
            return r;
        }

        // ── 시나리오 카드 ─────────────────────────────────────

        private static void Scenario(RectTransform cards)
        {
            var card = Sliced(cards, "HomeScenarioCard", $"{Kit}/panel_parch_big.png", 0, 0, 0, 0, 1f);
            card.raycastTarget = true;
            Button(card.gameObject);
            var r = card.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(12f, -ScenarioH);
            r.offsetMax = new Vector2(-12f, 0f);

            var artFrame = Sliced(r, "HomeScenarioArtFrame", $"{Kit}/panel_navy_mid.png", 0, 0, 0, 0, 1.6f);
            var af = artFrame.rectTransform;
            af.anchorMin = new Vector2(0.48f, 0f);
            af.anchorMax = Vector2.one;
            af.pivot = new Vector2(0.5f, 0.5f);
            af.offsetMin = new Vector2(0f, 13f);
            af.offsetMax = new Vector2(-14f, -13f);
            var view = Node(artFrame.rectTransform, "HomeScenarioArtView", 0, 0, 0, 0);
            Inset(view, 5f);
            view.gameObject.AddComponent<RectMask2D>();
            var art = Pic(view, "HomeScenarioArt", $"{Ch}/ch_art_1.png", 0, 0, 0, 0);
            art.preserveAspect = false;
            Envelope(art, 640f / 448f);

            var title = T(r, "HomeScenarioTitle", 64f, Ink, true, TextAlignmentOptions.Left);
            Box(title.rectTransform, 0f, 1f, 24f, -20f, 306f, 76f, 0f, 1f);
            Heavy(title, 0.45f);
            var pill = Sliced(r, "HomeChapterPill", $"{Kit}/panel_navy_mid.png", 0, 0, 0, 0, 2.6f);
            Box(pill.rectTransform, 0f, 1f, 28f, -100f, 124f, 38f, 0f, 1f);
            var pt = T(pill.transform, "HomeChapterPillText", 21f, Color.white, true, TextAlignmentOptions.Center);
            StretchIn(pt.rectTransform);
            var name = T(r, "HomeChapterName", 30f, Ink, true, TextAlignmentOptions.Left);
            Box(name.rectTransform, 0f, 1f, 26f, -144f, 300f, 42f, 0f, 1f);
            Heavy(name, 0.3f);

            var btn = Sliced(r, "HomeContinueButton", $"{Kit}/btn_gold_wide.png", 0, 0, 0, 0, 1.4f);
            btn.raycastTarget = true;
            Button(btn.gameObject);
            Box(btn.rectTransform, 0f, 0f, 24f, 18f, 294f, 72f, 0f, 0f);
            var bt = T(btn.transform, "HomeContinueText", 38f, Ink, true, TextAlignmentOptions.Center);
            StretchIn(bt.rectTransform);
            Heavy(bt, 0.35f);
        }

        // ── 탑 · 밤 카드 ─────────────────────────────────────

        private static void Mode(RectTransform cards, string key, float ax0, float ax1, float padL, float padR, string artPath, bool pill)
        {
            var card = Sliced(cards, $"Home{key}Card", $"{Kit}/panel_navy_big.png", 0, 0, 0, 0, 1.2f);
            card.raycastTarget = true;
            Button(card.gameObject);
            var r = card.rectTransform;
            r.anchorMin = new Vector2(ax0, 0f);
            r.anchorMax = new Vector2(ax1, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.offsetMin = new Vector2(padL, 0f);
            r.offsetMax = new Vector2(-padR, ModeH);

            var view = Node(r, $"Home{key}View", 0, 0, 0, 0);
            Inset(view, 6f);
            view.gameObject.AddComponent<RectMask2D>();
            var art = Pic(view, $"Home{key}Art", artPath, 0, 0, 0, 0);
            art.preserveAspect = false;
            Envelope(art, 900f / 860f);

            var title = T(r, $"Home{key}Title", 44f, Color.white, true, TextAlignmentOptions.Left);
            Box(title.rectTransform, 0f, 1f, 16f, -10f, 312f, 56f, 0f, 1f);
            if (pill)
            {
                var p = Sliced(r, $"Home{key}SubPill", $"{Kit}/panel_navy_mid.png", 0, 0, 0, 0, 2.6f);
                Box(p.rectTransform, 0f, 1f, 18f, -70f, 120f, 38f, 0f, 1f);
                var st = T(p.transform, $"Home{key}Sub", 22f, Color.white, true, TextAlignmentOptions.Center);
                StretchIn(st.rectTransform);
            }
            else
            {
                var st = T(r, $"Home{key}Sub", 20f, Soft, true, TextAlignmentOptions.Left);
                Box(st.rectTransform, 0f, 1f, 20f, -66f, 300f, 30f, 0f, 1f);
            }

            var foot = Sliced(r, $"Home{key}FootPlate", $"{Kit}/btn_navy.png", 0, 0, 0, 0, 1.6f);
            var fr = foot.rectTransform;
            fr.anchorMin = new Vector2(0f, 0f);
            fr.anchorMax = new Vector2(1f, 0f);
            fr.pivot = new Vector2(0.5f, 0f);
            fr.offsetMin = new Vector2(16f, 14f);
            fr.offsetMax = new Vector2(-16f, 72f);
            var ft = T(foot.transform, $"Home{key}Foot", 24f, Color.white, true, TextAlignmentOptions.Center);
            StretchIn(ft.rectTransform);
            ft.richText = true;
            Dot(r, $"Home{key}Dot");
        }

        // ── 묶기 ─────────────────────────────────────────────

        private static void Bind(HomeV5View view)
        {
            var so = new SerializedObject(view);
            var list = new List<(string key, Sprite sprite)>();
            foreach (var d in Directory.GetDirectories(UnitDir))
            {
                string key = Path.GetFileName(d);
                var drawn = Spr($"{Gr}/Portraits/portrait_{key}.png");
                var s = drawn != null ? drawn : Spr($"{UnitDir}/{key}/unit_{key}_e.png");
                if (s != null) list.Add((key, s));
            }
            Keyed(so, "_portraits", list);

            var arts = new List<Sprite>();
            for (int i = 1; Spr($"{Ch}/ch_art_{i}.png") is Sprite a; i++) arts.Add(a);
            var pa = so.FindProperty("_chapterArt");
            pa.arraySize = arts.Count;
            for (int i = 0; i < arts.Count; i++) pa.GetArrayElementAtIndex(i).objectReferenceValue = arts[i];

            var pg = so.FindProperty("_gradeBadges");
            string[] grades = { "b", "a", "s" };   // HostGrade B · A · S = R · SR · SSR
            pg.arraySize = grades.Length;
            for (int i = 0; i < grades.Length; i++) pg.GetArrayElementAtIndex(i).objectReferenceValue = Spr($"{Ch}/ch_grade_{grades[i]}.png");

            so.FindProperty("_kindBlade").objectReferenceValue = Spr($"{Ch}/ch_kind_blade.png");
            so.FindProperty("_kindForce").objectReferenceValue = Spr($"{Ch}/ch_kind_force.png");
            so.FindProperty("_kindMagic").objectReferenceValue = Spr($"{Ch}/ch_kind_magic.png");
            so.FindProperty("_passIcon").objectReferenceValue = Spr($"{Kit}/icon_pass.png");
            so.FindProperty("_passIconBought").objectReferenceValue = Spr($"{Kit}/icon_pass_gold.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 도우미 ───────────────────────────────────────────

        private static TextMeshProUGUI T(Transform parent, string name, float size, Color color, bool bold, TextAlignmentOptions align)
        {
            var t = bold ? Title(parent, name, 0, 0, 0, 0, size, color) : Body(parent, name, 0, 0, 0, 0, size, color);
            t.alignment = align;
            return t;
        }

        /// <summary>고정 글자는 언어팩이 채운다(코드가 안 채우는 칸).</summary>
        private static void Localized(TextMeshProUGUI t, string key)
        {
            var c = t.gameObject.AddComponent<Game.Module.Common.LocalizedText>();
            var so = new SerializedObject(c);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>시안의 먹색 굵은 글자 — 외곽선을 빼고(Plain) 굵기만 올린다.</summary>
        private static void Heavy(TextMeshProUGUI t, float dilate)
        {
            Plain(t);
            // 본(CHHostTitle)에 굵기 표시가 없으면 붙인다 — 없으면 언어 모듈이 굵은 폰트 · 획 보정을 안 입힌다
            var h = t.GetComponent<Game.Module.Common.HeavyText>();
            if (h == null) h = t.gameObject.AddComponent<Game.Module.Common.HeavyText>();
            h.Set(dilate, 0f, new Color32(0, 0, 0, 0));
        }

        private static void Dot(Transform parent, string name)
        {
            var d = Pic(parent, name, $"{Kit}/dot_red.png", 0, 0, 0, 0);
            Box(d.rectTransform, 1f, 1f, 8f, 8f, 26f, 26f, 1f, 1f);
            d.gameObject.SetActive(false);
        }

        /// <summary>기준점(ax, ay) = 피벗(기본 같은 값)에 (x, y) 만큼.</summary>
        private static void Box(RectTransform r, float ax, float ay, float x, float y, float w, float h, float px = -1f, float py = -1f)
        {
            r.anchorMin = r.anchorMax = new Vector2(ax, ay);
            r.pivot = new Vector2(px < 0f ? ax : px, py < 0f ? ay : py);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>가로로 늘고(좌우 여백) 아래 변에서 y 만큼 위, 높이 h.</summary>
        private static void HStretch(RectTransform r, float padL, float padR, float y, float h)
        {
            r.anchorMin = new Vector2(0f, 0f);
            r.anchorMax = new Vector2(1f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.offsetMin = new Vector2(padL, y);
            r.offsetMax = new Vector2(-padR, y + h);
        }

        private static void Inset(RectTransform r, float pad)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(pad, pad);
            r.offsetMax = new Vector2(-pad, -pad);
        }

        /// <summary>그림이 칸을 덮게(넘치는 쪽은 부모 마스크가 자른다).</summary>
        private static void Envelope(Image img, float aspect)
        {
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            var f = img.gameObject.AddComponent<AspectRatioFitter>();
            f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            f.aspectRatio = aspect;
        }
    }
}
