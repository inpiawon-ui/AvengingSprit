using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 한 언어의 본문 · 굵은 폰트와 재질 프리셋 꾸러미. 언어마다 한 장 · 주소 `Fonts/FontSet_{코드}`.
    ///
    /// ⚠ 예전에는 문자열 표(`StringTable`)가 다섯 언어의 폰트를 **직접** 물고 있었다(2026-10-30 점검).
    ///   표를 읽는 순간 폰트 여덟 개(원본 ttf 포함 약 65MB)가 통째로 올라왔고, 같은 폰트가 화면 묶음에
    ///   한 벌 더 들어 있었다. 지금은 표가 폰트를 물지 않고, 지금 언어의 꾸러미 하나만 주소로 읽는다.
    /// </summary>
    [CreateAssetMenu(fileName = "FontSet", menuName = "Game/LanguageFontSet")]
    public sealed class LanguageFontSet : ScriptableObject
    {
        public const string Folder = "Assets/BundleResource/Fonts";

        [SerializeField] private LanguageFont _font = new();

        public LanguageFont Font => _font;

        public static string AddressOf(Language language) => $"Fonts/FontSet_{LanguageCodes.Code(language)}";

#if UNITY_EDITOR
        /// <summary>에디터 도구용 — 주소를 거치지 않고 파일에서 바로 읽는다.</summary>
        public static LanguageFont LoadInEditor(Language language)
        {
            var set = UnityEditor.AssetDatabase.LoadAssetAtPath<LanguageFontSet>(
                $"{Folder}/FontSet_{LanguageCodes.Code(language)}.asset");
            return set != null ? set.Font : null;
        }
#endif
    }
}
