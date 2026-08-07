using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Loading.Views;

namespace GameFramework.Core.Module.Loading
{
    public sealed class LoadingModule : IModule
    {
        private readonly ILoadingView _view;
        private LoadingManager        _manager;
        private IDisposable           _sceneUnloadSub;
        private IDisposable           _sceneProgressSub;
        private IDisposable           _sceneLoadedSub;

        /// <summary>커스텀 뷰를 주입 가능. null이면 FullLoadingView 자동 생성.</summary>
        public LoadingModule(ILoadingView view = null)
        {
            _view = view ?? FullLoadingView.CreateDefault();
        }

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _manager = new LoadingManager(_view);
            CoreModule.Register<ILoadingManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize()
        {
            // 씬 전환 흐름에 로딩 UI 연동
            var bus = CoreModule.Get<IEventBus>();
            _sceneUnloadSub   = bus.Subscribe<OnSceneUnloading>   (_ => _manager.ShowAsync().Forget());
            _sceneProgressSub = bus.Subscribe<OnSceneLoadProgress>(e  => _manager.SetProgress(e.Progress));
            _sceneLoadedSub   = bus.Subscribe<OnSceneLoaded>      (_ => _manager.HideAsync().Forget());
        }

        public void Dispose()
        {
            _sceneUnloadSub?.Dispose();
            _sceneProgressSub?.Dispose();
            _sceneLoadedSub?.Dispose();
            CoreModule.Unregister<ILoadingManager>();
            _manager          = null;
            _sceneUnloadSub   = null;
            _sceneProgressSub = null;
            _sceneLoadedSub   = null;
            IsInitialized     = false;
        }
    }
}
