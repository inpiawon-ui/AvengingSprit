using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    /// <summary>호스트 해금 조건 유형. 잠금 셀 문구를 규격화하기 위해 2종으로만 제한한다.</summary>
    public enum HostUnlockType
    {
        /// <summary>시작 시 보유 (아마조네스)</summary>
        Owned,
        /// <summary>지정 챕터의 지정 스테이지 도달</summary>
        StageReach,
        /// <summary>지정 챕터 보스 격파</summary>
        ChapterBossClear,
    }

    /// <summary>
    /// 공격 방식. **"어떤 몸을 뺏었는가가 빌드를 결정한다"** 가 이 게임의 한 줄 컨셉이므로,
    /// 호스트마다 교전 거리·탄 수·발사 간격이 실제로 달라야 빙의에 의미가 생긴다.
    ///
    /// 수치(사거리·간격·피해)는 `GameConfig` 기본값에 호스트별 배율을 곱해 정한다.
    /// </summary>
    public enum AttackKind
    {
        /// <summary>근접 즉시 타격. 탄이 없다 (아마조네스·슬러거·흡혈귀)</summary>
        Melee,
        /// <summary>단발 직선탄 (설녀)</summary>
        Single,
        /// <summary>빠른 연사. 발당 피해가 낮다 (람보)</summary>
        Rapid,
        /// <summary>부채꼴 다발 (닌자·마피아·드래곤)</summary>
        Spread,
        /// <summary>관통탄. 맞아도 사라지지 않는다 (마법사·로봇)</summary>
        Pierce,
        /// <summary>장사거리 고피해 저연사 (히트맨)</summary>
        Snipe,
        /// <summary>자기 주위 광역 즉시 타격 (요가마스터)</summary>
        Pulse,
    }

    /// <summary>
    /// 사격 대상을 고르는 규칙 (기획서 A 3-3 Target Type).
    /// 호스트마다 "누구를 먼저 때리는가"가 다르면, 같은 화력이라도 교전 그림이 달라진다.
    /// </summary>
    public enum TargetType
    {
        /// <summary>가장 가까운 적. 기본값</summary>
        Nearest,
        /// <summary>체력이 가장 적은 적 — 마무리에 강하다</summary>
        LowestHp,
        /// <summary>체력이 가장 많은 적 — 탱커를 먼저 무너뜨린다</summary>
        HighestHp,
    }

    /// <summary>
    /// 호스트 1종의 마스터 데이터.
    /// 표시 스탯은 0~100 스케일이며 실전투 수치와 다르다 (기획 GD-SYS 용어 구분).
    /// </summary>
    [Serializable]
    public sealed class HostEntry
    {
        [SerializeField] private string _hostKey;
        [SerializeField] private string _nameEn;
        [SerializeField] private string _nameKr;
        [SerializeField] private string _role;

        [Header("표시 스탯 (0~100)")]
        [SerializeField] private int _hp;
        [SerializeField] private int _atk;
        [SerializeField] private int _spd;
        [SerializeField] private int _dash;

        [Header("공격 방식")]
        [SerializeField] private AttackKind _attackKind;
        [SerializeField] private int _shotCount = 1;
        [SerializeField] private float _spreadDegrees;
        [SerializeField] private float _rangeMul = 1f;
        [SerializeField] private float _intervalMul = 1f;
        [SerializeField] private float _damageMul = 1f;
        [Tooltip("명중 피해의 몇 %를 회복하는가 (흡혈귀)")]
        [SerializeField] private int _lifestealPercent;
        [Tooltip("명중 시 대상 이동속도를 몇 % 늦추는가 (설녀)")]
        [SerializeField] private int _slowPercent;
        [Tooltip("근접 시 사거리 안의 적 탄을 지우는가 (슬러거)")]
        [SerializeField] private bool _reflectsShots;

        [Tooltip("이동 중에도 쏠 수 있는가. 기본은 전부 false — 멈춰야 쏜다가 이 게임의 최상위 규칙이다. " +
                 "예외 호스트에만 켠다(기획서 A 3-3 Move Attack).")]
        [SerializeField] private bool _moveAttack;

        [Tooltip("사격 대상을 고르는 규칙 (기획서 A 3-3 Target Type)")]
        [SerializeField] private TargetType _targetType = TargetType.Nearest;

        [Tooltip("계열 태그. 태그형 버프가 이 몸에 붙는지를 가른다 (기획서 A 5-4)")]
        [SerializeField] private BuffTag _tag = BuffTag.None;

        [Header("적으로 등장할 때")]
        [Tooltip("빙의 우선순위. 높을수록 먼저 잡힌다. 같으면 가까운 쪽 (기획서 A 4-3)")]
        [SerializeField] private int _possessPriority;

        [Header("얼티밋")]
        [SerializeField] private string _ultimateKey;

        [Header("해금 조건")]
        [SerializeField] private HostUnlockType _unlockType;
        [SerializeField] private int _unlockChapter;
        [SerializeField] private int _unlockStage;

        public string HostKey => _hostKey;
        public string NameEn  => _nameEn;
        public string NameKr  => _nameKr;
        public string Role    => _role;
        public int Hp   => _hp;
        public int Atk  => _atk;
        public int Spd  => _spd;
        public int Dash => _dash;
        public AttackKind Kind => _attackKind;
        public int ShotCount => Mathf.Max(1, _shotCount);
        public float SpreadDegrees => _spreadDegrees;
        public float RangeMul => _rangeMul <= 0f ? 1f : _rangeMul;
        public float IntervalMul => _intervalMul <= 0f ? 1f : _intervalMul;
        public float DamageMul => _damageMul <= 0f ? 1f : _damageMul;
        public int LifestealPercent => _lifestealPercent;
        public int SlowPercent => _slowPercent;
        public bool ReflectsShots => _reflectsShots;
        public bool MoveAttack => _moveAttack;
        public TargetType Targeting => _targetType;
        public BuffTag Tag => _tag;
        public int PossessPriority => _possessPriority;

        /// <summary>호스트 선택·인게임 HUD 에 쓰는 짧은 교전 스타일 문구.</summary>
        public string AttackText => _attackKind switch
        {
            AttackKind.Melee  => _lifestealPercent > 0 ? "근접 · 흡혈" : "근접",
            AttackKind.Single => _slowPercent > 0 ? "단발 · 둔화" : "단발",
            AttackKind.Rapid  => "연사",
            AttackKind.Spread => $"확산 {ShotCount}발",
            AttackKind.Pierce => "관통",
            AttackKind.Snipe  => "저격",
            AttackKind.Pulse  => "주위 광역",
            _ => string.Empty,
        };

        public string UltimateKey => _ultimateKey;
        public HostUnlockType UnlockType => _unlockType;
        public int UnlockChapter => _unlockChapter;
        public int UnlockStage   => _unlockStage;

        /// <summary>잠금 셀에 표시할 해금 조건 문구.</summary>
        public string UnlockText => _unlockType switch
        {
            HostUnlockType.Owned            => string.Empty,
            HostUnlockType.StageReach       => $"CH{_unlockChapter} · {_unlockStage}스테이지",
            HostUnlockType.ChapterBossClear => $"CH{_unlockChapter} 보스 격파",
            _ => string.Empty,
        };

        /// <summary>Addressable 주소. constants.md 4절 — `host/{hostKey}`</summary>
        public string PrefabAddress => $"host/{_hostKey}";
    }

    /// <summary>
    /// 호스트 12종을 배열 하나로 관리한다.
    /// 개별 asset 분리 금지 — constants.md 6절 설계 제약 3.
    /// </summary>
    [CreateAssetMenu(fileName = "HostTable", menuName = "AVSR/Host Table")]
    public sealed class HostTable : ScriptableObject
    {
        [SerializeField] private HostEntry[] _entries = Array.Empty<HostEntry>();

        private Dictionary<string, HostEntry> _index;

        public IReadOnlyList<HostEntry> Entries => _entries;
        public int Count => _entries.Length;

        public HostEntry Get(string hostKey)
        {
            if (string.IsNullOrEmpty(hostKey)) return null;
            _index ??= BuildIndex();
            return _index.TryGetValue(hostKey, out var e) ? e : null;
        }

        public HostEntry GetAt(int index)
            => index >= 0 && index < _entries.Length ? _entries[index] : null;

        /// <summary>시작 보유 호스트. 신규 유저의 기본 선택 대상이다.</summary>
        public HostEntry FirstOwned()
        {
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].UnlockType == HostUnlockType.Owned)
                    return _entries[i];
            return _entries.Length > 0 ? _entries[0] : null;
        }

        private Dictionary<string, HostEntry> BuildIndex()
        {
            var d = new Dictionary<string, HostEntry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++)
                if (!string.IsNullOrEmpty(_entries[i].HostKey))
                    d[_entries[i].HostKey] = _entries[i];
            return d;
        }
    }
}
