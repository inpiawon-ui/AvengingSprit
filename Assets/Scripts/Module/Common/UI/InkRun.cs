using TMPro;
using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 글자 여러 칸을 **한 줄로 잇는다** — 「Lv.」 「28」 「/ 50」 처럼 크기 · 색이 다른 조각.
    ///
    /// 숫자 자릿수가 바뀌면(9 → 10) 뒤 조각이 제자리에 있으면 겹치거나 벌어진다.
    /// 앞 조각 잉크 오른쪽 끝 + 간격(시안에서 잰 값)에 다음 조각 잉크 왼쪽 끝을 붙인다.
    /// 첫 조각은 제자리(빌더가 둔 곳)에 둔다. 글자를 바꾼 뒤 <see cref="Apply"/> 를 부른다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InkRun : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] _parts = System.Array.Empty<TextMeshProUGUI>();
        /// <summary>parts[i] 끝 → parts[i+1] 시작 간격(부모 좌표 px).</summary>
        [SerializeField] private float[] _gaps = System.Array.Empty<float>();

        public void Set(TextMeshProUGUI[] parts, float[] gaps)
        {
            _parts = parts;
            _gaps = gaps;
        }

        public void Apply()
        {
            if (_parts == null || _parts.Length == 0) return;
            float right = float.NaN;
            for (int i = 0; i < _parts.Length; i++)
            {
                var t = _parts[i];
                if (t == null || !t.gameObject.activeSelf) continue;
                t.horizontalAlignment = HorizontalAlignmentOptions.Left;
                t.ForceMeshUpdate();
                var rt = t.rectTransform;
                float sx = rt.localScale.x;
                if (!float.IsNaN(right))
                {
                    float gap = i - 1 < _gaps.Length ? _gaps[i - 1] : 6f;
                    var p = rt.localPosition;
                    p.x = right + gap - t.textBounds.min.x * sx;
                    rt.localPosition = p;
                }
                right = rt.localPosition.x + t.textBounds.max.x * sx;
            }
        }
    }
}
