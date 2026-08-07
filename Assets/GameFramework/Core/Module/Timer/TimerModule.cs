using System;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;

namespace GameFramework.Core.Module.Timer
{
    [Module(Layer = ModuleLayer.Core, Provides = new[] { typeof(ITimerManager) })]
    public sealed class TimerModule : IModule, ITickable, IPausable
    {
        private TimerManager _manager;
        private IDisposable  _sceneUnloadSub;

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _manager = new TimerManager();
            CoreModule.Register<ITimerManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize()
        {
            // 씬 전환 시 모든 타이머 취소
            _sceneUnloadSub = CoreModule.Get<IEventBus>()
                .Subscribe<OnSceneUnloading>(_ => _manager.CancelAll());
        }

        public void Tick(float deltaTime) => _manager?.Tick(deltaTime);

        public void Pause()  => _manager?.PauseAll();
        public void Resume() => _manager?.ResumeAll();

        public void Dispose()
        {
            _sceneUnloadSub?.Dispose();
            _manager?.CancelAll();
            CoreModule.Unregister<ITimerManager>();
            _manager        = null;
            _sceneUnloadSub = null;
            IsInitialized   = false;
        }
    }
}
