using System;
using System.Collections.Generic;

namespace GameFramework.Core.Common
{
    public interface IModuleRegistry
    {
        void Register<T>(T instance);
        T    Get<T>();
        bool TryGet<T>(out T result);
        void Unregister<T>();
        IEnumerable<Type> GetRegisteredTypes();
    }
}
