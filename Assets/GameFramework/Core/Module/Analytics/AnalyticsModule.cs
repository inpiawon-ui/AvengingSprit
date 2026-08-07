using GameFramework.Core.Module.Analytics.Backends;
using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Core.Module.Analytics
{
    public sealed class AnalyticsModule : IModule
    {
        private readonly IAnalyticsBackend _backend;
        private AnalyticsManager           _manager;

        /// <summary>커스텀 백엔드를 주입하거나 기본 NoOp 백엔드를 사용합니다.</summary>
        public AnalyticsModule(IAnalyticsBackend backend = null)
        {
            _backend = backend ?? new NoOpAnalyticsBackend();
        }

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _manager = new AnalyticsManager(_backend);
            CoreModule.Register<IAnalyticsManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<IAnalyticsManager>();
            _manager = null;
            IsInitialized = false;
        }
    }
}
