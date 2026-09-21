using TMPro;
using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 하단 바 한 칸(HOST · PLAY · SHOP) — **선택된 칸**만 금색 테두리 · 노란 글자다(기획 2026-09-21).
    /// 그림은 두 벌(보통 · 선택)을 겹쳐 두고 하나만 켠다. 부제 글자 색도 상태마다 다르다(시안에서 잰 값).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NavTabView : MonoBehaviour
    {
        [SerializeField] private GameObject _on;
        [SerializeField] private GameObject _off;
        [SerializeField] private TextMeshProUGUI _sub;
        [SerializeField] private Color _subOn = Color.yellow;
        [SerializeField] private Color _subOff = Color.white;

        public void Set(GameObject on, GameObject off, TextMeshProUGUI sub, Color subOn, Color subOff)
        {
            _on = on; _off = off; _sub = sub; _subOn = subOn; _subOff = subOff;
        }

        public void SetSelected(bool selected)
        {
            if (_on != null) _on.SetActive(selected);
            if (_off != null) _off.SetActive(!selected);
            if (_sub != null) _sub.color = selected ? _subOn : _subOff;
        }
    }
}
