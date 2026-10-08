using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 팝업 연출 재생 (2026-10-08 · PD 통과 시안 <c>mock_fxstory_*</c>).
    ///
    /// 창이 떠 있는 동안 은은히 도는 장식(open)과 고른 순간의 반응(accept)을 그림 프레임으로 돌린다.
    /// 값은 창마다 붙은 <see cref="PopupFxSpec"/> 이 쥔다 — 여기에는 숫자를 적지 않는다.
    /// 인게임 창 4종(<c>InGameMainUI</c>)과 챕터 클리어 창(<c>ChapterResultPopup</c>)이 같이 쓴다.
    ///
    /// ⚠ accept 는 창이 닫혀도 끝까지 돌아야 해서 창 밖 덮개(<c>PopupFxOverlay</c>)에 그린다.
    /// ⚠ 시간은 <c>unscaledDeltaTime</c> — 창이 뜨면 전투가 멈추는 경우가 있다(레벨업).
    /// </summary>
    public sealed class PopupFxPlayer : MonoBehaviour
    {
        private sealed class FxRun
        {
            public PopupFxLayer Layer;
            public Image Image;
            public RectTransform Rect;
            public Sprite[] Frames;
            public float Time;
            public string Panel;
            public Vector2 From, To;
            public Color Tint;
        }

        private readonly List<FxRun> _runs = new();
        private readonly Stack<Image> _pool = new();
        private readonly Dictionary<string, Sprite[]> _frameCache = new();
        private readonly Dictionary<string, Color> _nodeTint = new();
        private SpriteAtlas _atlas;
        private Func<Transform> _avatar;
        private RectTransform _overlay;

        /// <summary>
        /// 연출 그림만 모은 아틀라스. 창 그림(<c>atlas/ingamemainui</c>)과 나눴다 — 프레임이 230장 넘어
        /// 한 장에 넣으면 창 아틀라스가 페이지를 여러 장 더 먹는다. 빛 번짐이라 압축해서 묶는다.
        /// </summary>
        private const string AtlasAddress = "atlas/popupfx";

        /// <summary>플레이어 몸(빔 출발점 · 「능력치 적용」 빛 자리)을 알려 주고 연출 아틀라스를 불러온다.</summary>
        public void Init(Func<Transform> avatar)
        {
            _avatar = avatar;
            if (_atlas == null) LoadAtlasAsync().Forget();   // fire-and-forget: 오기 전에 부르면 그 층만 건너뛴다
        }

        private async UniTaskVoid LoadAtlasAsync()
        {
            try { _atlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>(AtlasAddress); }
            catch (Exception e) { Debug.LogWarning($"[PopupFx] 연출 아틀라스 로드 실패 — {e.Message}"); }
            _frameCache.Clear();
        }

        private void Update()
        {
            // 덮개는 창 **위**에 있어야 한다 — 고른 뒤 창을 다시 맨 위로 올리는 곳(튕김 · 다른 창 열기)이 있어
            // 덮개가 창 밑에 깔려 고른 순간 빛이 통째로 가려졌다(게임 녹화 2026-10-08)
            if (_overlay != null && _overlay.parent != null
                && _overlay.GetSiblingIndex() != _overlay.parent.childCount - 1)
                _overlay.SetAsLastSibling();
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            for (int i = _runs.Count - 1; i >= 0; i--) Recycle(_runs[i]);
            _runs.Clear();
        }

        /// <summary>
        /// 창이 열릴 때 — open 단계 장식을 켠다. <paramref name="under"/> 를 주면 그 아래(같은 720x1280 좌표 판)에 띄운다
        /// (결과창처럼 창 자신은 화면 전체로 늘어나고 내용은 가운데 판에 있는 경우).
        /// </summary>
        public void Open(RectTransform panel, RectTransform under = null)
        {
            if (panel == null) return;
            Stop(panel);
            var spec = panel.GetComponent<PopupFxSpec>();
            if (spec == null) return;
            var parent = under != null ? under : panel;
            var layers = spec.Layers;
            for (int i = 0; i < layers.Length; i++)
            {
                var l = layers[i];
                if (l.Phase != "open") continue;
                // 칸에 붙는 장식(레벨업 카드 테두리 · 거래 알약 등) — 그 칸이 숨어 있으면 켜지 않는다
                // (대가 없는 거래에서 숨은 대가 알약 자리에 반짝임만 타고 있었다, 2026-10-08 게임 확인)
                RectTransform at = null;
                if (!string.IsNullOrEmpty(l.AtNode))
                {
                    at = FindDeep(panel, l.AtNode) as RectTransform;
                    if (at == null || !at.gameObject.activeInHierarchy) continue;
                }
                if (at != null && l.InNode)
                {
                    SpawnInNode(panel.name, at, spec.Additive, l);
                    continue;
                }
                var run = Spawn(panel.name, parent, spec.Additive, l, Vector2.zero);
                if (run == null || at == null) continue;
                Anchor(run, ScreenOf(parent, at) + new Vector2(l.X, l.Y));
                if (_nodeTint.TryGetValue(panel.name + "/" + l.AtNode, out var c)) run.Tint = c;
            }
        }

        /// <summary>
        /// 층 묶음 중 <paramref name="phase"/> 단계를 <paramref name="parent"/> 아래에 띄운다.
        /// 좌표 = <paramref name="origin"/> + 층 (X, Y)(아래로 +). 방 오브젝트처럼 프리팹 밖에서 쓰는 곳은 이걸 부른다.
        /// </summary>
        public void Play(string owner, RectTransform parent, IReadOnlyList<PopupFxLayer> layers, string phase,
                         Material additive, Vector2 origin)
        {
            if (parent == null || layers == null) return;
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].Phase == phase) Spawn(owner, parent, additive, layers[i], origin);
        }

        /// <summary>
        /// <paramref name="node"/> 칸에 붙는 장식의 색을 정해 둔다 — 레벨업 카드 테두리를 카드 희귀도 색으로.
        /// 창을 열기 전에 부르면 그 색으로 켜지고, 이미 떠 있는 것도 바로 바뀐다.
        /// </summary>
        public void Tint(RectTransform panel, string node, Color color)
        {
            if (panel == null) return;
            _nodeTint[panel.name + "/" + node] = color;
            for (int i = 0; i < _runs.Count; i++)
                if (_runs[i].Panel == panel.name && _runs[i].Layer.AtNode == node) _runs[i].Tint = color;
        }

        /// <summary>
        /// 고른 순간 — accept 단계 반응을 덮개에 띄운다. 창이 바로 닫혀도 끝까지 돈다.
        /// <paramref name="slot"/> 은 레이어 <c>AtNode</c> 의 <c>{slot}</c> 자리(상점 산 칸 번호 등).
        /// </summary>
        public void Fire(RectTransform panel, string slot = null)
        {
            if (panel == null) return;
            var spec = panel.GetComponent<PopupFxSpec>();
            if (spec == null) return;
            var overlay = EnsureOverlay(panel);
            // 고르면 거두는 장식(버튼 빛 등)
            for (int i = _runs.Count - 1; i >= 0; i--)
            {
                if (_runs[i].Panel != panel.name || !_runs[i].Layer.UntilAccept) continue;
                Recycle(_runs[i]);
                _runs.RemoveAt(i);
            }
            var layers = spec.Layers;
            for (int i = 0; i < layers.Length; i++)
            {
                var l = layers[i];
                if (l.Phase != "accept") continue;
                // 자리가 매번 바뀌는 것(상점 산 칸 · 고른 카드) — 그 칸 가운데 + (X, Y) 어긋남. 숨은 칸이면 건너뛴다
                RectTransform at = null;
                if (!string.IsNullOrEmpty(l.AtNode))
                {
                    at = FindDeep(panel, l.AtNode.Replace("{slot}", slot ?? string.Empty)) as RectTransform;
                    if (at == null || !at.gameObject.activeInHierarchy) continue;
                }
                if (at != null && l.InNode)
                {
                    SpawnInNode(panel.name + "#accept", at, spec.Additive, l);
                    continue;
                }
                // 창 글자 뒤로 가야 하는 것(back — 레벨업 흡수 줄기)은 창 안에, 나머지는 창이 닫혀도 남는 덮개에
                var run = Spawn(panel.name + "#accept", l.Back ? panel : overlay, spec.Additive, l, Vector2.zero);
                if (run == null) continue;
                if (at != null) Anchor(run, ScreenOf(panel, at) + new Vector2(l.X, l.Y));
            }
        }

        /// <summary>창이 닫힐 때 — open 단계 장식을 거둔다(accept 는 제 시간까지 돈다).</summary>
        public void Stop(RectTransform panel)
        {
            if (panel != null) Stop(panel.name);
        }

        public void Stop(string owner)
        {
            for (int i = _runs.Count - 1; i >= 0; i--)
            {
                if (_runs[i].Panel != owner) continue;
                Recycle(_runs[i]);
                _runs.RemoveAt(i);
            }
        }

        private FxRun Spawn(string owner, RectTransform parent, Material additive, PopupFxLayer l, Vector2 origin)
        {
            var frames = Frames(l.Frames);
            if (frames == null) return null;
            var img = _pool.Count > 0 ? _pool.Pop() : NewImage();
            img.transform.SetParent(parent, false);
            // 창 틀 뒤 = 패널의 어두운 바탕 바로 위(첫 자식 앞)
            if (l.Back) img.transform.SetSiblingIndex(0);
            else img.transform.SetAsLastSibling();
            img.material = l.Additive ? additive : null;
            img.sprite = frames[0];
            img.color = new Color(l.Tint.r, l.Tint.g, l.Tint.b, 0f);
            img.gameObject.SetActive(true);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = new Vector3(l.Flip ? -1f : 1f, 1f, 1f);
            var run = new FxRun
            {
                Layer = l, Image = img, Rect = rt, Frames = frames, Panel = owner,
                From = origin + new Vector2(l.X, l.Y), To = origin + new Vector2(l.X2, l.Y2), Tint = l.Tint,
            };
            // 빔이면 출발점, 아니면 놓는 자리가 플레이어 몸(방 회복 오브젝트의 치유 흡수 등)
            var avatar = l.FromAvatar ? _avatar?.Invoke() : null;
            if (avatar != null) run.From = ScreenOf(parent, avatar);
            if (!l.Beam) Place(run, run.From);
            _runs.Add(run);
            return run;
        }

        /// <summary>
        /// 칸 안 첫 자식으로 — 칸 그림(칸 자신의 Image) 바로 위, 칸 안 글자 · 아이콘 아래에 그려진다.
        /// 자리는 칸 가운데 + (X, Y). 칸이 튕기거나 움직이면 같이 움직인다.
        /// </summary>
        private void SpawnInNode(string owner, RectTransform node, Material additive, PopupFxLayer l)
        {
            var run = Spawn(owner, node, additive, l, Vector2.zero);
            if (run == null) return;
            run.Rect.SetSiblingIndex(0);
            var r = node.rect;
            Anchor(run, new Vector2(r.width * 0.5f + l.X, r.height * 0.5f + l.Y));
        }

        private static void Anchor(FxRun run, Vector2 at)
        {
            if (run.Layer.Beam) run.From = at;
            else Place(run, at);
        }

        private static void Place(FxRun run, Vector2 at)
        {
            run.Rect.anchoredPosition = new Vector2(at.x, -at.y);
            run.Rect.sizeDelta = new Vector2(run.Layer.W, run.Layer.H);
        }

        private void Tick(float dt)
        {
            for (int i = _runs.Count - 1; i >= 0; i--)
            {
                var r = _runs[i];
                var l = r.Layer;
                r.Time += dt;
                bool forever = l.T1 <= 0f;
                if (!forever && r.Time >= l.T1) { Recycle(r); _runs.RemoveAt(i); continue; }
                if (r.Time < l.T0) { SetAlpha(r, 0f); continue; }

                int k = (int)((r.Time - l.T0) / Mathf.Max(0.01f, l.Step));
                if (k >= r.Frames.Length)
                {
                    if (!l.Loop) { Recycle(r); _runs.RemoveAt(i); continue; }
                    k %= r.Frames.Length;
                }
                if (r.Image.sprite != r.Frames[k]) r.Image.sprite = r.Frames[k];

                float a = l.Alpha;
                if (l.Fade > 0f)
                {
                    a *= Mathf.Clamp01((r.Time - l.T0) / l.Fade);
                    if (!forever) a *= Mathf.Clamp01((l.T1 - r.Time) / l.Fade);
                }
                SetAlpha(r, a);

                if (!l.Beam) continue;
                float grow = Mathf.Clamp01((r.Time - l.T0) / 0.15f);
                var to = Vector2.Lerp(r.From, r.To, grow);
                var d = to - r.From;
                r.Rect.anchoredPosition = new Vector2((r.From.x + to.x) * 0.5f, -(r.From.y + to.y) * 0.5f);
                r.Rect.sizeDelta = new Vector2(d.magnitude, l.H);
                r.Rect.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        private static void SetAlpha(FxRun r, float a)
        {
            var c = r.Tint;
            r.Image.color = new Color(c.r, c.g, c.b, c.a * a);
        }

        private void Recycle(FxRun r)
        {
            if (r.Image == null) return;
            r.Image.gameObject.SetActive(false);
            r.Image.transform.SetParent(transform, false);
            _pool.Push(r.Image);
        }

        private static Image NewImage()
        {
            var go = new GameObject("PopupFx", typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private Sprite[] Frames(string name)
        {
            if (string.IsNullOrEmpty(name) || _atlas == null) return null;   // 아틀라스가 오기 전에는 기억하지 않는다
            if (_frameCache.TryGetValue(name, out var cached)) return cached;
            var list = new List<Sprite>(4);
            for (int i = 1; i <= 8; i++)
            {
                var sp = _atlas != null ? _atlas.GetSprite($"fx_{name}_{i}") : null;
                if (sp == null) break;
                list.Add(sp);
            }
            var frames = list.Count > 0 ? list.ToArray() : null;
            _frameCache[name] = frames;   // 없으면 null — 매번 다시 찾지 않게
            return frames;
        }

        /// <summary>창 밖 덮개 — 창과 같은 자리 · 크기로 맨 위에 둔다.</summary>
        private RectTransform EnsureOverlay(RectTransform like)
        {
            if (_overlay == null)
            {
                var go = new GameObject("PopupFxOverlay", typeof(RectTransform));
                _overlay = (RectTransform)go.transform;
            }
            _overlay.SetParent(like.parent, false);
            _overlay.anchorMin = like.anchorMin;
            _overlay.anchorMax = like.anchorMax;
            _overlay.pivot = like.pivot;
            _overlay.anchoredPosition = like.anchoredPosition;
            _overlay.sizeDelta = like.sizeDelta;
            _overlay.SetAsLastSibling();
            // 창은 제 캔버스로 정렬 밴드(HUD 20)에 올라가 있다(InGameMainUI.SortingBands). 덮개는 그 바로 위 —
            // 캔버스가 없으면 방 밴드(0)에 깔려 고른 순간 빛이 통째로 안 보였다(2026-10-08)
            if (like.GetComponent<Canvas>() is Canvas band && band.overrideSorting)
            {
                var c = _overlay.GetComponent<Canvas>();
                if (c == null) c = _overlay.gameObject.AddComponent<Canvas>();
                c.overrideSorting = true;
                c.sortingLayerID = band.sortingLayerID;
                // 같은 값이면 창 캔버스와 앞뒤가 정해지지 않아 창 밑에 깔렸다(게임 확인) — 한 칸 위로
                c.sortingOrder = band.sortingOrder + 1;
            }
            return _overlay;
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

        /// <summary>대상 가운데를 패널 좌표(왼쪽 위 기준, 아래로 +)로 — 보상 연출(띠 · 날아가는 것)도 같은 자로 잰다.</summary>
        public static Vector2 PanelPoint(RectTransform panel, Transform target) => ScreenOf(panel, target);

        /// <summary>연출 아틀라스의 프레임(fx_{name}_1 …). 결과창 금화 더미 단계 · 날아가는 구슬처럼 코드가 직접 쓰는 그림.</summary>
        public Sprite[] FramesOf(string name) => Frames(name);

        /// <summary>대상 가운데를 패널 좌표(왼쪽 위 기준, 아래로 +)로.</summary>
        private static Vector2 ScreenOf(RectTransform panel, Transform target)
        {
            var world = target is RectTransform rt ? rt.TransformPoint(rt.rect.center) : target.position;
            var local = (Vector2)panel.InverseTransformPoint(world);
            var r = panel.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }
    }
}
