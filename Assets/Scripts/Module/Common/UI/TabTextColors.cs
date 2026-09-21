using TMPro;
using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 탭 글자 색 두 벌 — 켜진 탭(노란 판 위 남색 글자) · 꺼진 탭(파란 판 위 흰 글자). 시안에서 잰 값을 빌더가 넣는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TabTextColors : MonoBehaviour
    {
        [SerializeField] private Color _on = Color.black;
        [SerializeField] private Color _off = Color.white;

        public void Set(Color on, Color off) { _on = on; _off = off; }

        public void Apply(bool on) => GetComponent<TextMeshProUGUI>().color = on ? _on : _off;
    }
}
