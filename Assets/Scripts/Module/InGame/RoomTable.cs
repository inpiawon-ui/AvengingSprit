using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 정본(Developer Handoff v1.5)의 방 데이터를 담는 테이블.
    ///
    /// 이 테이블은 **손으로 채우지 않는다.** `Tools > Game > Import Canon Runtime Data` 가
    /// `Projects/AVSR/Canon/Runtime/CH01_03_RUNTIME_DATA_v1.5.json` 에서 만들어 낸다.
    /// 정본이 갱신되면 JSON 을 갈아 끼우고 다시 임포트한다.
    ///
    /// 좌표는 **미터 그대로** 담는다. 정본의 방은 8.4 × 14 m 이고 카메라가 세로로 따라가는
    /// 구조라, 화면에 맞춰 미리 픽셀로 굽거나 0~1 로 정규화하면 그 정보가 지워진다.
    /// 미터 → 픽셀 환산은 런타임 한 곳에서만 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Room Table", fileName = "RoomTable")]
    public sealed class RoomTable : ScriptableObject
    {
        [SerializeField] private string _contractVersion;
        [SerializeField] private RoomEntry[] _rooms = Array.Empty<RoomEntry>();

        public string ContractVersion => _contractVersion;
        public IReadOnlyList<RoomEntry> Rooms => _rooms;

        public RoomEntry Get(string roomId)
        {
            for (int i = 0; i < _rooms.Length; i++)
                if (_rooms[i].RoomId == roomId) return _rooms[i];
            return null;
        }

        /// <summary>챕터의 첫 방. 정본 `chapters[].startNodeId` 다.</summary>
        public RoomEntry FirstOf(int chapter)
        {
            for (int i = 0; i < _rooms.Length; i++)
                if (_rooms[i].Chapter == chapter && _rooms[i].IsChapterStart) return _rooms[i];
            return null;
        }
    }

    [Serializable]
    public sealed class RoomEntry
    {
        [SerializeField] private string _roomId;
        [SerializeField] private int _chapter;
        [SerializeField] private string _type;          // Gate / Standard / Elite / Boss / Rest ...
        [SerializeField] private string _route;         // MAIN / BRANCH_A ...
        [SerializeField] private string _intent;
        [SerializeField] private bool _isChapterStart;

        [Header("방 크기 (미터). 정본 layout.layouts")]
        /// <summary>
        /// 이 방의 지형지물을 **손으로 배치했는가.**
        ///
        /// 정본(ROOM_GEOMETRY)은 `Template` 과 `CoverCount` 만 줄 뿐 엄폐물의 자리·크기·종류를
        /// 주지 않는다. 그래서 임포터가 자동으로 만들어 넣는데, 맵툴에서 손으로 고친 방까지
        /// 다시 덮으면 작업이 통째로 날아간다. 이 표시가 켜진 방은 임포터가 지형지물을 건드리지 않는다.
        /// (스폰·웨이브·보상 같은 **정본이 실제로 주는 값**은 그대로 갱신된다.)
        /// </summary>
        [SerializeField] private bool _handEdited;

        [SerializeField] private float _width = 8.4f;
        [SerializeField] private float _height = 14f;
        [Tooltip("VERTICAL_FOLLOW 면 방이 화면보다 높아 카메라가 세로로 따라간다")]
        [SerializeField] private string _cameraMode;
        [Tooltip("정본 v3.3 지오메트리 템플릿 — TWIN_PLATFORM / RING / OFFSET_COVER / " +
                 "SPLIT_LEVEL / PILLAR_CROSS / LANE_WIDE. 바닥 그림을 고르는 데 쓴다")]
        [SerializeField] private string _template;

        [Header("출입")]
        [SerializeField] private Vector2 _entry;
        [Tooltip("출구. 갈래가 둘이면 두 개다(CH2_N03 → N04A · N04B).\n" +
                 "비어 있으면 챕터의 끝이다(정본의 CHAPTER_CLEAR·GAME_SLICE_CLEAR).")]
        [SerializeField] private ExitEntry[] _exits = Array.Empty<ExitEntry>();
        [SerializeField] private string _unlockRule;

        [Header("보스 (보스방만)")]
        [Tooltip("정본 layout.bossLayouts. 보스는 enemySpawns 에 없다 — 페이즈별로 자리가 다르다")]
        [SerializeField] private string _bossId;
        [SerializeField] private string _bossName;
        [SerializeField] private Vector2 _bossAt;
        [SerializeField] private int _bossHp;
        [SerializeField] private int _bossAtk;
        [SerializeField] private float _bossMoveSpeed;
        [Tooltip("페이즈가 바뀌는 체력 비율. 정본 bossPhases 의 HPStart 를 내림차순으로 담는다")]
        [SerializeField] private float[] _bossPhaseGates = Array.Empty<float>();
        [SerializeField] private BossPhaseEntry[] _bossPhases = Array.Empty<BossPhaseEntry>();

        // 정본 v2.3 BOSS_ATTACK_RUNTIME — 보스마다 제 공격 4가지.
        // 이것이 없으면 `BossTable`(챕터당 하나)의 목록을 쓰게 되어
        // **같은 챕터의 중간 보스와 최종 보스가 똑같이 싸운다.**
        [SerializeField] private Game.Character.BossMove[] _bossMoves =
            Array.Empty<Game.Character.BossMove>();

        [Header("스폰")]
        // 정본 v3.3 ROOM_REWARD — 방마다 붙는 보상.
        // 이것이 있어야 판 안에서 쓸 골드가 생기고, 이벤트·상점이 값을 가진다.
        [SerializeField] private int _gold;
        [SerializeField] private int _exp;
        [SerializeField] private int _healPct;

        [SerializeField] private Vector2 _playerSpawn;
        [SerializeField] private SpawnEntry[] _spawns = Array.Empty<SpawnEntry>();
        [SerializeField] private ObjectEntry[] _objects = Array.Empty<ObjectEntry>();

        public string RoomId => _roomId;
        public int Chapter => _chapter;
        public string Type => _type;
        public string Route => _route;
        public string Intent => _intent;
        public bool IsChapterStart => _isChapterStart;
        public bool HandEdited => _handEdited;
        public float Width => _width;
        public float Height => _height;
        public string CameraMode => _cameraMode;
        public string Template => _template;
        public Vector2 Entry => _entry;
        public IReadOnlyList<ExitEntry> Exits => _exits;
        public string UnlockRule => _unlockRule;
        public string BossId => _bossId;
        public string BossName => _bossName;
        public Vector2 BossAt => _bossAt;
        public int BossHp => _bossHp;
        public int BossAtk => _bossAtk;
        public float BossMoveSpeed => _bossMoveSpeed;
        public IReadOnlyList<float> BossPhaseGates => _bossPhaseGates;
        public IReadOnlyList<BossPhaseEntry> BossPhases => _bossPhases;
        public IReadOnlyList<Game.Character.BossMove> BossMoves => _bossMoves;

        public BossPhaseEntry BossPhase(int phase)
        {
            for (int i = 0; i < _bossPhases.Length; i++)
                if (_bossPhases[i].Phase == phase) return _bossPhases[i];
            return null;
        }

        /// <summary>더 갈 곳이 없는 방. 챕터의 마지막이다.</summary>
        public bool IsChapterEnd => _exits == null || _exits.Length == 0;

        /// <summary>갈림길인가. 정본에서는 챕터마다 한 번씩 나온다(CH2_N03 · CH3_N03).</summary>
        public bool IsBranch => _exits != null && _exits.Length > 1;
        public int Gold => _gold;
        public int Exp => _exp;
        public int HealPct => _healPct;
        public Vector2 PlayerSpawn => _playerSpawn;
        public IReadOnlyList<SpawnEntry> Spawns => _spawns;
        public IReadOnlyList<ObjectEntry> Objects => _objects;

        /// <summary>보스 방인가. 정본의 타입 문자열은 "Boss Arena" 다.</summary>
        public bool IsBoss => !string.IsNullOrEmpty(_bossId);
    }

    /// <summary>
    /// 적 하나의 배치. 정본 `layout.enemySpawns` 한 줄이다.
    /// 스폰 출처는 이것 하나뿐이며 최상위 `spawns` 는 정본에서 제거됐다(SPAWN_SRC_01).
    /// </summary>
    [Serializable]
    public sealed class SpawnEntry
    {
        [Tooltip("엘리트인가. 수가 적은 대신 하나하나가 세다")]
        [SerializeField] private bool _elite;
        [SerializeField] private string _spawnId;
        [Tooltip("E001 · EL01 · B01 같은 정본 ID")]
        [SerializeField] private string _actorId;
        [SerializeField] private Vector2 _at;
        [SerializeField] private string _facing;
        [SerializeField] private float _delaySeconds;
        [Tooltip("ROOM_START / WAVE_CLEAR / TRIGGER ...")]
        [SerializeField] private string _trigger;
        [SerializeField] private string _telegraph;

        /// <summary>
        /// 엘리트인가. 예전에는 <c>ActorId</c> 가 <c>EL</c> 로 시작하는지로 봤다 —
        /// 정본이 엘리트를 별도 ID(EL01…)로 줬기 때문이다. 이제 자리는 배정표가
        /// 정하고 배우는 잡몹·호스트 키라, **표시를 따로 들고 있어야** 한다.
        /// </summary>
        public bool Elite => _elite;
        public string SpawnId => _spawnId;
        public string ActorId => _actorId;
        public Vector2 At => _at;
        public string Facing => _facing;
        public float DelaySeconds => _delaySeconds;
        public string Trigger => _trigger;

        /// <summary>
        /// 정본이 "이놈이 네 다음 몸이다" 라고 찍어 둔 자리인가 (`POSSESSION_TARGET`).
        ///
        /// 정본은 46기를 이렇게 찍어 두었고 **전부 웨이브 1**에 있다. 증원(139기)에는
        /// 하나도 없다 — 실수가 아니라 "증원 오기 전에 몸을 갈아타 둬라" 는 뜻이다.
        /// 그 의도가 화면에 안 보이면 증원은 그냥 기습이 된다.
        /// </summary>
        public bool IsPossessionTarget => _trigger == "POSSESSION_TARGET";
        public string Telegraph => _telegraph;
    }

    /// <summary>
    /// 보스 페이즈 하나. 정본 `bossPhases` 한 줄이다.
    ///
    /// 정본이 가장 세게 못박은 것이 "페이즈마다 **행동이** 바뀐다" 는 것이다 —
    /// 수치만 올라가는 것은 페이즈가 아니라고 못박혀 있다.
    /// </summary>
    [Serializable]
    public sealed class BossPhaseEntry
    {
        [SerializeField] private int _phase;
        [Tooltip("이 페이즈가 시작되는 체력 비율. P1 은 1.0")]
        [SerializeField] private float _hpStart = 1f;
        [Tooltip("정본 AttackPattern 문자열. 지금은 표시·기록용이고 구현은 아래 값들로 흉내낸다")]
        [SerializeField] private string _pattern;
        [Tooltip("예고 시간(초). 정본 Telegraph 문구에서 뽑아낸 값")]
        [SerializeField] private float _telegraphSeconds = 0.45f;
        [Tooltip("이 페이즈에 부르는 잡몹. 비어 있으면 안 부른다")]
        [SerializeField] private string[] _minionPool = Array.Empty<string>();
        [Tooltip("정본 SwitchWindowCount — 이 페이즈가 만들어야 하는 교체 기회의 수")]
        [SerializeField] private int _switchWindows;
        [SerializeField] private string _arenaBehavior;

        public int Phase => _phase;
        public float HpStart => _hpStart;
        public string Pattern => _pattern;
        public float TelegraphSeconds => _telegraphSeconds <= 0f ? 0.45f : _telegraphSeconds;
        public IReadOnlyList<string> MinionPool => _minionPool;
        public int SwitchWindows => _switchWindows;
        public string ArenaBehavior => _arenaBehavior;
    }

    /// <summary>출구 하나. 갈림길 방은 이것이 둘이고, 어느 쪽으로 나가느냐가 곧 선택이다.</summary>
    [Serializable]
    public sealed class ExitEntry
    {
        // 정본 ROUTE 의 `RouteArchetype` 과 점수. 갈림길에서 **문 위에 적어 준다** —
        // 둘 다 똑같이 생긴 문이면 고르는 것이 아니라 찍는 것이 된다.
        [SerializeField] private string _archetype;
        [SerializeField] private int _risk;
        [SerializeField] private int _reward;
        [SerializeField] private int _recovery;
        [SerializeField] private int _build;

        [SerializeField] private string _exitId;
        [SerializeField] private Vector2 _at;
        [SerializeField] private string _nextRoomId;

        public string ExitId => _exitId;
        public Vector2 At => _at;
        public string NextRoomId => _nextRoomId;
        public string Archetype => _archetype;
        public int Risk => _risk;
        public int Reward => _reward;
        public int Recovery => _recovery;
        public int Build => _build;
    }

    /// <summary>
    /// 방 안의 지형지물. 정본 `layout.objects` 한 줄이다.
    ///
    /// 이것이 없으면 `Pillar`·`Hazard Lane` 같은 방 이름이 이름값을 못 한다 —
    /// 전부 빈 사각형이 되어 방마다 다른 점이 적 배치뿐이게 된다.
    /// </summary>
    [Serializable]
    public sealed class ObjectEntry
    {
        [SerializeField] private string _objectId;
        [Tooltip("PILLAR · BARRICADE · LOW_COVER · HAZARD · DIVIDER · RICOCHET_WALL")]
        [SerializeField] private string _kind;
        [Tooltip("중심 좌표(미터)")]
        [SerializeField] private Vector2 _at;
        [Tooltip("가로·세로 크기(미터)")]
        [SerializeField] private Vector2 _size;

        [SerializeField] private bool _blocksMove;
        [SerializeField] private bool _blocksShot;
        /// <summary>
        /// **적** 탄도 막는가. 꺼져 있으면 적 탄만 넘어간다.
        ///
        /// 궁수의 전설 엄폐물 설계의 핵심이 이 비대칭이다 —
        /// "적 투사체는 대부분 장애물을 넘어가지만 플레이어의 화살은 넘어가지 못한다".
        /// 그래야 엄폐물이 **숨는 곳**이 아니라 **쏠 자리를 찾게 만드는 것**이 된다.
        /// 양쪽 다 막으면 그냥 벽이라, 뒤에 붙어 서 있기만 하면 끝난다.
        ///
        /// 키 큰 것(기둥·칸막이·상자)은 켠다. 낮은 것(낮은 벽·바리케이드)은 끈다.
        /// </summary>
        [SerializeField] private bool _blocksEnemyShot = true;

        [SerializeField] private bool _blocksSight;
        [Tooltip("부술 수 있는가. 정본에서 바리케이드만 true 다")]
        [SerializeField] private bool _destructible;

        [Header("해저드")]
        [Tooltip("NONE 이면 해저드가 아니다")]
        [SerializeField] private string _hazardKind;
        [SerializeField] private int _hazardDamage;
        [SerializeField] private float _hazardTick;

        [Tooltip("정본 14열. 종류마다 고정값(기둥 0.45 · 바리케이드 0.5 · 해저드 0.8)인데 " +
                 "계약에 이름이 없어 뜻을 확정하지 못했다. 담아만 두고 쓰지 않는다.")]
        [SerializeField] private float _unnamed;

        public string ObjectId => _objectId;
        public string Kind => _kind;
        public Vector2 At => _at;
        public Vector2 Size => _size;
        public bool BlocksMove => _blocksMove;
        public bool BlocksShot => _blocksShot;
        public bool BlocksEnemyShot => _blocksEnemyShot;
        public bool BlocksSight => _blocksSight;
        public bool Destructible => _destructible;
        public bool IsHazard => !string.IsNullOrEmpty(_hazardKind) && _hazardKind != "NONE";
        public int HazardDamage => _hazardDamage;
        public float HazardTick => _hazardTick <= 0f ? 1f : _hazardTick;
    }

}
