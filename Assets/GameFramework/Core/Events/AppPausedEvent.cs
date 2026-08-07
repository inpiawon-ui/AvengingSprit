using GameFramework.Core.Common;

namespace GameFramework.Core.Events
{
    /// <summary>
    /// 앱이 일시정지/재개될 때 발행된다.
    /// </summary>
    public struct AppPausedEvent : IEvent
    {
        public bool IsPaused;
    }
}
