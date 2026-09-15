using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 몸의 등급. 정본 `statTier` 와 같다. 로비에서 데려오는 골드 값이 이걸로 갈린다.
    /// </summary>
    public enum HostGrade { B, A, S }

    /// <summary>
    /// 성장하는 능력치 여섯. **순서가 곧 `GameConfig` 성장폭 배열의 첨자다** —
    /// 여기를 바꾸면 그 배열도 함께 바꿔야 한다.
    ///
    /// ⚠ `_possessHpPercent`(빙의 시작 HP%)는 여기 넣지 않는다. 나머지는
    ///   "이 몸의 능력" 인데 그것만 "뺏을 때의 조건" 이라 층이 다르다.
    ///   뺏으면 HP 바에 바로 보이므로 미리 알려 줄 이유도 없다.
    /// </summary>
    public enum HostStat { Hp, Atk, Crit, AtkSpeed, Range, MoveSpeed }

    /// <summary>
    /// 레벨 한 칸의 능력치. `HostEntry._levelStats` 의 원소다.
    /// 고스트 Lv 가 인덱스이므로 **모든 몸이 같은 길이**를 갖는다.
    /// </summary>
    [Serializable]
    public struct HostLevelStat
    {
        public int Hp;
        public int Atk;
    }

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

        [Header("표시 스탯")]
        [SerializeField] private int _hp;
        [SerializeField] private int _atk;
        [Tooltip("이동속도 (0~100).")]
        [SerializeField] private int _spd;
        // ⚠ 예전 이름은 `_dash` 였다. 대시 스탯인 줄 알았지만 정본 임포터가
        //   **공격속도**(`atkSpeedIndex`)를 넣고 있었다 — 라벨과 값이 어긋나
        //   유저가 보던 `DASH 66` 이 실은 공격속도였다. 이름을 값에 맞췄다.
        [Tooltip("공격속도 (0~100). 클수록 빠르다.")]
        [SerializeField] private int _atkSpeed;

        // 방어력은 다른 표시 스탯과 달리 **등급 1~10** 으로 적는다.
        // 23명을 한 장에 놓고 서로 견줘 매긴 값이라(명세 2026-09-14) 0~100 으로 늘리면
        // 없는 정밀도가 생긴 것처럼 보인다. 실제 감소율은 아래 프로퍼티가 정한다.
        [Tooltip("방어력 등급 1~10. 등급 하나가 받는 피해 3% 감소다.")]
        [Range(1, 10)]
        [SerializeField] private int _defenseGrade = 5;

        [Tooltip("치명타 확률 시작값(%). 배율은 전역 고정이라 GameConfig 가 갖는다.")]
        [Range(0, 100)]
        [SerializeField] private int _critPercent = 10;

        [Header("주 성장 스탯 2개 — 크게 오른다. 나머지 넷은 작게 오른다.")]
        [SerializeField] private HostStat _primaryStatA = HostStat.Hp;
        [SerializeField] private HostStat _primaryStatB = HostStat.Atk;

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
        [Tooltip("m/s. 정본 MoveSpeed — 아직 안 싸울 때의 걸음. 픽셀 변환은 배틀의 ppm 이 한다")]
        [SerializeField] private float _canonMoveSpeed;
        [Tooltip("m/s. 정본 EngageSpeed — 싸우러 다가올 때의 속도. " +
                 "정본이 두 열을 따로 준다. MoveSpeed(1.0~2.5)만 쓰면 내가 3.4~5.2 라 " +
                 "적이 영영 못 따라온다 — 근접은 한 번도 닿지 못한다.")]
        [SerializeField] private float _canonEngageSpeed;
        [Tooltip("m. 정본 attacks 의 AP_E### 사거리 — 적으로 나올 때")]
        [SerializeField] private float _canonRange;
        [SerializeField] private float _canonInterval;
        [Tooltip("m/s. 정본 projectiles 의 탄속")]
        [SerializeField] private float _canonShotSpeed;
        [Tooltip("정본 projectiles 의 탄 수 — 적으로 나올 때. 정본은 적을 전부 1발로 둔다. " +
                 "내가 탔을 때의 탄 수는 ShotCount 쪽이다. 한 칸을 같이 쓰면 " +
                 "적 갱스터까지 3발을 쏜다.")]
        [SerializeField] private int _canonShotCount;
        [Tooltip("초. 정본 aiProfiles 의 Telegraph — 때리기 전에 자세를 잡는 시간. " +
                 "이게 없으면 예고 없이 맞아서 피할 방법이 없다. 정본 NO_OFFSCREEN_TELEGRAPH 와 같은 취지다.")]
        [SerializeField] private float _canonTelegraph;
        [Tooltip("정본 aiProfiles 의 MaxConcurrent — 이 종류가 **동시에** 때릴 수 있는 최대 마릿수. " +
                 "제한이 없으면 방 안 전원이 같은 순간에 쏴서 근접으로는 들어갈 틈이 없다.")]
        [SerializeField] private int _canonMaxConcurrent;

        [Header("정본 실수치 — 내가 이 몸을 탔을 때 (attacks AP_H##)")]
        [Tooltip("정본은 같은 배우라도 적일 때와 내가 탔을 때 교전값을 따로 준다. " +
                 "0 이면 이 몸에 해당하는 호스트 프로필이 정본에 없다는 뜻이다.")]
        // ⚠ 같은 배우라도 **내가 탔을 때와 적일 때 체력·공격력이 다르다.**
        //   정본 HOST_MASTER 는 지수(hpIndex 94~132)와 기준값(hp 142 / atk 14)으로 주고,
        //   ENEMY_RUNTIME 은 절대값(85~166 / 8~11)으로 준다. 서로 다른 표다.
        //   예전에는 칸이 하나뿐이라 적 값이 호스트 값까지 덮어써서, 내 몸이
        //   의도한 체력의 절반으로 돌아다녔다 — 방을 도저히 못 버티던 원인이다.
        /// <summary>
        /// 적으로 나올 때 탄이 얼마나 휘는가. 0 이면 직진.
        /// 정본이 `PrimaryTrait: HOMING` 이라고 못박은 종만 0 이 아니다 —
        /// 지금은 코만도(미사일) 하나뿐이다("readable homing" = 눈에 보이게 천천히).
        /// </summary>
        [SerializeField] private float _canonHoming;
        [SerializeField] private int _canonHostHp;
        [SerializeField] private int _canonHostAtk;
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

        [Header("액티브 스킬")]
        [SerializeField] private string _activeSkillKey;

        /// <summary>
        /// 이 몸의 액티브 스킬 쿨다운(초). 티어는 4단이다 —
        /// **T1 8 · T2 14 · T3 20 · T4 28.** 현행 14초가 T2 자리라 절반은 그대로다.
        ///
        /// 세기 조절 손잡이는 **쿨 하나뿐이다**(설계안 §3-1). 센 스킬은 쿨이 길다.
        /// 피해·지속·쿨을 전부 따로 만지면 어느 것이 센지 표에서 안 읽힌다.
        ///
        /// ⚠ **0 이면 `GameConfig` 기본값으로 떨어진다.** 사거리·간격에서 `_canonHost*`
        ///   계열이 항상 먼저 먹어 배율 칸이 통째로 죽어 있던 함정이 있었다
        ///   (`AVSR_JobClasses.md` §6). 그 일을 되풀이하지 않도록 우선순위를
        ///   **여기 한 곳에만** 적는다 — 읽는 쪽은 `ActiveSkillCooldown(fallback)` 하나다.
        /// </summary>
        [SerializeField] private float _activeSkillCooldown;

        /// <summary>
        /// 이 몸의 패시브 스킬 키. **비어 있으면 패시브가 없다(12명).**
        ///
        /// 액티브는 23명 전원이 하나씩 갖지만 패시브는 11명뿐이라
        /// 표를 나눠 뒀다(`PassiveSkillTable`). 여기는 그 표를 가리키는 키만 든다.
        ///
        /// ⚠ 숙련도 0(봉인)이면 액티브와 **함께** 잠긴다 — 둘은 같이 열린다.
        ///   잠금 판정은 `IPlayerDataService.IsSkillSealed(hostKey)` 한 곳이다.
        /// </summary>
        [SerializeField] private string _passiveSkillKey;

        /// <summary>
        /// 이 몸의 등급. 정본 `statTier` 를 그대로 받는다 — **S 1명 · A 11명 · B 11명.**
        ///
        /// 로비에서 **데려오는 값(골드)** 이 이 등급으로 갈린다.
        /// 숙련도에 비례시키지 않는다 — 키운 몸일수록 비싸지면
        /// 공들인 쪽이 벌을 받아 진입 장벽만 높아진다.
        /// </summary>
        [SerializeField] private HostGrade _grade = HostGrade.B;

        /// <summary>
        /// **레벨별 능력치.** 고스트 Lv 가 그대로 이 배열의 인덱스가 된다
        /// (`_levelStats[0]` = Lv1).
        ///
        /// ⚠ 고스트에는 스탯이 없다. 고스트가 주는 것은 **레벨**뿐이고,
        ///   그 레벨에서 HP·ATK 가 얼마인지는 **이 표가** 정한다.
        ///
        ///     고스트 Lv20 → 아마존에 빙의 → 아마존 Lv20 스탯
        ///     고스트 Lv20 → 구루에  빙의 → 구루  Lv20 스탯
        ///
        ///   배율 하나를 전원에게 곱하면 23명이 같은 비율로 커져서
        ///   성장해도 **몸끼리의 관계가 안 변한다.** 그래서 몸마다 표를 갖는다.
        ///
        ///   호스트마다 레벨을 따로 쌓게 만들지도 않는다 — "키운 몸이 방에 없다 →
        ///   Lv1 몸을 탄다 → 죽는다 → 빙의를 피한다" 가 되어 핵심 재미가 죽는다.
        ///
        /// **비어 있으면** 레벨과 무관하게 정본 기본값(`_canonHost*`)을 쓴다.
        /// 값이 정해지기 전까지는 그 상태다.
        /// </summary>
        [SerializeField] private HostLevelStat[] _levelStats = Array.Empty<HostLevelStat>();

        [Header("해금 조건")]
        [SerializeField] private HostUnlockType _unlockType;
        [SerializeField] private int _unlockChapter;
        [SerializeField] private int _unlockStage;

        /// <summary>
        /// 잡몹 한 종을 만든다. **호스트 테이블에는 들어가지 않는다** —
        /// 로비 선택지에도, 정본 `HOST_MASTER` 에도 없는 배우다.
        ///
        /// 잡몹은 빼앗을 수 없는 적이다(`NotPossessable`). 그런데 전투 코드는
        /// 근접·사거리·간격 같은 값을 전부 `HostEntry` 에서 읽는다 — 프로필이 null 이면
        /// `IsMelee` 가 false 로 떨어져 근접 잡몹이 원거리처럼 굴게 된다.
        /// 그래서 데이터만 채운 껍데기 하나를 만들어 쥐여 준다.
        /// </summary>
        /// <summary>유령 자신의 키. 목록 맨 앞에 서는 "몸 없이 들어간다" 칸이다.</summary>
        public const string GhostKey = "ghost";

        /// <summary>
        /// 목록에 세우는 **유령 칸**.
        ///
        /// 버튼을 따로 두지 않고 호스트 목록 맨 앞에 넣는다 —
        /// 고르는 자리가 하나면 "무엇을 데려갈까" 가 한 번의 판단이 된다.
        /// 몸이 아니므로 스킬도 등급값도 없다. 골드가 들지 않는다.
        /// </summary>
        public static HostEntry CreateGhost() => new HostEntry
        {
            _hostKey = GhostKey,
            _nameKr = "유령",
            _nameEn = "GHOST",
            _spriteKey = GhostKey,
            _unlockType = HostUnlockType.Owned,
            _grade = HostGrade.B,
        };

        /// <summary>이 칸이 유령인가. 몸값·스킬·숙련도가 전부 없다.</summary>
        public bool IsGhost => _hostKey == GhostKey;

        public static HostEntry CreateTrash(string key, string nameKr, AttackKind kind,
                                            int hp, int atk, float moveMps, float engageMps,
                                            float rangeMeters, float interval, float telegraph,
                                            int shotCount = 1, float spreadDegrees = 0f)
        {
            return new HostEntry
            {
                _hostKey = key,
                _nameKr = nameKr,
                _nameEn = key,
                _spriteKey = key,
                _attackKind = kind,
                _possessKind = PossessKind.NotPossessable,
                _canonHp = hp,
                _canonAtk = atk,
                _canonMoveSpeed = moveMps,
                _canonEngageSpeed = engageMps,
                _canonRange = rangeMeters,
                _canonInterval = interval,
                _canonTelegraph = telegraph,
                // 부채꼴로 쏘는 잡몹(순찰기)은 여기서 발수를 받는다.
                // ⚠ 발당 피해는 `PerformAttack` 의 `split` 이 알아서 나눈다 —
                //   탄 수가 그대로 화력 배수가 되지 않는다.
                _canonShotCount = Mathf.Max(1, shotCount),
                _spreadDegrees = spreadDegrees,
                _canonMaxConcurrent = 0,   // 잡몹은 물량이 정체다 — 동시 공격을 막지 않는다
                // ⚠ 방어등급을 **여기서 정한다.** 안 적으면 필드 기본값 5(15%)를 물고 나와
                //   해골 한 마리가 표에 있는 몸만큼 단단해진다 — 정한 값이 아니라 사고다.
                //   잡몹은 물량이 정체이므로 가장 낮은 1등급(3%)이다.
                _defenseGrade = TrashDefenseGrade,
            };
        }

        public string HostKey => _hostKey;
        public string NameEn  => _nameEn;
        public string NameKr  => _nameKr;
        public int Hp   => _hp;
        public int Atk  => _atk;
        public int Spd  => _spd;
        public int AtkSpeed => _atkSpeed;
        public int CritPercent => _critPercent;
        public HostStat PrimaryStatA => _primaryStatA;
        public HostStat PrimaryStatB => _primaryStatB;

        /// <summary>이 스탯이 이 몸의 주 성장 스탯인가.</summary>
        public bool IsPrimary(HostStat stat) => _primaryStatA == stat || _primaryStatB == stat;
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
        /// <summary>싸우러 올 때의 속도. 없으면(엘리트) MoveSpeed 가 이미 그 값이다.</summary>
        public float CanonEngageSpeed => _canonEngageSpeed > 0f ? _canonEngageSpeed : _canonMoveSpeed;
        public float CanonRange => _canonRange;
        public float CanonInterval => _canonInterval;
        public float CanonHoming => _canonHoming;
        public int CanonHostHp => _canonHostHp;
        public int CanonHostAtk => _canonHostAtk;
        public float CanonHostRange => _canonHostRange;
        public float CanonHostInterval => _canonHostInterval;
        public float CanonHostMoveSpeed => _canonHostMoveSpeed;
        public float CanonShotSpeed => _canonShotSpeed;
        public float CanonTelegraph => _canonTelegraph;
        /// <summary>동시에 때릴 수 있는 마릿수. 0 이면 제한 없음.</summary>
        public int CanonMaxConcurrent => _canonMaxConcurrent;
        /// <summary>적으로 나올 때의 탄 수. 0 이면 정본에 없다는 뜻이라 ShotCount 로 되돌아간다.</summary>
        public int EnemyShotCount => _canonShotCount > 0 ? _canonShotCount : ShotCount;
        public float CanonHostShotSpeed => _canonHostShotSpeed;
        public bool ActorOnly => _actorOnly;
        public string SpriteKey => string.IsNullOrEmpty(_spriteKey) ? _hostKey : _spriteKey;

        public string ActiveSkillKey => _activeSkillKey;

        /// <summary>
        /// 이 몸의 쿨다운. 표에 값이 없으면(0) <paramref name="fallback"/> 을 쓴다.
        /// **쿨을 읽는 곳은 여기 하나뿐이다** — 두 곳에서 각자 판단하면 어긋난다.
        /// </summary>
        public float ActiveSkillCooldown(float fallback)
            => _activeSkillCooldown > 0f ? _activeSkillCooldown : fallback;

        /// <summary>패시브 스킬 키. 비어 있으면 이 몸에는 패시브가 없다.</summary>
        public string PassiveSkillKey => _passiveSkillKey;

        public HostGrade Grade => _grade;

        /// <summary>등급 하나가 깎는 피해 비율(%). 등급 10 이면 30% 다.</summary>
        public const int DefensePercentPerGrade = 3;

        /// <summary>잡몹의 방어등급. 잡는 맛이 죽지 않게 가장 낮은 칸이다.</summary>
        public const int TrashDefenseGrade = 1;

        /// <summary>방어력 등급 1~10.</summary>
        public int DefenseGrade => Mathf.Clamp(_defenseGrade, 1, 10);

        // ── 등급 1~10 ────────────────────────────────────────────
        //
        // 확정본이 준 칸(`_hp` · `_atk` · `_spd` · `_atkSpeed`)은 **0~100 눈금**이다.
        // 열로 나누면 그대로 1~10 등급이 된다 — 호퍼(기관단총) 공속 99 → 10,
        // 코만도(미사일) 49 → 5. 즉 **등급표는 이미 확정본이 준 것**이고,
        // 이쪽은 그 눈금을 등급으로 읽는 자리일 뿐이다.
        //
        // ⚠ 실제 수치로 바꾸는 곳은 여기가 아니라 `GameConfig` 의 곡선 하나다.
        //   방어력이 `DefensePercent` 한 곳에서만 %로 바뀌는 것과 같은 규칙이다.

        private static int GradeOf(int hundred) => Mathf.Clamp(Mathf.RoundToInt(hundred / 10f), 1, 10);

        /// <summary>체력 등급 1~10.</summary>
        public int HpGrade   => GradeOf(_hp);

        /// <summary>공격력 등급 1~10. 초당 피해량(DPS)을 정한다 — 한 방 피해가 아니다.</summary>
        public int AtkGrade  => GradeOf(_atk);

        /// <summary>이동속도 등급 1~10.</summary>
        public int SpdGrade  => GradeOf(_spd);

        /// <summary>공격속도 등급 1~10. 초당 때리는 횟수를 정한다.</summary>
        public int RateGrade => GradeOf(_atkSpeed);

        /// <summary>
        /// 치명타 등급 1~10.
        ///
        /// ⚠ 확정본의 `_critPercent` 는 **23명 전원 10 으로 같다** — 차등 자료가 없다.
        ///   그래서 주 성장 스탯으로 가른다. 치명타를 주 스탯으로 든 몸이 잘 터진다.
        ///   확정본에 칸이 생기면 이 규칙을 걷어내고 그 칸을 읽는다.
        /// </summary>
        public int CritGrade => IsPrimary(HostStat.Crit) ? 8 : 3;

        /// <summary>
        /// 사거리 등급 1~10. 1.4 m 가 1 등급, 한 등급이 0.8 m 다.
        ///
        /// ⚠ 사거리만은 **등급에서 되돌리지 않는다.** 직업(근거리·중거리·원거리)을
        ///   가르는 값이라 0.4 m 만 어긋나도 판이 달라진다. 표시용으로만 쓴다.
        /// </summary>
        public int RangeGrade
        {
            get
            {
                float m = CanonHostRange > 0f ? CanonHostRange : CanonRange;
                return Mathf.Clamp(Mathf.RoundToInt((m - 1.4f) / 0.8f) + 1, 1, 10);
            }
        }

        /// <summary>
        /// 받는 피해를 깎는 비율(%). **등급을 실제 수치로 바꾸는 곳은 여기 하나뿐이다** —
        /// 읽는 쪽마다 곱하면 한쪽만 고치고 나머지를 잊는다.
        /// </summary>
        public int DefensePercent => DefenseGrade * DefensePercentPerGrade;

        /// <summary>레벨 표가 채워져 있는가. 비었으면 정본 기본값으로 떨어진다.</summary>
        public bool HasLevelStats => _levelStats != null && _levelStats.Length > 0;

        /// <summary>
        /// 이 레벨의 능력치. 표 범위를 넘으면 **마지막 칸**을 쓴다 —
        /// 상한을 넘겼다고 스탯이 0 이 되면 안 된다.
        /// </summary>
        public HostLevelStat StatAt(int level)
        {
            if (!HasLevelStats) return default;
            int i = Mathf.Clamp(level - 1, 0, _levelStats.Length - 1);
            return _levelStats[i];
        }

        /// <summary>패시브를 가진 몸인가. 23명 중 11명만 true 다.</summary>
        public bool HasPassiveSkill => !string.IsNullOrEmpty(_passiveSkillKey);
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
