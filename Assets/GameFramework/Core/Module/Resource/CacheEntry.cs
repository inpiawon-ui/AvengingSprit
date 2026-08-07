using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameFramework.Core.Module.Resource
{
    internal struct CacheEntry
    {
        public AsyncOperationHandle Handle;
        public int                  RefCount;
        public string               Scope;
    }
}
