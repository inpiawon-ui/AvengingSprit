using System;
using System.Collections.Generic;
using Game.Module.Common;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 패시브 스킬이 어떻게 터지는가.
    ///
    /// 액티브와 달리 **누르지 않는다.** 그래서 화면에서 읽히는 방식이 갈린다 —
    /// 상시형은 조건이 보여야 하고, 확률형은 터진 것이 보여야 한다.
    /// </summary>
    public enum PassiveKind
    {
        /// <summary>
        /// 100% 발동. 조건이 **좁을 때만** 쓴다.
        /// "항상 +10%" 같은 것은 두지 않는다 — 화면에서 안 보이면 없는 것과 같다.
        /// </summary>
        Always,

        /// <summary>
        /// 확률 발동. 판을 뒤집는 것에만 쓴다.
        /// **연출이 필수다** — 터진 것이 안 보이면 안 되는 것과 같다.
        /// </summary>
        Chance,
    }

    /// <summary>
    /// 패시브 스킬 1종의 마스터 데이터.
    ///
    /// 액티브와 **표를 나눠 둔다.** 액티브는 23명 전원이 하나씩 갖고
    /// 패시브는 11명만 갖는다 — 성격이 달라서, 한 표에 섞으면
    /// "이 몸의 액티브" 를 꺼낼 때마다 종류로 걸러야 한다.
    /// </summary>
    [Serializable]
    public sealed class PassiveSkillEntry
    {
        [SerializeField] private string _passiveSkillKey;
        [SerializeField] private string _nameEn;
        [SerializeField] private string _nameKr;
        [TextArea(2, 3)]
        [SerializeField] private string _description;

        [SerializeField] private PassiveKind _kind = PassiveKind.Always;

        [Tooltip("확률형일 때 발동 확률(%). 상시형은 0 이며 화면에 표기하지 않는다.")]
        [Range(0, 100)]
        [SerializeField] private int _chancePercent;

        [Tooltip("숙련도에 따라 자라는 수치. Lv1~4 · Lv5~10 두 구간이다.")]
        [SerializeField] private SkillScaling _scaling;

        public SkillScaling Scaling => _scaling;

        public string PassiveSkillKey => _passiveSkillKey;
        public string NameEn      => _nameEn;
        public string NameKr      => _nameKr;
        public string Description => _description;

        /// <summary>화면에 보이는 이름. 지금 언어로 — 번역이 없으면 원문(<see cref="NameKr"/>).</summary>
        public string DisplayName => Localize.FromTable($"pskill.{_passiveSkillKey}.name", _nameKr);
        /// <summary>화면에 보이는 설명. 지금 언어로 — 번역이 없으면 원문(<see cref="Description"/>).</summary>
        public string DisplayDescription => Localize.FromTable($"pskill.{_passiveSkillKey}.desc", _description);
        public PassiveKind Kind   => _kind;

        /// <summary>확률형만 0 보다 크다. 카드의 `PassiveSkillChanceText` 가 이 값을 그린다.</summary>
        public int ChancePercent => _kind == PassiveKind.Chance ? _chancePercent : 0;
    }

    /// <summary>패시브 스킬 11종을 배열 하나로 관리한다.</summary>
    [CreateAssetMenu(fileName = "PassiveSkillTable", menuName = "AVSR/Passive Skill Table")]
    public sealed class PassiveSkillTable : ScriptableObject
    {
        [SerializeField] private PassiveSkillEntry[] _entries = Array.Empty<PassiveSkillEntry>();

        private Dictionary<string, PassiveSkillEntry> _index;

        public IReadOnlyList<PassiveSkillEntry> Entries => _entries;

        public PassiveSkillEntry Get(string passiveSkillKey)
        {
            if (string.IsNullOrEmpty(passiveSkillKey)) return null;
            _index ??= BuildIndex();
            return _index.TryGetValue(passiveSkillKey, out var e) ? e : null;
        }

        private Dictionary<string, PassiveSkillEntry> BuildIndex()
        {
            var d = new Dictionary<string, PassiveSkillEntry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++)
                if (!string.IsNullOrEmpty(_entries[i].PassiveSkillKey))
                    d[_entries[i].PassiveSkillKey] = _entries[i];
            return d;
        }
    }
}
