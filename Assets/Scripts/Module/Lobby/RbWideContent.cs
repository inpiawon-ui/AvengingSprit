using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 넓은 화면(4:3 태블릿) — 720 폭으로 지은 스크롤 본문을 보기 칸 폭에 맞게 통째로 키운다. 세로는 스크롤이 받는다.
    /// 본문은 위 가운데 기준 · 폭 720(빌더가 맞춘다). 폰(보기 칸 ≤ 720)은 1배 그대로.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RbWideContent : MonoBehaviour
    {
        private const float DesignWidth = 720f;

        private RectTransform _rt;
        private RectTransform _view;
        private float _scale = 1f;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _view = _rt.parent as RectTransform;
        }

        // 보기 칸 폭은 해상도 · 안전 영역이 바뀔 때만 달라진다 — 같으면 아무것도 쓰지 않는다
        private void LateUpdate()
        {
            if (_view == null) return;
            float k = Mathf.Max(1f, _view.rect.width / DesignWidth);
            if (Mathf.Approximately(k, _scale)) return;
            _scale = k;
            _rt.localScale = new Vector3(k, k, 1f);
        }
    }
}
