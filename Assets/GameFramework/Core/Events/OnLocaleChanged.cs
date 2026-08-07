using GameFramework.Core.Common;

namespace GameFramework.Core.Events
{
    /// <summary>
    /// 현지화 로케일이 변경되어 새 데이터 로드가 완료되었음을 알리는 이벤트.
    /// LocalizationManager.SetLocaleAsync 완료 시 IEventBus로 발행된다.
    /// </summary>
    public struct OnLocaleChanged : IEvent
    {
        public string NewLocale;
    }
}
