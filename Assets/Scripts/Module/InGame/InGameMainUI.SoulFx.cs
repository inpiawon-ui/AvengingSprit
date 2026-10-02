using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 버튼 둘레의 빛 (기획 2026-10-02).
    ///
    /// 유령일 때 탈 수 있는 몸이 잡히면 `PossessButtonGlow` 가 숨쉬듯 빛난다 —
    /// 방 안의 빛줄기(「저 몸」)와 짝을 이뤄 「지금 이 버튼」을 알린다.
    /// 몸을 입고 있을 때(그 버튼이 탈출일 때)는 켜지 않는다. 나가라고 재촉하는 꼴이 된다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float GlowPulseSeconds = 0.9f;
        private const float GlowMinAlpha = 0.45f;

        private bool _possessGlowOn;
        /// <summary>겹쳐 불릴 때 앞 맥박을 그만두게 하는 번호.</summary>
        private int _possessGlowId;

        private void SetPossessGlow(bool on)
        {
            if (on == _possessGlowOn) return;
            _possessGlowOn = on;

            var img = _ui.Get<Image>("PossessButtonGlow");
            if (img == null) return;
            img.gameObject.SetActive(on);
            if (on) PulseGlowAsync(img, ++_possessGlowId).Forget();   // fire-and-forget: 꺼질 때까지 도는 맥박
            else _possessGlowId++;
        }

        private async UniTaskVoid PulseGlowAsync(Image img, int id)
        {
            float t = 0f;
            while (this != null && img != null && id == _possessGlowId)
            {
                float k = Mathf.PingPong(t / GlowPulseSeconds * 2f, 1f);
                img.color = new Color(1f, 1f, 1f, Mathf.Lerp(GlowMinAlpha, 1f, k));
                img.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.06f, k);
                await UniTask.Yield();
                t += Time.unscaledDeltaTime;
            }
        }
    }
}
