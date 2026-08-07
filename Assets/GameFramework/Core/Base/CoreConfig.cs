using System;
using System.Collections.Generic;

namespace GameFramework.Core.Base
{
    /// <summary>
    /// 모듈 설정(IXxxSettings)을 등록·조회하는 정적 레지스트리.
    /// CoreModule이 IXxxManager를 관리하듯, CoreConfig는 IXxxSettings를 관리한다.
    /// CoreBootstrap.RegisterConfigs()에서 Set을 호출하고, 모듈 Register()에서 TryGet으로 읽는다.
    /// </summary>
    public static class CoreConfig
    {
        private static readonly Dictionary<Type, object> _configs = new();

        /// <summary>설정을 등록한다. 이미 등록된 타입은 덮어쓴다 (CoreModule.Register와 달리 교체 허용).</summary>
        public static void Set<T>(T config) where T : class
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _configs[typeof(T)] = config;
        }

        /// <summary>등록된 설정을 반환한다. 미등록 시 InvalidOperationException 발생.</summary>
        public static T Get<T>() where T : class
        {
            if (_configs.TryGetValue(typeof(T), out var c))
                return (T)c;
            throw new InvalidOperationException($"[CoreConfig] Config not registered: {typeof(T).FullName}");
        }

        /// <summary>등록된 설정을 반환한다. 미등록 시 false 반환.</summary>
        public static bool TryGet<T>(out T result) where T : class
        {
            if (_configs.TryGetValue(typeof(T), out var c))
            {
                result = (T)c;
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>특정 타입의 설정을 제거한다.</summary>
        public static void Remove<T>() => _configs.Remove(typeof(T));

        /// <summary>모든 설정을 초기화한다. 테스트 종료 후 복원 등에 사용.</summary>
        public static void Reset() => _configs.Clear();
    }
}
