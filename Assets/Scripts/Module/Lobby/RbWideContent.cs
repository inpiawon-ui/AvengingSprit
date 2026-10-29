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

        /// <summary>
        /// 0 이면 예전처럼 폭만 맞춘다. 값이 있으면 그 설계 높이가 보기 칸에 다 들어가게 세로로도 줄인다 —
        /// 스크롤 없는 한 화면(호스트 성장, PD 2026-10-29). 4:3 에서 폭으로 1.33배 키우면 아래가 잘렸다.
        /// </summary>
        public float FitHeight { get; set; }

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
            if (FitHeight > 0f && _view.rect.height > 0f) k = Mathf.Min(k, _view.rect.height / FitHeight);
            if (Mathf.Approximately(k, _scale)) return;
            _scale = k;
            _rt.localScale = new Vector3(k, k, 1f);
        }
    }
}
