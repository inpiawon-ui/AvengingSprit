using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Core.Module.EventBus
{
    public interface IEventBusDeferred : IEventBus
    {
        void Enqueue<T>(T evt) where T : IEvent;
        void FlushAll();
        void ClearQueue();
        int  PendingCount { get; }
    }
}
