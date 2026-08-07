using System;
using System.Collections.Generic;

namespace GameFramework.Core.Common
{
    // 참조형 리액티브 변수. 값 변경 시 구독자에게 자동으로 알림을 발송한다.
    public sealed class RefVar<T> : INotify<T>, IDisposable
    {
        // ─────────────────────────────────────────────────────
        // 내부 구독 토큰 — EventBus의 SubscriptionToken과 동일한 패턴
        // ─────────────────────────────────────────────────────
        private sealed class Subscription : IDisposable
        {
            private Action _onDispose;
            public Subscription(Action onDispose) => _onDispose = onDispose;
            public void Dispose()
            {
                _onDispose?.Invoke();
                _onDispose = null;
            }
        }

        // ─────────────────────────────────────────────────────
        // 상태
        // ─────────────────────────────────────────────────────
        private T _value;
        private readonly List<Action<T>> _subscribers = new();
        // Notify 중 Subscribe/Unsubscribe 대비 스냅샷 버퍼 — 재사용으로 ToArray() GC 제거
        private readonly List<Action<T>> _notifyBuffer = new();
        private bool _isDisposed;
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        public RefVar(T initialValue = default) => _value = initialValue;

        // ─────────────────────────────────────────────────────
        // 값 접근
        // ─────────────────────────────────────────────────────
        public T Value
        {
            get => _value;
            set
            {
                if (Comparer.Equals(_value, value)) return;
                _value = value;
                Notify();
            }
        }

        // ─────────────────────────────────────────────────────
        // 구독
        // ─────────────────────────────────────────────────────
        public IDisposable Subscribe(Action<T> onChanged, bool notifyImmediately = true)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(RefVar<T>));
            if (onChanged == null) throw new ArgumentNullException(nameof(onChanged));

            _subscribers.Add(onChanged);
            if (notifyImmediately) onChanged(_value);

            return new Subscription(() => _subscribers.Remove(onChanged));
        }

        // ─────────────────────────────────────────────────────
        // 바인딩 / 강제 알림
        // ─────────────────────────────────────────────────────
        // 단방향 바인딩 — 반환 토큰 Dispose로 해제
        public IDisposable BindTo(RefVar<T> target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Subscribe(v => target.Value = v, notifyImmediately: true);
        }

        // 참조형 내부 상태 변경 후 수동 알림 발송
        public void ForceNotify() => Notify();

        // ─────────────────────────────────────────────────────
        // 연산자
        // ─────────────────────────────────────────────────────
        public static implicit operator T(RefVar<T> r) => r._value;

        public static bool operator ==(RefVar<T> r, T value)
            => r is not null && Comparer.Equals(r._value, value);
        public static bool operator !=(RefVar<T> r, T value) => !(r == value);

        public override bool Equals(object obj) => obj switch
        {
            T t             => Comparer.Equals(_value, t),
            RefVar<T> other => Comparer.Equals(_value, other._value),
            _               => false
        };
        public override int    GetHashCode() => _value?.GetHashCode() ?? 0;
        public override string ToString()    => _value?.ToString() ?? string.Empty;

        // ─────────────────────────────────────────────────────
        // IDisposable
        // ─────────────────────────────────────────────────────
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _subscribers.Clear();
        }

        // ─────────────────────────────────────────────────────
        // 내부
        // ─────────────────────────────────────────────────────
        // Notify 중 Subscribe/Unsubscribe 발생 시 안전하게 처리 (EventBus 패턴 동일)
        private void Notify()
        {
            _notifyBuffer.Clear();
            _notifyBuffer.AddRange(_subscribers);
            for (int i = 0; i < _notifyBuffer.Count; i++)
                _notifyBuffer[i]?.Invoke(_value);
            _notifyBuffer.Clear();
        }
    }
}
