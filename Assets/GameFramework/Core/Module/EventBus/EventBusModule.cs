using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Core.Module.EventBus
{
    [Module(Layer = ModuleLayer.Core, Provides = new[] { typeof(IEventBus), typeof(IEventBusDeferred) })]
    public sealed class EventBusModule : IModule, ITickable
    {
        private EventBus _bus;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _bus = new EventBus();
            CoreModule.Register<IEventBus>(_bus);
            CoreModule.Register<IEventBusDeferred>(_bus);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Tick(float deltaTime) => _bus?.FlushAll();

        public void Dispose()
        {
            _bus?.ClearQueue();
            CoreModule.Unregister<IEventBus>();
            CoreModule.Unregister<IEventBusDeferred>();
            _bus = null;
            IsInitialized = false;
        }
    }
}
