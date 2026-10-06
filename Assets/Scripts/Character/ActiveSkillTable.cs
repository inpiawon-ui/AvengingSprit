using System;
using System.Collections.Generic;
using Game.Module.Common;
using UnityEngine;

namespace Game.Character
{
    /// <summary>성급 창에 보이는 스킬 값의 단위.</summary>
    public enum SkillGrowthUnit { None, Seconds, Multiplier, Percent }

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

        /// <summary>화면에 보이는 이름. 지금 언어로 — 번역이 없으면 원문(<see cref="NameKr"/>).</summary>
        public string DisplayName => Localize.FromTable($"askill.{_activeSkillKey}.name", _nameKr);
        /// <summary>화면에 보이는 설명. 지금 언어로 — 번역이 없으면 원문(<see cref="Description"/>).</summary>
        public string DisplayDescription => Localize.FromTable($"askill.{_activeSkillKey}.desc", _description);
        public SkillScaling Scaling => _scaling;

        // 성급 창에 「지금 → 다음」 으로 띄우는 값(기획 2026-10-06). 기본 축(`BaseAt`)이 전투에서 실제로 정하는 것 하나다.
        // 단위 · 배수는 그 값이 화면에서 읽히는 모양이다(예: 표식 지속은 초, 도약 강타는 공격력 × 6 배율).
        // 단위가 None 이면 기본 축을 전투가 안 읽는 스킬이다 — 창에 줄이 안 생긴다.
        [SerializeField] private SkillGrowthUnit _growthUnit;
        [SerializeField] private float _growthMul = 1f;

        public SkillGrowthUnit GrowthUnit => _growthUnit;

        /// <summary>성급 <paramref name="level"/> 일 때 창에 보이는 값(단위 배수 적용). 단위가 없으면 0.</summary>
        public float GrowthAt(int level) => _growthUnit == SkillGrowthUnit.None ? 0f : _scaling.BaseAt(level) * _growthMul;

        /// <summary>그 값의 이름(「표식 시간」 등). 언어팩 `askill.{키}.growth`.</summary>
        // FromTable 은 한국어면 표 원문 칸을 돌려준다 — 이 글자는 원문이 없어 언어팩에서 바로 꺼낸다
        public string GrowthLabel => Localize.Get($"askill.{_activeSkillKey}.growth");

        public void SetGrowth(SkillGrowthUnit unit, float mul) { _growthUnit = unit; _growthMul = mul; }
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
