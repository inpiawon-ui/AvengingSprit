using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// <see cref="DangerShape"/> 를 바닥에 그린다 — 세 겹이다(2026-10-07 「PPT 도형 같다」 지적으로 다시 짰다).
    ///
    ///   채움  도형 안을 에너지 무늬로 옅게 채운다. 무늬는 천천히 흐른다
    ///   차오름 예고가 진행되는 만큼 **안에서부터** 진하게 차오른다 — 다 차면 맞는다
    ///   테두리 도형 둘레를 빛나는 띠로 두른다. 끝이 가까울수록 빠르게 숨 쉰다
    ///
    /// 나타날 때는 0.18초 동안 작게 시작해 제 크기로 커지고, 터지는 순간 한 번 번쩍인다.
    ///
    /// ⚠ **도형을 여기서 만들지 않는다.** 넘겨받은 것을 그대로 그린다 —
    ///   판정과 같은 구조체를 같은 함수(`Outline`)로 그리므로, 그린 것과 맞는 것이
    ///   어긋날 수가 없다. 커지고 차오르는 연출은 전부 `DangerShape.Grown`(판정 도형 **안쪽**으로만
    ///   줄인 복사본)으로 한다 — 판정보다 크게 그리는 일은 없다.
    ///
    /// 그림 `Fx/danger_fill` · `Fx/danger_core` · `Fx/danger_edge`(초록 안전지대는 `Fx/safe_*`)은
    /// 따로 떨어진 텍스처다 — 아틀라스에 묶으면 타일이 안 돈다(`CanTile` 주석).
    /// 아직 안 왔으면 예전처럼 빗금 · 단색으로 그린다 — 그림이 늦어도 굴러가야 한다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DangerView : MaskableGraphic
    {
        private enum Layer { Fill, Core, Edge }

        /// <summary>빗금 한 칸이 화면에서 차지하는 크기(px). 타일이 64 라 그대로 쓴다.</summary>
        private const float HatchPixels = 64f;
        /// <summary>에너지 무늬 한 장이 화면에서 차지하는 크기(px).</summary>
        private const float FillPixels = 192f;
        /// <summary>테두리 띠 그림 한 장이 둘레를 따라 차지하는 길이(px)와 띠 두께(px).</summary>
        private const float EdgeTilePixels = 128f;
        /// <summary>
        /// 테두리 띠 두께(px). 2차 그림(PD 확정 2026-10-07 — 테두리 E · 안쪽 D)은 띠 안에 이중선 + 안쪽으로 번지는
        /// 그라데이션이 다 들어 있다 — 그라데이션이 안쪽으로 넉넉히 퍼지게 두껍게 편다(선 자체는 3 px 안팎).
        /// </summary>
        private const float EdgeWidth = 34f;

        private const float AppearSeconds = 0.18f;
        private const float AppearFrom = 0.72f;
        private const float FlashSeconds = 0.16f;

        private static readonly Color DangerTint = new(1f, 0.30f, 0.28f, 0.55f);
        private static readonly Color SafeTint = new(0.35f, 1f, 0.45f, 0.42f);
        private static readonly Color CoreTint = new(1f, 0.36f, 0.32f, 1f);

        // ── 그림 — 한 번 받아 모두가 같이 쓴다 ───────────────────────
        private static readonly string[] ArtAddress =
            { "Fx/danger_fill", "Fx/danger_core", "Fx/danger_edge", "Fx/safe_fill", "Fx/safe_core", "Fx/safe_edge" };
        private static readonly Texture2D[] s_art = new Texture2D[6];
        private static bool s_artRequested;

        private readonly List<Vector2> _verts = new(128);
        private readonly List<int> _tris = new(256);
        private readonly List<Vector2> _edgeVerts = new(256);
        private readonly Dictionary<long, int> _edgeCount = new(256);
        private readonly Dictionary<long, int> _edgeOther = new(256);
        private readonly Dictionary<int, int> _edgeVert = new(256);   // 자리 키 → 그 자리의 첫 꼭짓점 번호

        private Layer _layer;
        private DangerView _core, _edge;     // 바닥 겹(채움)만 둘을 거느린다

        private DangerShape _shape;
        private Vector2 _roomSize;
        private Sprite _hatch;
        private bool _safe;

        private float _progress;
        private float _age;
        private float _flash;   // 터진 뒤 남은 번쩍임(초). 0 이면 꺼진다

        private Texture2D Art => s_art[(_safe ? 3 : 0) + (int)_layer];

        /// <summary>
        /// 빗금을 **타일로 쓸 수 있는가**.
        ///
        /// ⚠ 아틀라스에 묶인 스프라이트는 `.texture` 가 **아틀라스 한 장 전체**다.
        ///   여기 UV 는 `좌표 ÷ 64` 라 720px 도형이면 0~11 까지 간다 —
        ///   아틀라스 텍스처는 wrap 이 Clamp 라 1 을 넘는 순간 가장자리(투명)를
        ///   계속 샘플한다. **도형이 그려지긴 하는데 통째로 투명해진다.**
        ///   실제로 그래서 24패턴의 바닥 도형이 한 번도 안 보였다.
        ///
        ///   스프라이트 크기와 텍스처 크기가 같으면 아틀라스 밖(제 텍스처)이라
        ///   타일이 제대로 돈다. 다르면 아틀라스다 — 그때는 **단색으로 칠한다.**
        ///   무늬를 잃는 것이 안 보이는 것보다 낫다.
        /// </summary>
        private bool CanTile
        {
            get
            {
                if (_hatch == null || _hatch.texture == null) return false;
                return Mathf.Approximately(_hatch.rect.width, _hatch.texture.width)
                    && Mathf.Approximately(_hatch.rect.height, _hatch.texture.height);
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (Art != null) return Art;
                if (_layer == Layer.Fill && CanTile) return _hatch.texture;
                return s_WhiteTexture;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public static DangerView Create(Transform parent)
        {
            RequestArt();
            var v = Make(parent, "DangerView", Layer.Fill);
            // 차오름 · 테두리는 채움 **위에** 그린다 — 자식이라 같이 켜지고 같이 꺼진다
            v._core = Make(v.transform, "DangerCore", Layer.Core);
            v._edge = Make(v.transform, "DangerEdge", Layer.Edge);
            v._core.gameObject.SetActive(true);
            v._edge.gameObject.SetActive(true);
            v.gameObject.SetActive(false);
            return v;
        }

        private static DangerView Make(Transform parent, string name, Layer layer)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<DangerView>();
            v._layer = layer;
            var rt = (RectTransform)go.transform;
            // 방 좌표계 그대로 쓴다 — 왼쪽 위가 (0,0), 아래로 갈수록 y 가 음수다.
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            // 크기는 `Show` 에서 방 크기로 채운다. 여기 0 으로 두면 안 보인다 — 아래 참조.
            rt.sizeDelta = Vector2.zero;
            go.SetActive(false);
            return v;
        }

        /// <summary>그림을 한 번만 부른다. 실패해도 예전 모양으로 그린다.</summary>
        private static void RequestArt()
        {
            if (s_artRequested) return;
            s_artRequested = true;
            LoadArtAsync().Forget();   // fire-and-forget: 그림이 오기 전에도 단색으로 그린다
        }

        private static async UniTaskVoid LoadArtAsync()
        {
            if (!CoreModule.TryGet<IResourceManager>(out var res)) { s_artRequested = false; return; }
            for (int i = 0; i < ArtAddress.Length; i++)
            {
                try { s_art[i] = await res.LoadAsync<Texture2D>(ArtAddress[i]); }
                catch (System.Exception e) { Debug.LogWarning($"[Danger] 그림 없음 {ArtAddress[i]} — {e.Message}"); }
            }
        }

        /// <summary>그릴 것을 넘긴다. <paramref name="safe"/> 면 초록 안전지대로 그린다.</summary>
        public void Show(DangerShape shape, Vector2 roomSize, Sprite hatch, bool safe)
        {
            _shape = shape;
            _roomSize = roomSize;
            _hatch = hatch;
            _safe = safe;
            _progress = 0f;
            _age = 0f;
            _flash = 0f;

            // ⚠⚠ **이 한 줄이 없으면 도형이 아예 안 그려진다.**
            //
            //   방 화면(`_field`)에는 `RectMask2D` 가 붙어 있다(방이 창보다 길어서다).
            //   `RectMask2D` 는 자기 밑의 `MaskableGraphic` 을 **잘라 내기 전에 통째로
            //   버릴지부터 고른다** — `Cull(clipRect, valid)` 안에서
            //   `clipRect.Overlaps(내 사각형)` 을 묻고, 겹치지 않으면 그리지 않는다.
            //
            //   그런데 이 오브젝트의 `sizeDelta` 는 0 이었다. 앵커·피벗이 (0,1) 이라
            //   **방의 왼쪽 위 모서리에 찍힌 점 하나**가 내 사각형이다.
            //   `Overlaps` 는 열린 구간 비교(`other.xMin < xMax`)라 경계에 딱 붙은 점은
            //   겹치지 않는다 — 방 폭이 창 폭과 같아서 x 는 늘 정확히 경계였다.
            //   **24개 패턴의 바닥 도형이 한 번도 그려지지 않은 진짜 이유가 이것이다.**
            //   (화살표·이름표는 제 크기와 자리를 가진 `Image` 라 멀쩡히 떴다.
            //    그래서 "화살표는 뜨는데 도형만 없다" 로 보였다.)
            //
            //   메시 좌표는 피벗 기준이라 크기를 채워도 **그림은 제자리다.**
            var rt = rectTransform;
            if (rt.sizeDelta != roomSize) rt.sizeDelta = roomSize;

            color = TintNow();
            gameObject.SetActive(!shape.IsNone);
            SetVerticesDirty();
            SetMaterialDirty();
            if (_core != null) _core.Show(shape, roomSize, hatch, safe);
            if (_edge != null) _edge.Show(shape, roomSize, hatch, safe);
        }

        /// <summary>
        /// 그리고 있던 도형의 **자리만** 갈아 끼운다.
        ///
        /// ⚠ `Show` 를 다시 부르면 안 된다 — 맥박(`_pulse`)과 진행도(`_progress`)를
        ///   0 으로 되돌려서, 매 프레임 부르면 예고가 영영 빨라지지 않는다.
        ///   따라다니는 예고(킹핀 「처형 조준」)가 이 자리를 쓴다.
        /// </summary>
        public void Reaim(DangerShape shape)
        {
            if (!IsShowing) return;
            _shape = shape;
            SetVerticesDirty();
            if (_core != null) _core.Reaim(shape);
            if (_edge != null) _edge.Reaim(shape);
        }

        /// <summary>
        /// 거둔다. 예고가 **터져서** 거두는 것이면 한 번 번쩍이고 사라진다(0.16초).
        /// 판정은 부르는 쪽이 이미 끝냈다 — 번쩍임은 그림일 뿐 아무것도 안 친다.
        /// </summary>
        public void Hide()
        {
            if (IsShowing && _progress >= 0.95f && _layer == Layer.Fill)
            {
                _flash = FlashSeconds;
                if (_core != null) _core._flash = FlashSeconds;
                if (_edge != null) _edge._flash = FlashSeconds;
                return;   // 번쩍임이 끝나면 `Update` 가 끈다
            }
            HideNow();
        }

        private void HideNow()
        {
            _shape = default;
            _flash = 0f;
            gameObject.SetActive(false);
        }

        public bool IsShowing => gameObject.activeSelf && !_shape.IsNone && _flash <= 0f;

        /// <summary>
        /// 예고가 얼마나 찼는지(0~1) 알려 준다. 끝이 가까울수록 빠르게 숨 쉰다.
        /// </summary>
        public void Tick(float dt, float progress01)
        {
            if (!IsShowing) return;
            Advance(dt, progress01);
            if (_core != null) _core.Advance(dt, progress01);
            if (_edge != null) _edge.Advance(dt, progress01);
        }

        private void Advance(float dt, float progress01)
        {
            _progress = Mathf.Clamp01(progress01);
            _age += dt;
            // ⚠ 숨쉬기(맥박)는 멈췄다 — PD 2026-10-08 「노티가 애니로 들어가서 정신없다, 진행 단계처럼 차오르다 다 차면 액션」.
            //   범위는 옅게 가만히 서 있고, 안에서부터 차오르는 겹(Core)만 움직인다.
            color = TintNow();
            SetVerticesDirty();   // 무늬가 흐르고 테두리가 숨 쉰다 — 매 프레임 새로 짠다(보스전 예고 몇 개뿐)
        }

        // 터진 뒤 번쩍임은 부르는 쪽이 더는 `Tick` 을 안 부르므로 스스로 돈다
        private void Update()
        {
            if (_flash <= 0f || _layer != Layer.Fill) return;
            float dt = Time.deltaTime;
            Flash(dt);
            if (_core != null) _core.Flash(dt);
            if (_edge != null) _edge.Flash(dt);
            if (_flash <= 0f) HideNow();
        }

        private void Flash(float dt)
        {
            _flash = Mathf.Max(0f, _flash - dt);
            color = TintNow();
            SetVerticesDirty();
        }

        /// <summary>겹마다 진하기. 그림이 있으면 그림 색을 그대로 쓰고 진하기만 바꾼다.</summary>
        private Color TintNow()
        {
            bool art = Art != null;
            var c = art ? Color.white : (_safe ? SafeTint : DangerTint);
            // 차오르는 겹의 그림은 흰빛이라 그대로 두면 다 찰 무렵 **하얀 띠 · 하얀 원**이 됐다(2026-10-08 녹화) — 위험 빨강으로 물들인다
            if (art && !_safe && _layer == Layer.Core) c = CoreTint;
            float flash = _flash > 0f ? _flash / FlashSeconds : 0f;
            switch (_layer)
            {
                case Layer.Fill:
                    // 2차 그림은 그라데이션이 테두리 띠에 있다 — 채움은 도형 전체를 아주 옅게 물들이기만 한다
                    c.a = _safe ? (art ? 0.16f : SafeTint.a)
                                : (art ? 0.09f : 0.28f);   // 범위 전체는 옅게 · 가만히(아직 안 찬 곳도 위험이라는 것이 보이게)
                    break;
                case Layer.Core:
                    // 다 찰 무렵 가장 진하다. 안전지대는 차오르지 않는다(늘 서 있을 자리다)
                    // 차오름이 곧 진행 바다 — 다 차면 친다. PD 2026-10-08 「너무 찐하다 — 티는 나되 방해 안 되게 50~70%」 → 약 0.6배
                    c.a = _safe ? 0f : Mathf.Lerp(0.18f, 0.38f, _progress);
                    break;
                default:
                    c.a = 0.6f;
                    break;
            }
            if (flash > 0f) c.a = Mathf.Max(c.a, flash * 0.7f);   // 터지는 순간 한 번 진해진다(그래도 화면을 덮지 않게)
            return c;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_shape.IsNone) return;

            // 나타날 때 작게 시작해 제 크기로 — 판정 도형 **안쪽**에서만 커진다
            float appear = Mathf.Clamp01(_age / AppearSeconds);
            appear = 1f - (1f - appear) * (1f - appear) * (1f - appear);
            float scale = Mathf.Lerp(AppearFrom, 1f, appear);
            var shape = _shape;
            if (_layer == Layer.Core)
            {
                // 차오름 — 진행도만큼 안에서부터. 그럴 수 없는 도형(줄 · 분면)은 진하기로만 찬다
                float p = _flash > 0f ? 1f : _progress;
                if (!_shape.TryGrown(Mathf.Max(0.02f, p), _roomSize, out shape)) shape = _shape;
            }
            else if (scale < 0.999f && _shape.TryGrown(scale, _roomSize, out var grown))
            {
                shape = grown;
            }

            _verts.Clear();
            _tris.Clear();
            // ⚠ 판정과 **같은 함수**다. 여기만 고치는 일이 없어야 한다.
            shape.Outline(_verts, _tris, _roomSize);
            if (_verts.Count == 0 || _tris.Count == 0) return;

            if (_layer == Layer.Edge) { PopulateEdge(vh); return; }

            var c = color;
            bool art = Art != null;
            // 무늬는 **화면에 고정**된 격자 위에서 천천히 흐른다. 도형을 따라 늘어나면 늘어난 티가 난다.
            var flow = Vector2.zero;   // 무늬는 흐르지 않는다(정신없다 — PD 2026-10-08)
            for (int i = 0; i < _verts.Count; i++)
            {
                var p = _verts[i];
                Vector2 uv;
                if (art) uv = new Vector2(p.x / FillPixels, p.y / FillPixels) + flow;
                else if (_layer == Layer.Fill && CanTile) uv = new Vector2(p.x / HatchPixels, p.y / HatchPixels);
                else uv = new Vector2(0.5f, 0.5f);   // 흰 텍스처를 단색으로 칠한다
                vh.AddVert(p, c, uv);
            }
            for (int i = 0; i + 2 < _tris.Count; i += 3)
                vh.AddTriangle(_tris[i], _tris[i + 1], _tris[i + 2]);
        }

        /// <summary>
        /// 둘레 띠. 삼각형 가운데 **한 번만 나오는 변**이 둘레다 — 도형 종류마다 따로 짜지 않는다.
        /// 띠는 둘레에서 **안쪽으로만** 두른다(판정 밖으로 안 나간다). 그림의 아래 끝(v 0)이 둘레다.
        /// </summary>
        private void PopulateEdge(VertexHelper vh)
        {
            _edgeCount.Clear();
            _edgeOther.Clear();
            _edgeVert.Clear();
            for (int t = 0; t + 2 < _tris.Count; t += 3)
                for (int k = 0; k < 3; k++)
                {
                    int a = _tris[t + k], b = _tris[t + (k + 1) % 3], other = _tris[t + (k + 2) % 3];
                    // ⚠ 꼭짓점 **번호가 아니라 자리로** 짝을 짓는다. 원은 한 바퀴 끝 꼭짓점이 처음 것과 같은 자리의
                    //   다른 번호라, 번호로 보면 그 살(중심 → 끝)이 둘레로 읽혀 원 안에 줄이 하나 그어졌다
                    int pa = PosKey(_verts[a]), pb = PosKey(_verts[b]);
                    if (pa == pb) continue;
                    long key = pa < pb ? ((long)pa << 32) | (uint)pb : ((long)pb << 32) | (uint)pa;
                    if (!_edgeVert.ContainsKey(pa)) _edgeVert[pa] = a;
                    if (!_edgeVert.ContainsKey(pb)) _edgeVert[pb] = b;
                    _edgeCount.TryGetValue(key, out int n);
                    _edgeCount[key] = n + 1;
                    _edgeOther[key] = other;
                }

            float width = EdgeWidth * (_flash > 0f ? 1.3f : 1f);
            var c = color;
            bool art = Art != null;
            float flowU = 0f;   // 빛이 둘레를 따라 흐르지 않는다(PD 2026-10-08)
            foreach (var pair in _edgeCount)
            {
                if (pair.Value != 1) continue;
                int a = _edgeVert[(int)(pair.Key >> 32)], b = _edgeVert[(int)(pair.Key & 0xFFFFFFFF)];
                var pa = _verts[a];
                var pb = _verts[b];
                var along = pb - pa;
                float len = along.magnitude;
                if (len < 0.5f) continue;
                var n = new Vector2(-along.y, along.x) / len;
                // 안쪽 = 그 변을 가진 삼각형의 세 번째 꼭짓점 쪽
                if (Vector2.Dot(_verts[_edgeOther[pair.Key]] - pa, n) < 0f) n = -n;
                float w = Mathf.Min(width, len);   // 짧은 변(고리 끝 등)에서 띠가 도형 밖으로 넘치지 않게
                int i = vh.currentVertCount;
                float u0 = flowU + (pa.x + pa.y) / EdgeTilePixels;   // 이웃 변과 무늬가 대충 이어지게 자리로 시작한다
                float u1 = u0 + len / EdgeTilePixels;
                vh.AddVert(pa, c, art ? new Vector2(u0, 0f) : new Vector2(0.5f, 0.5f));
                vh.AddVert(pb, c, art ? new Vector2(u1, 0f) : new Vector2(0.5f, 0.5f));
                vh.AddVert(pb + n * w, c, art ? new Vector2(u1, 1f) : new Vector2(0.5f, 0.5f));
                vh.AddVert(pa + n * w, c, art ? new Vector2(u0, 1f) : new Vector2(0.5f, 0.5f));
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i, i + 2, i + 3);
            }
        }

        /// <summary>꼭짓점 자리를 0.5 px 격자로 묶은 키. 같은 자리에 겹친 꼭짓점이 같은 키가 된다.</summary>
        private static int PosKey(Vector2 p)
        {
            int x = Mathf.RoundToInt(p.x * 2f), y = Mathf.RoundToInt(p.y * 2f);
            unchecked { return (x * 73856093) ^ (y * 19349663); }
        }
    }
}
