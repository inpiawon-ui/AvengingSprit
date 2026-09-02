using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// <see cref="DangerShape"/> 를 바닥에 그린다.
    ///
    /// ⚠ **도형을 여기서 만들지 않는다.** 넘겨받은 것을 그대로 그린다 —
    ///   판정과 같은 구조체를 같은 함수(`Outline`)로 그리므로, 그린 것과 맞는 것이
    ///   어긋날 수가 없다. 여기서 "조금 크게 그리면 보기 좋겠다" 를 하는 순간
    ///   그 보장이 깨진다.
    ///
    /// 채움은 빗금 타일(`fx_danger_hatch` · `fx_safe_hatch`)이다.
    /// 없으면 단색으로 칠한다 — 그림이 늦어도 굴러가야 한다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DangerView : MaskableGraphic
    {
        /// <summary>빗금 한 칸이 화면에서 차지하는 크기(px). 타일이 64 라 그대로 쓴다.</summary>
        private const float HatchPixels = 64f;

        private static readonly Color DangerTint = new(1f, 0.30f, 0.28f, 0.55f);
        private static readonly Color SafeTint = new(0.35f, 1f, 0.45f, 0.42f);

        private readonly List<Vector2> _verts = new(128);
        private readonly List<int> _tris = new(256);

        private DangerShape _shape;
        private Vector2 _roomSize;
        private Sprite _hatch;
        private bool _safe;

        /// <summary>깜빡임 — 예고가 끝나갈수록 빨라진다. 시간이 얼마 안 남았다는 신호다.</summary>
        private float _pulse;
        private float _progress;

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

        public override Texture mainTexture => CanTile ? _hatch.texture : s_WhiteTexture;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public static DangerView Create(Transform parent)
        {
            var go = new GameObject("DangerView", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<DangerView>();
            var rt = (RectTransform)go.transform;
            // 방 좌표계 그대로 쓴다 — 왼쪽 위가 (0,0), 아래로 갈수록 y 가 음수다.
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            go.SetActive(false);
            return v;
        }

        /// <summary>그릴 것을 넘긴다. <paramref name="safe"/> 면 초록 안전지대로 그린다.</summary>
        public void Show(DangerShape shape, Vector2 roomSize, Sprite hatch, bool safe)
        {
            _shape = shape;
            _roomSize = roomSize;
            _hatch = hatch;
            _safe = safe;
            _progress = 0f;
            _pulse = 0f;
            color = safe ? SafeTint : DangerTint;
            gameObject.SetActive(!shape.IsNone);
            SetVerticesDirty();
            SetMaterialDirty();
        }

        public void Hide()
        {
            _shape = default;
            gameObject.SetActive(false);
        }

        public bool IsShowing => gameObject.activeSelf && !_shape.IsNone;

        /// <summary>
        /// 예고가 얼마나 찼는지(0~1) 알려 준다. 끝이 가까울수록 빠르게 깜빡인다.
        /// </summary>
        public void Tick(float dt, float progress01)
        {
            if (!IsShowing) return;
            _progress = Mathf.Clamp01(progress01);
            // 0.15초 주기에서 0.05초까지 빨라진다
            _pulse += dt / Mathf.Lerp(0.30f, 0.10f, _progress);
            float a = Mathf.Lerp(0.35f, 0.75f, Mathf.Abs(Mathf.Sin(_pulse * Mathf.PI)));
            var c = _safe ? SafeTint : DangerTint;
            c.a = _safe ? SafeTint.a : a;
            if (color != c) color = c;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_shape.IsNone) return;

            _verts.Clear();
            _tris.Clear();
            // ⚠ 판정과 **같은 함수**다. 여기만 고치는 일이 없어야 한다.
            _shape.Outline(_verts, _tris, _roomSize);
            if (_verts.Count == 0 || _tris.Count == 0) return;

            var c = color;
            for (int i = 0; i < _verts.Count; i++)
            {
                var p = _verts[i];
                // 빗금은 **화면에 고정**된 격자다. 도형을 따라 늘어나면 늘어난 티가 나고,
                // 도형이 움직일 때 무늬가 같이 끌려가 어지럽다.
                // 타일을 못 쓰면 UV 를 한 점에 고정한다 — 흰 텍스처를 단색으로 칠한다.
                var uv = CanTile ? new Vector2(p.x / HatchPixels, p.y / HatchPixels)
                                 : new Vector2(0.5f, 0.5f);
                vh.AddVert(p, c, uv);
            }
            for (int i = 0; i + 2 < _tris.Count; i += 3)
                vh.AddTriangle(_tris[i], _tris[i + 1], _tris[i + 2]);
        }
    }
}
