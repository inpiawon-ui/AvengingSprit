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
        // 165px(1.9m) 은 몸에 거의 달라붙어야 잡혔다. 기획 슬라이드 1-5 의 10m 는 반대로
        // 방(8.4×14m) 을 통째로 덮어 "어디서든 아무나" 가 된다. 그 사이를 잡은 값이다.
        // 720px = 8.4m 이므로 1m = 85.7px.
        [Tooltip("고스트 빙의 사거리(px). 5.0m — 기획서 1-5 의 초기 테스트값 10m 를 절반으로.")]
        [SerializeField] private float _possessRange = 429f;    // 5.0m

        [Tooltip("스스로 몸을 놓아줄 때 치르는 Ghost HP (최대치 대비 %). 기획서 1-2 A")]
        [SerializeField] private int _ghostLeaveCostPercent = 15;

        [Tooltip("몸이 죽어서 유령이 될 때 치르는 Ghost HP (최대치 대비 %). 기획서 1-2 A")]
        [SerializeField] private int _ghostDeathCostPercent = 20;

        [Tooltip("놓아준 뒤 다시 빙의할 수 있게 되기까지. 빙의 버튼의 덮개가 이걸 보여준다.")]
        [SerializeField] private float _repossessLockSeconds = 1.2f;


        [Tooltip("유령 상태에서 초당 깎이는 체력. 호스트가 살아 있는 동안에는 멈춘다. " +
                 "Ghost HP 를 '남은 시간'으로 만들어, 빙의를 미루는 것 자체에 대가를 붙인다.")]
        [SerializeField] private float _ghostDrainPerSecond = 3f;

        [Tooltip("호스트를 잃은 직후 무적·주변 감속이 유지되는 시간. 이 동안에는 자연 감소도 멈춘다.")]
        [SerializeField] private float _ghostProtectSeconds = 1f;

        [Tooltip("보호 시간 동안 주변 적이 느려지는 비율(%).")]
        [SerializeField] private int _protectSlowPercent = 50;

        [Tooltip("빙의 직후 무적 시간. 기획서의 빙의 무적(0.35)과 호스트 진입 무적(0.5)을 이어 붙인 값.")]
        /// <summary>
        /// 빙의가 몸에 닿기까지 걸리는 시간.
        ///
        /// 정본은 0.35 초인데 **그 길이로는 빙의한 느낌이 안 난다.** 한 호흡이 필요해서
        /// 두 배로 늘렸다 — 이 게임의 이름값이 걸린 동작이라 정본보다 연출을 우선한다.
        /// 0 으로 두면 예전처럼 즉시 들어간다.
        /// </summary>
        [SerializeField] private float _possessChannelSeconds = 0.7f;

        /// <summary>
        /// 빙의하는 동안 화면이 얼마나 당겨지는가. 1 이면 당기지 않는다.
        /// 크게 주면 방 밖이 보인다 — 방 화면에 마스크가 없다.
        /// </summary>
        [SerializeField] private float _possessZoom = 1.12f;


        [SerializeField] private float _possessInvulnSeconds = 0.85f;

        [Tooltip("멈춘 뒤 사격이 시작되기까지의 시간. 궁수의 전설 규칙 — 이동 중에는 쏘지 않는다.")]
        // 정본은 0.15 인데 손에서는 뜸을 들이는 것으로 느껴진다.
        // "멈춰야 쏜다" 규칙은 유지하되 멈춘 뒤의 기다림만 줄인다.
        [SerializeField] private float _attackResumeSeconds = 0.06f;

        [Tooltip("빙의한 호스트가 최대 체력의 몇 %로 시작하는가 (기획서 A 3-3). " +
                 "몸을 뺏어도 온전한 몸이 아니라는 뜻 — 교체가 공짜가 아니게 만든다.")]
        [Range(10, 100)]
        [SerializeField] private int _hostStartHpPercent = 70;

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

        [Header("잡몹 공격 속도")]
        [Tooltip("공격 간격 배율. 정본 값에 곱한다 — 작을수록 자주 때린다")]
        [SerializeField] private float _enemyIntervalMul = 0.85f;
        [Tooltip("공격 예고(자세 잡는 시간) 배율. 작을수록 빨리 나가지만 피할 틈도 준다")]
        [SerializeField] private float _enemyWindupMul = 0.7f;

        [Header("잡몹 체력 — 전체 높이")]
        [Tooltip("잡몹 체력 전체 배율. 챕터·방 배율에 더 곱한다 (보스 제외)")]
        [SerializeField] private float _enemyHpMul = 2f;

        [Header("격투 쉴드 — 때릴 때마다 차고, 손을 놓으면 녹는다")]
        [Tooltip("타격당 차는 양 (최대 HP의 %) — 소수점을 쓴다. 3%는 너무 빨라 1.5%로 내렸다")]
        [SerializeField] private float _shieldPerHitPercent = 1.5f;
        [Tooltip("쌓을 수 있는 상한 (최대 HP의 %)")]
        [SerializeField] private int _shieldCapPercent = 30;
        [Tooltip("마지막 타격 후 그대로 버티는 시간(초)")]
        [SerializeField] private float _shieldHoldSeconds = 0.6f;
        [Tooltip("그 뒤 녹는 속도 (최대 HP의 %/초)")]
        [SerializeField] private int _shieldDecayPercentPerSecond = 12;

        [Header("호스트 — 표시 스탯(0~100) → 전투 수치 환산")]
        [SerializeField] private float _hostHpPerPoint = 6f;
        [SerializeField] private int _hostHpBase = 60;
        [SerializeField] private float _hostAtkPerPoint = 0.32f;
        [SerializeField] private int _hostAtkBase = 6;
        [SerializeField] private float _hostSpeedPerPoint = 2.4f;
        [SerializeField] private float _hostSpeedBase = 140f;
        // ⚠ 적 사거리와 **같은 값**을 쓴다. 한쪽만 올리면 그쪽이 일방적으로 때린다 —
        //    적을 900 으로 올렸을 때 내 몸은 265 라 갱스터로 아무것도 못 쐈다.
        //    편의 차이는 여기가 아니라 **캐릭터의 RangeMul** 로만 나야 한다.
        [SerializeField] private float _hostAttackRange = 900f;
        [SerializeField] private float _hostAttackInterval = 0.55f;

        [Header("적")]
        [SerializeField] private float _enemyHpScale = 0.55f;
        [SerializeField] private float _enemyAtkScale = 0.7f;
        [SerializeField] private float _enemySpeedScale = 0.62f;
        // 화면에 보이면 곧 사거리 안이다. 150 이던 것을 900 으로 올렸다 —
        // 짧으면 적이 붙으러 걸어오는 동안이 빈 시간이 되고, 다 붙고 나면
        // 한 덩어리가 되어 피할 자리가 없다. 흩어져서 쏘는 쪽이 낫다.
        [SerializeField] private float _enemyAttackRange = 900f;
        [SerializeField] private float _enemyAttackInterval = 1.1f;
        [Tooltip("이 거리 안에 들어오면 플레이어를 인지하고 달려든다. 밖이면 제자리 대기.")]
        [SerializeField] private float _enemyDetectRange = 300f;
        [Tooltip("이보다 가까운 적끼리 서로 밀어낸다. 0 이면 겹쳐서 한 마리처럼 보인다.")]
        /// <summary>
        /// 근접이 때릴 수 있는 거리. **근접이냐 원거리냐로 갈리고**, 원거리는
        /// 캐릭터의 RangeMul 을 쓴다. 사거리를 편(적/나)으로 나누지 않는 이유는
        /// 한쪽만 올리면 그쪽이 일방적으로 때리기 때문이다.
        ///
        /// ⚠ 90 아래로 내리면 **닿지 않는다.** 적 그림이 84, 내 그림이 96 이라
        ///    맞붙었을 때 중심 사이가 이미 90 이고, 겹침 방지(EnemySeparation 82)가
        ///    그보다 가까이 붙는 것을 막는다. 50 으로 두면 서로 파고들어야만 닿는다.
        /// </summary>
        /// <summary>
        /// 화면 위 캐릭터 크기 배수. 그림 자체를 다시 그리지 않고 상자만 키운다.
        ///
        /// 이 값을 올리면 **닿는 거리도 같이 커져야 한다** — 근접 사거리와 겹침 방지는
        /// 그림 크기에서 나온 값이라, 크기만 키우면 서로 파고들어야만 주먹이 닿는다.
        /// 그래서 두 값에 이 배수를 함께 곱한다.
        /// </summary>
        /// <summary>
        /// 내가 탄 몸의 발사 간격 배수. **적에게는 안 걸린다.**
        ///
        /// 정본 간격(0.78~1.9 초)은 적이 쓰기엔 맞지만 내가 쓰기엔 느리다 —
        /// 이 게임은 멈춰야 쏘는 규칙이라, 멈춘 김에 몇 발 나가야 멈출 맛이 난다.
        /// 캐릭터 사이의 빠르고 느린 차이는 비율이라 그대로 유지된다.
        /// </summary>
        [SerializeField] private float _hostAttackSpeedMul = 0.3f;

        [SerializeField] private float _unitScale = 1.5f;

        /// <summary>
        /// 근접 사거리(px).
        ///
        /// ⚠ **`UnitScale` 을 곱하지 않는다.** 예전에는 「그림이 크면 팔도 길다」로 보고
        ///   몸 크기에 묶어 두었는데, 그러면 **보기 좋으라고 몸만 키울 수가 없다** —
        ///   1.05 → 1.2 로 올리는 순간 근접 사거리도 14 % 같이 늘어 난이도가 바뀐다.
        ///   둘은 다른 이유로 정하는 값이라 따로 둔다.
        /// </summary>
        [SerializeField] private float _meleeAttackRange = 96.6f;

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
        // 원작 탄은 캔버스(24) 안에서 작게 그려져 있다 — 상자를 그 여백만큼 키워야
        // 화면에서 탄으로 보인다. 26 이면 총알이 6px 짜리 점이 된다.
        [SerializeField] private float _shotSize = 104f;
        [SerializeField] private float _shotHitRadius = 34f;
        [SerializeField] private float _shotLifeSeconds = 1.6f;

        [Tooltip("둔화 지속 시간 (설녀 명중 시)")]
        [SerializeField] private float _slowSeconds = 1.6f;

        [Header("액티브 스킬")]
        [Tooltip("호스트 표(`HostTable._activeSkillCooldown`)에 값이 없을 때만 쓰는 기본값. " +
                 "쿨 차등은 호스트 표가 정한다 — 여기서 전원을 같은 값으로 묶지 않는다.")]
        [SerializeField] private float _activeSkillCooldownSeconds = 14f;
        [SerializeField] private int _activeSkillDamage = 140;

        [Header("보상")]
        [SerializeField] private int _rewardGoldPerRoom = 120;
        [SerializeField] private int _rewardGhostExpPerRoom = 8;

        public int StagesPerChapter => Mathf.Max(1, _stagesPerChapter);
        public float ExitTouchRadius => _exitTouchRadius;
        public int HostStartHpPercent => Mathf.Clamp(_hostStartHpPercent, 10, 100);
        public float EmergencyDelaySeconds => _emergencyDelaySeconds;
        public int EmergencyHostHpPercent => Mathf.Clamp(_emergencyHostHpPercent, 5, 100);
        public int EmergencyGhostCost => Mathf.Max(0, _emergencyGhostCost);
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
        public float PossessChannelSeconds => _possessChannelSeconds;
        public float PossessZoom => Mathf.Max(1f, _possessZoom);
        public float PossessRange => _possessRange;
        public int GhostLeaveCostPercent => _ghostLeaveCostPercent;
        public int GhostDeathCostPercent => _ghostDeathCostPercent;
        public float RepossessLockSeconds => _repossessLockSeconds;

        public float AttackResumeSeconds => _attackResumeSeconds;

        public float HostAttackRange => _hostAttackRange;
        public float HostAttackInterval => _hostAttackInterval;
        public float ShieldPerHitPercent => Mathf.Max(0f, _shieldPerHitPercent);
        public int ShieldCapPercent => Mathf.Max(0, _shieldCapPercent);
        public float ShieldHoldSeconds => Mathf.Max(0f, _shieldHoldSeconds);

        /// <summary>초당 녹는 비율(0~1). 0 이면 안 녹는다.</summary>
        public float ShieldDecayPerSecond => Mathf.Max(0, _shieldDecayPercentPerSecond) / 100f;

        public int HostHp(int statHp) => _hostHpBase + Mathf.RoundToInt(statHp * _hostHpPerPoint);
        public int HostAtk(int statAtk) => _hostAtkBase + Mathf.RoundToInt(statAtk * _hostAtkPerPoint);
        public float HostSpeed(int statSpd) => _hostSpeedBase + statSpd * _hostSpeedPerPoint;

        public float EnemyAttackRange => _enemyAttackRange;
        public float EnemyDetectRange => _enemyDetectRange;
        public float HostAttackSpeedMul => Mathf.Clamp(_hostAttackSpeedMul, 0.1f, 3f);
        public float UnitScale => Mathf.Max(0.1f, _unitScale);
        /// <summary>근접 사거리. 몸 크기와 **따로** 간다 — 위 필드 주석 참고.</summary>
        public float MeleeAttackRange => _meleeAttackRange;
        public float EnemySeparation => _enemySeparation * UnitScale;
        public float EnemyAttackInterval => _enemyAttackInterval;
        public int EnemiesPerRoom(int roomIndex)
            => Mathf.Clamp(_enemiesPerRoomMin + roomIndex / 2, _enemiesPerRoomMin, _enemiesPerRoomMax);
        public int EnemyHp(int statHp) => Mathf.Max(1, Mathf.RoundToInt(HostHp(statHp) * _enemyHpScale));

        /// <summary>
        /// 공격 간격 배율 (2026-09-10). 정본 값에 곱한다.
        ///
        /// 2.0(정본의 절반 빈도) → 1.34 → **0.85** 로 두 번 줄였다.
        /// 지형이 촘촘하던 시절에 늦춰 둔 값인데, 방마다 물건 2~3개로 줄이고 나니
        /// 그냥 느리기만 했다.
        /// </summary>
        public float EnemyIntervalMul => Mathf.Max(0.05f, _enemyIntervalMul);

        /// <summary>
        /// 공격 예고 배율 (2026-09-10).
        ///
        /// ⚠ **간격만 줄여서는 체감이 안 바뀐다.** 한 대에 걸리는 시간은
        /// 「간격 + 예고」인데 예고가 0.4~0.8초라 갱스터 한 대가 2.11초였다.
        /// 예고는 피할 틈이므로 0 으로 만들지 않는다 — 0.7 배까지만 줄인다.
        /// </summary>
        public float EnemyWindupMul => Mathf.Clamp(_enemyWindupMul, 0.1f, 1f);

        /// <summary>
        /// 잡몹 체력 전체에 곱하는 값 (2026-09-09 — 「너무 약하다, 2배로」).
        ///
        /// 챕터·방 배율과 **따로** 둔다. 저 둘은 곡선(어느 챕터가 얼마나 센가)이고
        /// 이건 높이(전체를 얼마나 단단하게 볼 것인가)다. 한 표에 섞으면
        /// 곡선을 손볼 때마다 높이가 같이 흔들린다.
        ///
        /// ⚠ 보스는 안 걸린다 — `BossHp` 는 다른 길로 간다.
        /// </summary>
        public float EnemyHpMul => Mathf.Max(0.01f, _enemyHpMul);
        public int EnemyAtk(int statAtk) => Mathf.Max(1, Mathf.RoundToInt(HostAtk(statAtk) * _enemyAtkScale));
        public float EnemySpeed(int statSpd) => HostSpeed(statSpd) * _enemySpeedScale;

        // ── 잡몹이 챕터·방을 따라 세진다 ────────────────────────────
        //
        // ⚠ 예전에는 **배우 스탯 하나로 끝**이었다. `EnemyHp`·`EnemyAtk` 가 챕터를
        //   안 받아서, CH1 아마존과 CH6 아마존이 똑같이 체력 60 이었다.
        //   그런데 보스는 챕터를 따라 오르고(아래 `BossHp`), 플레이어는 60방 내내
        //   레벨업 배율(`RunBuffs`)을 쌓는다 — **잡몹만 제자리**라 후반 방이
        //   허무해졌다(기획 2026-09-08).

        /// <summary>
        /// 챕터마다 잡몹이 세지는 배율.
        ///
        /// **정본 보스 체력 곡선을 그대로 쓴다.** `BossDefTable` 의
        /// 1650 / 2400 / 3150 / 4300 / 5200 / 6900 을 CH1 로 나눈 값이다 —
        /// 보스와 잡몹이 같은 속도로 세져야 방 난이도가 고르게 오른다.
        ///
        /// ⚠ 보스 표의 체력을 고치면 **여기도 같이 고친다.** 두 곡선이 갈라지면
        ///   어떤 챕터는 보스만 세고 어떤 챕터는 잡몹만 세진다.
        /// </summary>
        private static readonly float[] EnemyChapterMuls =
            { 1.00f, 1.45f, 1.91f, 2.61f, 3.15f, 4.18f };

        /// <summary>
        /// 체력에만 따로 곱하는 값. 공격력은 안 건드린다.
        ///
        /// ⚠ **1챕터는 절반이다**(기획 2026-09-08 — "1챕터 애들 피를 반으로 줄여봐
        ///   잡는데 너무 오래 걸려"). 첫 챕터는 아직 레벨업 배율이 하나도 안 쌓인
        ///   때라, 정본 체력 그대로면 한 마리 잡는 데 너무 오래 걸린다.
        ///   **아픈 것은 그대로 두고 무른 것만** 만든다 — 체력만 줄이는 이유다.
        /// </summary>
        private static readonly float[] EnemyChapterHpTrims =
            { 0.50f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f };

        /// <summary>공격력에 쓰는 챕터 배율.</summary>
        public float EnemyChapterMul(int chapter)
            => EnemyChapterMuls[Mathf.Clamp(chapter, 1, EnemyChapterMuls.Length) - 1];

        /// <summary>체력에 쓰는 챕터 배율. 위 배율에 챕터별 체력 손질을 곱한다.</summary>
        public float EnemyChapterHpMul(int chapter)
            => EnemyChapterMul(chapter)
             * EnemyChapterHpTrims[Mathf.Clamp(chapter, 1, EnemyChapterHpTrims.Length) - 1];

        /// <summary>같은 챕터 안에서 방이 뒤로 갈수록 붙는 배율. 001 은 1.00, 009 는 1.20.</summary>
        public float EnemyRoomMul(int roomNo)
            => 1f + EnemyRoomMulSpan * Mathf.Clamp01((Mathf.Clamp(roomNo, 1, 9) - 1) / 8f);

        /// <summary>방 배율의 폭. 챕터 배율(최대 4.18배)에 비하면 양념이다.</summary>
        private const float EnemyRoomMulSpan = 0.20f;

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

        public float ActiveSkillCooldownSeconds => _activeSkillCooldownSeconds;
        public int ActiveSkillDamage => _activeSkillDamage;

        public int RewardGold(int rooms) => _rewardGoldPerRoom * rooms;
        public int RewardGhostExp(int rooms) => _rewardGhostExpPerRoom * rooms;

        // ── 성장 — 숙련도 · 파편 · 고스트 레벨 ────────────────────
        //
        // ⚠ 이 값들은 **여기에만** 있어야 한다. 예전에 코드 상수로 두었던 탓에
        //   숫자 하나 바꾸려면 컴파일을 다시 해야 했다. 밸런스는 플레이하며 잡는다.

        [Header("성장 — 숙련도 · 파편")]
        [Tooltip("숙련도 Lv1~10 을 얻는 데 드는 파편(일반 등급 기준). 한 번도 내려가지 않는다. Lv5(특수 효과 해제)와 Lv10 에서만 크게 튄다 — 문턱이 숫자에서 읽혀야 한다.")]
        [SerializeField] private int[] _shardCurve = { 8, 12, 16, 20, 35, 40, 46, 52, 58, 90 };

        [Tooltip("등급별 요구량 배수. 순서는 HostGrade — B / A / S. 드문 몸일수록 한 단계가 비싸다.")]
        [SerializeField] private float[] _gradeMultiplier = { 1.0f, 1.4f, 2.0f };

        [Tooltip("파편 드롭. x = 그냥 죽였을 때, y = 빙의해 쓰다가 잃었을 때. 잃었을 때가 반드시 더 커야 한다 — 반대면 파밍이 빙의를 벌줘서 플레이어가 핵심 재미를 스스로 피한다.")]
        [SerializeField] private Vector2Int[] _dropByFrequency =
            { new Vector2Int(1, 3), new Vector2Int(2, 6), new Vector2Int(10, 30) };

        [Tooltip("고스트 레벨 상한.")]
        [SerializeField] private int _ghostLevelMax = 50;

        // ── 스탯 성장 ────────────────────────────────────────────
        //
        // 배열 첨자는 `HostStat` 순서다 — Hp · Atk · Crit · AtkSpeed · Range · MoveSpeed.
        //
        // ⚠ 치명타만 단위가 다르다. 나머지 다섯은 **만렙에서의 배율**이고
        //   치명타는 **레벨당 더하는 %p** 다. 확률을 배율로 키우면 Lv10 에
        //   100% 를 넘어 버려서, 같은 배열에 넣되 뜻은 갈라 둔다.

        [Header("스탯 성장 — 주/부 성장폭")]
        [Tooltip("주 성장 스탯의 폭. HP·ATK·공속·사거리·이속은 만렙 배율, 치명타는 레벨당 %p.")]
        [SerializeField] private float[] _statGrowthPrimary   = { 2.4f, 2.0f, 2.5f, 1.35f, 1.15f, 1.20f };

        [Tooltip("부 성장 스탯의 폭. 같은 규칙이다.")]
        [SerializeField] private float[] _statGrowthSecondary = { 1.6f, 1.6f, 1.0f, 1.05f, 1.05f, 1.05f };

        [Tooltip("치명타 피해 배율. 전역 고정이며 스탯이 아니다.")]
        [SerializeField] private float _critMultiplier = 2.0f;

        [Header("사거리 밴드 — 직업 순서: 격투 · 중거리 · 원거리 · 관통")]
        [Tooltip("성장한 사거리의 직업별 상한(m). 이걸 넘으면 직업 판정이 흔들린다.")]
        [SerializeField] private float[] _rangeGrowthMax = { 2.8f, 5.9f, 9.5f, 9.2f };

        public float CritMultiplier => _critMultiplier;

        /// <summary>
        /// 이 스탯의 성장폭. 주 성장 스탯이면 크게, 아니면 작게.
        /// 배열이 짧으면 1(=안 자람)로 떨어진다.
        /// </summary>
        public float StatGrowth(HostStat stat, bool primary)
        {
            var a = primary ? _statGrowthPrimary : _statGrowthSecondary;
            int i = (int)stat;
            return a == null || i < 0 || i >= a.Length ? 1f : a[i];
        }

        /// <summary>
        /// 직업별 사거리 성장 상한(m).
        ///
        /// ⚠ 중거리는 **5.9** 다. 직업 판정 경계가 6.0 이라 여기 닿으면
        ///   성장한 몸이 원거리로 재분류되어 착탄 범위(중거리 상시 규칙)가 사라진다.
        /// </summary>
        // ── 무대 이름 ────────────────────────────────────────────
        //
        // **챕터 하나가 무대 하나다.** 원작 스테이지가 6곳이고 우리도 챕터가 6개다.
        //
        // 예전에는 48방을 챕터 3개 × 앞뒤로 갈라 여섯 구간을 만들었다. 방 표가
        // 6챕터 60방으로 바뀌면서 그 구간 나누기가 남아 CH1 2번 방에 「유령 연구소」
        // 가 떴다 — 유령 연구소는 이제 CH5 다. 구간을 지우고 챕터에 맞춘다.
        //
        // 이름은 주장이 아니라 **방 표의 바닥에서 뽑았다**(챕터별 일반 방 8개 집계):
        //   CH1 junkyard · CH2 missile · CH3 street · CH4 rooftop · CH5 lab · CH6 refinery

        [System.Serializable]
        public struct StageName
        {
            [Tooltip("몇 챕터인가 (1~6).")]
            public int Chapter;
            [Tooltip("이 방 번호부터 이 이름을 쓴다.")]
            public int FromRoom;
            [Tooltip("화면에 뜨는 무대 이름.")]
            public string NameKr;
        }

        [Header("무대 이름 — 챕터 × 방 번호 구간")]
        [SerializeField] private StageName[] _stageNames =
        {
            new StageName { Chapter = 1, FromRoom = 1, NameKr = "쓰레기 집적장" },
            new StageName { Chapter = 2, FromRoom = 1, NameKr = "미사일 저장기지" },
            new StageName { Chapter = 3, FromRoom = 1, NameKr = "밤의 도시 거리" },
            new StageName { Chapter = 4, FromRoom = 1, NameKr = "밤의 공중기지 옥상" },
            new StageName { Chapter = 5, FromRoom = 1, NameKr = "유령 연구소" },
            new StageName { Chapter = 6, FromRoom = 1, NameKr = "야간 정유소" },
        };

        /// <summary>이 챕터·방 번호의 무대 이름. 못 찾으면 빈 문자열.</summary>
        public string StageNameOf(int chapter, int room)
        {
            if (_stageNames == null) return string.Empty;
            string found = string.Empty;
            for (int i = 0; i < _stageNames.Length; i++)
            {
                var e = _stageNames[i];
                if (e.Chapter != chapter || room < e.FromRoom) continue;
                found = e.NameKr;   // 조건을 만족하는 **마지막** 것이 지금 구간이다
            }
            return found;
        }

        public float RangeGrowthMax(int jobIndex)
            => _rangeGrowthMax == null || _rangeGrowthMax.Length == 0
                ? 99f
                : _rangeGrowthMax[Mathf.Clamp(jobIndex, 0, _rangeGrowthMax.Length - 1)];

        [Tooltip("고스트 레벨업 골드 = 기본 + 증가폭 × (레벨 - 1).")]
        [SerializeField] private int _ghostLevelCostBase = 200;
        [SerializeField] private int _ghostLevelCostStep = 60;

        public int GhostLevelMax => Mathf.Max(1, _ghostLevelMax);

        public int GhostLevelCost(int level)
            => _ghostLevelCostBase + _ghostLevelCostStep * Mathf.Max(0, level - 1);

        /// <summary>숙련도 <paramref name="level"/> → 다음 단계 비용 (등급 배수 전).</summary>
        public int ShardCurveAt(int level)
            => _shardCurve == null || level < 0 || level >= _shardCurve.Length ? 0 : _shardCurve[level];

        public int MasteryMax => _shardCurve?.Length ?? 0;

        /// <summary>등급별 요구량 배수. 표가 짧으면 마지막 칸을 쓴다.</summary>
        public float GradeMultiplier(HostGrade grade)
        {
            if (_gradeMultiplier == null || _gradeMultiplier.Length == 0) return 1f;
            int i = Mathf.Clamp((int)grade, 0, _gradeMultiplier.Length - 1);
            return _gradeMultiplier[i];
        }

        /// <summary>드롭 단계 → (죽였을 때, 잃었을 때).</summary>
        public Vector2Int DropAt(int tier)
        {
            if (_dropByFrequency == null || _dropByFrequency.Length == 0) return new Vector2Int(1, 3);
            return _dropByFrequency[Mathf.Clamp(tier, 0, _dropByFrequency.Length - 1)];
        }
    }
}
