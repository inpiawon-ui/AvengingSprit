using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common
{
    /// <summary>
    /// 노치·홈바 등 기기의 세이프 에어리어를 RectTransform 앵커에 반영하고,
    /// 부모 CanvasScaler의 Match 값을 화면 비율에 따라 자동 조정한다.
    /// 씬의 SafeAreaPanel에 부착한다. (05_prefabs 규약)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        // 16:9(가로) 기준 비율. 이 값 이상이면 Height 기준(match=1), 미만이면 Width 기준(match=0).
        private const float LandscapeRatioThreshold = 1.778f;

        private RectTransform _rectTransform;
        private CanvasScaler _canvasScaler;

        // 변화 감지용 캐시 — 값이 바뀔 때만 재적용해 매 프레임 할당을 피한다.
        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasScaler = GetComponentInParent<CanvasScaler>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            // 세이프 에어리어·해상도·방향 중 하나라도 바뀌면 재적용
            if (Screen.safeArea != _lastSafeArea
                || Screen.orientation != _lastOrientation
                || Screen.width != _lastResolution.x
                || Screen.height != _lastResolution.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _lastOrientation = Screen.orientation;
            _lastResolution = new Vector2Int(Screen.width, Screen.height);

            ApplyCanvasMatch();
            ApplySafeAreaRect();
        }

        // 화면 비율에 따라 CanvasScaler Match를 조정한다. 고정값으로 두지 않는 이유는 05_prefabs 참조.
        private void ApplyCanvasMatch()
        {
            if (_canvasScaler == null)
            {
                return;
            }

            float screenRatio = (float)Screen.width / Screen.height;
            _canvasScaler.matchWidthOrHeight = screenRatio >= LandscapeRatioThreshold ? 1f : 0f;
        }

        // Screen.safeArea(픽셀)를 0~1 앵커로 변환해 패널이 안전 영역만 채우도록 한다.
        private void ApplySafeAreaRect()
        {
            Rect safeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
