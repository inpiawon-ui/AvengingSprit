using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.Resource;

namespace GameFramework.Core.Module.UI
{
    [Module(
        Layer     = ModuleLayer.Core,
        Provides  = new[] { typeof(IUIManager) },
        DependsOn = new[] { typeof(IResourceManager) })]
    public sealed class UIModule : IModule
    {
        private UIManager _manager;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            var resource = CoreModule.Get<IResourceManager>();
            _manager = new UIManager(resource);
            CoreModule.Register<IUIManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            // 모든 패널 인스턴스를 동기적으로 즉시 파괴하고 Addressable 핸들을 일괄 해제한다.
            // (CloseAllAsync는 fire-and-forget으로는 핸들 해제 시점이 비결정적이므로 사용하지 않는다)
            _manager?.DisposeAll();
            CoreModule.Unregister<IUIManager>();
            _manager      = null;
            IsInitialized = false;
        }
    }
}
