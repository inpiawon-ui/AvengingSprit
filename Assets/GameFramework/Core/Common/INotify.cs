using System;

namespace GameFramework.Core.Common
{
    // 값을 보유하고, 변경 시 구독자에게 알릴 수 있는 컨테이너 인터페이스
    public interface INotify<T>
    {
        T Value { get; }

        // notifyImmediately: true → 구독 시점에 현재 값으로 즉시 onChanged 1회 호출
        IDisposable Subscribe(Action<T> onChanged, bool notifyImmediately = true);
    }
}
