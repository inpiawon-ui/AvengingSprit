using System.Collections.Generic;
using System.IO;
using Game.Module.Common;
using Game.Module.InGame;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// 인게임 팝업 4종(레벨업 · 회복의 제단 · 악마의 거래 · 상점) + 챕터 클리어 결과창을 C 「원혼 회로」로 꽂는다 (2026-10-08).
    ///
    /// 값은 두 파일이 정한다 — 여기에는 숫자를 적지 않는다(파이썬 미리보기와 같은 자):
    ///   `Projects/AVSR/Tools/popup_c_layout.py` → `popup_c_layout.json`  자리 · 크기 · 글자 역할 · 늘이기 테두리
    ///   `Projects/AVSR/Tools/fx_story.py`       → `popup_fx.json`        창 연출 층(PopupFxSpec)
    /// 그림은 `Projects/AVSR/_exchange/out/` 에서 `Assets/BaseResource/{InGameMainUI|Card|ChapterScreens}/` 로 먼저 옮겨 둔다.
    ///
    /// 글자 역할(코덱스 합의 consult_popup_typography.md): 굵은 칸은 진짜 굵은 폰트 + HeavyText(획 · 외곽선) —
    /// 언어가 바뀌면 LanguageModule 이 그 언어의 굵은 폰트로 갈아 끼운다. 가짜 굵게(fontStyle Bold)는 끈다.
    /// </summary>
    public static class InGamePopupCLayout
    {
        private const string PrefabPath = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private static readonly string[] ArtDirs =
        {
            "Assets/BaseResource/InGameMainUI/", "Assets/BaseResource/ChapterScreens/", "Assets/BaseResource/Card/",
        };
        private const string FontDir = "Assets/BaseResource/Fonts/";
        private const string AdditiveMatPath = "Assets/BaseResource/Shaders/UIAdditiveSprite.mat";

        [System.Serializable]
        private class Node
        {
            public string panel, name, parent, sprite, create, color, align, before, role, outlineColor, group, font;
            public int x, y, w, h;
            public bool sliced, first, nowrap, heavy, skip, hide;
            public float fs, mn, dilate, outline, lineSpacing;
        }

        [System.Serializable]
        private class Border
        {
            public string sprite;
            public float l, b, r, t;
        }

        [System.Serializable]
        private class Layout
        {
            public Node[] nodes;
            public Border[] borders;
        }

        // popup_fx.json — 화면 이름 키가 바뀌는 사전이라 JsonUtility 로 못 읽는다. 간단한 파서로 읽는다.
        [System.Serializable]
        private class FxLayer
        {
            public string frames, phase, atNode;
            public float x, y, w, h, step, t0, t1, alpha, fade, x2, y2;
            public bool loop, additive, flip, back, beam, fromAvatar, untilAccept, inNode;
            public int[] tint;
        }

        [System.Serializable]
        private class FxScreen
        {
            public string panel;
            public FxLayer[] layers;
        }

        [System.Serializable]
        private class FxAll
        {
            public FxScreen[] screens;
        }

        [MenuItem("Tools/Game/인게임 팝업 C 배치 꽂기")]
        public static void Apply()
        {
            string tools = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Projects", "AVSR", "Tools"));
            var layoutPath = Path.Combine(tools, "popup_c_layout.json");
            if (!File.Exists(layoutPath)) { Debug.LogError($"[팝업 C] 배치 파일 없음: {layoutPath}"); return; }
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(layoutPath));
            var borders = new Dictionary<string, Vector4>();
            if (layout.borders != null)
                foreach (var b in layout.borders) borders[b.sprite] = new Vector4(b.l, b.b, b.r, b.t);

            FixImporters(borders);
            BuildFxAtlas();
            var additive = EnsureAdditiveMaterial();
            var fonts = LoadFonts();

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            int done = 0, made = 0;
            try
            {
                foreach (var n in layout.nodes)
                {
                    if (n.skip) continue;
                    var panel = FindDeep(root.transform, n.panel);
                    if (panel == null) { Debug.LogError($"[팝업 C] 패널 없음 {n.panel}"); continue; }
                    var parent = string.IsNullOrEmpty(n.parent) ? panel : FindDeep(panel, n.parent);
                    if (parent == null) { Debug.LogError($"[팝업 C] 부모 없음 {n.parent}"); continue; }

                    var rt = FindDeep(panel, n.name) as RectTransform;
                    if (rt == null)
                    {
                        if (string.IsNullOrEmpty(n.create)) { Debug.LogError($"[팝업 C] 노드 없음 {n.name}"); continue; }
                        rt = Make(n, parent);
                        made++;
                    }
                    if (rt.parent != parent) rt.SetParent(parent, false);
                    if (n.first) rt.SetAsFirstSibling();
                    // 새로 만든 판은 맨 뒤(맨 위)에 붙는다 — 제 글자를 덮지 않게 그 글자 바로 앞으로
                    if (!string.IsNullOrEmpty(n.before))
                    {
                        var next = FindDeep(parent, n.before);
                        // ⚠ 이미 앞에 있으면 건드리지 않는다 — 앞에 있는 것을 next 자리로 옮기면 next 뒤로 가서,
                        //   돌릴 때마다 순서가 뒤집혔다(상점 골드 알약이 글자를 덮음, 2026-10-08 게임 검사)
                        if (next != null && next.parent == rt.parent && rt.GetSiblingIndex() > next.GetSiblingIndex())
                            rt.SetSiblingIndex(next.GetSiblingIndex());
                    }

                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(n.x, -n.y);
                    rt.sizeDelta = new Vector2(n.w, n.h);

                    if (!string.IsNullOrEmpty(n.sprite)) SetSprite(rt, n, borders);
                    if (n.hide && rt.GetComponent<Image>() is Image hidden)
                    {
                        // 그림은 틀에 그려져 있다 — 누르기만 받는다
                        hidden.sprite = null;
                        hidden.color = new Color(1f, 1f, 1f, 0f);
                    }
                    if (n.fs > 0f) SetText(rt, n, panel, fonts);
                    done++;
                }
                int fxCount = BakeFx(root.transform, tools, additive);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[팝업 C] 노드 {done}개 꽂음 · 새로 만든 것 {made}개 · 연출 층 {fxCount}개");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ── 연출 층 굽기 ───────────────────────────────────────
        private static int BakeFx(Transform root, string tools, Material additive)
        {
            var path = Path.Combine(tools, "popup_fx.json");
            if (!File.Exists(path)) { Debug.LogWarning("[팝업 C] popup_fx.json 없음 — 연출은 건너뛴다"); return 0; }
            // {"devil": {...}, "levelup": {...}} → {"screens": [{...}, {...}]} 로 바꿔 JsonUtility 로 읽는다
            var all = JsonUtility.FromJson<FxAll>(ToArrayJson(File.ReadAllText(path)));
            int count = 0;
            foreach (var s in all.screens)
            {
                var panel = FindDeep(root, s.panel);
                if (panel == null) { Debug.LogWarning($"[팝업 C] 연출 패널 없음 {s.panel}"); continue; }
                var spec = panel.GetComponent<PopupFxSpec>();
                if (spec == null) spec = panel.gameObject.AddComponent<PopupFxSpec>();
                var layers = new PopupFxLayer[s.layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    var l = s.layers[i];
                    var t = l.tint != null && l.tint.Length >= 4 ? l.tint : new[] { 255, 255, 255, 255 };
                    layers[i] = new PopupFxLayer
                    {
                        Frames = l.frames, Phase = l.phase, Back = l.back, X = l.x, Y = l.y, W = l.w, H = l.h,
                        Step = l.step, T0 = l.t0, T1 = l.t1, Loop = l.loop, Additive = l.additive,
                        Tint = new Color(t[0] / 255f, t[1] / 255f, t[2] / 255f, t[3] / 255f), Alpha = l.alpha,
                        Flip = l.flip, Fade = l.fade, Beam = l.beam, X2 = l.x2, Y2 = l.y2, FromAvatar = l.fromAvatar,
                        AtNode = l.atNode, UntilAccept = l.untilAccept, InNode = l.inNode,
                    };
                }
                spec.Set(layers, additive);
                count += layers.Length;
            }
            return count;
        }

        /// <summary>최상위 사전을 배열로 — 각 값 객체를 그대로 이어 붙인다(값 안의 중괄호 짝을 센다).</summary>
        private static string ToArrayJson(string src)
        {
            var parts = new List<string>();
            int depth = 0, start = -1;
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                if (c == '"')
                {
                    for (i++; i < src.Length && src[i] != '"'; i++) if (src[i] == '\\') i++;
                    continue;
                }
                if (c == '{') { depth++; if (depth == 2) start = i; }
                else if (c == '}') { if (depth == 2 && start >= 0) { parts.Add(src.Substring(start, i - start + 1)); start = -1; } depth--; }
            }
            return "{\"screens\":[" + string.Join(",", parts) + "]}";
        }

        // ── 연출 아틀라스 ─────────────────────────────────────
        //
        // 연출 프레임(fx_*, 230장 남짓)은 창 그림과 따로 묶는다(`atlas/popupfx`). 창 아틀라스에 넣으면 무압축 페이지가
        // 여러 장 더 생긴다. 빛 번짐 그림이라 블록 압축해도 티가 안 나서 압축 · 2048 페이지로 묶는다.

        private const string FxDir = "Assets/BaseResource/PopupFx";
        private const string FxAtlasPath = "Assets/BundleResource/Atlas/popupfx.spriteatlasv2";

        private static void BuildFxAtlas()
        {
            if (!AssetDatabase.IsValidFolder(FxDir)) { Debug.LogError($"[팝업 C] 연출 그림 폴더 없음: {FxDir}"); return; }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FxDir }))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is not TextureImporter ti) continue;
                if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled && ti.alphaIsTransparency) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }

            var atlas = File.Exists(FxAtlasPath) ? SpriteAtlasAsset.Load(FxAtlasPath) : null;
            if (atlas == null)
            {
                atlas = new SpriteAtlasAsset();
                atlas.Add(new[] { AssetDatabase.LoadAssetAtPath<Object>(FxDir) });
                SpriteAtlasAsset.Save(atlas, FxAtlasPath);
                AssetDatabase.ImportAsset(FxAtlasPath, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(FxAtlasPath) is SpriteAtlasImporter imp)
            {
                imp.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4 };
                imp.textureSettings = new SpriteAtlasTextureSettings
                {
                    filterMode = FilterMode.Bilinear, generateMipMaps = false, sRGB = true,
                };
                imp.SetPlatformSettings(new TextureImporterPlatformSettings
                {
                    name = "DefaultTexturePlatform", overridden = true, maxTextureSize = 2048,
                    textureCompression = TextureImporterCompression.Compressed,
                });
                imp.SaveAndReimport();
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings != null ? settings.FindGroup("atlas") : null;
            if (group == null) { Debug.LogError("[팝업 C] Addressable 그룹 atlas 없음"); return; }
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(FxAtlasPath), group);
            entry.address = "atlas/popupfx";
            entry.SetLabel("label_atlas", true);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();   // 안 하면 주소 등록이 파일에 안 남아 빌드에서 아틀라스를 못 찾는다(2026-10-08)
            Debug.Log("[팝업 C] 연출 아틀라스 atlas/popupfx ← " + FxDir);
        }

        // ── 그림 ───────────────────────────────────────────────
        private static void FixImporters(Dictionary<string, Vector4> borders)
        {
            foreach (var dir in ArtDirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*.png"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var imp = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                    if (imp == null) continue;
                    bool dirty = false;
                    if (imp.textureType != TextureImporterType.Sprite)
                    {
                        imp.textureType = TextureImporterType.Sprite;
                        imp.spriteImportMode = SpriteImportMode.Single;
                        imp.spritePixelsPerUnit = 100f;
                        imp.filterMode = FilterMode.Point;
                        imp.mipmapEnabled = false;
                        imp.alphaIsTransparency = true;
                        imp.textureCompression = TextureImporterCompression.Uncompressed;
                        dirty = true;
                    }
                    if (borders.TryGetValue(name, out var b) && imp.spriteBorder != b)
                    {
                        imp.spriteBorder = b;
                        dirty = true;
                    }
                    if (dirty) imp.SaveAndReimport();
                }
            }
        }

        private static Material EnsureAdditiveMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AdditiveMatPath);
            if (m != null) return m;
            var shader = Shader.Find("UI/AdditiveSprite");
            if (shader == null) { Debug.LogError("[팝업 C] UI/AdditiveSprite 셰이더 없음"); return null; }
            m = new Material(shader) { name = "UIAdditiveSprite" };
            AssetDatabase.CreateAsset(m, AdditiveMatPath);
            return m;
        }

        private static Sprite FindSprite(string name)
        {
            foreach (var dir in ArtDirs)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(dir + name + ".png");
                if (sp != null) return sp;
            }
            return null;
        }

        private static void SetSprite(RectTransform rt, Node n, Dictionary<string, Vector4> borders)
        {
            var img = rt.GetComponent<Image>();
            if (img == null) return;
            var sp = FindSprite(n.sprite);
            if (sp == null) { Debug.LogWarning($"[팝업 C] 스프라이트 없음 {n.sprite}"); return; }
            img.sprite = sp;
            img.color = Color.white;
            img.type = n.sliced && borders.ContainsKey(n.sprite) ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = false;
            img.enabled = true;
        }

        // ── 글자 ───────────────────────────────────────────────
        private sealed class Fonts
        {
            public TMP_FontAsset Regular, Bold, Pixel;
            public Material Soft;
        }

        private static Fonts LoadFonts() => new()
        {
            Regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "NotoSansKR SDF.asset"),
            Bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "NotoSansKR-Bold SDF.asset"),
            Pixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "OriginalPixel SDF.asset"),
            Soft = AssetDatabase.LoadAssetAtPath<Material>(FontDir + "NotoSansKR SDF - Soft.mat"),
        };

        private static RectTransform Make(Node n, Transform parent)
        {
            var go = new GameObject(n.name, typeof(RectTransform)) { layer = 5 };
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

        private static void SetText(RectTransform rt, Node n, Transform panel, Fonts f)
        {
            var tmp = rt.GetComponent<TMP_Text>();
            if (tmp == null) return;
            var heavy = rt.GetComponent<HeavyText>();
            if (n.font == "pixel")
            {
                if (f.Pixel != null) { tmp.font = f.Pixel; tmp.fontSharedMaterial = f.Pixel.material; }
                if (heavy != null) Object.DestroyImmediate(heavy, true);
            }
            else if (n.heavy)
            {
                if (f.Bold != null) tmp.font = f.Bold;
                if (heavy == null) heavy = rt.gameObject.AddComponent<HeavyText>();
                var oc = ParseColor(n.outlineColor, new Color32(7, 16, 26, 230));
                heavy.Set(n.dilate, n.outline, oc);
                // 에디터에서도 보이게 — 런타임은 LanguageModule 이 언어별 굵은 폰트로 같은 값을 다시 입힌다
                if (f.Bold != null) tmp.fontSharedMaterial = f.Bold.material;
            }
            else
            {
                if (f.Regular != null) tmp.font = f.Regular;
                if (f.Soft != null) tmp.fontSharedMaterial = f.Soft;
                if (heavy != null) Object.DestroyImmediate(heavy, true);
            }
            tmp.fontStyle &= ~FontStyles.Bold;      // 가짜 굵게 금지
            tmp.characterSpacing = 0f;
            tmp.wordSpacing = 0f;
            tmp.lineSpacing = n.lineSpacing;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = n.fs;
            tmp.fontSizeMin = n.mn;
            tmp.fontSize = n.fs;
            if (!string.IsNullOrEmpty(n.color) && ColorUtility.TryParseHtmlString(n.color, out var c))
            {
                tmp.color = c;
                // 결과창 제목은 금빛 세로 그라데이션이 색에 곱해진다 — 흰 바탕일 때만 남긴다
                if (n.role != "result_title") tmp.enableVertexGradient = false;
            }
            tmp.alignment = n.align == "left" ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            tmp.textWrappingMode = n.nowrap ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        private static Color32 ParseColor(string hex, Color32 fallback)
            => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : fallback;

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
