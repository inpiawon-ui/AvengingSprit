using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 글자 한 줄. 키 하나에 언어별 문구를 나란히 둔다.
    ///
    /// **한국어가 원문이다.** 번역이 비어 있으면 한국어로 떨어진다 — 빠진 번역이
    /// 화면에서 바로 눈에 띄어야 고친다. 영어로 떨어뜨리면 일본어 판에서 영어가 섞여
    /// 「원래 영어인 칸」과 구별이 안 된다.
    /// </summary>
    [Serializable]
    public sealed class StringEntry
    {
        [SerializeField] private string _key;
        [SerializeField, TextArea(1, 4)] private string _ko;
        [SerializeField, TextArea(1, 4)] private string _ja;
        [SerializeField, TextArea(1, 4)] private string _en;

        public StringEntry(string key, string korean)
        {
            _key = key;
            _ko = korean;
        }

        public string Key => _key;
        public string Korean => _ko;

        /// <summary>그 언어의 문구. 비어 있으면 null — 떨어뜨리는 규칙은 부르는 쪽이 정한다.</summary>
        public string Raw(Language language)
        {
            var value = language switch
            {
                Language.Japanese => _ja,
                Language.English  => _en,
                _                 => _ko,
            };
            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>에디터 도구가 채운다. 런타임에서는 부르지 않는다.</summary>
        public void Write(Language language, string value)
        {
            switch (language)
            {
                case Language.Japanese: _ja = value; break;
                case Language.English:  _en = value; break;
                default:                _ko = value; break;
            }
        }
    }

    /// <summary>
    /// 언어마다 쓰는 본문 폰트.
    ///
    /// ⚠ 일본어를 한글 폰트로 그리면 **한자가 한국식 자형**으로 나오고, 闘·撃·霊·択·険 같은
    ///   게임 핵심 한자가 아예 빠져 있다(실측 2026-09-15). 언어가 바뀌면 폰트도 바뀌어야 한다.
    ///
    /// 재질 프리셋(외곽선·그림자·빛)은 **폰트 아틀라스에 묶여** 있어서 폰트만 바꾸면
    /// 글자가 깨진다. 같은 이름 꼬리(` - Outline` 등)를 가진 프리셋을 찾아 함께 바꾼다.
    /// </summary>
    [Serializable]
    public sealed class LanguageFont
    {
        [SerializeField] private Language _language;
        [SerializeField] private TMP_FontAsset _gothic;
        [SerializeField] private Material[] _presets = Array.Empty<Material>();

        public Language Language => _language;
        public TMP_FontAsset Gothic => _gothic;
        public IReadOnlyList<Material> Presets => _presets;
    }

    /// <summary>
    /// 게임 글자의 **단일 출처** (2026-09-15, 일본 출시).
    /// 에셋: `Assets/BundleResource/TableData/StringTable.asset` · 주소 `TableData/StringTable`
    ///
    /// 키 규칙 — 점으로 가른다. 앞마디가 어디서 쓰는지다.
    ///   `ui.lobby.play`          화면 글자
    ///   `host.{hostKey}.name`    표에서 온 글자 (원문은 표의 `_nameKr` 가 쥔다)
    ///   `event.{id}.title`
    /// </summary>
    [CreateAssetMenu(fileName = "StringTable", menuName = "Game/StringTable")]
    public sealed class StringTable : ScriptableObject
    {
        [Tooltip("저장된 선택이 없을 때 쓰는 언어. 일본 출시판은 일본어.")]
        [SerializeField] private Language _defaultLanguage = Language.Japanese;

        [Tooltip("켜면 저장된 선택이 없을 때 기기 언어를 따른다. 끄면 위 기본 언어로 시작한다.")]
        [SerializeField] private bool _followSystemLanguage;

        [SerializeField] private List<LanguageFont> _fonts = new();
        [SerializeField] private List<StringEntry> _entries = new();

        private Dictionary<string, StringEntry> _index;

        public Language DefaultLanguage => _defaultLanguage;
        public bool FollowSystemLanguage => _followSystemLanguage;
        public IReadOnlyList<LanguageFont> Fonts => _fonts;
        public IReadOnlyList<StringEntry> Entries => _entries;

        public StringEntry Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_index == null) BuildIndex();
            return _index.TryGetValue(key, out var entry) ? entry : null;
        }

        public LanguageFont FontFor(Language language)
        {
            for (int i = 0; i < _fonts.Count; i++)
                if (_fonts[i] != null && _fonts[i].Language == language) return _fonts[i];
            return null;
        }

        /// <summary>에디터 도구용 — 없으면 한국어 원문으로 새 줄을 만든다.</summary>
        public StringEntry Upsert(string key, string korean)
        {
            var entry = Find(key);
            if (entry == null)
            {
                entry = new StringEntry(key, korean);
                _entries.Add(entry);
                _index[key] = entry;
            }
            else if (!string.IsNullOrEmpty(korean))
            {
                entry.Write(Language.Korean, korean);
            }
            return entry;
        }

        private void BuildIndex()
        {
            _index = new Dictionary<string, StringEntry>(_entries.Count);
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null || string.IsNullOrEmpty(e.Key)) continue;
                _index[e.Key] = e;   // 같은 키가 둘이면 뒤의 것이 이긴다
            }
        }

        private void OnValidate() => _index = null;
    }
}
