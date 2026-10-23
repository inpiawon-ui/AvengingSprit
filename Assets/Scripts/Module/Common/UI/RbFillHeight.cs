using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 세로 스크롤 내용 판 — 보기 칸이 시안 높이보다 길면(20:9) 판을 보기 칸만큼 늘린다.
    /// 판 아래에 붙인 묶음(바닥 앵커)이 화면 아래로 내려가 시안처럼 바닥에 선다. 짧으면(16:9) 시안 높이 그대로 두고 민다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RbFillHeight : MonoBehaviour
    {
        [SerializeField] private float _designHeight;

        private RectTransform _rt;

        private void Awake() => _rt = (RectTransform)transform;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Update()
        {
            // 보기 칸은 부모라 제 크기 바뀜 알림이 여기로 오지 않는다 — 값이 달라질 때만 적는다(할당 없음)
            Apply();
        }

        private void Apply()
        {
            if (_rt == null || !(_rt.parent is RectTransform view)) return;
            float h = Mathf.Max(_designHeight, view.rect.height);
            if (Mathf.Abs(_rt.sizeDelta.y - h) > 0.5f) _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, h);
        }

        public void SetDesignHeight(float h) => _designHeight = h;
    }
}
