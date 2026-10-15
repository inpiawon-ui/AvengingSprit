using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Game.Editor.UiHarness
{
    /// <summary>
    /// 화면의 보이는 UI 부품을 전부 훑어 레이아웃·리소스 규칙을 잰다. 게임을 모른다 — 판·예외는 <see cref="UiHarnessRules"/>.
    /// 좌표는 화면 픽셀(아래가 0, 오버레이 캔버스 기준).
    ///
    /// <para>레이아웃 — C1 담기(명시한 판: 실패 · 추정 판: 경고) · C2 마스크에 일부 잘림 · C3 화면 밖 · C4 글자 넘침(하한 축소는 경고) · C5 글자 겹침
    /// · C6 글자가 뒤에 그려진 판에 가림(테두리처럼 가운데가 빈 9분할 판은 테 띠만 가린다고 본다).</para>
    /// <para>리소스 — R1 그림 비율 찌그러짐 · R2 9분할 테두리 없이 늘림 · R3 원본보다 크게 확대(흐림, 경고) · R4 원본이 표시보다 4배 넘게 큼(용량, 경고).</para>
    /// <para>글자 범위는 TMP 글자 상자(quad)라 실제 잉크보다 조금 넓다 — 경계 판정은 캡처로 다시 본다.</para>
    /// </summary>
    public static class UiLayoutChecker
    {
        public sealed class Violation
        {
            public string Rule;
            public UiSeverity Severity;
            public string Path;
            public string Against;
            public Rect Box;
            public Rect Limit;
            public float Over;
            public string Note;
        }

        private const float Slack = 1.5f;
        private const float MinOverlapArea = 6f;
        private const float MinOverlapSide = 3f;
        private const float GlyphInsetX = 0.08f;
        private const float GlyphInsetY = 0.15f;   // 글자 상자 끝이 1px 남짓 스치는 것은 겹침이 아니다
        private const float AspectTolerance = 0.06f;
        private const float UpscaleLimit = 1.25f;
        private const float OversizeLimit = 4f;

        private static readonly Vector3[] s_corners = new Vector3[4];

        public static List<Violation> Check(Transform root, UiHarnessRules rules)
        {
            var result = new List<Violation>();
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            var texts = new List<(TextMeshProUGUI Label, Rect Ink, string Path)>();
            var containers = FindContainers(root, rules);
            Prepare(root, rules);
            var hidden = HiddenLayers(root, rules);
            var modal = TopModal(root, rules, hidden);
            var drawn = new List<(Graphic Graphic, string Path)>();   // 그리는 순서(위계 깊이 우선) — C6 가림 판정

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(false))
            {
                if (!IsVisible(graphic) || IsUnder(graphic.transform, hidden)) continue;

                // 모달 뒤 화면 — 레이아웃 검사(대상·가리개 양쪽)에서 뺀다. 리소스 결함은 가림과 무관하므로 그림만 잰다.
                if (modal != null && graphic.transform != modal && !graphic.transform.IsChildOf(modal))
                {
                    if (graphic is Image behind && behind.sprite != null)
                    {
                        var behindInk = InkRect(graphic);
                        if (behindInk.width > 0f && behindInk.height > 0f)
                            CheckResource(result, rules, behind, PathOf(graphic.transform, root), behindInk);
                    }

                    continue;
                }

                drawn.Add((graphic, null));

                var label = graphic as TextMeshProUGUI;
                if (label != null && string.IsNullOrWhiteSpace(label.text)) continue;

                string path = PathOf(graphic.transform, root);
                var ink = InkRect(graphic);
                if (ink.width <= 0f || ink.height <= 0f) continue;

                // C2 — 모든 위 마스크의 교집합. 통째로 밖이면 보이지 않으니(스크롤로 비켜 남) 나머지 검사도 건너뛴다.
                if (TryClip(graphic.transform, out var clip, out string clipPath))
                {
                    if (!clip.Overlaps(ink)) continue;
                    Add(result, rules, "C2", UiSeverity.Fail, path, clipPath, ink, clip);
                }

                if (ink.width * ink.height < screen.width * screen.height * 0.5f)
                    Add(result, rules, "C3", UiSeverity.Fail, path, "Screen", ink, screen);

                // C1 — 명시한 판이 있으면 그것(실패), 없으면 가장 가까운 보이는 스프라이트 판(경고 — 후보).
                var container = NearestContainer(graphic.transform, containers);
                if (container.Rect != null)
                {
                    var outer = WorldRect(container.Rect);
                    float inset = outer.height * container.Inset;
                    var inner = Rect.MinMaxRect(outer.xMin + inset, outer.yMin + inset, outer.xMax - inset, outer.yMax - inset);
                    Add(result, rules, "C1", UiSeverity.Fail, path, PathOf(container.Rect, root), ink, inner);
                }
                else
                {
                    var plate = NearestPlate(graphic.transform);
                    if (plate != null) Add(result, rules, "C1?", UiSeverity.Warn, path, PathOf(plate.transform, root), ink, WorldRect(plate.rectTransform));
                }

                if (label != null)
                {
                    // 넘침 모드(Overflow)는 칸 밖에도 다 그려진다 — 실제로 잘리는 모드에서만 실패. 칸 밖 글자는 C1·C3 가 잉크로 잰다.
                    if (label.isTextOverflowing && label.overflowMode != TextOverflowModes.Overflow && !rules.IsAllowed("C4", path))
                        result.Add(new Violation { Rule = "C4", Severity = UiSeverity.Fail, Path = path, Against = "글자 칸", Box = ink,
                                                   Limit = WorldRect(label.rectTransform), Note = "글자가 칸을 넘친다" });
                    else if (label.enableAutoSizing && label.fontSize <= label.fontSizeMin + 0.01f && !rules.IsAllowed("C4", path))
                        result.Add(new Violation { Rule = "C4", Severity = UiSeverity.Warn, Path = path, Against = "fontSizeMin " + label.fontSizeMin,
                                                   Box = ink, Limit = WorldRect(label.rectTransform), Note = "자동 축소가 하한 — 더 길면 넘친다" });
                    texts.Add((label, ink, path));
                }
                else if (graphic is Image image && image.sprite != null)
                {
                    CheckResource(result, rules, image, path, ink);
                }
            }

            // C6 — 글자가 그 뒤에 그려진 그림에 가림. 겹친 자리마다 그림 원본의 알파를 표본으로 본다(둥근 모서리·투명 여백은 가림이 아니다).
            // 그림이 마스크 안에 있으면 마스크 밖 부분은 그려지지 않으므로 뺀다.
            for (int i = 0; i < texts.Count; i++)
            {
                var label = texts[i].Label;
                int order = drawn.FindIndex(d => d.Graphic == label);
                if (order < 0) continue;

                var glyphRects = GlyphRects(label);
                for (int k = order + 1; k < drawn.Count; k++)
                {
                    if (!(drawn[k].Graphic is Image cover) || cover.color.a < 0.9f) continue;
                    if (cover.transform.IsChildOf(label.transform) || label.transform.IsChildOf(cover.transform)) continue;

                    var coverBox = WorldRect(cover.rectTransform);
                    if (TryClip(cover.transform, out var coverClip, out _)) coverBox = Intersect(coverBox, coverClip);
                    if (coverBox.width <= 0f || !coverBox.Overlaps(texts[i].Ink)) continue;

                    float covered = CoveredSide(glyphRects, cover, coverBox);
                    if (covered < MinOverlapSide) continue;

                    string coverPath = PathOf(cover.transform, root);
                    if (rules.IsAllowed("C6", texts[i].Path) || rules.IsAllowed("C6", coverPath)) continue;

                    result.Add(new Violation { Rule = "C6", Severity = UiSeverity.Fail, Path = texts[i].Path, Against = coverPath, Box = texts[i].Ink,
                                               Limit = coverBox, Over = covered, Note = "뒤에 그려진 그림이 글자를 가린다" });
                    break;
                }
            }

            // C5 — 글자끼리 겹침(같은 문자열도 검사한다 — 같은 글자를 겹쳐 그린 테 장식은 프로필의 AllowRule("C5", 경로)로만 뺀다). 글자 칸 전체가 아니라 «글자 하나하나» 상자로 잰다 —
            // 칸 상자는 줄 높이까지 잡아 위아래로 붙여 쓴 두 줄(«5/5» 와 «MAX»)을 겹침으로 오판했다.
            var glyphs = new List<List<Rect>>(texts.Count);
            for (int i = 0; i < texts.Count; i++) glyphs.Add(GlyphRects(texts[i].Label));

            for (int i = 0; i < texts.Count; i++)
            {
                for (int j = i + 1; j < texts.Count; j++)
                {
                    var a = texts[i];
                    var b = texts[j];
                    if (!a.Ink.Overlaps(b.Ink)) continue;
                    if (a.Label.transform.IsChildOf(b.Label.transform) || b.Label.transform.IsChildOf(a.Label.transform)) continue;
                    if (!TryGlyphOverlap(glyphs[i], glyphs[j], out float side)) continue;
                    if (rules.IsAllowed("C5", a.Path) || rules.IsAllowed("C5", b.Path)) continue;

                    result.Add(new Violation { Rule = "C5", Severity = UiSeverity.Fail, Path = a.Path, Against = b.Path, Box = a.Ink, Limit = b.Ink,
                                               Over = side, Note = "글자끼리 겹친다" });
                }
            }

            return result;
        }

        /// <summary> 글자 상자들 중 그림의 불투명한 부분에 가린 가장 큰 폭(px) — 겹친 범위를 4×4 로 표본해 절반 넘게 불투명하면 가림. </summary>
        private static float CoveredSide(List<Rect> glyphs, Image cover, Rect coverBox)
        {
            float worst = 0f;
            for (int g = 0; g < glyphs.Count; g++)
            {
                var overlap = Intersect(glyphs[g], coverBox);
                if (overlap.width < MinOverlapSide || overlap.height < MinOverlapSide) continue;

                int solid = 0;
                for (int ix = 0; ix < 4; ix++)
                    for (int iy = 0; iy < 4; iy++)
                    {
                        var point = new Vector2(overlap.xMin + (ix + 0.5f) * overlap.width / 4f, overlap.yMin + (iy + 0.5f) * overlap.height / 4f);
                        if (UiSpriteAlpha.IsSolidAt(cover, point)) solid++;
                    }

                if (solid >= 8) worst = Mathf.Max(worst, Mathf.Min(overlap.width, overlap.height));
            }

            return worst;
        }

        private static Rect Intersect(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

        /// <summary> 보이는 글자 하나하나의 화면 상자(글꼴 글리프 범위 — 외곽선 여백 제외). </summary>
        private static List<Rect> GlyphRects(TextMeshProUGUI label)
        {
            var list = new List<Rect>();
            var info = label.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;

                var a = label.transform.TransformPoint(c.bottomLeft);
                var b = label.transform.TransformPoint(c.topRight);
                var r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                // 글리프 상자는 실제 잉크보다 위아래로 몇 px 크다(캡처 대조로 확인한 값: 위쪽 몇 px 빈칸을 겹침으로 오판한 사례가 있었다). 안쪽으로 줄여 잰다.
                float dx = r.width * GlyphInsetX, dy = r.height * GlyphInsetY;
                list.Add(Rect.MinMaxRect(r.xMin + dx, r.yMin + dy, r.xMax - dx, r.yMax - dy));
            }

            return list;
        }

        private static bool TryGlyphOverlap(List<Rect> a, List<Rect> b, out float side)
        {
            side = 0f;
            for (int i = 0; i < a.Count; i++)
            {
                for (int j = 0; j < b.Count; j++)
                {
                    float w = Mathf.Min(a[i].xMax, b[j].xMax) - Mathf.Max(a[i].xMin, b[j].xMin);
                    float h = Mathf.Min(a[i].yMax, b[j].yMax) - Mathf.Max(a[i].yMin, b[j].yMin);
                    if (w < MinOverlapSide || h < MinOverlapSide || w * h < MinOverlapArea) continue;
                    side = Mathf.Max(side, Mathf.Min(w, h));
                }
            }

            return side > 0f;
        }

        // ─────────────────────────────────────────
        // 리소스
        // ─────────────────────────────────────────

        private static void CheckResource(List<Violation> result, UiHarnessRules rules, Image image, string path, Rect shown)
        {
            var sprite = image.sprite;
            var size = sprite.rect.size;
            bool hasBorder = sprite.border.sqrMagnitude > 0f;

            // R1 — 통그림을 비율 유지 없이 다른 비율 칸에 늘렸다(찌그러짐).
            if (image.type == Image.Type.Simple && !image.preserveAspect && shown.height > 0f)
            {
                float want = size.x / size.y;
                float got = shown.width / shown.height;
                if (Mathf.Abs(got / want - 1f) > AspectTolerance && !rules.IsAllowed("R1", path))
                    result.Add(new Violation { Rule = "R1", Severity = UiSeverity.Fail, Path = path, Against = sprite.name, Box = shown,
                                               Over = Mathf.Abs(got / want - 1f) * 100f, Note = $"그림 비율 {want:0.00} → 표시 {got:0.00} (찌그러짐 %)" });
            }

            // R2 — 9분할로 쓰는데 테두리가 없다(모서리째 늘어난다).
            if (image.type == Image.Type.Sliced && !hasBorder &&
                (Mathf.Abs(shown.width - size.x) > size.x * 0.1f || Mathf.Abs(shown.height - size.y) > size.y * 0.1f) &&
                !rules.IsAllowed("R2", path))
                result.Add(new Violation { Rule = "R2", Severity = UiSeverity.Fail, Path = path, Against = sprite.name, Box = shown,
                                           Note = "9분할 테두리(sprite border)가 0 — 모서리째 늘어난다" });

            if (image.type != Image.Type.Simple) return;

            float scale = Mathf.Max(shown.width / size.x, shown.height / size.y);
            if (scale > UpscaleLimit && !rules.IsAllowed("R3", path))
                result.Add(new Violation { Rule = "R3", Severity = UiSeverity.Warn, Path = path, Against = sprite.name, Box = shown, Over = scale,
                                           Note = $"원본 {size.x:0}×{size.y:0} 을 {scale:0.0}배 확대 — 흐려진다" });
            else if (scale < 1f / OversizeLimit && !rules.IsAllowed("R4", path))
                result.Add(new Violation { Rule = "R4", Severity = UiSeverity.Warn, Path = path, Against = sprite.name, Box = shown, Over = 1f / scale,
                                           Note = $"원본 {size.x:0}×{size.y:0} 이 표시의 {1f / scale:0.0}배 — 용량 낭비" });
        }

        // ─────────────────────────────────────────

        private static void Add(List<Violation> result, UiHarnessRules rules, string rule, UiSeverity severity, string path, string against,
                                Rect box, Rect limit)
        {
            float over = Mathf.Max(limit.xMin - box.xMin, box.xMax - limit.xMax, limit.yMin - box.yMin, box.yMax - limit.yMax);
            if (over <= Slack) return;
            if (rules.IsAllowed(rule.TrimEnd('?'), path)) return;

            result.Add(new Violation { Rule = rule, Severity = severity, Path = path, Against = against, Box = box, Limit = limit, Over = over });
        }

        /// <summary> 겹쳐 쌓는 묶음에서 맨 위가 아닌(가려진) 화면들. </summary>
        private static List<Transform> HiddenLayers(Transform root, UiHarnessRules rules)
        {
            var hidden = new List<Transform>();
            if (rules.Exclusive.Count == 0) return hidden;

            foreach (var t in root.GetComponentsInChildren<Transform>(false))
            {
                string path = PathOf(t, root);
                foreach (var (parent, childName) in rules.Exclusive)
                {
                    if (!parent.IsMatch(path)) continue;

                    Transform top = null;
                    for (int i = t.childCount - 1; i >= 0; i--)
                    {
                        var child = t.GetChild(i);
                        if (!childName.IsMatch(child.name) || !HasVisible(child)) continue;
                        if (top == null) { top = child; continue; }
                        hidden.Add(child);
                    }
                }
            }

            return hidden;
        }

        /// <summary>
        /// 열려 있는 모달 창 하나 — 모달 규칙에 맞는 «보이는» 요소 중 다른 맞는 요소의 자손이 아닌 것(가장 바깥)만 후보로 하고,
        /// 그 가운데 가장 위에 그려지는 것(루트 캔버스 정렬 → 덮어쓰기 캔버스 정렬 → 위계 순서)을 고른다. 없으면 null.
        /// </summary>
        private static Transform TopModal(Transform root, UiHarnessRules rules, List<Transform> hidden)
        {
            if (rules.Modals.Count == 0) return null;

            var matched = new List<(Transform T, UiHarnessRules.ModalRule Rule, int Order)>();
            int order = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(false))
            {
                order++;
                if (t == root || IsUnder(t, hidden)) continue;

                string path = PathOf(t, root);
                foreach (var rule in rules.Modals)
                {
                    if (!rule.Path.IsMatch(path) || !HasVisible(t)) continue;
                    matched.Add((t, rule, order));
                    break;
                }
            }

            if (matched.Count == 0) return null;

            Transform best = null;
            UiHarnessRules.ModalRule bestRule = null;
            (int Root, int Own, int Order) bestKey = default;
            foreach (var m in matched)
            {
                bool nested = false;
                foreach (var other in matched)
                    if (other.T != m.T && m.T.IsChildOf(other.T)) { nested = true; break; }
                if (nested) continue;

                var canvas = m.T.GetComponentInParent<Canvas>();
                int rootSort = canvas != null ? canvas.rootCanvas.sortingOrder : 0;
                int ownSort = canvas != null && canvas.overrideSorting ? canvas.sortingOrder : 0;
                var key = (rootSort, ownSort, m.Order);
                if (best == null || key.CompareTo(bestKey) > 0)
                {
                    best = m.T;
                    bestRule = m.Rule;
                    bestKey = key;
                }
            }

            if (bestRule != null) bestRule.Hits++;
            return best;
        }

        /// <summary> 켜져 있어도 내용이 다 꺼진 화면은 «보이는 화면» 이 아니다 — 보이는 그래픽이 하나라도 있어야 한다. </summary>
        private static bool HasVisible(Transform t)
        {
            if (!t.gameObject.activeInHierarchy) return false;
            foreach (var graphic in t.GetComponentsInChildren<Graphic>(false))
                if (IsVisible(graphic)) return true;
            return false;
        }

        private static bool IsUnder(Transform t, List<Transform> parents)
        {
            for (int i = 0; i < parents.Count; i++)
                if (t == parents[i] || t.IsChildOf(parents[i])) return true;
            return false;
        }

        private static List<RectTransform> FindContainers(Transform root, UiHarnessRules rules)
        {
            var list = new List<RectTransform>();
            if (rules.Containers.Count == 0) return list;

            foreach (var rect in root.GetComponentsInChildren<RectTransform>(false))
            {
                string path = PathOf(rect, root);
                foreach (var c in rules.Containers)
                    if (c.Path.IsMatch(path)) { list.Add(rect); break; }
            }

            return list;
        }

        private static (RectTransform Rect, float Inset) NearestContainer(Transform child, List<RectTransform> containers)
        {
            for (var t = child.parent; t != null; t = t.parent)
            {
                int index = containers.IndexOf(t as RectTransform);
                if (index >= 0) return (containers[index], s_inset.TryGetValue(containers[index], out float inset) ? inset : 0f);
            }

            return (null, 0f);
        }

        private static readonly Dictionary<RectTransform, float> s_inset = new Dictionary<RectTransform, float>();

        /// <summary> 판 안쪽 테 두께를 찾을 수 있게 미리 적어 둔다. </summary>
        private static void Prepare(Transform root, UiHarnessRules rules)
        {
            s_inset.Clear();
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(false))
            {
                string path = PathOf(rect, root);
                foreach (var c in rules.Containers)
                    if (c.Path.IsMatch(path)) { s_inset[rect] = c.Inset; break; }
            }
        }

        private static bool TryClip(Transform t, out Rect clip, out string clipPath)
        {
            clip = default;
            clipPath = null;
            bool has = false;
            for (var p = t.parent; p != null; p = p.parent)
            {
                var mask = p.GetComponent<RectMask2D>();
                if (mask == null || !mask.enabled) continue;

                var r = WorldRect(mask.rectTransform);
                if (!has) { clip = r; clipPath = p.name; has = true; }
                else
                {
                    clip = Rect.MinMaxRect(Mathf.Max(clip.xMin, r.xMin), Mathf.Max(clip.yMin, r.yMin), Mathf.Min(clip.xMax, r.xMax), Mathf.Min(clip.yMax, r.yMax));
                    clipPath = p.name + "∩" + clipPath;
                }
            }

            return has;
        }

        private static bool IsVisible(Graphic graphic)
        {
            if (!graphic.enabled || !graphic.gameObject.activeInHierarchy || graphic.canvasRenderer.cull) return false;

            float alpha = graphic.color.a * graphic.canvasRenderer.GetAlpha();
            foreach (var group in graphic.GetComponentsInParent<CanvasGroup>())
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }

            return alpha > 0.02f;
        }

        private static Image NearestPlate(Transform child)
        {
            for (var t = child.parent; t != null; t = t.parent)
            {
                var canvas = t.GetComponent<Canvas>();
                if (canvas != null && canvas.isRootCanvas) return null;
                var image = t.GetComponent<Image>();
                if (image != null && image.enabled && image.sprite != null && image.color.a > 0.02f) return image;
            }

            return null;
        }

        private static Rect InkRect(Graphic graphic)
        {
            if (graphic is TextMeshProUGUI label)
            {
                label.ForceMeshUpdate();
                var bounds = label.textBounds;
                if (bounds.size.x <= 0f) return Rect.zero;

                var a = label.transform.TransformPoint(bounds.min);
                var b = label.transform.TransformPoint(bounds.max);
                return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            }

            var box = WorldRect(graphic.rectTransform);
            if (graphic is Image image && image.preserveAspect && image.sprite != null)
            {
                float aspect = image.sprite.rect.width / image.sprite.rect.height;
                float w = Mathf.Min(box.width, box.height * aspect);
                float h = w / aspect;
                return new Rect(box.center.x - w * 0.5f, box.center.y - h * 0.5f, w, h);
            }

            return box;
        }

        public static Rect WorldRect(RectTransform rect)
        {
            rect.GetWorldCorners(s_corners);
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                xMin = Mathf.Min(xMin, s_corners[i].x);
                yMin = Mathf.Min(yMin, s_corners[i].y);
                xMax = Mathf.Max(xMax, s_corners[i].x);
                yMax = Mathf.Max(yMax, s_corners[i].y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var p = t; p != null && p != root; p = p.parent) parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
