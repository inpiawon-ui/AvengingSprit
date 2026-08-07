using System;
using GameFramework.Core.Common;
using System.Collections.Generic;

namespace GameFramework.Core.Base
{
    internal sealed class ModuleRegistry : IModuleRegistry
    {
        private readonly Dictionary<Type, object> _registry = new();

        public void Register<T>(T instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (_registry.ContainsKey(typeof(T)))
                throw new InvalidOperationException(
                    $"[ModuleRegistry] {typeof(T).FullName}이 이미 등록되어 있습니다. " +
                    "AutoRegisterModules가 중복 호출되었거나 같은 Provides를 가진 모듈이 두 번 스캔되었을 가능성이 있습니다.");
            _registry[typeof(T)] = instance;
        }

        public T Get<T>()
        {
            if (_registry.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException(
                $"[CoreModule] Service not registered: {typeof(T).FullName}");
        }

        public bool TryGet<T>(out T result)
        {
            if (_registry.TryGetValue(typeof(T), out var service))
            {
                result = (T)service;
                return true;
            }
            result = default;
            return false;
        }

        public void Unregister<T>() => _registry.Remove(typeof(T));
        public IEnumerable<Type> GetRegisteredTypes() => _registry.Keys;
    }
}
