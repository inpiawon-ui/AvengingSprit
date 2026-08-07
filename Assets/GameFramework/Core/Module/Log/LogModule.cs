using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.Log.Handlers;

namespace GameFramework.Core.Module.Log
{
    [Module(Layer = ModuleLayer.Core, Provides = new[] { typeof(ILog) })]
    public sealed class LogModule : IModule
    {
        private LogManager _logManager;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _logManager = new LogManager();
            _logManager.AddHandler(new UnityConsoleHandler());
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _logManager.AddHandler(new OverlayHandler());
#endif
            CoreModule.Register<ILog>(_logManager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<ILog>();
            _logManager = null;
            IsInitialized = false;
        }
    }
}
