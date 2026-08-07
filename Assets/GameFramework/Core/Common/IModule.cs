namespace GameFramework.Core.Common
{
    public interface IModule
    {
        bool IsInitialized { get; }
        void Register();
        void Initialize();
        void Dispose();
    }
}