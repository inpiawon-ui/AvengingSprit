using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;

namespace GameFramework.Core.Module.Scene
{
    [Module(
        Layer     = ModuleLayer.Core,
        Provides  = new[] { typeof(ISceneManager) },
        DependsOn = new[] { typeof(IEventBus) })]
    public sealed class SceneModule : IModule
    {
        private SceneManager _manager;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            var bus  = CoreModule.Get<IEventBus>();
            _manager = new SceneManager(bus);
            CoreModule.Register<ISceneManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<ISceneManager>();
            _manager      = null;
            IsInitialized = false;
        }
    }
}
