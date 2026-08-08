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
        [SerializeField] private int _roomsPerStage = 6;
        [Tooltip("보스가 나오는 주기(스테이지). 3 이면 3·6·9… 스테이지의 마지막 방에만 나온다.")]
        [SerializeField] private int _bossEveryStages = 3;

        [Header("고스트")]
        [SerializeField] private int _ghostHpMax = 120;
        [SerializeField] private float _ghostMoveSpeed = 320f;
        [SerializeField] private float _possessRange = 110f;

        [Tooltip("유령 상태에서 받는 피해 배율. 1.0 이면 적 4기에 1.6초 만에 소멸해 빙의할 틈이 없다.")]
        [SerializeField] private float _ghostDamageScale = 0.22f;

        [Tooltip("멈춘 뒤 사격이 시작되기까지의 시간. 궁수의 전설 규칙 — 이동 중에는 쏘지 않는다.")]
        [SerializeField] private float _attackResumeSeconds = 0.12f;

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

        public int RoomsPerStage => _roomsPerStage;
        public int BossEveryStages => Mathf.Max(1, _bossEveryStages);

        public int GhostHpMax => _ghostHpMax;
        public float GhostMoveSpeed => _ghostMoveSpeed;
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
