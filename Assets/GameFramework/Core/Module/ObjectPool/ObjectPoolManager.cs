using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Core.Module.ObjectPool
{
    public sealed class ObjectPoolManager : IObjectPoolManager
    {
        private readonly Dictionary<Type, object> _pools = new();

        public void Register<T>(GameObject prefab, int initialSize = 8, int maxSize = 64) where T : Component
        {
            var type = typeof(T);
            if (!_pools.ContainsKey(type))
                _pools[type] = new ObjectPool<T>(prefab, initialSize, maxSize);
        }

        public T Get<T>() where T : Component
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
                return ((ObjectPool<T>)pool).Get();

            Debug.LogWarning($"[ObjectPool] Pool not registered for type: {typeof(T).Name}");
            return null;
        }

        public void Return<T>(T inst) where T : Component
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
                ((ObjectPool<T>)pool).Return(inst);
        }

        public void Clear<T>() where T : Component
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
            {
                ((ObjectPool<T>)pool).Clear();
                _pools.Remove(typeof(T));
            }
        }

        public void ClearAll()
        {
            // 각 풀의 Clear()를 IObjectPool 인터페이스로 호출 — 리플렉션 불필요
            foreach (var pool in _pools.Values)
                ((IObjectPool)pool).Clear();
            _pools.Clear();
        }
    }
}
