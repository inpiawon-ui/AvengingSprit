using System;
using System.Collections.Generic;
using GameFramework.Core.Common;

namespace GameFramework.Core.Base
{
    public static class CoreModule
    {
        private static IModuleRegistry _registry = new ModuleRegistry();

        public static void Register<T>(T instance) => _registry.Register(instance);
        /// <summary>
        /// 등록된 매니저 인터페이스(IXxxManager 등)를 조회한다. 미등록 시 예외 발생.
        /// IModule 제약이 없는 이유: 등록 대상은 IModule 구현체가 아니라 매니저 인터페이스이기 때문이다.
        /// </summary>
        public static T Get<T>() => _registry.Get<T>();
        /// <summary>미등록 시 false 반환. 선택적 의존성이거나 존재 여부를 먼저 확인해야 할 때 사용.</summary>
        public static bool TryGet<T>(out T result) => _registry.TryGet(out result);
        public static void Unregister<T>() => _registry.Unregister<T>();
        public static IEnumerable<Type> GetRegisteredTypes() => _registry.GetRegisteredTypes();

        /// <summary>테스트 등에서 레지스트리 전체 교체 시 사용</summary>
        public static void SetRegistry(IModuleRegistry registry) => _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        /// <summary>레지스트리를 기본 구현체로 초기화 (테스트 종료 후 복원 등)</summary>
        public static void Reset() => _registry = new ModuleRegistry();
    }
}