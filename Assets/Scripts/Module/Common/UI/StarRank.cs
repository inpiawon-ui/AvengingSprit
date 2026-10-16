using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 성급 별 줄 — 1~4성은 작은 은별, 5성마다 큰 금별(PD 2026-10-16 「반 칸 별 별로 · 큰 별 하나를 5성으로 · 금별은 좀 큰 별」).
    ///   3성 = 은 3 · 5성 = 금 1 · 7성 = 금 1 + 은 2 · 10성 = 금 2. 0(미보유)은 별을 숨긴다.
    /// 화면마다 별 칸 다섯(`{prefix}0~4`)을 이미 갖고 있다 — 그 칸을 다시 써서 왼쪽부터 큰 금별 → 은별 순으로 가운데 정렬한다.
    /// 그림: `ChapterHost/ch_star_gold.png` · `ch_star_silver.png`(같은 크기로 그려 큰 별은 <see cref="BigScale"/> 배로 키운다).
    /// </summary>
    public static class StarRank
    {
        public const int PerGold = 5;
        public const float BigScale = 1.6f;

        /// <param name="root">별 칸들의 부모</param>
        /// <param name="prefix">별 칸 이름 앞부분 — `CardStar` 면 `CardStar0` ~ `CardStar4`</param>
        /// <param name="rank">성급(0~10)</param>
        /// <param name="center">줄 가운데(부모 가운데 기준)</param>
        /// <param name="small">은별 한 변</param>
        /// <param name="gap">별 사이</param>
        public static void Apply(Transform root, string prefix, int rank, Sprite gold, Sprite silver,
                                 Vector2 center, float small, float gap)
        {
            if (root == null) return;
            rank = Mathf.Max(0, rank);
            int golds = rank / PerGold, silvers = rank % PerGold;
            float big = small * BigScale;
            float width = golds * big + silvers * small + Mathf.Max(0, golds + silvers - 1) * gap;
            float x = center.x - width * 0.5f;
            for (int i = 0; i < 5; i++)
            {
                var slot = root.Find($"{prefix}{i}") as RectTransform;
                if (slot == null) continue;
                bool isGold = i < golds, on = i < golds + silvers;
                slot.gameObject.SetActive(on);
                if (!on) continue;
                float size = isGold ? big : small;
                slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0.5f, 0.5f);
                slot.sizeDelta = new Vector2(size, size);
                slot.anchoredPosition = new Vector2(x + size * 0.5f, center.y);
                x += size + gap;
                if (slot.TryGetComponent<Image>(out var img))
                {
                    img.sprite = isGold ? gold : silver;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.type = Image.Type.Simple;
                    img.fillAmount = 1f;
                }
            }
        }
    }
}
