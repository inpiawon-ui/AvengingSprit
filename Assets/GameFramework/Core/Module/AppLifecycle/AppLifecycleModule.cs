using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace GameFramework.Core.Module.AppLifecycle
{
    [Module(
        Layer     = ModuleLayer.Core,
        Provides  = new[] { typeof(IAppLifecycleManager) },
        DependsOn = new[] { typeof(IEventBus) }
    )]
    public sealed class AppLifecycleModule : IModule
    {
        private AppLifecycleManager  _manager;
        private GameObject           _listenerGo;

    public bool   IsInitialized { get; private set; }

        public void Register()
        {
            // Register()에서 IEventBus를 직접 사용 — DependsOn = IEventBus 선언됨
            var bus = CoreModule.Get<IEventBus>();
            _manager = new AppLifecycleManager(bus);
            CoreModule.Register<IAppLifecycleManager>(_manager);

            // Unity 메시지 수신 전담 GameObject — 씬 전환 후에도 유지
            _listenerGo = new GameObject("[AppLifecycleListener]");
            UnityEngine.Object.DontDestroyOnLoad(_listenerGo);
            var listener = _listenerGo.AddComponent<AppLifecycleListener>();
            listener.Initialize(_manager.OnPause, _manager.OnFocus);

            IsInitialized = true;
        }

        public void Initialize()
        {
            // AppLifecycleModule은 이벤트를 구독하지 않고 발행만 한다.
            // 구독이 필요한 경우 이 메서드에 IDisposable 토큰을 저장한다.
        }

        public void Dispose()
        {
            if (_listenerGo != null) UnityEngine.Object.Destroy(_listenerGo);
            CoreModule.Unregister<IAppLifecycleManager>();

            _listenerGo   = null;
            _manager      = null;
            IsInitialized = false;
        }
    }
}
