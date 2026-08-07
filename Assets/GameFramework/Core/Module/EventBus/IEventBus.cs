using GameFramework.Core.Common;
using System;

namespace GameFramework.Core.Module.EventBus
{
    public interface IEventBus
    {
        void        Publish<T>(T evt) where T : IEvent;
        IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal) where T : IEvent;
        void        Unsubscribe<T>(Action<T> handler) where T : IEvent;
    }
}
