using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 액티브 스킬 1종의 마스터 데이터.
    /// 정본은 제안서 로스터 페이지(slide_07). 목업의 `AMAZONESS FURY` 는 초기 시안이므로 쓰지 않는다.
    /// </summary>
    [Serializable]
    public sealed class ActiveSkillEntry
    {
        [SerializeField] private string _activeSkillKey;
        [SerializeField] private string _nameEn;
        [SerializeField] private string _nameKr;
        [TextArea(2, 3)]
        [SerializeField] private string _description;

        [Tooltip("숙련도에 따라 자라는 수치. Lv1~4 · Lv5~10 두 구간이다.")]
        [SerializeField] private SkillScaling _scaling;

        public string ActiveSkillKey => _activeSkillKey;
        public string NameEn      => _nameEn;
        public string NameKr      => _nameKr;
        public string Description => _description;
        public SkillScaling Scaling => _scaling;
    }

    /// <summary>액티브 스킬 12종을 배열 하나로 관리한다.</summary>
    [CreateAssetMenu(fileName = "ActiveSkillTable", menuName = "AVSR/Active Skill Table")]
    public sealed class ActiveSkillTable : ScriptableObject
    {
        [SerializeField] private ActiveSkillEntry[] _entries = Array.Empty<ActiveSkillEntry>();

        private Dictionary<string, ActiveSkillEntry> _index;

        public IReadOnlyList<ActiveSkillEntry> Entries => _entries;

        public ActiveSkillEntry Get(string activeSkillKey)
        {
            if (string.IsNullOrEmpty(activeSkillKey)) return null;
            _index ??= BuildIndex();
            return _index.TryGetValue(activeSkillKey, out var e) ? e : null;
        }

        private Dictionary<string, ActiveSkillEntry> BuildIndex()
        {
            var d = new Dictionary<string, ActiveSkillEntry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++)
                if (!string.IsNullOrEmpty(_entries[i].ActiveSkillKey))
                    d[_entries[i].ActiveSkillKey] = _entries[i];
            return d;
        }
    }
}
