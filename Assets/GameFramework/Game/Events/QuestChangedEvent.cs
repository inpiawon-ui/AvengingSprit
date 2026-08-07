using GameFramework.Core.Common;
using static GameFramework.Game.Common.Enums;

namespace GameFramework.Game.Events
{
    /// <summary>
    /// QuestModule에서 QuestType 값이 변경될 때 발행되는 이벤트.
    /// SetQuest 또는 AddQuest 호출 완료 후 IEventBus를 통해 발행된다.
    /// </summary>
    public struct QuestChangedEvent : IEvent
    {
        public QuestType QuestType;
        public long NewValue;
    }
}
