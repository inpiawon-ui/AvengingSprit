using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 게임이 지원하는 언어. **순서를 바꾸지 않는다** — 저장값(PlayerPrefs)이 정수다.
    /// </summary>
    public enum Language
    {
        Korean = 0,
        Japanese = 1,
        English = 2,
    }

    public static class LanguageCodes
    {
        /// <summary>표기용 코드. 파일 이름·로그에 쓴다.</summary>
        public static string Code(Language language) => language switch
        {
            Language.Japanese => "ja",
            Language.English  => "en",
            _                 => "ko",
        };

        /// <summary>기기 언어를 게임 언어로 옮긴다. 지원하지 않는 언어는 영어다.</summary>
        public static Language FromSystem(SystemLanguage system) => system switch
        {
            SystemLanguage.Korean   => Language.Korean,
            SystemLanguage.Japanese => Language.Japanese,
            _                       => Language.English,
        };
    }
}
