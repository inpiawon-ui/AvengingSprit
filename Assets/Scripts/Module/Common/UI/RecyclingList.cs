using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>재사용 목록이 줄 하나를 채울 때 부르는 쪽.</summary>
    public interface IRecyclingListSource
    {
        /// <summary>item 칸에 index 번째 줄 내용을 채운다. slot 은 그 칸 고유 번호(칸마다 딸린 것 — 썸네일 슬롯 등 — 을 찾을 때).</summary>
        void FillItem(int index, RectTransform item, int slot);
    }

    /// <summary>
    /// 재사용 스크롤 목록(PD 2026-10-30 「아주 적은 목록이 아니면 모든 스크롤을 재사용 스크롤뷰로」) — 05_prefabs ScrollView 규칙.
    /// 줄 틀 하나를 화면에 보이는 수 + 2 칸만 복제해 두고, 스크롤할 때 화면 밖으로 나간 칸을 반대편으로 옮겨 다시 채운다.
    /// 줄 수가 100이 넘어도 오브젝트는 보이는 만큼만 있다. 세로 · 가로 한 줄 목록(같은 간격)만 다룬다.
    /// </summary>
    public sealed class RecyclingList
    {
        private readonly ScrollRect _scroll;
        private readonly RectTransform _template;
        private readonly float _pitch;
        private readonly float _pad;
        private readonly bool _horizontal;
        private readonly Vector2 _home;
        private readonly List<RectTransform> _items = new();
        private readonly List<int> _shownIndex = new();
        private IRecyclingListSource _source;
        private int _count;

        public int SlotCount => _items.Count;

        /// <param name="template">첫 줄 자리(내용 안). 복제의 틀이자 0번 줄 자리다.</param>
        /// <param name="pitch">줄 간격(px)</param>
        /// <param name="pad">목록 끝 여백(px)</param>
        public RecyclingList(ScrollRect scroll, RectTransform template, float pitch, float pad, bool horizontal = false)
        {
            _scroll = scroll;
            _template = template;
            _pitch = pitch;
            _pad = pad;
            _horizontal = horizontal;
            _home = template.anchoredPosition;
            _scroll.onValueChanged.AddListener(_ => Refresh(false));
        }

        /// <summary>줄 수를 정하고 보이는 칸을 다시 채운다.</summary>
        public void SetCount(int count, IRecyclingListSource source)
        {
            _source = source;
            _count = Mathf.Max(0, count);
            var content = _scroll.content;
            float length = _count * _pitch + _pad;
            content.sizeDelta = _horizontal ? new Vector2(length, content.sizeDelta.y) : new Vector2(content.sizeDelta.x, length);
            EnsureItems();
            Refresh(true);
        }

        /// <summary>index 번째 줄이 보이게 스크롤한다(위 · 왼쪽에서 한 줄 여유).</summary>
        public void ScrollTo(int index)
        {
            var view = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
            var content = _scroll.content;
            float range = Mathf.Max(1f, (_horizontal ? content.rect.width - view.rect.width : content.rect.height - view.rect.height));
            float k = Mathf.Clamp01((index - 1) * _pitch / range);
            if (_horizontal) _scroll.horizontalNormalizedPosition = k;
            else _scroll.verticalNormalizedPosition = 1f - k;
            Refresh(false);
        }

        private void EnsureItems()
        {
            var view = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
            float viewLength = _horizontal ? view.rect.width : view.rect.height;
            int need = Mathf.Min(_count, Mathf.CeilToInt(viewLength / Mathf.Max(1f, _pitch)) + 2);
            if (_items.Count == 0) { _items.Add(_template); _shownIndex.Add(-1); }
            while (_items.Count < need)
            {
                var copy = (RectTransform)Object.Instantiate(_template, _template.parent);
                copy.name = $"{_template.name}{_items.Count}";
                _items.Add(copy);
                _shownIndex.Add(-1);
            }
        }

        private void Refresh(bool force)
        {
            if (_source == null) return;
            var content = _scroll.content;
            // 내용이 스크롤된 거리 — 세로는 위로 올라간 만큼(+), 가로는 왼쪽으로 밀린 만큼(−)
            float scrolled = _horizontal ? -content.anchoredPosition.x : content.anchoredPosition.y;
            int first = Mathf.Clamp(Mathf.FloorToInt(scrolled / Mathf.Max(1f, _pitch)) - 1, 0, Mathf.Max(0, _count - _items.Count));
            for (int k = 0; k < _items.Count; k++)
            {
                var item = _items[k];
                // 칸 k 는 first 부터 차례로 — 같은 줄을 다른 칸이 받지 않게 「index % 칸 수 == k」 로 고정한다
                int index = first + ((k - first % _items.Count) + _items.Count) % _items.Count;
                bool on = index < _count;
                if (item.gameObject.activeSelf != on) item.gameObject.SetActive(on);
                if (!on) { _shownIndex[k] = -1; continue; }
                if (!force && _shownIndex[k] == index) continue;
                _shownIndex[k] = index;
                item.anchoredPosition = _horizontal ? _home + new Vector2(_pitch * index, 0f) : _home + new Vector2(0f, -_pitch * index);
                _source.FillItem(index, item, k);
            }
        }
    }
}
