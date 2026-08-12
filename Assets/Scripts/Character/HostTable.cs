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
    /// <summary>
    /// 몸을 빼앗을 수 있는 방식 (정본 POSSESSION_MATRIX).
    ///
    /// 전부 즉시면 "가장 센 몸으로 갈아탄다"가 언제나 정답이 된다.
    /// 어떤 몸은 먼저 두들겨 놔야 열린다 — 그래서 지금 이 몸으로 싸울 이유가 생긴다.
    /// </summary>
    public enum PossessKind
    {
        /// <summary>조건 없이 바로 (갱스터·폭력배·구루·야구선수)</summary>
        Immediate = 0,
        /// <summary>조건을 채워야 열린다</summary>
        Condition = 1,
        /// <summary>절대 못 뺏는다 (방패병·센서드론·보스)</summary>
        NotPossessable = 2,
    }

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
        [Tooltip("정본의 EnemyID(E001 …). 방 데이터의 스폰이 이 ID 로 배우를 가리킨다.\n" +
                 "대조표: Projects/AVSR/AVSR_Roster.md")]
        [SerializeField] private string _enemyId;

        [Tooltip("빙의 우선순위. 높을수록 먼저 잡힌다. 같으면 가까운 쪽 (기획서 A 4-3)")]
        [SerializeField] private int _possessPriority;

        [Tooltip("빙의 방식 (정본 POSSESSION_MATRIX)")]
        [SerializeField] private PossessKind _possessKind = PossessKind.Immediate;

        [Tooltip("Condition 일 때 — 체력이 이 % 이하로 떨어져야 열린다.\n" +
                 "정본의 조건은 화상3·빙결·장갑파괴 같은 상태이상인데 아직 그 시스템이 없다. " +
                 "지금은 전부 체력 임계로 대신 판정한다 — '먼저 두들겨 놔야 열린다'는 모양은 같다.")]
        [Range(0, 100)]
        [SerializeField] private int _possessHpPercent = 50;

        [Header("유지 훅 (정본 HOST_MAINTAIN_HOOK)")]
        [Tooltip("이 몸을 계속 탔을 때 쌓이는 것의 이름. 정본 MaintainHook. " +
                 "화면에 이름이 보여야 무엇을 버리는지 알고 교체를 망설인다.")]
        [SerializeField] private string _maintainHook;

        [Header("정본 실수치 — CanonImporter 가 덮어쓴다. 손으로 고치지 않는다")]
        [Tooltip("정본 enemies.MaxHP. 0 이면 정본에 없는 창작 배우라 표시 스탯 공식으로 되돌아간다.")]
        [SerializeField] private int _canonHp;
        [SerializeField] private int _canonAtk;
        [Tooltip("m/s. 픽셀 변환은 방 크기에서 나온 ppm 으로 배틀에서 한다")]
        [SerializeField] private float _canonMoveSpeed;
        [Tooltip("m. 정본 attacks 의 AP_E### 사거리 — 적으로 나올 때")]
        [SerializeField] private float _canonRange;
        [SerializeField] private float _canonInterval;
        [Tooltip("m/s. 정본 projectiles 의 탄속")]
        [SerializeField] private float _canonShotSpeed;
        [Tooltip("정본 projectiles 의 탄 수 — 적으로 나올 때. 정본은 적을 전부 1발로 둔다. " +
                 "내가 탔을 때의 탄 수는 ShotCount 쪽이다. 한 칸을 같이 쓰면 " +
                 "적 갱스터까지 3발을 쏜다.")]
        [SerializeField] private int _canonShotCount;

        [Header("정본 실수치 — 내가 이 몸을 탔을 때 (attacks AP_H##)")]
        [Tooltip("정본은 같은 배우라도 적일 때와 내가 탔을 때 교전값을 따로 준다. " +
                 "0 이면 이 몸에 해당하는 호스트 프로필이 정본에 없다는 뜻이다.")]
        [SerializeField] private float _canonHostRange;
        [SerializeField] private float _canonHostInterval;
        [SerializeField] private float _canonHostMoveSpeed;
        [SerializeField] private float _canonHostShotSpeed;

        [Tooltip("정본에는 있지만 플레이어가 고를 수 없는 배우 (방패병·센서드론·엘리트). " +
                 "전투에서는 세우되 호스트 선택 화면에는 내보내지 않는다.")]
        [SerializeField] private bool _actorOnly;

        [Tooltip("그림 아틀라스 키. 비어 있으면 HostKey 를 쓴다.\n" +
                 "아직 제 그림이 없는 배우가 다른 몸의 그림을 빌려 설 때만 채운다 — " +
                 "비워 두면 아틀라스를 못 찾아 **보이지 않는 적**이 되고 방이 안 끝난다.")]
        [SerializeField] private string _spriteKey;

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
        public string EnemyId => _enemyId;
        public string MaintainHook => _maintainHook;
        public int PossessPriority => _possessPriority;
        public PossessKind PossessKind => _possessKind;
        public int PossessHpPercent => Mathf.Clamp(_possessHpPercent, 1, 100);

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

        /// <summary>정본에 실수치가 있는가. 없으면 표시 스탯 × 배율 공식으로 되돌아간다.</summary>
        public bool HasCanon => _canonHp > 0;
        /// <summary>내가 탔을 때의 정본 교전값이 있는가.</summary>
        public bool HasCanonHost => _canonHostRange > 0f;

        public int CanonHp => _canonHp;
        public int CanonAtk => _canonAtk;
        public float CanonMoveSpeed => _canonMoveSpeed;
        public float CanonRange => _canonRange;
        public float CanonInterval => _canonInterval;
        public float CanonHostRange => _canonHostRange;
        public float CanonHostInterval => _canonHostInterval;
        public float CanonHostMoveSpeed => _canonHostMoveSpeed;
        public float CanonShotSpeed => _canonShotSpeed;
        /// <summary>적으로 나올 때의 탄 수. 0 이면 정본에 없다는 뜻이라 ShotCount 로 되돌아간다.</summary>
        public int EnemyShotCount => _canonShotCount > 0 ? _canonShotCount : ShotCount;
        public float CanonHostShotSpeed => _canonHostShotSpeed;
        public bool ActorOnly => _actorOnly;
        public string SpriteKey => string.IsNullOrEmpty(_spriteKey) ? _hostKey : _spriteKey;

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
