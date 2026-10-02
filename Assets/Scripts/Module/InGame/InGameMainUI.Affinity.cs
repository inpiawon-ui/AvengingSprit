using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 상성 표시 (시험판 2026-10-02).
    ///
    /// 화면에 들어가는 것은 둘이다.
    ///   · `HostAffinityIcon` — CURRENT HOST 칸의 보석. 지금 몸이 날 · 힘 · 술 가운데 어느 쪽인가.
    ///   · `AffinityTriangle` — 삼각 상성판. **방에 들어설 때와 몸을 갈아탔을 때** 방 위쪽에
    ///     크게 떴다가 사라진다. 늘 떠 있으면 방을 가리고, 구석에 작게 두면 안 보인다.
    ///
    /// ⚠ `Time.timeScale` 을 안 쓴다 — 타격 멈칫(HitStop)이 걸려도 연출이 얼지 않아야 한다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float TriangleInSeconds = 0.18f;
        private const float TriangleHoldSeconds = 1.5f;
        private const float TriangleOutSeconds = 0.35f;
        private const float TriangleOvershoot = 1.25f;

        private string _affinityHostKey;
        /// <summary>겹쳐 불릴 때 앞 연출을 그만두게 하는 번호.</summary>
        private int _triangleShowId;

        private void ShowAffinityTriangle()
        {
            var img = _ui.Get<Image>("AffinityTriangle");
            if (img == null) return;

            var sprite = _hasRealHost && _battle != null ? _battle.AffinityTriangleOf(_affinityHostKey) : null;
            _triangleShowId++;
            if (sprite == null) { img.gameObject.SetActive(false); return; }

            img.sprite = sprite;
            PlayTriangleAsync(img, _triangleShowId).Forget();   // fire-and-forget: 연출은 기다릴 것이 없다
        }

        private async UniTaskVoid PlayTriangleAsync(Image img, int id)
        {
            img.gameObject.SetActive(true);
            var tr = img.rectTransform;
            float total = TriangleInSeconds + TriangleHoldSeconds + TriangleOutSeconds;
            float t = 0f;
            while (t < total)
            {
                if (this == null || img == null || id != _triangleShowId) return;
                float alpha = t < TriangleInSeconds ? t / TriangleInSeconds
                            : t < TriangleInSeconds + TriangleHoldSeconds ? 1f
                            : 1f - (t - TriangleInSeconds - TriangleHoldSeconds) / TriangleOutSeconds;
                // 큰 데서 줄어들며 앉는다 — 팝업과 같은 규칙.
                float scale = t < TriangleInSeconds
                    ? Mathf.Lerp(TriangleOvershoot, 1f, t / TriangleInSeconds) : 1f;
                img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                tr.localScale = Vector3.one * scale;
                await UniTask.Yield();
                t += Time.unscaledDeltaTime;
            }
            if (this != null && img != null && id == _triangleShowId) img.gameObject.SetActive(false);
        }
    }
}
