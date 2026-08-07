using GameFramework.Core.Common;

namespace GameFramework.Core.Events
{
    /// <summary>
    /// 앱 포커스가 변경될 때 발행된다.
    /// </summary>
    public struct AppFocusChangedEvent : IEvent
    {
        public bool HasFocus;
    }
}
