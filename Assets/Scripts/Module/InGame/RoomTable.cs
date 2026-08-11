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
        [SerializeField] private float _width = 8.4f;
        [SerializeField] private float _height = 14f;
        [Tooltip("VERTICAL_FOLLOW 면 방이 화면보다 높아 카메라가 세로로 따라간다")]
        [SerializeField] private string _cameraMode;

        [Header("출입")]
        [SerializeField] private Vector2 _entry;
        [SerializeField] private Vector2 _exit;
        [Tooltip("다음 방. 갈래가 둘이면 두 개다(CH2_N03 → N04A · N04B).\n" +
                 "비어 있으면 챕터의 끝이다(정본의 CHAPTER_CLEAR·GAME_SLICE_CLEAR).")]
        [SerializeField] private string[] _nextRoomIds = Array.Empty<string>();
        [SerializeField] private string _unlockRule;

        [Header("보스 (보스방만)")]
        [Tooltip("정본 layout.bossLayouts. 보스는 enemySpawns 에 없다 — 페이즈별로 자리가 다르다")]
        [SerializeField] private string _bossId;
        [SerializeField] private Vector2 _bossAt;

        [Header("스폰")]
        [SerializeField] private Vector2 _playerSpawn;
        [SerializeField] private SpawnEntry[] _spawns = Array.Empty<SpawnEntry>();
        [SerializeField] private WaveEntry[] _waves = Array.Empty<WaveEntry>();
        [SerializeField] private ObjectEntry[] _objects = Array.Empty<ObjectEntry>();

        public string RoomId => _roomId;
        public int Chapter => _chapter;
        public string Type => _type;
        public string Route => _route;
        public string Intent => _intent;
        public bool IsChapterStart => _isChapterStart;
        public float Width => _width;
        public float Height => _height;
        public string CameraMode => _cameraMode;
        public Vector2 Entry => _entry;
        public Vector2 Exit => _exit;
        public IReadOnlyList<string> NextRoomIds => _nextRoomIds;
        public string UnlockRule => _unlockRule;
        public string BossId => _bossId;
        public Vector2 BossAt => _bossAt;

        /// <summary>더 갈 곳이 없는 방. 챕터의 마지막이다.</summary>
        public bool IsChapterEnd => _nextRoomIds == null || _nextRoomIds.Length == 0;
        public Vector2 PlayerSpawn => _playerSpawn;
        public IReadOnlyList<SpawnEntry> Spawns => _spawns;
        public IReadOnlyList<WaveEntry> Waves => _waves;
        public IReadOnlyList<ObjectEntry> Objects => _objects;

        /// <summary>
        /// 이 방의 마지막 웨이브 번호. **스폰만 보고 센다.**
        ///
        /// 정본의 웨이브 표에는 2웨이브라고 적혀 있는데 실제 스폰은 1웨이브뿐인 방이
        /// 10개 있다(CH1_N08 등). 표를 믿으면 그 방들은 아무도 안 나오는 빈 웨이브를
        /// 기다리며 몇 초씩 멈춘다. 스폰이 유일한 런타임 출처다(SPAWN_SRC_01).
        /// 웨이브 표는 시작 지연 값만 쓴다.
        /// </summary>
        public int LastWave
        {
            get
            {
                int n = 1;
                for (int i = 0; i < _spawns.Length; i++) n = Mathf.Max(n, _spawns[i].Wave);
                return n;
            }
        }

        public WaveEntry Wave(int index)
        {
            for (int i = 0; i < _waves.Length; i++)
                if (_waves[i].Index == index) return _waves[i];
            return null;
        }

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
        [SerializeField] private int _wave;
        [SerializeField] private string _spawnId;
        [Tooltip("E001 · EL01 · B01 같은 정본 ID")]
        [SerializeField] private string _actorId;
        [SerializeField] private Vector2 _at;
        [SerializeField] private string _facing;
        [SerializeField] private float _delaySeconds;
        [Tooltip("ROOM_START / WAVE_CLEAR / TRIGGER ...")]
        [SerializeField] private string _trigger;
        [SerializeField] private string _telegraph;

        public int Wave => _wave;
        public string SpawnId => _spawnId;
        public string ActorId => _actorId;
        public Vector2 At => _at;
        public string Facing => _facing;
        public float DelaySeconds => _delaySeconds;
        public string Trigger => _trigger;
        public string Telegraph => _telegraph;
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
        public bool BlocksSight => _blocksSight;
        public bool Destructible => _destructible;
        public bool IsHazard => !string.IsNullOrEmpty(_hazardKind) && _hazardKind != "NONE";
        public int HazardDamage => _hazardDamage;
        public float HazardTick => _hazardTick <= 0f ? 1f : _hazardTick;
    }

    [Serializable]
    public sealed class WaveEntry
    {
        [SerializeField] private int _index;
        [SerializeField] private float _startDelay;
        [Tooltip("E001x2,E002x1 — 검증용. 실제 스폰은 SpawnEntry 가 만든다")]
        [SerializeField] private string _composition;
        [SerializeField] private string _clearRule;

        public int Index => _index;
        public float StartDelay => _startDelay;
        public string Composition => _composition;
        public string ClearRule => _clearRule;
    }
}
