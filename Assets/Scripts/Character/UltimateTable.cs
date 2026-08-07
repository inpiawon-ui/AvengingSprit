using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 얼티밋 1종의 마스터 데이터.
    /// 정본은 제안서 로스터 페이지(slide_07). 목업의 `AMAZONESS FURY` 는 초기 시안이므로 쓰지 않는다.
    /// </summary>
    [Serializable]
    public sealed class UltimateEntry
    {
        [SerializeField] private string _ultimateKey;
        [SerializeField] private string _nameEn;
        [SerializeField] private string _nameKr;
        [TextArea(2, 3)]
        [SerializeField] private string _description;

        public string UltimateKey => _ultimateKey;
        public string NameEn      => _nameEn;
        public string NameKr      => _nameKr;
        public string Description => _description;
    }

    /// <summary>얼티밋 12종을 배열 하나로 관리한다.</summary>
    [CreateAssetMenu(fileName = "UltimateTable", menuName = "AVSR/Ultimate Table")]
    public sealed class UltimateTable : ScriptableObject
    {
        [SerializeField] private UltimateEntry[] _entries = Array.Empty<UltimateEntry>();

        private Dictionary<string, UltimateEntry> _index;

        public IReadOnlyList<UltimateEntry> Entries => _entries;

        public UltimateEntry Get(string ultimateKey)
        {
            if (string.IsNullOrEmpty(ultimateKey)) return null;
            _index ??= BuildIndex();
            return _index.TryGetValue(ultimateKey, out var e) ? e : null;
        }

        private Dictionary<string, UltimateEntry> BuildIndex()
        {
            var d = new Dictionary<string, UltimateEntry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++)
                if (!string.IsNullOrEmpty(_entries[i].UltimateKey))
                    d[_entries[i].UltimateKey] = _entries[i];
            return d;
        }
    }
}
