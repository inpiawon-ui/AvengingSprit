using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// 인게임 팝업 4종(레벨업 · 회복의 제단 · 악마의 거래 · 상점)을 C 「원혼 회로」 배치로 꽂는다 (2026-10-08).
    ///
    /// 값은 `Projects/AVSR/Tools/popup_c_layout.py` 한 곳에서 정하고 `popup_c_layout.json` 으로 넘어온다 —
    /// 파이썬 미리보기와 같은 자를 쓰려고 여기에는 숫자를 적지 않는다.
    /// 그림은 `Projects/AVSR/_exchange/out/setc/` 에서 `Assets/BaseResource/{InGameMainUI|Card}/` 로 먼저 옮겨 둔다.
    ///
    /// 하는 일: 스프라이트 임포트 설정(9칸 늘이기 테두리) → 프리팹 노드 자리 · 크기 · 글자 크기 · 색 → 없는 노드 만들기.
    /// </summary>
    public static class InGamePopupCLayout
    {
        private const string PrefabPath = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string UiArtDir = "Assets/BaseResource/InGameMainUI/";

        // 늘려 쓰는 판 — 테두리(왼 · 아래 · 오른 · 위, 원본 px)는 고정하고 가운데만 늘린다
        private static readonly Dictionary<string, Vector4> SlicedBorders = new()
        {
            { "shrinechoiceslot", new Vector4(80f, 18f, 76f, 18f) },
            { "eventcostpill", new Vector4(24f, 12f, 24f, 12f) },
        };

        [System.Serializable]
        private class Node
        {
            public string panel, name, parent, sprite, create, color, align, before;
            public int x, y, w, h;
            public bool sliced, first, nowrap;
            public float fs, mn;
        }

        [System.Serializable]
        private class Layout
        {
            public Node[] nodes;
        }

        [MenuItem("Tools/Game/인게임 팝업 C 배치 꽂기")]
        public static void Apply()
        {
            var jsonPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Projects", "AVSR", "Tools",
                                                         "popup_c_layout.json"));
            if (!File.Exists(jsonPath)) { Debug.LogError($"[팝업 C] 배치 파일 없음: {jsonPath}"); return; }
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(jsonPath));

            FixImporters();

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            int done = 0, made = 0;
            try
            {
                foreach (var n in layout.nodes)
                {
                    var panel = FindDeep(root.transform, n.panel);
                    if (panel == null) { Debug.LogError($"[팝업 C] 패널 없음 {n.panel}"); continue; }
                    var parent = string.IsNullOrEmpty(n.parent) ? panel : FindDeep(panel, n.parent);
                    if (parent == null) { Debug.LogError($"[팝업 C] 부모 없음 {n.parent}"); continue; }

                    var rt = FindDeep(panel, n.name) as RectTransform;
                    if (rt == null)
                    {
                        if (string.IsNullOrEmpty(n.create)) { Debug.LogError($"[팝업 C] 노드 없음 {n.name}"); continue; }
                        rt = Make(n, parent, panel);
                        made++;
                    }
                    if (rt.parent != parent) rt.SetParent(parent, false);
                    if (n.first) rt.SetAsFirstSibling();
                    // 새로 만든 판은 맨 뒤(맨 위)에 붙는다 — 제 글자를 덮지 않게 그 글자 바로 앞으로
                    if (!string.IsNullOrEmpty(n.before))
                    {
                        var next = FindDeep(parent, n.before);
                        if (next != null && next.parent == rt.parent)
                        {
                            rt.SetSiblingIndex(next.GetSiblingIndex());
                            if (rt.GetSiblingIndex() > next.GetSiblingIndex()) rt.SetSiblingIndex(next.GetSiblingIndex());
                        }
                    }
                    if (n.create == "text") BorrowFont(rt, parent, panel);

                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(n.x, -n.y);
                    rt.sizeDelta = new Vector2(n.w, n.h);

                    if (!string.IsNullOrEmpty(n.sprite)) SetSprite(rt, n);
                    if (n.fs > 0f) SetText(rt, n);
                    done++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"[팝업 C] 노드 {done}개 꽂음 · 새로 만든 것 {made}개");
        }

        // 새로 들어온 그림 — 처음 들어오면 기본(텍스처)로 잡혀 스프라이트로 못 쓴다
        private static readonly string[] NewArt =
        {
            "levelupframe", "eventrewardpill", "shopgoldpill",
            "obj_ring_heal", "obj_ring_devil", "obj_ring_shop", "obj_mark_heal", "obj_mark_devil", "obj_mark_shop",
            "shrine_full_heal", "shrine_soul_heal", "shrine_max_hp", "shrine_atk", "shrine_speed", "shrine_range",
            "shop_bomb", "shop_freeze", "shop_ally", "shop_heal",
        };

        private static void FixImporters()
        {
            foreach (var name in NewArt)
            {
                var imp = AssetImporter.GetAtPath(UiArtDir + name + ".png") as TextureImporter;
                if (imp == null) { Debug.LogWarning($"[팝업 C] 그림 없음 {name}"); continue; }
                if (imp.textureType == TextureImporterType.Sprite) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            foreach (var kv in SlicedBorders)
            {
                var imp = AssetImporter.GetAtPath(UiArtDir + kv.Key + ".png") as TextureImporter;
                if (imp == null) { Debug.LogWarning($"[팝업 C] 그림 없음 {kv.Key}"); continue; }
                if (imp.spriteBorder == kv.Value) continue;
                imp.spriteBorder = kv.Value;
                imp.SaveAndReimport();
            }
        }

        private static RectTransform Make(Node n, Transform parent, Transform panel)
        {
            var go = new GameObject(n.name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (n.create == "image")
            {
                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
            }
            else
            {
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;
            }
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// 새로 만든 글자 칸은 **같은 부모 안의 본문 글자**에서 글꼴 · 재질을 빌린다 — 언어팩이 글꼴을 갈아 끼울 때 같이 바뀌게.
        /// ⚠ 패널 첫 글자(제목)를 빌렸더니 제목의 넓은 자간까지 따라와 설명이 「고 스 트  체 력」처럼 벌어졌다(2026-10-08).
        /// </summary>
        private static void BorrowFont(RectTransform rt, Transform parent, Transform panel)
        {
            var tmp = rt.GetComponent<TMP_Text>();
            if (tmp == null) return;
            TMP_Text like = null;
            for (int i = 0; i < parent.childCount && like == null; i++)
            {
                var c = parent.GetChild(i);
                if (c == rt) continue;
                var cand = c.GetComponent<TMP_Text>();
                // 자간을 벌린 제목 글자는 빌리지 않는다
                if (cand != null && cand.characterSpacing == 0f && !c.name.Contains("Title")) like = cand;
            }
            if (like == null)
            {
                foreach (var t in panel.GetComponentsInChildren<TMP_Text>(true))
                    if (t != tmp && t.characterSpacing == 0f) { like = t; break; }
            }
            if (like == null) return;
            tmp.font = like.font;
            tmp.fontSharedMaterial = like.fontSharedMaterial;
            tmp.fontStyle = like.fontStyle;
            tmp.characterSpacing = 0f;
            tmp.wordSpacing = 0f;
        }

        private static void SetSprite(RectTransform rt, Node n)
        {
            var img = rt.GetComponent<Image>();
            if (img == null) return;
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(UiArtDir + n.sprite + ".png");
            if (sp == null) { Debug.LogWarning($"[팝업 C] 스프라이트 없음 {n.sprite}"); return; }
            img.sprite = sp;
            img.color = Color.white;
            img.type = n.sliced && SlicedBorders.ContainsKey(n.sprite) ? Image.Type.Sliced : Image.Type.Simple;
            img.enabled = true;
        }

        private static void SetText(RectTransform rt, Node n)
        {
            var tmp = rt.GetComponent<TMP_Text>();
            if (tmp == null) return;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = n.fs;
            tmp.fontSizeMin = n.mn;
            tmp.fontSize = n.fs;
            if (!string.IsNullOrEmpty(n.color) && ColorUtility.TryParseHtmlString(n.color, out var c)) tmp.color = c;
            tmp.alignment = n.align == "left" ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            tmp.textWrappingMode = n.nowrap ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
