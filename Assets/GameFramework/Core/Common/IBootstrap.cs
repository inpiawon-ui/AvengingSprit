namespace GameFramework.Core.Common
{
    public interface IBootstrap
    {
        void RegisterConfigs();
        void RegisterModules();
        void InitializeAll();
        void DisposeAll();
    }
}
