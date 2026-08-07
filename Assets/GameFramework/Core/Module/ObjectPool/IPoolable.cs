namespace GameFramework.Core.Module.ObjectPool
{
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
    }
}
