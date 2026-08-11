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
