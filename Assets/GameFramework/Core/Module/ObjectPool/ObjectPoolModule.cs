using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Core.Module.ObjectPool
{
    [Module(Layer = ModuleLayer.Core, Provides = new[] { typeof(IObjectPoolManager) })]
    public sealed class ObjectPoolModule : IModule
    {
        private ObjectPoolManager _manager;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _manager = new ObjectPoolManager();
            CoreModule.Register<IObjectPoolManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            _manager?.ClearAll();
            CoreModule.Unregister<IObjectPoolManager>();
            _manager = null;
            IsInitialized = false;
        }
    }
}
