using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 이징 곡선 — DOTween 의 같은 이름 곡선과 같은 식(Robert Penner). 프로젝트에 DOTween 이 없어 여기 둔다.
    /// 값은 0~1 진행도를 받아 0~1(넘칠 수 있음)을 돌려준다.
    /// </summary>
    public static class Ease
    {
        private const float BackOvershoot = 1.70158f;

        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);

        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

        public static float InQuad(float t) => t * t;

        /// <summary>끝에서 살짝 넘쳤다 돌아온다 — 창 · 아이콘 등장(「1.2 배 → 1 배」 느낌을 곡선으로).</summary>
        public static float OutBack(float t, float overshoot = BackOvershoot)
        {
            float c3 = overshoot + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + overshoot * u * u;
        }

        /// <summary>시작에서 살짝 뒤로 물러났다 나간다 — 빨려 들어가는 퇴장.</summary>
        public static float InBack(float t, float overshoot = BackOvershoot)
        {
            float c3 = overshoot + 1f;
            return c3 * t * t * t - overshoot * t * t;
        }

        /// <summary>바닥에 떨어져 통통 튄다 — 상자가 놓일 때.</summary>
        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
