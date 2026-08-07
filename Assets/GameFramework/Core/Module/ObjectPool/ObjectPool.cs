using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Core.Module.ObjectPool
{
    /// <summary>단일 컴포넌트 타입에 대한 풀</summary>
    internal sealed class ObjectPool<T> : IObjectPool where T : Component
    {
        private readonly Stack<T>   _available = new();
        private readonly HashSet<T> _inUse     = new();
        private readonly GameObject _prefab;
        private readonly Transform  _root;
        private readonly int        _maxSize;

        public int AvailableCount => _available.Count;
        public int InUseCount     => _inUse.Count;

        public ObjectPool(GameObject prefab, int initialSize, int maxSize)
        {
            _prefab  = prefab;
            _maxSize = maxSize;

            // 풀 루트 오브젝트 (씬 정리 시 자동 해제)
            _root = new GameObject($"[Pool] {typeof(T).Name}").transform;
            Object.DontDestroyOnLoad(_root.gameObject);

            // initialSize가 maxSize를 초과하면 maxSize에 맞춰 생성
            int count = Mathf.Min(initialSize, maxSize);
            for (int i = 0; i < count; i++)
                _available.Push(CreateInstance());
        }

        public T Get()
        {
            if (_inUse.Count >= _maxSize)
                return null;

            T inst = _available.Count > 0 ? _available.Pop() : CreateInstance();
            _inUse.Add(inst);
            inst.gameObject.SetActive(true);

            if (inst is IPoolable p) p.OnGetFromPool();
            return inst;
        }

        public void Return(T inst)
        {
            if (!_inUse.Remove(inst)) return;

            if (inst is IPoolable p) p.OnReturnToPool();
            inst.gameObject.SetActive(false);
            inst.transform.SetParent(_root);
            _available.Push(inst);
        }

        public void Clear()
        {
            foreach (var inst in _inUse)
                if (inst != null) Object.Destroy(inst.gameObject);
            _inUse.Clear();

            while (_available.Count > 0)
            {
                var inst = _available.Pop();
                if (inst != null) Object.Destroy(inst.gameObject);
            }

            if (_root != null)
                Object.Destroy(_root.gameObject);
        }

        private T CreateInstance()
        {
            var go = Object.Instantiate(_prefab, _root);
            go.SetActive(false);
            var comp = go.GetComponent<T>() ?? go.AddComponent<T>();
            return comp;
        }
    }
}
