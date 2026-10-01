using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「남는 가로를 **늘려서** 채워라」 표시이자 그 일을 하는 컴포넌트.
    ///
    /// 이 화면은 9:16(720 폭)으로 그렸다. 태블릿 4:3 에서는 보이는 폭이 960 이라
    /// 그대로 두면 판이 가운데 720 에만 모이고 좌우에 240 이 남는다 —
    /// 「유아이가 가운데 몰렸다」(2026-10-01 지적).
    ///
    /// 그래서 **가로만 배율로 늘린다.** 세로는 건드리지 않는다 —
    /// 세로로 남는 자리(20:9)는 배경 그림의 위아래 여백이 메운다.
    /// 세로까지 늘리면 20:9 에서 글자가 25 % 길쭉해진다.
    ///
    /// ⚠ 판·글자가 **다 같이** 늘어나므로 배경에 그려진 테두리와 어긋나지 않는다.
    ///   배경도 이 안에 들어 있기 때문이다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFitStretchX : MonoBehaviour
    {
        /// <summary>이 화면을 그린 기준 폭. 9:16 이다.</summary>
        [SerializeField] private float _baseWidth = 720f;

        private RectTransform _self;
        private RectTransform _parent;
        private float _applied = -1f;

        private void Awake()
        {
            _self = (RectTransform)transform;
            _parent = transform.parent as RectTransform;
            Apply();
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_self == null) _self = (RectTransform)transform;
            if (_parent == null) _parent = transform.parent as RectTransform;
            if (_parent == null || _baseWidth <= 0f) return;

            // ⚠ 내 상자는 **기준 폭(720)으로 고정**되어 있어야 한다. `ScreenFit` 이 스트레치로
            //   바꿔 놓으면 상자가 이미 960 인데 배율까지 겹쳐 두 번 늘어난다(2026-10-01).
            //   `ScreenFitLock` 으로 막아 두지만, 혹시 몰라 여기서도 되돌린다.
            if (_self.anchorMin != new Vector2(0.5f, 0.5f) || _self.anchorMax != new Vector2(0.5f, 0.5f))
            {
                var size = _self.rect.size;
                _self.anchorMin = _self.anchorMax = _self.pivot = new Vector2(0.5f, 0.5f);
                _self.sizeDelta = new Vector2(_baseWidth, size.y);
                _self.anchoredPosition = Vector2.zero;
            }

            float width = _parent.rect.width;
            if (width <= 1f) return;

            // 좁아지는 쪽으로는 줄이지 않는다 — 줄이면 9:16 에서 글자가 작아진다
            float scale = Mathf.Max(1f, width / _baseWidth);
            if (Mathf.Abs(scale - _applied) < 0.001f) return;

            _applied = scale;
            _self.localScale = new Vector3(scale, 1f, 1f);
        }
    }
}
