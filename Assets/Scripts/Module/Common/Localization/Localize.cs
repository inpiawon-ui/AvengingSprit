using GameFramework.Core.Base;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 글자를 꺼내는 창구. `CoreModule.Get&lt;ILanguageService&gt;()` 를 매번 적지 않게 한다.
    ///
    /// 모듈이 아직 없으면(에디터 도구·단독 테스트) **키를 그대로** 돌려준다 — 예외로 멈추지 않는다.
    /// </summary>
    public static class Localize
    {
        public static string Get(string key)
            => CoreModule.TryGet<ILanguageService>(out var s) ? s.Get(key) : key;

        public static string Format(string key, params object[] args)
            => CoreModule.TryGet<ILanguageService>(out var s) ? s.Format(key, args) : key;

        /// <summary>표에서 온 글자. 번역이 없으면 원문 <paramref name="korean"/> 그대로.</summary>
        public static string FromTable(string key, string korean)
            => CoreModule.TryGet<ILanguageService>(out var s) ? s.FromTable(key, korean) : korean;

        public static Language Current
            => CoreModule.TryGet<ILanguageService>(out var s) ? s.Current : Language.Korean;

        /// <summary>화면이 생길 때 한 번 — 본문 폰트를 지금 언어 것으로.</summary>
        public static void ApplyFonts(Transform root)
        {
            if (CoreModule.TryGet<ILanguageService>(out var s)) s.ApplyFonts(root);
        }
    }
}
