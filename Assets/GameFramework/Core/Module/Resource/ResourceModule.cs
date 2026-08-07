using System;
using GameFramework.Core.Module.Addressable;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;

namespace GameFramework.Core.Module.Resource
{
    // IAddressableDownloadManager는 Register()에서 직접 사용하지 않지만,
    // ResourceManager가 Addressable 에셋을 로드하기 전에
    // AddressableDownloadModule이 카탈로그를 초기화해야 하므로 순서를 강제한다.
    // (향후 ResourceManager는 IAddressableDownloadManager를 통해 에셋을 로드하도록 개선 예정)
    [Module(
        Layer     = ModuleLayer.Core,
        Provides  = new[] { typeof(IResourceManager) },
        DependsOn = new[] { typeof(IAddressableDownloadManager) })]
    public sealed class ResourceModule : IModule
    {
        private ResourceManager _manager;
        private IDisposable     _sceneUnloadSub;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _manager = new ResourceManager();
            CoreModule.Register<IResourceManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize()
        {
            // 씬 전환 시 해당 씬 스코프 에셋 자동 해제 (SceneModule 직접 참조 없음)
            _sceneUnloadSub = CoreModule.Get<IEventBus>()
                .Subscribe<OnSceneUnloading>(e => _manager.ReleaseAll(e.SceneName));
        }

        public void Dispose()
        {
            _sceneUnloadSub?.Dispose();
            _manager.ReleaseAll();
            CoreModule.Unregister<IResourceManager>();
            _manager        = null;
            _sceneUnloadSub = null;
            IsInitialized   = false;
        }
    }
}
