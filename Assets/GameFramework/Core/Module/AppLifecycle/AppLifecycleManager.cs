using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;

namespace GameFramework.Core.Module.AppLifecycle
{
    /// <summary>
    /// 앱 라이프사이클 상태를 관리하고 IEventBus로 이벤트를 발행한다.
    /// </summary>
    public sealed class AppLifecycleManager : IAppLifecycleManager
    {
        private readonly IEventBus _bus;

        public bool IsPaused { get; private set; }
        public bool HasFocus { get; private set; } = true;

        public AppLifecycleManager(IEventBus bus)
        {
            _bus = bus;
        }

        /// <summary>
        /// AppLifecycleListener가 OnApplicationPause 콜백을 전달한다.
        /// </summary>
        public void OnPause(bool isPaused)
        {
            IsPaused = isPaused;
            _bus.Publish(new AppPausedEvent { IsPaused = isPaused });
        }

        /// <summary>
        /// AppLifecycleListener가 OnApplicationFocus 콜백을 전달한다.
        /// </summary>
        public void OnFocus(bool hasFocus)
        {
            HasFocus = hasFocus;
            _bus.Publish(new AppFocusChangedEvent { HasFocus = hasFocus });
        }
    }
}
