using GameFramework.Core.Common;
using static GameFramework.Game.Common.Enums;

namespace GameFramework.Game.Events
{
    /// <summary>
    /// ActionModule에서 ActionType 값이 변경될 때 발행되는 이벤트.
    /// SetAction 또는 AddAction 호출 완료 후 IEventBus를 통해 발행된다.
    /// </summary>
    public struct ActionChangedEvent : IEvent
    {
        public ActionType ActionType;
        public long NewValue;
    }
}
