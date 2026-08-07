using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Localization
{
    public interface ILocalizationManager
    {
        string CurrentLocale { get; }

        /// <summary>
        /// 로케일을 전환합니다. 데이터 로드 완료 후 IEventBus로 OnLocaleChanged 이벤트가 발행됩니다.
        /// 구독: IEventBus.Subscribe&lt;OnLocaleChanged&gt;(e =&gt; ...);
        /// </summary>
        UniTask SetLocaleAsync(string locale);

        /// <summary>키에 해당하는 현지화 문자열을 반환합니다. 없으면 키를 그대로 반환합니다.</summary>
        string Get(string key);

        /// <summary>키에 해당하는 현지화 문자열을 string.Format으로 포맷합니다.</summary>
        string GetFormat(string key, params object[] args);
    }
}
