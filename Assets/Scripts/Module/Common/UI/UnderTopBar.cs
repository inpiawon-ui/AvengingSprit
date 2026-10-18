using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 위 막대가 태블릿에서 커진 만큼(<see cref="BarZoom"/>) 이 창을 **아래 바 위를 기준으로 살짝 줄여** 막대 밑으로 들인다.
    /// 4:3 에서 위 막대가 120 → 160 이 되자 출전 · 소환 · 상점 · 패스 창의 맨 위 제목이 막대에 40 가려졌다(2026-10-18).
    /// 창은 화면 높이(1280) 기준으로 그렸다 — 자리를 다시 짜지 않고, 아래 바 윗변은 그대로 두고 그 위만 비율로 줄인다.
    /// 9:16 · 20:9 은 막대 배율이 1 이라 아무 일도 안 한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnderTopBar : MonoBehaviour
    {
        /// <summary>배율 1 일 때 위 막대 · 아래 바 높이(캔버스 px).</summary>
        private const float TopBar = 120f, NavBar = 110f;

        private float _lastWidth = -1f;

        private void OnEnable() => _lastWidth = -1f;

        private void LateUpdate()
        {
            var self = (RectTransform)transform;
            if (self.parent is not RectTransform parent) return;
            float w = parent.rect.width;
            if (Mathf.Approximately(w, _lastWidth)) return;
            _lastWidth = w;
            float h = parent.rect.height;
            float extra = TopBar * (BarZoom.ScaleFor(w) - 1f);
            float s = h > NavBar ? 1f - extra / (h - NavBar) : 1f;
            self.pivot = new Vector2(0.5f, NavBar / Mathf.Max(1f, h));   // 아래 바 윗변이 움직이지 않는 점
            self.localScale = new Vector3(s, s, 1f);
        }
    }
}
