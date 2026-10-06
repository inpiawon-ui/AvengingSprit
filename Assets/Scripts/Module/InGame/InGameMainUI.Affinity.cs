using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 상성 표시 (시험판 2026-10-02).
    ///
    /// 화면에 들어가는 것은 둘이다.
    ///   · `HostAffinityIcon` — CURRENT HOST 칸의 보석. 지금 몸이 파워 · 무기 · 마법 가운데 어느 쪽인가.
    ///   · `AffinityTriangle` — 삼각 상성판. **CHAPTER 칸 왼쪽에 늘 떠 있다.** 내 쪽 꼭짓점이 빛나고,
    ///     내가 이기는 쪽으로 가는 화살표는 초록, 나를 이기는 쪽에서 오는 화살표는 붉다.
    ///
    /// ⚠ 처음에는 방에 들어설 때 방 위쪽에 크게 띄웠다가 사라지게 했는데, 거기 선 적을 가렸다.
    ///   「위쪽 UI 에, 챕터 정보를 옆으로 밀고 그 사이에」(기획 2026-10-02) — HUD 안으로 옮겼다.
    ///
    /// ⚠ `Time.timeScale` 을 안 쓴다 — 타격 멈칫(HitStop)이 걸려도 연출이 얼지 않아야 한다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float TrianglePopSeconds = 0.22f;
        private const float TrianglePopScale = 1.35f;

        private string _affinityHostKey;
        private Sprite _triangleShown;
        /// <summary>겹쳐 불릴 때 앞 연출을 그만두게 하는 번호.</summary>
        private int _trianglePopId;

        private void ShowAffinityTriangle()
        {
            var img = _ui.Get<Image>("AffinityTriangle");
            if (img == null) return;

            var sprite = _hasRealHost && _battle != null ? _battle.AffinityTriangleOf(_affinityHostKey) : null;
            img.gameObject.SetActive(sprite != null);
            if (sprite == null) { _triangleShown = null; return; }

            // 쪽이 바뀌었을 때만 튕긴다 — 같은 몸으로 방만 넘어갈 때마다 뛰면 시끄럽다.
            bool changed = sprite != _triangleShown;
            _triangleShown = sprite;
            img.sprite = sprite;
            img.color = Color.white;
            if (changed) PopTriangleAsync(img, ++_trianglePopId).Forget();   // fire-and-forget: 연출은 기다릴 것이 없다
        }

        private async UniTaskVoid PopTriangleAsync(Image img, int id)
        {
            var tr = img.rectTransform;
            float t = 0f;
            while (t < TrianglePopSeconds)
            {
                if (this == null || img == null || id != _trianglePopId) return;
                tr.localScale = Vector3.one * Mathf.Lerp(TrianglePopScale, 1f, t / TrianglePopSeconds);
                await UniTask.Yield();
                t += Time.unscaledDeltaTime;
            }
            if (this != null && img != null && id == _trianglePopId) tr.localScale = Vector3.one;
        }
    }
}
