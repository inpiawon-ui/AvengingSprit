using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 그린 판(폭 720 · 높이 <see cref="DesignHeight"/>)이 부모 칸에 다 들어가게 **같은 배율로 줄인다**(키우지는 않는다).
    /// RE:BORN 실패 결과처럼 시안 세로가 16:9 한 화면(1280)보다 긴 판 — 20:9 에서는 그대로, 16:9 · 4:3 에서는 조금 작아진다.
    /// 판은 가운데 앵커 · 가운데 피벗으로 둔다. 세로가 남으면(20:9) 가운데가 아니라 위 여백을 <see cref="_topInset"/> 까지만 준다 —
    /// 시안은 위 140 줄(게임 93)부터 그려져 있어, 가운데에 두면 시안보다 아래로 처졌다(2026-10-27 레벨업 · 결과 대조).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RbFitScale : MonoBehaviour
    {
        [SerializeField] private float _designWidth = 720f;
        [SerializeField] private float _designHeight = 1280f;
        [Tooltip("남는 세로가 있을 때 위 여백의 상한(게임 px). 0 이면 늘 가운데")]
        [SerializeField] private float _topInset = 140f * 2f / 3f;

        private RectTransform _rt;
        private Vector2 _lastView = new(-1f, -1f);

        public float DesignHeight
        {
            get => _designHeight;
            set
            {
                _designHeight = Mathf.Max(1f, value);
                _lastView = new Vector2(-1f, -1f);
            }
        }

        private void Awake() => _rt = (RectTransform)transform;

        private void OnEnable() => _lastView = new Vector2(-1f, -1f);

        private void LateUpdate()
        {
            if (_rt == null || _rt.parent is not RectTransform view) return;
            var size = view.rect.size;
            if ((size - _lastView).sqrMagnitude < 0.25f) return;
            _lastView = size;
            _rt.sizeDelta = new Vector2(_designWidth, _designHeight);
            float k = Mathf.Min(1f, size.x / _designWidth, size.y / _designHeight);
            _rt.localScale = new Vector3(k, k, 1f);
            float spare = size.y - _designHeight * k;
            float top = _topInset > 0f ? Mathf.Min(spare * 0.5f, _topInset) : spare * 0.5f;
            _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, spare * 0.5f - top);
        }
    }
}
