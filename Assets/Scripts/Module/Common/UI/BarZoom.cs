using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 위 막대 · 아래 바를 **화면 폭에 맞춰 통째로 키운다**(PD 2026-10-18 「4:3 일 때 탑뷰가 이상하다 — 스케일링 해 줘」).
    /// 막대는 720 폭으로 그렸다. 태블릿 4:3 은 보이는 폭이 960 이라 막대가 가운데에 몰리고 양옆만 띠로 늘어 어색했다.
    /// 배율 = 부모 폭 ÷ 720 (1 ~ <see cref="MaxScale"/>). 9:16 · 20:9 은 폭이 720 이라 1 — 그린 그대로다.
    /// 기준점(pivot)이 위 막대는 위 변, 아래 바는 아래 변이라 커져도 화면 끝에 붙어 있다.
    /// 막대 사이에 끼는 화면(홈 등)은 <see cref="ScaleFor"/> 로 같은 배율을 읽어 여백을 맞춘다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BarZoom : MonoBehaviour
    {
        public const float BaseWidth = 720f;
        public const float MaxScale = 1.5f;

        /// <summary>부모 폭에 맞는 막대 배율.</summary>
        public static float ScaleFor(float parentWidth) => Mathf.Clamp(parentWidth / BaseWidth, 1f, MaxScale);

        private float _lastWidth = -1f;

        private void OnEnable() => _lastWidth = -1f;

        // 막대 자신은 크기가 고정이라 화면이 바뀌어도 제 크기 변경 알림이 안 온다 — 부모 폭이 바뀐 때만 다시 맞춘다
        private void LateUpdate()
        {
            if (transform.parent is not RectTransform parent) return;
            float w = parent.rect.width;
            if (Mathf.Approximately(w, _lastWidth)) return;
            _lastWidth = w;
            float k = ScaleFor(w);
            transform.localScale = new Vector3(k, k, 1f);
        }
    }
}
