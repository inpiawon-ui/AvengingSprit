using TMPro;
using UnityEngine;

namespace Game.Module.Common
{
    public interface ILanguageService
    {
        /// <summary>지금 언어.</summary>
        Language Current { get; }

        /// <summary>문자열 표가 올라왔는가. 올라오기 전에는 원문(한국어)·키로 답한다.</summary>
        bool IsReady { get; }

        /// <summary>지금 언어의 본문 폰트. 표가 없으면 null.</summary>
        TMP_FontAsset GothicFont { get; }

        /// <summary>
        /// 키의 문구. 번역이 비어 있으면 한국어 원문, 키가 없으면 키 자체를 돌려준다.
        /// </summary>
        string Get(string key);

        /// <summary>`string.Format` 을 거친 문구. 자리표시는 `{0}` 식이다.</summary>
        string Format(string key, params object[] args);

        /// <summary>
        /// 표(HostTable 등)에서 온 글자. 문자열 표에 키가 없으면 <paramref name="korean"/> 을 그대로 쓴다 —
        /// 원문은 표가 쥐고 있고(임포터가 덮어쓴다), 번역만 문자열 표에 둔다.
        /// </summary>
        string FromTable(string key, string korean);

        /// <summary>언어를 바꾸고 저장한다. 폰트를 갈아 끼우고 `LanguageChangedEvent` 를 발행한다.</summary>
        void SetLanguage(Language language);

        /// <summary>
        /// <paramref name="root"/> 아래 글자들의 본문 폰트를 지금 언어 것으로 맞춘다.
        /// 화면(`~UI`·`~Panel`)이 생길 때 한 번 부른다 — 이미 떠 있는 화면은 언어가 바뀔 때 알아서 바뀐다.
        /// </summary>
        void ApplyFonts(Transform root);
    }
}
