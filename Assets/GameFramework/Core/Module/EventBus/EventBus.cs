using System;
using System.Collections.Generic;
using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Core.Module.EventBus
{
    public sealed class EventBus : IEventBusDeferred
    {
        // ──────────────────────────────────────────────
        // 내부 타입
        // ──────────────────────────────────────────────

        /// <summary>타입별 채널 추상화 — FlushAll에서 타입 소거(type-erasure)를 해결</summary>
        private interface IEventChannel
        {
            void Publish(IEvent evt);
        }

        private sealed class EventChannel<T> : IEventChannel where T : IEvent
        {
            // 키: -(int)priority → 높은 Priority가 SortedList 앞에 오도록 음수 정렬
            private readonly SortedList<int, List<Action<T>>> _handlers = new();

            // 발화 중 Subscribe/Unsubscribe 대비 스냅샷 버퍼 — 재사용으로 ToArray() GC 제거
            private readonly List<Action<T>> _snapshot = new();

            public void Subscribe(Action<T> handler, EventPriority priority)
            {
                int key = -(int)priority;
                if (!_handlers.TryGetValue(key, out var list))
                {
                    list = new List<Action<T>>();
                    _handlers[key] = list;
                }
                list.Add(handler);
            }

            public void Unsubscribe(Action<T> handler)
            {
                // SortedList.Values는 내부적으로 캐시됨 — 반복마다 새 객체 생성 없음
                var lists = _handlers.Values;
                for (int i = 0; i < lists.Count; i++)
                    lists[i].Remove(handler);
            }

            public void Publish(T evt)
            {
                // foreach(_handlers)는 SortedList 열거자를 IEnumerator<>로 박싱 → 매 호출 GC 할당
                // Values[i] 인덱스 접근으로 대체해 박싱 제거
                var lists = _handlers.Values;
                for (int i = 0; i < lists.Count; i++)
                {
                    // 재사용 버퍼에 복사 후 호출 — capacity 안정 후 힙 할당 없음
                    _snapshot.Clear();
                    _snapshot.AddRange(lists[i]);
                    for (int j = 0; j < _snapshot.Count; j++)
                        _snapshot[j](evt);
                }
                _snapshot.Clear();
            }

            // IEventChannel (type-erased)
            void IEventChannel.Publish(IEvent evt) => Publish((T)evt);
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private Action _onDispose;
            public SubscriptionToken(Action onDispose) => _onDispose = onDispose;
            public void Dispose()
            {
                _onDispose?.Invoke();
                _onDispose = null;
            }
        }

        // ──────────────────────────────────────────────
        // 상태
        // ──────────────────────────────────────────────

        private readonly Dictionary<Type, IEventChannel>           _channels = new();
        private readonly Queue<(IEventChannel ch, IEvent evt)>     _queue    = new();

        // ──────────────────────────────────────────────
        // 헬퍼
        // ──────────────────────────────────────────────

        private EventChannel<T> GetOrCreate<T>() where T : IEvent
        {
            var type = typeof(T);
            if (!_channels.TryGetValue(type, out var ch))
            {
                ch = new EventChannel<T>();
                _channels[type] = ch;
            }
            return (EventChannel<T>)ch;
        }

        // ──────────────────────────────────────────────
        // IEventBus
        // ──────────────────────────────────────────────

        public void Publish<T>(T evt) where T : IEvent
        {
            if (_channels.TryGetValue(typeof(T), out var ch))
                ((EventChannel<T>)ch).Publish(evt);
        }

        public IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal) where T : IEvent
        {
            GetOrCreate<T>().Subscribe(handler, priority);
            return new SubscriptionToken(() => Unsubscribe<T>(handler));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (_channels.TryGetValue(typeof(T), out var ch))
                ((EventChannel<T>)ch).Unsubscribe(handler);
        }

        // ──────────────────────────────────────────────
        // IEventBusDeferred
        // ──────────────────────────────────────────────

        public void Enqueue<T>(T evt) where T : IEvent =>
            _queue.Enqueue((GetOrCreate<T>(), evt));

        public void FlushAll()
        {
            while (_queue.Count > 0)
            {
                var (ch, evt) = _queue.Dequeue();
                ch.Publish(evt);
            }
        }

        public void ClearQueue() => _queue.Clear();
        public int  PendingCount => _queue.Count;
    }
}
