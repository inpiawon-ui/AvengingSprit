using UnityEngine;

namespace GameFramework.Core.Module.ObjectPool
{
    public interface IObjectPoolManager
    {
        void Register<T>(GameObject prefab, int initialSize = 8, int maxSize = 64) where T : Component;
        T    Get<T>()          where T : Component;
        void Return<T>(T inst) where T : Component;
        void Clear<T>()        where T : Component;
        void ClearAll();
    }
}
