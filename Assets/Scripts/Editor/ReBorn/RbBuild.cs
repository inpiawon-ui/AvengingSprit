// RE:BORN UI 리뉴얼(2026-10-23) — 새 화면 빌더 공용 도구.
// 시안(1080 × 2340, 위 140 · 아래 110 은 노치 · 홈 표시줄 여백)을 게임 720 기준으로 옮긴다. 스킬 ui-mockup-to-game 1단계:
// 「부품마다 시안 픽셀 상자를 재고, 시안 좌표 → 앵커 위치 도우미 하나로만 배치한다」.
using Game.Module.Common;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.ReBorn
{
    /// <summary>시안 상자를 어느 쪽 가장자리에 붙이나. 16:9 는 20:9 시안보다 세로가 짧아 가운데가 줄어든다.</summary>
    public enum Edge { Top, Bottom, Center }

    public static class RbBuild
    {
        /// <summary>시안 1080 → 게임 720.</summary>
        public const float S = 2f / 3f;
        /// <summary>시안에서 내용이 시작 · 끝나는 줄(위 노치 · 아래 홈 표시줄 여백을 뺀 곳).</summary>
        public const float MockTop = 140f, MockBottom = 2230f;
        /// <summary>가운데 붙이기의 기준 줄(시안 내용의 가운데).</summary>
        public const float MockMid = (MockTop + MockBottom) * 0.5f;

        public const string Kit = "Assets/BaseResource/ReBornKit";
        public const string FontDir = "Assets/BaseResource/Fonts/ReBorn";

        // 디자인 가이드의 글자 색 — 흰 · 흐린 흰 · 노랑(주요) · 초록(상승) · 빨강(부족) · 시안
        public static readonly Color White = Color.white;
        public static readonly Color Dim = new(0.70f, 0.74f, 0.84f, 1f);
        public static readonly Color Yellow = new(1f, 0.79f, 0.24f, 1f);
        public static readonly Color Green = new(0.29f, 0.88f, 0.54f, 1f);
        public static readonly Color Red = new(1f, 0.35f, 0.35f, 1f);
        public static readonly Color Cyan = new(0.24f, 0.90f, 1f, 1f);
        public static readonly Color Ink = new(0.16f, 0.11f, 0.03f, 1f);   // 노란 버튼 위 글자

        private static TMP_FontAsset s_body, s_heavy, s_number;
        public static TMP_FontAsset Body => s_body ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Pretendard-SemiBold SDF.asset");
        public static TMP_FontAsset Heavy => s_heavy ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Pretendard-Bold SDF.asset");
        public static TMP_FontAsset Number => s_number ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Galmuri11-Bold SDF.asset");

        // ── 자리 ────────────────────────────────────────────

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent != null ? parent.gameObject.layer : 5;
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// 시안 상자(1080 기준 왼 · 위 · 오른 · 아래, 화면 전체 좌표)를 부모(화면 전체 판) 안에 놓는다.
        /// 가로는 **가운데 앵커**(규칙 05 — 왼쪽에 못 박으면 넓은 화면에서 쏠린다), 세로는 붙일 가장자리 기준.
        /// </summary>
        public static RectTransform Place(RectTransform rt, Edge edge, float x0, float y0, float x1, float y1)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            float ay = edge == Edge.Top ? 1f : edge == Edge.Bottom ? 0f : 0.5f;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2((x1 - x0) * S, (y1 - y0) * S);
            float y = edge == Edge.Top ? -(cy - MockTop) * S
                    : edge == Edge.Bottom ? (MockBottom - cy) * S
                    : -(cy - MockMid) * S;
            rt.anchoredPosition = new Vector2((cx - 540f) * S, y);
            return rt;
        }

        /// <summary>부모 안 상대 상자 — 부모의 시안 상자(px0, py0)를 원점으로 한 자식 상자.</summary>
        public static RectTransform Local(RectTransform rt, float px0, float py0, float px1, float py1,
                                          float x0, float y0, float x1, float y1)
        {
            float pw = (px1 - px0), ph = (py1 - py0);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2((x1 - x0) * S, (y1 - y0) * S);
            float cx = (x0 + x1) * 0.5f - px0 - pw * 0.5f, cy = (y0 + y1) * 0.5f - py0 - ph * 0.5f;
            rt.anchoredPosition = new Vector2(cx * S, -cy * S);
            return rt;
        }

        /// <summary>
        /// 스크롤 내용 안 — 위 기준 줄(refTop, 시안 y)에서 잰 상자. 내용 판은 위에 붙고 가로 가운데가 시안 540 이다.
        /// </summary>
        public static RectTransform PlaceIn(RectTransform rt, float refTop, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2((x1 - x0) * S, (y1 - y0) * S);
            rt.anchoredPosition = new Vector2(((x0 + x1) * 0.5f - 540f) * S, -((y0 + y1) * 0.5f - refTop) * S);
            return rt;
        }

        /// <summary>
        /// 세로 스크롤 판 — 시안 y top ~ bottom 사이(위는 위 가장자리, 아래는 아래 가장자리 기준이라 16:9 에서 줄어든다).
        /// 보기 칸은 RectMask2D 만(규칙 05). 내용 판(돌려줌)은 위에 붙고 높이는 시안 contentH.
        /// 내용이 판보다 작으면(20:9) 움직이지 않는다(Clamped).
        /// </summary>
        public static RectTransform VScroll(RectTransform parent, string name, float top, float bottom, float contentH)
        {
            var view = Node(parent, name);
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one; view.pivot = new Vector2(0.5f, 0.5f);
            view.offsetMin = new Vector2(0f, (MockBottom - bottom) * S);
            view.offsetMax = new Vector2(0f, -(top - MockTop) * S);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);   // 끌기를 받을 칸(보이지 않음)
            var content = Node(view, name + "Content");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, contentH * S); content.anchoredPosition = Vector2.zero;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = view; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = true;
            return content;
        }

        /// <summary>막대 — 바탕 + 채움(채움은 왼쪽 기준, 코드가 anchorMax.x 로 비율을 건다).</summary>
        public static RectTransform Bar(RectTransform rt, string fillName, string fillPath)
        {
            Img(rt, "Components/rb_bar_track").preserveAspect = false;
            var fill = Node(rt, fillName);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.5f, 1f); fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = Vector2.zero; fill.anchoredPosition = Vector2.zero;
            Img(fill, fillPath).preserveAspect = false;
            return fill;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        // ── 그림 ────────────────────────────────────────────

        public static Sprite KitSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/{path}.png");
            if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(path.EndsWith(".png") ? path : path + ".png");
            if (s == null) Debug.LogWarning($"[RE:BORN 빌더] 그림 없음: {path}");
            return s;
        }

        /// <summary>그림 판. 9-slice 테두리가 있으면 Sliced(늘려 쓰는 판 — R2).</summary>
        public static Image Img(RectTransform rt, string path, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = KitSprite(path);
            img.raycastTarget = raycast;
            if (img.sprite != null && img.sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
                // 테두리는 720 기준으로 잘라 둔 크기 그대로
                img.pixelsPerUnitMultiplier = 1f;
            }
            else img.preserveAspect = true;
            return img;
        }

        /// <summary>그림이 아직 없는 자리 — 색 판으로 세우고 보고에 「그림 필요」로 적는다(스킬 2단계).</summary>
        public static Image Placeholder(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        // ── 글자 ────────────────────────────────────────────

        /// <summary>
        /// 글자 칸. 크기는 시안 1080 기준 px(720 으로 줄인다). 넘치면 자동 축소 — 하한은 시안 32 → 21(묶음 규칙 「글자 최소 32」).
        /// key 가 있으면 고정 글자(LocalizedText), 없으면 코드가 채우는 칸.
        /// </summary>
        public static TextMeshProUGUI Text(RectTransform rt, string key, string korean, float size1080, Color color,
                                           TextAlignmentOptions align = TextAlignmentOptions.Center, bool heavy = false,
                                           bool number = false)
        {
            // ⚠ 꺼진 계층에 붙인 TMP 는 실행 때 처음 켜지며 기본값(크기 36 · 최대 72)으로 돌아간다(2026-10-23 설정 창) — 다 만든 뒤에 끈다
            if (!rt.gameObject.activeInHierarchy) Debug.LogWarning($"[RE:BORN 빌더] 꺼진 곳에 글자를 붙임 — 실행 때 크기가 풀린다: {rt.name}");
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = number ? Number : heavy ? Heavy : Body;
            t.text = korean;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true;
            t.fontSizeMax = size1080 * S;
            t.fontSizeMin = Mathf.Min(size1080, 32f) * S;
            t.raycastTarget = false;
            if (heavy && !number) rt.gameObject.AddComponent<HeavyText>();
            if (!string.IsNullOrEmpty(key)) rt.gameObject.AddComponent<LocalizedText>().SetKey(key);
            return t;
        }

        // ── 버튼 ────────────────────────────────────────────

        /// <summary>
        /// 버튼 — 키트 그림 `{base}` · `{base}_pressed` · `{base}_disabled` 를 그림 바꾸기로 건다
        /// (누름 = 두께만큼 내려간 그림 — 크기를 바꾸지 않는다, 묶음 규칙).
        /// </summary>
        public static Button Btn(RectTransform rt, string basePath)
        {
            var img = Img(rt, basePath, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var pressed = AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/{basePath}_pressed.png");
            var disabled = AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/{basePath}_disabled.png");
            if (pressed != null || disabled != null)
            {
                b.transition = Selectable.Transition.SpriteSwap;
                b.spriteState = new SpriteState { pressedSprite = pressed, selectedSprite = null, disabledSprite = disabled };
            }
            else b.transition = Selectable.Transition.ColorTint;
            return b;
        }

        /// <summary>그림 없는 투명 누름 칸(아이콘 + 글자 묶음 등). 터치 영역 = 보이는 크기.</summary>
        public static Button HitArea(RectTransform rt)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            return b;
        }

        // ── 프리팹 ───────────────────────────────────────────

        public static GameObject Root(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Stretch((RectTransform)go.transform);
            return go;
        }

        public static void Save(GameObject root, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        /// <summary>화면 프리팹을 Addressable `ui` 그룹에 건다(WireScenesAndPrefabs 와 같은 그룹 · 라벨 규칙).</summary>
        public static void Register(string path, string address, string label)
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var group = settings != null ? settings.FindGroup("ui") : null;
            if (group == null) { Debug.LogError("[RE:BORN 빌더] Addressable 그룹 ui 없음"); return; }
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = address;
            if (!string.IsNullOrEmpty(label)) entry.SetLabel(label, true, true);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
