using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 성급 별 줄(PD 2026-11-02 시안 「조각을 모으며, 1성에서 10성까지」).
    ///   1~5성 = 노란 별이 하나씩 는다 · 6~9성 = 별 다섯 중 왼쪽부터 붉은 별로 바뀐다 · 10성 = 무지개 별 하나(크게).
    ///   0(미보유)은 별을 숨긴다.
    /// 화면마다 별 칸 다섯(`{prefix}0~4`)을 이미 갖고 있다 — 그 칸을 다시 써서 가운데 정렬한다.
    /// 그림: `ReBornKit/Icons/rb_icon_rank_gold_64` · `rb_icon_rank_red_64` · `rb_icon_rank_rainbow_128`.
    /// (예전 규칙 「1~4 은별 · 5성마다 큰 금별」(PD 2026-10-16)은 이 시안으로 바뀌었다.)
    /// </summary>
    public static class StarRank
    {
        /// <summary>한 줄 별 수 — 노란 줄 다섯, 붉은 줄 다섯을 지나면 무지개.</summary>
        public const int PerRow = 5;
        public const int Top = PerRow * 2;
        /// <summary>무지개 별 크기 — 노란 별 한 변의 배수.</summary>
        public const float BigScale = 1.8f;

        /// <summary>이 성급이 막 바뀐 별 칸 — 성급이 오를 때 그 칸에 빛을 낸다.</summary>
        public static int SlotOf(int rank)
            => rank >= Top ? 0 : rank > PerRow ? rank - PerRow - 1 : Mathf.Clamp(rank - 1, 0, PerRow - 1);

        /// <summary>그 성급 별 줄의 폭 — 줄을 왼쪽에 붙여 놓을 때 쓴다.</summary>
        public static float RowWidth(int rank, float small, float gap)
        {
            rank = Mathf.Clamp(rank, 0, Top);
            if (rank >= Top) return small * BigScale;
            int count = Mathf.Min(rank, PerRow);
            return count * small + Mathf.Max(0, count - 1) * gap;
        }

        /// <param name="root">별 칸들의 부모</param>
        /// <param name="prefix">별 칸 이름 앞부분 — `CardStar` 면 `CardStar0` ~ `CardStar4`</param>
        /// <param name="rank">성급(0~10)</param>
        /// <param name="center">줄 가운데(부모 가운데 기준)</param>
        /// <param name="small">노란 별 한 변</param>
        /// <param name="gap">별 사이</param>
        public static void Apply(Transform root, string prefix, int rank, Sprite yellow, Sprite red, Sprite rainbow,
                                 Vector2 center, float small, float gap)
        {
            if (root == null) return;
            rank = Mathf.Clamp(rank, 0, Top);
            bool top = rank >= Top;
            int count = top ? 1 : Mathf.Min(rank, PerRow);
            int reds = top ? 0 : Mathf.Max(0, rank - PerRow);
            float size = top ? small * BigScale : small;
            float width = count * size + Mathf.Max(0, count - 1) * gap;
            float x = center.x - width * 0.5f;
            for (int i = 0; i < PerRow; i++)
            {
                var slot = root.Find($"{prefix}{i}") as RectTransform;
                if (slot == null) continue;
                bool on = i < count;
                slot.gameObject.SetActive(on);
                if (!on) continue;
                slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0.5f, 0.5f);
                slot.sizeDelta = new Vector2(size, size);
                slot.anchoredPosition = new Vector2(x + size * 0.5f, center.y);
                x += size + gap;
                if (slot.TryGetComponent<Image>(out var img))
                {
                    img.sprite = top ? rainbow : i < reds ? red : yellow;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.type = Image.Type.Simple;
                    img.fillAmount = 1f;
                }
            }
        }
    }
}
