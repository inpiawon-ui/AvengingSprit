using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Localization
{
    /// <summary>
    /// 수동 등록 전용 모듈 ([Module] 어트리뷰트 없음 — 생성자 파라미터).
    /// EventBusModule 이후에 등록되어야 한다. Register()에서 IEventBus를 직접 사용한다.
    /// </summary>
    public sealed class LocalizationModule : IModule
    {
        private readonly string     _defaultLocale;
        private LocalizationManager _manager;

        public LocalizationModule(string defaultLocale = "ko")
        {
            _defaultLocale = defaultLocale;
        }

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            var bus  = CoreModule.Get<IEventBus>();
            _manager = new LocalizationManager(bus);
            CoreModule.Register<ILocalizationManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() =>
            _manager.SetLocaleAsync(_defaultLocale).Forget();

        public void Dispose()
        {
            CoreModule.Unregister<ILocalizationManager>();
            _manager = null;
            IsInitialized = false;
        }
    }
}
