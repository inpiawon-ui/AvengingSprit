using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Core.Module.Addressable
{
    /// <summary>
    /// AddressableDownloadManager 를 생성하고 CoreModule 에 등록하는 IModule 구현체.
    ///
    /// GameBootstrapper 에서 등록 순서:
    ///   AddressableDownloadModule 은 ResourceModule 보다 먼저 등록해야 합니다.
    ///   (BootController 가 IAddressableDownloadManager 로 카탈로그/다운로드를 처리한 뒤
    ///    IResourceManager 로 에셋을 로드하는 흐름)
    /// </summary>
    [Module(Layer = ModuleLayer.Core, Provides = new[] { typeof(IAddressableDownloadManager) })]
    public sealed class AddressableDownloadModule : IModule
    {
        private AddressableDownloadManager _manager;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _manager = new AddressableDownloadManager();
            CoreModule.Register<IAddressableDownloadManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<IAddressableDownloadManager>();
            _manager      = null;
            IsInitialized = false;
        }
    }
}
