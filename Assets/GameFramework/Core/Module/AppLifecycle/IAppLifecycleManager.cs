namespace GameFramework.Core.Module.AppLifecycle
{
    /// <summary>
    /// 앱 라이프사이클 상태 조회 인터페이스.
    /// </summary>
    public interface IAppLifecycleManager
    {
        /// <summary>앱이 현재 일시정지 상태인지 여부.</summary>
        bool IsPaused { get; }

        /// <summary>앱이 현재 포커스를 가지고 있는지 여부.</summary>
        bool HasFocus { get; }
    }
}
