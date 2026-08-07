using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 프리팹 자식을 **이름으로** 찾아 쓰는 헬퍼.
    ///
    /// 화면 설계서의 `요소 이름` = 프리팹 GameObject 이름 = 여기서 쓰는 조회 키다.
    /// 세 곳이 같으므로 Inspector 수동 연결 없이 코드에서 바로 집을 수 있다.
    /// 이름이 중복되는 요소(NotifyBadge 등)는 부모를 지정해 좁힌다.
    /// </summary>
    public sealed class UIBinder
    {
        private readonly Dictionary<string, Transform> _map = new();

        public UIBinder(Transform root)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                // 중복 이름은 먼저 발견한 것을 쓴다. 좁혀야 하면 Find(parent, name) 사용.
                if (!_map.ContainsKey(t.name)) _map[t.name] = t;
            }
        }

        public Transform Find(string name)
            => _map.TryGetValue(name, out var t) ? t : null;

        /// <summary>같은 이름이 여러 개일 때 부모를 지정해 좁힌다.</summary>
        public Transform Find(Transform parent, string name)
        {
            if (parent == null) return Find(name);
            var all = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }

        public T Get<T>(string name) where T : Component
        {
            var t = Find(name);
            if (t == null)
            {
                Debug.LogWarning($"[UIBinder] 요소 없음: {name}");
                return null;
            }
            var c = t.GetComponent<T>();
            if (c == null) Debug.LogWarning($"[UIBinder] {name} 에 {typeof(T).Name} 없음");
            return c;
        }

        public GameObject GetObject(string name) => Find(name)?.gameObject;

        public void SetText(string name, string value)
        {
            var tmp = Get<TextMeshProUGUI>(name);
            if (tmp != null) tmp.text = value;
        }

        public void SetActive(string name, bool active)
        {
            var t = Find(name);
            if (t != null) t.gameObject.SetActive(active);
        }

        /// <summary>버튼 클릭 배선. 기존 리스너를 지우고 새로 건다.</summary>
        public void OnClick(string name, UnityEngine.Events.UnityAction action)
        {
            var b = Get<Button>(name);
            if (b == null) return;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(action);
        }

        /// <summary>게이지 채움. `~Fill` 요소의 가로 크기를 비율로 조절한다.</summary>
        public void SetFill(string fillName, float ratio, float fullWidth)
        {
            var t = Find(fillName) as RectTransform;
            if (t == null) return;
            ratio = Mathf.Clamp01(ratio);
            var sd = t.sizeDelta;
            t.sizeDelta = new Vector2(fullWidth * ratio, sd.y);
        }
    }
}
