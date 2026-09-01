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

        // ── CanvasScaler Match 자동 조정 ──────────────────────────
        //
        // 규칙은 하나다 — **화면에 다 들어오는 쪽으로 맞춘다(contain).**
        // 가로·세로 배율 중 **작은 쪽**을 고르면 기준 해상도의 모든 칸이 화면 안에 남는다.
        //
        // ⚠ 예전에는 `Screen.width / Screen.height >= 1.778` 로 갈랐는데 두 가지가 틀렸다.
        //
        //   ① 세로 화면에서 `width/height` 는 0.5625 다. 1.778 을 넘을 수가 없어
        //      **언제나 match = 0(Width 기준)** 이었다. 갈림길이 아예 죽어 있었다.
        //
        //   ② 그 match = 0 이 태블릿에서 화면을 자른다.
        //        기준 720×1280 · 태블릿 768×1024
        //        가로로 맞추면 배율 768/720 = 1.067 → 세로로 보이는 칸은 1024/1.067 = 960
        //        **1280 중 320 칸이 아래로 잘려 나간다.** "밑이 짤린다" 가 이것이다.
        //        세로로 맞추면 배율 1024/1280 = 0.8 → 가로 960 칸, 좌우에 여백만 생긴다.
        //
        // 폰(16:9~20:9)은 세로 배율이 더 커서 예전처럼 match = 0 그대로다 — 달라지지 않는다.
        // 바뀌는 것은 화면이 기준보다 **덜 길쭉한** 기기(태블릿·폴더블)뿐이다.
        private void ApplyCanvasMatch()
        {
            if (_canvasScaler == null)
            {
                return;
            }

            Vector2 reference = _canvasScaler.referenceResolution;
            if (reference.x <= 0f || reference.y <= 0f || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            float scaleByWidth = Screen.width / reference.x;
            float scaleByHeight = Screen.height / reference.y;

            // 세로가 더 빠듯하면 세로로 맞춘다(match = 1) — 그래야 아래가 안 잘린다.
            _canvasScaler.matchWidthOrHeight = scaleByHeight < scaleByWidth ? 1f : 0f;
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
