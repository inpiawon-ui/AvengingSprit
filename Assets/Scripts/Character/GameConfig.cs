using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 인게임 공통 수치의 단일 출처. 04_scenes.md 규약 — 각 스크립트에 하드코딩하지 않는다.
    /// 에셋: `Assets/BundleResource/TableData/GameConfig.asset` · 주소 `TableData/GameConfig`
    ///
    /// 호스트 테이블의 스탯은 0~100 표시 스케일이다(확정 사항). 실제 전투 수치는
    /// 여기 계수로 환산한다 — 표시값을 바꾸지 않고 전투 밸런스만 조정하기 위함이다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("진행")]
        // 방 하나 = 스테이지 하나다. 방을 비우면 출구가 열리고, 통과하면 다음 스테이지로 간다.
        // 챕터의 **마지막 스테이지가 보스**이고, 그 보스를 잡으면 챕터 클리어다.
        [Tooltip("한 챕터를 이루는 스테이지 수. 마지막 스테이지가 보스방이다.")]
        [SerializeField] private int _stagesPerChapter = 3;

        [Tooltip("방을 비운 뒤 열리는 출구의 접촉 판정 반경.")]
        [SerializeField] private float _exitTouchRadius = 70f;

        [Header("고스트")]
        [SerializeField] private int _ghostHpMax = 100;
        [SerializeField] private float _ghostMoveSpeed = 320f;
        [SerializeField] private float _possessRange = 110f;

        [Tooltip("유령 상태에서 받는 피해 배율. 1.0 이면 적 4기에 1.6초 만에 소멸해 빙의할 틈이 없다.")]
        [SerializeField] private float _ghostDamageScale = 0.22f;

        [Tooltip("유령 상태에서 초당 깎이는 체력. 호스트가 살아 있는 동안에는 멈춘다. " +
                 "Ghost HP 를 '남은 시간'으로 만들어, 빙의를 미루는 것 자체에 대가를 붙인다.")]
        [SerializeField] private float _ghostDrainPerSecond = 3f;

        [Tooltip("호스트를 잃은 직후 무적·주변 감속이 유지되는 시간. 이 동안에는 자연 감소도 멈춘다.")]
        [SerializeField] private float _ghostProtectSeconds = 1f;

        [Tooltip("보호 시간 동안 주변 적이 느려지는 비율(%).")]
        [SerializeField] private int _protectSlowPercent = 50;

        [Tooltip("빙의 직후 무적 시간. 기획서의 빙의 무적(0.35)과 호스트 진입 무적(0.5)을 이어 붙인 값.")]
        [SerializeField] private float _possessInvulnSeconds = 0.85f;

        [Tooltip("멈춘 뒤 사격이 시작되기까지의 시간. 궁수의 전설 규칙 — 이동 중에는 쏘지 않는다.")]
        [SerializeField] private float _attackResumeSeconds = 0.12f;

        [Tooltip("빙의한 호스트가 최대 체력의 몇 %로 시작하는가 (기획서 A 3-3). " +
                 "몸을 뺏어도 온전한 몸이 아니라는 뜻 — 교체가 공짜가 아니게 만든다.")]
        [Range(10, 100)]
        [SerializeField] private int _hostStartHpPercent = 70;

        [Header("전술 빙의 — 살아 있는 몸을 버리고 갈아탄다 (정본 constants)")]
        [Tooltip("살아 있는 호스트를 두고 다른 몸으로 갈아탈 때 내는 Ghost HP. 정본 잠금값 6.")]
        [SerializeField] private int _tacticalGhostCost = 6;
        [Tooltip("전술 빙의 재사용 대기(초). 정본 잠금값 8. 이게 없으면 매 적마다 갈아타는 게 최적해가 된다.")]
        [SerializeField] private float _tacticalCooldownSeconds = 8f;
        [Tooltip("몸을 입은 채 뺏을 수 있는 거리. 유령 사거리(110)를 그대로 쓰면 " +
                 "호스트는 265 밖에서 쏘고 있어 버튼이 영영 안 켜진다. 교전 거리에 맞춘다.")]
        [SerializeField] private float _tacticalPossessRange = 280f;

        [Header("긴급 호스트 (기획서 A 8-3)")]
        [Tooltip("빙의할 대상이 하나도 없을 때, 이만큼 기다린 뒤 몸을 하나 만들어 준다.")]
        [SerializeField] private float _emergencyDelaySeconds = 1f;
        [Tooltip("긴급 호스트의 시작 체력(%). 일반 빙의(70%)보다 훨씬 나쁘다 — 구제책이지 선택지가 아니다.")]
        [SerializeField] private int _emergencyHostHpPercent = 30;
        [Tooltip("긴급 호스트를 쓸 때 추가로 깎이는 Ghost HP")]
        [SerializeField] private int _emergencyGhostCost = 20;

        [Header("룸 타입 (기획서 A 06)")]
        [Tooltip("정예 방의 적 수. 적게 나오지만 하나하나가 세다.")]
        [SerializeField] private int _eliteEnemyCount = 2;
        [Tooltip("정예 적의 체력 배율")]
        [SerializeField] private float _eliteHpMul = 2.4f;
        [Tooltip("정예 적의 공격력 배율")]
        [SerializeField] private float _eliteAtkMul = 1.5f;
        [Tooltip("회복 방에서 돌려주는 Ghost HP")]
        [SerializeField] private int _restGhostHeal = 30;

        [Header("런 레벨 — 적을 잡아 모으고, 차면 버프 3택1 (기획서 A 5-2)")]
        [Tooltip("일반 적 1기 처치로 얻는 EXP")]
        [SerializeField] private int _expPerEnemy = 10;
        [Tooltip("보스 처치로 얻는 EXP")]
        [SerializeField] private int _expPerBoss = 60;
        [Tooltip("Lv.1 → Lv.2 에 필요한 EXP")]
        [SerializeField] private int _expToLevelBase = 30;
        [Tooltip("레벨이 오를 때마다 필요량이 몇 % 늘어나는가")]
        [SerializeField] private int _expGrowthPercent = 45;

        [Header("호스트 — 표시 스탯(0~100) → 전투 수치 환산")]
        [SerializeField] private float _hostHpPerPoint = 6f;
        [SerializeField] private int _hostHpBase = 60;
        [SerializeField] private float _hostAtkPerPoint = 0.32f;
        [SerializeField] private int _hostAtkBase = 6;
        [SerializeField] private float _hostSpeedPerPoint = 2.4f;
        [SerializeField] private float _hostSpeedBase = 140f;
        [SerializeField] private float _hostAttackRange = 265f;
        [SerializeField] private float _hostAttackInterval = 0.55f;

        [Header("적")]
        [SerializeField] private float _enemyHpScale = 0.55f;
        [SerializeField] private float _enemyAtkScale = 0.7f;
        [SerializeField] private float _enemySpeedScale = 0.62f;
        [SerializeField] private float _enemyAttackRange = 150f;
        [SerializeField] private float _enemyAttackInterval = 1.1f;
        [Tooltip("이 거리 안에 들어오면 플레이어를 인지하고 달려든다. 밖이면 제자리 대기.")]
        [SerializeField] private float _enemyDetectRange = 300f;
        [Tooltip("이보다 가까운 적끼리 서로 밀어낸다. 0 이면 겹쳐서 한 마리처럼 보인다.")]
        [SerializeField] private float _enemySeparation = 82f;
        [SerializeField] private int _enemiesPerRoomMin = 4;
        [SerializeField] private int _enemiesPerRoomMax = 7;

        [Header("보스")]
        [SerializeField] private int _bossHpBase = 900;
        [SerializeField] private int _bossAtk = 26;
        [SerializeField] private float _bossMoveSpeed = 70f;
        [SerializeField] private float _bossAttackRange = 260f;
        [SerializeField] private float _bossAttackInterval = 1.6f;

        [Header("투사체 — 기본 공격은 탄이 날아가 맞아야 피해가 들어간다")]
        [SerializeField] private float _shotSpeedPlayer = 720f;
        [SerializeField] private float _shotSpeedEnemy = 420f;
        [SerializeField] private float _shotSize = 26f;
        [SerializeField] private float _shotHitRadius = 34f;
        [SerializeField] private float _shotLifeSeconds = 1.6f;

        [Tooltip("둔화 지속 시간 (설녀 명중 시)")]
        [SerializeField] private float _slowSeconds = 1.6f;

        [Header("얼티밋")]
        [SerializeField] private float _ultimateChargeSeconds = 14f;
        [SerializeField] private int _ultimateDamage = 140;

        [Header("보상")]
        [SerializeField] private int _rewardGoldPerRoom = 120;
        [SerializeField] private int _rewardGhostExpPerRoom = 8;

        public int StagesPerChapter => Mathf.Max(1, _stagesPerChapter);
        public float ExitTouchRadius => _exitTouchRadius;
        public int HostStartHpPercent => Mathf.Clamp(_hostStartHpPercent, 10, 100);
        public float EmergencyDelaySeconds => _emergencyDelaySeconds;
        public int EmergencyHostHpPercent => Mathf.Clamp(_emergencyHostHpPercent, 5, 100);
        public int EmergencyGhostCost => Mathf.Max(0, _emergencyGhostCost);
        public int TacticalGhostCost => Mathf.Max(0, _tacticalGhostCost);
        public float TacticalCooldownSeconds => Mathf.Max(0f, _tacticalCooldownSeconds);
        public float TacticalPossessRange => _tacticalPossessRange;
        public int EliteEnemyCount => Mathf.Max(1, _eliteEnemyCount);
        public float EliteHpMul => _eliteHpMul;
        public float EliteAtkMul => _eliteAtkMul;
        public int RestGhostHeal => Mathf.Max(0, _restGhostHeal);
        public int ExpPerEnemy => _expPerEnemy;
        public int ExpPerBoss => _expPerBoss;

        /// <summary>해당 레벨에서 다음 레벨까지 필요한 EXP. 레벨마다 등비로 늘어난다.</summary>
        public int ExpToNext(int level)
        {
            float need = _expToLevelBase;
            for (int i = 1; i < Mathf.Max(1, level); i++) need *= 1f + _expGrowthPercent / 100f;
            return Mathf.Max(1, Mathf.RoundToInt(need));
        }

        public int GhostHpMax => _ghostHpMax;
        public float GhostMoveSpeed => _ghostMoveSpeed;
        public float GhostDrainPerSecond => _ghostDrainPerSecond;
        public float GhostProtectSeconds => _ghostProtectSeconds;
        public int ProtectSlowPercent => _protectSlowPercent;
        public float PossessInvulnSeconds => _possessInvulnSeconds;
        public float PossessRange => _possessRange;
        public int GhostDamage(int raw) => Mathf.Max(1, Mathf.RoundToInt(raw * _ghostDamageScale));

        public float AttackResumeSeconds => _attackResumeSeconds;

        public float HostAttackRange => _hostAttackRange;
        public float HostAttackInterval => _hostAttackInterval;
        public int HostHp(int statHp) => _hostHpBase + Mathf.RoundToInt(statHp * _hostHpPerPoint);
        public int HostAtk(int statAtk) => _hostAtkBase + Mathf.RoundToInt(statAtk * _hostAtkPerPoint);
        public float HostSpeed(int statSpd) => _hostSpeedBase + statSpd * _hostSpeedPerPoint;

        public float EnemyAttackRange => _enemyAttackRange;
        public float EnemyDetectRange => _enemyDetectRange;
        public float EnemySeparation => _enemySeparation;
        public float EnemyAttackInterval => _enemyAttackInterval;
        public int EnemiesPerRoom(int roomIndex)
            => Mathf.Clamp(_enemiesPerRoomMin + roomIndex / 2, _enemiesPerRoomMin, _enemiesPerRoomMax);
        public int EnemyHp(int statHp) => Mathf.Max(1, Mathf.RoundToInt(HostHp(statHp) * _enemyHpScale));
        public int EnemyAtk(int statAtk) => Mathf.Max(1, Mathf.RoundToInt(HostAtk(statAtk) * _enemyAtkScale));
        public float EnemySpeed(int statSpd) => HostSpeed(statSpd) * _enemySpeedScale;

        public int BossHp(int chapter) => _bossHpBase + Mathf.Max(0, chapter - 1) * 400;
        public int BossAtk => _bossAtk;
        public float BossMoveSpeed => _bossMoveSpeed;
        public float BossAttackRange => _bossAttackRange;
        public float BossAttackInterval => _bossAttackInterval;

        public float ShotSpeedPlayer => _shotSpeedPlayer;
        public float ShotSpeedEnemy => _shotSpeedEnemy;
        public float ShotSize => _shotSize;
        public float ShotHitRadius => _shotHitRadius;
        public float ShotLifeSeconds => _shotLifeSeconds;
        public float SlowSeconds => _slowSeconds <= 0f ? 1.6f : _slowSeconds;

        public float UltimateChargeSeconds => _ultimateChargeSeconds;
        public int UltimateDamage => _ultimateDamage;

        public int RewardGold(int rooms) => _rewardGoldPerRoom * rooms;
        public int RewardGhostExp(int rooms) => _rewardGhostExpPerRoom * rooms;
    }
}
