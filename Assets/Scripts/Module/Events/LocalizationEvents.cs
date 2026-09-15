using Game.Module.Common;
using GameFramework.Core.Common;

namespace Game.Module.Events
{
    /// <summary>언어가 바뀌었다(또는 문자열 표가 처음 올라왔다). 글자를 다시 써야 한다.</summary>
    public struct LanguageChangedEvent : IEvent
    {
        public Language NewLanguage;
    }
}
