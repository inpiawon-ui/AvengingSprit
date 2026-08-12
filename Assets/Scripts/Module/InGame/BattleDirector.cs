using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 전투 진행 전체를 맡는다 — 룸 생성, 유닛 AI, 빙의, 얼티밋, 종료 판정.
    ///
    /// 게임 흐름(GameComposition 4절)
    ///   룸 입장 → 오토어택 교전 → [호스트 사망] 고스트 복귀 → 재빙의
    ///   → 전멸 → 룸 클리어 → 다음 룸 → (마지막 룸) 보스 → 스테이지 클리어
    ///
    /// 상태는 전부 이 클래스가 들고, 화면 표시는 이벤트로만 흘려보낸다.
    /// UI 가 이 클래스를 직접 참조하지 않아야 룸 로직을 UI 없이 테스트할 수 있다.
    /// </summary>
    public sealed class BattleDirector : MonoBehaviour
    {
        private const string AtlasAddress = "atlas/ingamemainui";

        /// <summary>
        /// 캐릭터 아틀라스 주소 접두사. 캐릭터 한 종이 아틀라스 하나다
        /// (`Assets/BaseResource/Unit/{key}/` ↔ `atlas/unit_{key}`).
        ///
        /// 화면 아틀라스에 섞지 않는 이유: 한 방에 실제로 나오는 캐릭터는 몇 종뿐인데
        /// 통짜 아틀라스는 12종을 전부 메모리에 올린다. 방향 5장에 공격 프레임까지
        /// 붙으면 한 종이 20장이 되어 감당이 안 된다.
        /// </summary>
        private const string UnitAtlasPrefix = "atlas/unit_";

        private RectTransform _field;
        private RectTransform _unitLayer;
        private GameConfig _config;
        private IPlayerDataService _player;
        private SpriteAtlas _atlas;                                        // HUD·탄·바닥
        private readonly Dictionary<string, SpriteAtlas> _unitAtlas = new();  // 캐릭터 키 → 아틀라스
        private IEventBus _bus;

        private Unit _ghost;
        private Unit _host;                       // 빙의 중이 아니면 null
        private readonly List<Unit> _enemies = new();
        private readonly List<Unit> _dead = new();   // 정리용 재사용 버퍼 (hot path 할당 금지)

        /// <summary>
        /// 사망 연출이 도는 몸. `_enemies` 에서는 이미 빠져 있어 표적도 충돌도 되지 않고,
        /// 그림만 남아 쓰러지다 사라진다. 연출이 끝나면 여기서 빼고 없앤다.
        /// </summary>
        private readonly List<Unit> _dying = new();

        /// <summary>피해 수치. 탄과 같은 풀 방식 — 타격마다 만들면 교전 중 GC 가 튄다.</summary>
        private readonly List<DamageText> _damageTexts = new();
        private RectTransform _textLayer;

        /// <summary>탄이 터지는 그림. 탄과 같은 풀 방식이다.</summary>
        private const int MaxImpacts = 24;
        private readonly List<Impact> _impacts = new();

        private const int MaxShots = 64;
        private readonly List<Projectile> _shots = new();
        private RectTransform _shotLayer;
        private RectTransform _fieldLayer;

        private int _roomIndex = -1;
        private int _ghostHp;
        /// <summary>유령 자연 감소의 소수점 이월. 프레임마다 반올림하면 3/초가 안 맞는다.</summary>
        private float _drainCarry;
        private float _invuln;
        private float _ghostProtect;
        /// <summary>열린 문 하나. 갈림길 방은 둘이고 어느 쪽으로 나가느냐가 곧 선택이다.</summary>
        private sealed class ExitGate
        {
            public RectTransform View;
            public string NextRoomId;
        }

        private readonly List<ExitGate> _exits = new();
        private float _ultimateCharge;
        private bool _running;
        private Unit _possessTarget;
        private bool _hadPossessTarget;
        private bool _hadPossessBlocked;

        // ── 유지 훅 ───────────────────────────────────────────────
        // 정본이 "fun-critical" 로 못박은 장치다. 몸을 오래 탈수록 그 몸에서만
        // 쌓이는 것이 생기고, 갈아타면 사라진다.
        //
        // 이게 없으면 전술 빙의는 **비용만 있고 잃는 게 없는** 선택이 된다.
        // 값을 내는 쪽만 있으면 "안 바꾸는" 것이 언제나 정답이라, 물음 자체가 성립하지 않는다.
        //
        // 정본은 훅의 **이름**만 준다(표식 릴레이·콤보 미터…). 실제 효과는 그 몸의
        // 시그니처를 구현해야 나오므로, 지금은 공통 규칙 하나로 대신한다 —
        // 명중이 쌓이면 단계가 오르고 단계마다 피해가 는다. 모양은 같다.
        private const int MaintainMaxStack = 3;
        private const int MaintainHitsPerStack = 8;
        private const float MaintainDamagePerStack = 0.12f;

        private int _maintainHits;
        private int _maintainStack;

        // ── 시너지 S01 · 갱스터 → 닌자 ────────────────────────────
        // 정본이 "버프 없이 항상 발동(ALWAYS_BASE)" 으로 못박은 대표 사례다.
        // 갱스터로 표식을 찍고 닌자로 갈아타면 표식 대상에 순간이동 처형이 나간다.
        //
        // 시너지는 **단방향**이다. 갱스터 → 닌자는 되고 닌자 → 갱스터는 안 된다.
        // 이전 몸이 세상에 남긴 것을 다음 몸이 물려받는 구조라 방향이 뒤집히면 성립하지 않는다.
        //
        // 표식은 적에게 붙는다. 몸을 갈아타도 사라지지 않아야 시너지가 성립한다 —
        // 유지 훅(내 몸에 쌓이는 것)은 교체하면 사라지지만, 세상에 남긴 것은 남는다.
        private const string SynergyMarkSource = "gangster";
        private const string SynergyMarkReceiver = "ninja";
        private const string SynergyS01 = "S01";
        private const float MarkSeconds = 6f;
        private const float BlinkRangeMul = 2.2f;      // 순간이동이라 평소 사거리보다 멀리 닿는다
        private const float BlinkDamageMul = 2.5f;
        private const float BlinkAoeRadius = 150f;
        private const float BlinkCooldown = 1.6f;

        private float _blinkCooldown;
        private readonly HashSet<string> _synergySeen = new();
        private float _tacticalCooldown;
        private int _tacticalShown = -1;

        private float _stopTimer;

        private const float ChargeSeconds = 0.9f;
        private const float ChargeSpeedMul = 5.5f;
        private const int MaxRoomUnits = 14;
        private const float SummonRadius = 200f;

        /// <summary>적 배치 띠 — 필드 높이 대비. 아래쪽은 플레이어 시작 위치를 위해 비운다.</summary>
        private const float EnemyBandTop = 0.06f;
        private const float EnemyBandBottom = 0.44f;
        /// <summary>플레이어 시작 높이. 적 띠 끝과 탐지 거리보다 멀어야 첫 프레임에 안 달려든다.</summary>
        private const float PlayerStartY = 0.88f;

        // ── 방 규격 · 세로 따라가는 카메라 ────────────────────────
        // 정본의 방은 **8.4 × 14 m** 이고 34방 전부 카메라 모드가 `VERTICAL_FOLLOW` 다
        // (보스방만 16 m). 가로는 화면에 다 들어가고 세로가 화면보다 길다.
        //
        // 이걸 한 화면에 눌러 담으면 세로 거리가 0.67배로 찌그러져 사거리·회피 간격이
        // 전부 달라진다. 정본 좌표를 쓰는 의미가 사라지므로, 방을 실제 크기로 두고
        // 카메라가 따라간다.
        //
        // `RoomField` 가 보이는 창(뷰포트)이고 `UnitLayer` 가 방 전체다.
        // 창은 그대로 두고 방을 세로로 밀어 카메라를 흉내낸다.
        private const float RoomMeterWidth = 8.4f;
        private const float RoomMeterHeight = 14f;
        private const float BossRoomMeterHeight = 16f;

        /// <summary>카메라가 따라붙는 속도. 즉시 붙이면 걸음마다 화면이 튄다.</summary>
        private const float CameraFollow = 8f;

        private RectTransform _floor;
        private Image _floorImage;
        private Sprite _defaultFloor;
        private float _pxPerMeter = 1f;
        private Vector2 _roomSize;      // 픽셀
        private float _scroll;
        /// <summary>밀어내기 속도 — 이동 속도 대비. 너무 크면 서로 튕겨 나간다.</summary>
        private const float SeparationSpeedRatio = 0.55f;

        private BossTable _bossTable;
        private readonly BossBrain _brain = new();
        private Unit _boss;
        private float _telegraphPulse;
        private float _chargeDamageMul = 1f;

        private BuffTable _buffTable;
        private readonly RunBuffs _buffs = new();
        private readonly List<BuffEntry> _offer = new();
        private readonly System.Random _rng = new();
        private bool _awaitingBuff;

        /// <summary>런 레벨. 적을 잡아 EXP 를 모으고, 차면 버프 3택1 이 열린다 (기획서 A 5-2).</summary>
        private int _level = 1;
        private int _exp;

        /// <summary>빙의할 대상이 하나도 없는 상태가 이어진 시간 (기획서 A 8-3).</summary>
        private float _emergencyWait;
        private bool _emergencyUsedThisRoom;
        private RoomKind _roomKind = RoomKind.Normal;

        /// <summary>이 런에 쌓인 버프. 스테이지를 나가면 사라진다.</summary>
        public RunBuffs Buffs => _buffs;
        public bool IsAwaitingBuff => _awaitingBuff;

        public Vector2 MoveInput { get; set; }

        /// <summary>지금 사격 중인가. 멈춰서 사거리 안에 적이 있을 때만 true (궁수의 전설 규칙).</summary>
        public bool IsFiring { get; private set; }
        public float UltimateRatio => _config == null ? 0f
            : Mathf.Clamp01(_ultimateCharge / _config.UltimateChargeSeconds);
        public bool CanPossess => _host == null && _possessTarget != null;
        public bool IsRunning => _running;

        public async UniTask BootAsync(RectTransform field, RectTransform unitLayer)
        {
            _field = field;
            _unitLayer = unitLayer;

            // 방이 창보다 크므로 잘라 내야 한다. 없으면 화면 밖 적이 상단 HUD 위에 그려진다.
            if (_field.GetComponent<RectMask2D>() == null) _field.gameObject.AddComponent<RectMask2D>();

            // 바닥도 방의 일부다. 바닥만 제자리에 두면 카메라가 움직이는 것이 아니라
            // **물건들이 미끄러지는 것**으로 보인다 — 기준이 없으면 이동을 읽을 수 없다.
            var floorT = _field.Find("RoomFloor") as RectTransform;
            if (floorT != null)
            {
                _floor = floorT;
                _floorImage = _floor.GetComponent<Image>();
                // 세로로 길어진 방을 늘려 채우면 바닥 무늬가 뭉개진다. 타일로 반복한다.
                if (_floorImage != null)
                {
                    _floorImage.type = Image.Type.Tiled;
                    _defaultFloor = _floorImage.sprite;
                }
            }
            _pxPerMeter = _field.rect.width / RoomMeterWidth;
            SetRoomSize(RoomMeterHeight);
            _bus = CoreModule.Get<IEventBus>();
            CoreModule.TryGet(out _player);

            var res = CoreModule.Get<IResourceManager>();
            await LoadPanelAtlasAsync(res);
            try { _atlas = await res.LoadAsync<SpriteAtlas>(AtlasAddress); }
            catch (Exception e) { Debug.LogError($"[Battle] 아틀라스 로드 실패 — {e.Message}"); }
            try { _config = await res.LoadAsync<GameConfig>("TableData/GameConfig"); }
            catch (Exception e) { Debug.LogError($"[Battle] GameConfig 로드 실패 — {e.Message}"); }
            if (_config == null) return;

            try { _rooms = await res.LoadAsync<RoomTable>("TableData/RoomTable"); }
            catch (Exception e) { Debug.LogWarning($"[Battle] RoomTable 없음 — 절차적 생성으로 간다. {e.Message}"); }
            try { _bossTable = await res.LoadAsync<BossTable>("TableData/BossTable"); }
            catch (Exception e) { Debug.LogError($"[Battle] BossTable 로드 실패 — {e.Message}"); }
            try { _buffTable = await res.LoadAsync<BuffTable>("TableData/BuffTable"); }
            catch (Exception e) { Debug.LogError($"[Battle] BuffTable 로드 실패 — {e.Message}"); }

            // 스폰은 동기 코드다. 테이블이 다 올라온 뒤에 이 런이 쓸 캐릭터를 먼저 올린다.
            await LoadUnitAtlasesAsync(res, RunUnitKeys());
            _buffs.Clear();   // 버프는 런 한정 — 스테이지 진입마다 초기화한다

            // 장판은 바닥에 깔린다 — 유닛보다 **아래**다. 위에 그리면 캐릭터가
            // 장판에 잠겨 어디 서 있는지 안 보인다.
            var fieldGo = new GameObject("FieldLayer", typeof(RectTransform));
            fieldGo.transform.SetParent(_unitLayer.parent, false);
            _fieldLayer = (RectTransform)fieldGo.transform;
            _fieldLayer.anchorMin = _unitLayer.anchorMin;
            _fieldLayer.anchorMax = _unitLayer.anchorMax;
            _fieldLayer.pivot = _unitLayer.pivot;
            _fieldLayer.anchoredPosition = _unitLayer.anchoredPosition;
            _fieldLayer.sizeDelta = _unitLayer.sizeDelta;
            _fieldLayer.SetSiblingIndex(_unitLayer.GetSiblingIndex());

            // 탄은 유닛보다 위에 그린다 — 유닛 뒤로 숨으면 피격 판단이 안 보인다
            var shotGo = new GameObject("ShotLayer", typeof(RectTransform));
            shotGo.transform.SetParent(_unitLayer.parent, false);
            _shotLayer = (RectTransform)shotGo.transform;
            _shotLayer.anchorMin = _unitLayer.anchorMin;
            _shotLayer.anchorMax = _unitLayer.anchorMax;
            _shotLayer.pivot = _unitLayer.pivot;
            _shotLayer.anchoredPosition = _unitLayer.anchoredPosition;
            _shotLayer.sizeDelta = _unitLayer.sizeDelta;

            // 피해 수치는 탄보다 위에 그린다 — 탄에 가리면 읽을 수 없다.
            var textGo = new GameObject("DamageTextLayer", typeof(RectTransform));
            textGo.transform.SetParent(_unitLayer.parent, false);
            _textLayer = (RectTransform)textGo.transform;
            _textLayer.anchorMin = _unitLayer.anchorMin;
            _textLayer.anchorMax = _unitLayer.anchorMax;
            _textLayer.pivot = _unitLayer.pivot;
            _textLayer.anchoredPosition = _unitLayer.anchoredPosition;
            _textLayer.sizeDelta = _unitLayer.sizeDelta;

            SpawnGhost();
            EnterStartHost();
            EnterRoom(0);
            _running = true;
        }

        // ─────────────────────────────────────────────────────────
        private void SpawnGhost()
        {
            _ghostHp = GhostHpMax;
            _tacticalCooldown = 0f;      // 런은 언제나 교체 가능한 상태로 시작한다
            _eliteRoomsCleared = 0;
            _synergySeen.Clear();
            _blinkCooldown = 0f;
            _tacticalShown = -1;
            _ghost = NewUnit("Ghost");
            _ghost.Setup(UnitSide.Player, "ghost", "GHOST", UnitGet("ghost"),
                         GhostHpMax, 0, _config.GhostMoveSpeed, 0f, 1f,
                         new Vector2(72f, 90f));
            _ghost.Position = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * PlayerStartY);
            PublishHp();
        }

        /// <summary>
        /// 로비에서 고른 호스트를 입고 시작한다.
        /// 캐릭터를 골라 놓고 유령으로 떨어지면 그 선택이 화면에 나타나지 않는다.
        ///
        /// 빼앗은 몸(빙의 70%)과 달리 **체력은 가득** 채운다 — 훔친 몸이 아니라
        /// 데려온 몸이다. 몸을 잃으면 그때부터 유령이 되고, 기존 흐름(빙의·긴급 투입)이
        /// 그대로 이어진다.
        ///
        /// 고른 호스트가 없으면(데이터 미준비 등) 아무것도 하지 않는다 —
        /// 유령으로 시작하던 예전 흐름 그대로다.
        /// </summary>
        private void EnterStartHost()
        {
            var entry = PickPlayerHost();
            if (entry == null)
            {
                // 조용히 유령으로 시작하면 "왜 내 캐릭터가 아니지"의 원인을 못 찾는다.
                Debug.LogWarning("[Battle] 고른 호스트를 못 찾아 유령으로 시작한다 — "
                                 + $"유저데이터 준비={_player != null && _player.IsReady}");
                return;
            }
            EnterHost(entry, entry.HostKey, entry.NameKr, _ghost.Position, 100);
        }

        // ── 정본 방 ───────────────────────────────────────────────
        // 절차적 생성과 나란히 둔다. `_canonRoomId` 가 가리키는 방이 테이블에 있으면
        // 그 방을 쓰고, 없으면 예전 방식으로 만든다. 34방을 한 번에 갈아 끼우면
        // 어디서 깨졌는지 알 수 없어서, 한 방씩 옮겨 붙인다.
        private const string FirstCanonRoom = "CH1_N01";

        private RoomTable _rooms;
        private RoomEntry _canonRoom;
        private string _canonRoomId = FirstCanonRoom;
        private int _wave = 1;
        /// <summary>이번 런에서 비운 정예 방 수. 정본 R_ELITE 가 여기에 붙는다.</summary>
        private int _eliteRoomsCleared;
        private float _waveDelay = -1f;
        private readonly HashSet<string> _missingActors = new();

        /// <summary>정본 좌표(미터, 좌하단 기준) → 우리 좌표(픽셀, 좌상단 기준 · 아래가 음수).</summary>
        private Vector2 ToPixels(Vector2 meters)
            => new(meters.x * _pxPerMeter, -(_roomSize.y - meters.y * _pxPerMeter));

        private static RoomKind KindOfCanon(RoomEntry room)
            => room.IsBoss ? RoomKind.Boss
             : room.Type != null && room.Type.StartsWith("Elite") ? RoomKind.Elite
             : room.Type != null && room.Type.StartsWith("Recovery") ? RoomKind.Rest
             : RoomKind.Normal;

        /// <summary>
        /// 정본 EnemyID(E001 …) 로 프로필을 찾는다.
        /// 아직 그림이 없는 배우는 대역을 세운다 — null 로 두면 **보이지 않는 적**이 되어
        /// 방이 클리어되지 않는다. 무엇이 대역인지는 한 번만 알린다.
        /// </summary>
        private HostEntry ActorProfile(string actorId, IReadOnlyList<HostEntry> hosts)
        {
            for (int i = 0; i < hosts.Count; i++)
                if (hosts[i].EnemyId == actorId) return hosts[i];

            if (_missingActors.Add(actorId))
                Debug.LogWarning($"[Battle] {actorId} 의 그림이 아직 없다 — 대역으로 세운다");
            return hosts.Count > 0 ? hosts[0] : null;
        }

        /// <summary>
        /// 정본 방의 웨이브 하나를 세운다. 스폰 출처는 `layout.enemySpawns` 하나뿐이다
        /// (SPAWN_SRC_01). 웨이브 1은 방에 들어서는 즉시, 그다음은 앞 웨이브를 비운 뒤 나온다.
        /// </summary>
        private void SpawnWave(RoomEntry room, int wave)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;

            var spawns = room.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                var s = spawns[i];
                if (s.Wave != wave) continue;
                var e = ActorProfile(s.ActorId, hosts);
                if (e == null) continue;

                // 엘리트는 정본에서 별도 ID(EL01…)로 온다. 수가 적은 대신 하나하나가 세다.
                bool elite = s.ActorId != null && s.ActorId.StartsWith("EL");
                var u = NewUnit($"Enemy_{s.ActorId}_{s.SpawnId}");
                u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, UnitGet(e.SpriteKey),
                        // 정본 엘리트(EL##)는 자기 행에 이미 센 체력이 적혀 있다.
                        // 거기에 배율까지 곱하면 두 번 세진다 — 정본이 있으면 배율은 안 쓴다.
                        Mathf.RoundToInt(EnemyHpOf(e) * (elite && !e.HasCanon ? _config.EliteHpMul : 1f)),
                        Mathf.RoundToInt(EnemyAtkOf(e) * (elite && !e.HasCanon ? _config.EliteAtkMul : 1f)),
                        EnemySpeedOf(e),
                        EnemyRangeOf(e),
                        EnemyIntervalOf(e),
                        new Vector2(84f, 78f), isBoss: false, profile: e);
                u.Position = ToPixels(s.At);
                u.PossessPriority = e.PossessPriority;
                u.PossessRange = 0f;
                u.SetState(EnemyState.Idle);
                ApplyFacingSprites(u, e.SpriteKey);
                _enemies.Add(u);
            }
            _wave = wave;
        }

        /// <summary>
        /// 다음 웨이브를 부른다. 앞 웨이브를 다 비우면 잠깐 뜸을 들인 뒤 나온다 —
        /// 비우자마자 곧바로 쏟아지면 방을 정리했다는 감각이 사라진다.
        /// 아직 남은 웨이브가 있으면 true(= 방이 아직 안 끝났다).
        /// </summary>
        private bool TickWave(float dt)
        {
            if (_canonRoom == null || _wave >= _canonRoom.LastWave) return false;
            if (_enemies.Count > 0) { _waveDelay = -1f; return true; }

            if (_waveDelay < 0f)
            {
                var next = _canonRoom.Wave(_wave + 1);
                _waveDelay = next != null ? Mathf.Max(0.4f, next.StartDelay) : 1f;
            }
            _waveDelay -= dt;
            if (_waveDelay > 0f) return true;

            _waveDelay = -1f;
            SpawnWave(_canonRoom, _wave + 1);
            _bus.Publish(new WaveStartedEvent { Wave = _wave, WaveTotal = _canonRoom.LastWave });
            return true;
        }

        // ── 지형지물 ──────────────────────────────────────────────
        // 이게 없으면 `Pillar`·`Hazard Lane` 같은 방 이름이 이름값을 못 한다.
        // 방마다 다른 점이 적 배치뿐이게 되어 34방이 다 같은 방으로 느껴진다.

        private sealed class Obstacle
        {
            public Rect Bounds;          // 픽셀. 중심이 아니라 좌상단 기준(우리 좌표계)
            public bool BlocksMove;
            public bool BlocksShot;
            public bool IsHazard;
            public int Damage;
            public float Tick;
            public GameObject View;
        }

        private readonly List<Obstacle> _obstacles = new();
        private readonly Dictionary<Unit, float> _hazardTimer = new();

        /// <summary>
        /// 그림이 발자국보다 위로 더 솟는 높이(픽셀).
        ///
        /// 쿼터뷰라 기둥은 바닥에 찍힌 넓이보다 위로 훨씬 높다. 그림을 충돌 사각형에
        /// 딱 맞추면 기둥이 납작한 타일이 되어 "가릴 수 있는 것"으로 안 보인다.
        /// 그림 캔버스는 `발자국 + 솟음` 이고, 캔버스 아래쪽이 발자국과 맞물린다.
        ///
        /// 해저드는 0이다. 불길이 사각형 밖으로 나가면 어디까지가 아픈 자리인지 흐려진다.
        /// </summary>
        private static readonly Dictionary<string, float> ObstacleRise = new()
        {
            { "PILLAR",        114f },
            { "DIVIDER",        70f },
            { "RICOCHET_WALL",  90f },
            { "BARRICADE",      55f },
            { "LOW_COVER",      34f },
            { "HAZARD",          0f },
        };

        private static readonly Dictionary<string, Color> ObstacleColor = new()
        {
            { "PILLAR",        new Color(0.34f, 0.31f, 0.42f, 1f) },
            { "BARRICADE",     new Color(0.40f, 0.33f, 0.28f, 1f) },
            { "LOW_COVER",     new Color(0.30f, 0.34f, 0.40f, 1f) },
            { "DIVIDER",       new Color(0.28f, 0.27f, 0.36f, 1f) },
            { "RICOCHET_WALL", new Color(0.44f, 0.44f, 0.52f, 1f) },
            { "HAZARD",        new Color(0.86f, 0.34f, 0.18f, 0.45f) },
        };

        private void ClearObstacles()
        {
            for (int i = 0; i < _obstacles.Count; i++)
                if (_obstacles[i].View != null) Destroy(_obstacles[i].View);
            _obstacles.Clear();
            _hazardTimer.Clear();
        }

        private void SpawnObstacles(RoomEntry room)
        {
            var list = room.Objects;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                var center = ToPixels(o.At);
                var size = o.Size * _pxPerMeter;
                var rect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f,
                                    size.x, size.y);

                // 그림은 발자국보다 위로 솟는다. 아래쪽을 발자국에 맞물려 놓아야
                // 발밑이 어긋나지 않는다.
                float rise = ObstacleRise.TryGetValue(o.Kind ?? "", out var r) ? r : 0f;
                var viewSize = new Vector2(size.x, size.y + rise);
                var viewCenter = new Vector2(center.x, center.y + rise * 0.5f);

                var go = new GameObject($"Obj_{o.Kind}_{o.ObjectId}",
                                        typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_unitLayer, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = viewSize;
                rt.anchoredPosition = viewCenter;

                var img = go.GetComponent<Image>();
                // 같은 종류라도 세로벽·가로벽처럼 비율이 다른 것이 있다(RICOCHET_WALL).
                // 한 장으로 돌려쓰면 늘어나 픽셀이 뭉개지므로 방향별로 찾아본다.
                string kind = (o.Kind ?? "").ToLowerInvariant();
                var sprite = GetSprite($"obj_{kind}_{(size.y >= size.x ? "v" : "h")}")
                             ?? GetSprite($"obj_{kind}");
                img.sprite = sprite;
                img.color = sprite != null
                    ? Color.white
                    : ObstacleColor.TryGetValue(o.Kind ?? "", out var c)   // 그림 오기 전 자리표시자
                        ? c : new Color(0.33f, 0.32f, 0.40f, 1f);
                img.raycastTarget = false;

                _obstacles.Add(new Obstacle
                {
                    Bounds = rect, BlocksMove = o.BlocksMove, BlocksShot = o.BlocksShot,
                    IsHazard = o.IsHazard, Damage = o.HazardDamage, Tick = o.HazardTick,
                    View = go,
                });
            }
        }

        /// <summary>발밑 판정 상자. 몸 전체로 보면 머리가 기둥에 걸려 못 지나간다.</summary>
        private static Vector2 FootHalf(Unit u)
        {
            var h = ((RectTransform)u.transform).sizeDelta * 0.5f;
            return new Vector2(h.x * 0.55f, h.y * 0.22f);
        }

        private bool BlockedAt(Vector2 pos, Vector2 half)
        {
            var foot = new Vector2(pos.x, pos.y - half.y);
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.BlocksMove) continue;
                if (Mathf.Abs(foot.x - o.Bounds.center.x) < o.Bounds.width * 0.5f + half.x &&
                    Mathf.Abs(foot.y - o.Bounds.center.y) < o.Bounds.height * 0.5f + half.y)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 막힌 것을 타고 미끄러지며 움직인다.
        ///
        /// 먼저 밀어 넣고 빠져나오게 하면 입력과 밀어내기가 매 프레임 싸워서
        /// 벽에 붙었을 때 캐릭터가 떨린다. 아예 **들어가지 않게** 하는 편이 낫다.
        /// 대각선이 막히면 x 만, 그것도 막히면 y 만 시도한다 — 벽을 따라 흐른다.
        /// </summary>
        private Vector2 SlideMove(Unit u, Vector2 from, Vector2 delta)
        {
            if (_obstacles.Count == 0) return from + delta;
            var half = FootHalf(u);

            var p = from + delta;
            if (!BlockedAt(p, half)) return p;

            var px = new Vector2(from.x + delta.x, from.y);
            if (!BlockedAt(px, half)) return px;

            var py = new Vector2(from.x, from.y + delta.y);
            if (!BlockedAt(py, half)) return py;

            return from;
        }

        /// <summary>
        /// 이미 막힌 것 안에 있으면 밀어낸다. 스폰이나 순간이동으로 갇힌 경우의 구제책이다.
        /// 평소 이동은 `SlideMove` 가 애초에 들어가지 않게 막는다.
        /// 가장 얕게 겹친 축으로 빼야 모서리에서 반대편으로 튀지 않는다.
        /// </summary>
        private void ResolveObstacles(Unit u)
        {
            if (_obstacles.Count == 0 || u == null) return;
            var half = FootHalf(u);
            var p = u.Position;
            if (!BlockedAt(p, half)) return;

            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.BlocksMove) continue;

                var foot = new Vector2(p.x, p.y - half.y);
                float dx = foot.x - o.Bounds.center.x;
                float dy = foot.y - o.Bounds.center.y;
                float ox = o.Bounds.width * 0.5f + half.x - Mathf.Abs(dx);
                float oy = o.Bounds.height * 0.5f + half.y - Mathf.Abs(dy);
                if (ox <= 0f || oy <= 0f) continue;

                // 정확히 중심에 겹치면 부호가 0 이라 방향을 못 정한다. 아래로 밀어낸다.
                if (ox < oy) p.x += (dx >= 0f ? 1f : -1f) * ox;
                else p.y += (dy >= 0f ? 1f : -1f) * oy;
            }
            u.Position = p;
        }

        // 앞뒤 정렬 — 발밑이 아래인 것이 위에 그려진다.
        // 쿼터뷰라 기둥이 항상 뒤에 깔리면 기둥 앞에 선 캐릭터까지 기둥에 가려진다.
        // 매 프레임 새 리스트를 만들면 hot path 할당이 되므로 버퍼를 재사용한다.
        private readonly List<Transform> _depthT = new();
        private readonly List<float> _depthY = new();

        private void AddDepth(Unit u)
        {
            if (u == null || !u.gameObject.activeSelf) return;
            _depthT.Add(u.transform);
            _depthY.Add(u.Position.y - ((RectTransform)u.transform).sizeDelta.y * 0.5f);
        }

        private void SortDepth()
        {
            if (_obstacles.Count == 0) return;   // 지형지물이 없으면 정렬할 이유가 없다

            _depthT.Clear();
            _depthY.Clear();
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.View == null) continue;
                _depthT.Add(o.View.transform);
                _depthY.Add(o.Bounds.yMin);      // 발자국의 아래 변
            }
            AddDepth(_ghost);
            AddDepth(_host);
            for (int i = 0; i < _enemies.Count; i++) AddDepth(_enemies[i]);
            for (int i = 0; i < _dying.Count; i++) AddDepth(_dying[i]);
            // 문은 정렬에서 빼면 순서가 매 프레임 밀려 깜빡인다. 늘 맨 뒤에 둔다 —
            // 방 위쪽 끝에 있어 무엇을 가릴 일이 없다.
            for (int i = 0; i < _exits.Count; i++)
                if (_exits[i].View != null)
                {
                    _depthT.Add(_exits[i].View);
                    _depthY.Add(float.MaxValue);
                }

            // 삽입 정렬 — 항목이 스무 개 남짓이고 프레임마다 거의 정렬돼 있다.
            for (int i = 1; i < _depthT.Count; i++)
            {
                var t = _depthT[i]; float y = _depthY[i];
                int j = i - 1;
                while (j >= 0 && _depthY[j] < y)   // 큰 값(위쪽)이 앞으로
                {
                    _depthT[j + 1] = _depthT[j]; _depthY[j + 1] = _depthY[j]; j--;
                }
                _depthT[j + 1] = t; _depthY[j + 1] = y;
            }
            for (int i = 0; i < _depthT.Count; i++)
                if (_depthT[i].GetSiblingIndex() != i) _depthT[i].SetSiblingIndex(i);
        }

        /// <summary>해저드 위에 서 있으면 주기적으로 깎인다.</summary>
        private void TickHazards(float dt)
        {
            if (_obstacles.Count == 0) return;
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.IsHazard || o.Damage <= 0) continue;
                Burn(o, Avatar, dt);
                for (int e = 0; e < _enemies.Count; e++) Burn(o, _enemies[e], dt);
            }
        }

        private void Burn(Obstacle o, Unit u, float dt)
        {
            if (u == null || !u.IsAlive || u.IsDying) return;
            var foot = new Vector2(u.Position.x,
                                   u.Position.y - ((RectTransform)u.transform).sizeDelta.y * 0.4f);
            if (!o.Bounds.Contains(foot))
            {
                _hazardTimer.Remove(u);
                return;
            }

            _hazardTimer.TryGetValue(u, out float t);
            t -= dt;
            if (t > 0f) { _hazardTimer[u] = t; return; }

            // 처음 밟는 순간 바로 한 번 아프게 한다. 그래야 밟았다는 것을 안다.
            _hazardTimer[u] = o.Tick;
            if (u == _host || u == _ghost) DamagePlayer(o.Damage);
            else { u.TakeDamage(o.Damage); ShowDamage(u.Position, o.Damage, true); }
        }

        /// <summary>
        /// 방 크기를 정한다. 가로는 늘 화면 폭(8.4 m)이고 세로만 방마다 다르다.
        /// `UnitLayer` 를 방 크기로 키우고 위쪽에 붙인다 — 좌표계가 위에서 아래로
        /// 음수인 채 그대로 유지되도록.
        /// </summary>
        private void SetRoomSize(float meterHeight)
        {
            _roomSize = new Vector2(RoomMeterWidth * _pxPerMeter, meterHeight * _pxPerMeter);
            _unitLayer.anchorMin = _unitLayer.anchorMax = new Vector2(0f, 1f);
            _unitLayer.pivot = new Vector2(0f, 1f);
            _unitLayer.sizeDelta = _roomSize;
            _scroll = 0f;
            // 방 크기가 바뀌면 탄·숫자 레이어도 같은 크기·같은 자리여야 한다
            if (_shotLayer != null) _shotLayer.sizeDelta = _roomSize;
            if (_textLayer != null) _textLayer.sizeDelta = _roomSize;
            if (_floor != null)
            {
                _floor.anchorMin = _floor.anchorMax = new Vector2(0f, 1f);
                _floor.pivot = new Vector2(0f, 1f);
                _floor.sizeDelta = _roomSize;
            }
            ApplyScroll();
        }

        /// <summary>
        /// 세로 카메라. 창은 고정이고 방을 민다.
        /// 플레이어를 창 한가운데 두되 방의 위아래 끝을 넘어가지 않는다 —
        /// 넘어가면 방 밖의 빈 공간이 보인다.
        /// </summary>
        private void TickCamera(float dt)
        {
            var a = Avatar;
            if (a == null || _unitLayer == null) return;

            _scroll = Mathf.Lerp(_scroll, WantScroll(a), 1f - Mathf.Exp(-CameraFollow * dt));
            ApplyScroll();
            TickZoom(dt);
        }

        // ── 빙의 줌 ──────────────────────────────────────────────
        //
        // 빙의하는 동안 화면을 살짝 당긴다. 이 게임의 이름값이 걸린 동작인데
        // 아무 변화 없이 캐릭터만 바뀌면 그냥 조작 하나로 읽힌다.

        private const float ZoomSpeed = 9f;

        private float _zoom = 1f;
        private Vector2 _fieldHome;
        private bool _fieldHomeSet;

        private void TickZoom(float dt)
        {
            if (_field == null || _config == null) return;
            if (!_fieldHomeSet) { _fieldHome = _field.anchoredPosition; _fieldHomeSet = true; }

            float want = IsChanneling ? _config.PossessZoom : 1f;
            _zoom = Mathf.Lerp(_zoom, want, 1f - Mathf.Exp(-ZoomSpeed * dt));
            if (Mathf.Abs(_zoom - 1f) < 0.0005f) _zoom = 1f;

            _field.localScale = new Vector3(_zoom, _zoom, 1f);

            // 방 화면의 피벗이 좌상단이라 그냥 키우면 오른쪽 아래로 밀려난다.
            // 가운데가 제자리에 있도록 되돌린다.
            float k = _zoom - 1f;
            var size = _field.rect.size;
            _field.anchoredPosition = _fieldHome + new Vector2(-k * size.x * 0.5f,
                                                                k * size.y * 0.5f);
        }

        /// <summary>
        /// 스크롤을 세 레이어에 함께 먹인다.
        ///
        /// 탄·피해 수치는 유닛보다 위에 그리려고 **형제 레이어**로 뽑아 놨다.
        /// 그래서 유닛 레이어만 밀면 탄과 숫자가 그 자리에 남아 캐릭터와 따로 논다 —
        /// 방이 화면보다 길어진 뒤로 최대 400px 까지 어긋났다.
        /// </summary>
        private void ApplyScroll()
        {
            // 정수로 맞춰 놓지 않으면 픽셀 그림이 매 프레임 미세하게 흔들린다
            var at = new Vector2(0f, Mathf.Round(_scroll));
            _unitLayer.anchoredPosition = at;
            if (_shotLayer != null) _shotLayer.anchoredPosition = at;
            if (_textLayer != null) _textLayer.anchoredPosition = at;
            if (_fieldLayer != null) _fieldLayer.anchoredPosition = at;
            if (_floor != null) _floor.anchoredPosition = at;
        }

        private float WantScroll(Unit a)
        {
            float viewH = _field.rect.height;
            return Mathf.Clamp(-a.Position.y - viewH * 0.5f, 0f,
                               Mathf.Max(0f, _roomSize.y - viewH));
        }

        /// <summary>
        /// 방에 들어선 순간의 화면. 흘러가면 안 된다 —
        /// `SetRoomSize` 가 0으로 돌려놓은 뒤 부드럽게 따라가면 방마다 화면이
        /// 위에서 아래로 주르륵 미끄러진다("갑자기 내려갔다 올라오는" 것의 정체).
        /// </summary>
        private void SnapCamera()
        {
            var a = Avatar;
            if (a == null || _unitLayer == null) return;
            _scroll = WantScroll(a);
            ApplyScroll();
        }

        /// <summary>
        /// 지금 창에 보이는가. 방이 화면보다 길어져 생긴 판정이다.
        /// 가장자리에서 깜빡이지 않도록 한 칸 여유를 둔다.
        /// </summary>
        private bool IsOnScreen(Unit u)
        {
            if (u == null) return false;
            const float Margin = 60f;
            float y = u.Position.y + _scroll;      // 창 기준 좌표(0 이 위, 아래로 음수)
            return y <= Margin && y >= -_field.rect.height - Margin;
        }

        private Unit NewUnit(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_unitLayer, false);
            return go.AddComponent<Unit>();
        }

        private Sprite GetSprite(string n) => _atlas != null ? _atlas.GetSprite(n) : null;

        /// <summary>스프라이트 이름 → 캐릭터 아틀라스 키. `unit_boss` → `boss`.</summary>
        private static string UnitKeyOf(string spriteName)
            => spriteName != null && spriteName.StartsWith("unit_") ? spriteName.Substring(5) : spriteName;

        /// <summary>캐릭터 스프라이트 조회. 아틀라스가 안 올라와 있으면 null 이다.</summary>
        private Sprite UnitGet(string key, string suffix = null)
        {
            if (key == null || !_unitAtlas.TryGetValue(key, out var atlas) || atlas == null) return null;
            if (suffix != null) return atlas.GetSprite($"unit_{key}_{suffix}");

            // 방향 없는 기본 그림은 방향 5장이 붙기 전 한 프레임 동안만 쓰인다.
            // 없으면 정면(s)으로 대신한다 — 이것 때문에 통째로 안 보이면 손해가 크다.
            return atlas.GetSprite($"unit_{key}") ?? atlas.GetSprite($"unit_{key}_s");
        }

        /// <summary>
        /// 이 런에서 쓸 캐릭터 아틀라스를 미리 올린다.
        /// 스폰은 동기 코드라 이 시점에 다 올라와 있어야 한다 — 늦으면 그림 없이 스폰된다.
        /// </summary>
        private async UniTask LoadUnitAtlasesAsync(IResourceManager res, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key) || _unitAtlas.ContainsKey(key)) continue;
                try { _unitAtlas[key] = await res.LoadAsync<SpriteAtlas>(UnitAtlasPrefix + key); }
                catch (Exception e)
                {
                    // 한 종이 없다고 런을 멈추지 않는다 — 그 캐릭터만 그림 없이 나온다.
                    Debug.LogError($"[Battle] 캐릭터 아틀라스 로드 실패 unit_{key} — {e.Message}");
                }
            }
        }

        /// <summary>
        /// 방 <paramref name="index"/> 의 <paramref name="i"/> 번째 적이 쓸 호스트.
        /// 미리 올릴 아틀라스를 고를 때와 실제로 스폰할 때가 반드시 같아야 하므로
        /// 뽑는 식을 한 곳에만 둔다 — 갈라지면 그림 없는 적이 나온다.
        /// </summary>
        private static HostEntry EnemyAt(IReadOnlyList<HostEntry> hosts, int index, int i)
            => hosts[(i * 5 + index * 3 + 1) % hosts.Count];

        /// <summary>이 런이 건드릴 수 있는 캐릭터 키를 모은다.</summary>
        private List<string> RunUnitKeys()
        {
            var keys = new List<string> { "ghost" };

            int chapter = _player != null ? _player.CurrentChapter : 1;
            var bossDef = _bossTable != null ? _bossTable.ForChapter(chapter) : null;
            keys.Add(UnitKeyOf(bossDef != null ? bossDef.SpriteName : "unit_boss"));
            // 보스 그림은 아직 3체 중 어느 것도 안 왔다. 대체용 임시 그림을 함께 올려 둔다.
            keys.Add("boss");

            // 빙의로 몸을 갈아타도 로비에서 고른 호스트는 긴급 투입으로 나올 수 있다.
            var emergency = PickPlayerHost();
            if (emergency != null) keys.Add(emergency.SpriteKey);

            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return keys;

            // 정본 경로가 있으면 그 길에 실제로 나오는 배우만 미리 받는다.
            // 전부 받으면 쓰지도 않을 아틀라스가 딸려 온다.
            if (_rooms != null && _rooms.Get(FirstCanonRoom) != null)
            {
                var id = FirstCanonRoom;
                int guard = 0;
                while (!string.IsNullOrEmpty(id) && guard++ < 64)
                {
                    var room = _rooms.Get(id);
                    if (room == null) break;
                    for (int i = 0; i < room.Spawns.Count; i++)
                    {
                        var e = ActorProfile(room.Spawns[i].ActorId, hosts);
                        if (e != null && !keys.Contains(e.SpriteKey)) keys.Add(e.SpriteKey);
                    }
                    id = room.Exits.Count > 0 ? room.Exits[0].NextRoomId : null;
                }
                return keys;
            }

            // 방 종류(일반·정예)에 따라 마릿수가 달라지므로 둘 중 많은 쪽까지 훑는다.
            for (int index = 0; index < _config.StagesPerChapter; index++)
            {
                int count = Mathf.Max(_config.EliteEnemyCount, _config.EnemiesPerRoom(index));
                for (int i = 0; i < count; i++)
                {
                    var key = EnemyAt(hosts, index, i).SpriteKey;
                    if (!keys.Contains(key)) keys.Add(key);
                }
            }
            return keys;
        }

        /// <summary>
        /// 방향 스프라이트 5장을 찾아 붙인다. 하나라도 없으면 붙이지 않는다 —
        /// 없는 방향만 원래 그림으로 나오면 캐릭터가 방향마다 바뀌어 보인다.
        /// 12종을 한 번에 만들지 않고 한 종씩 넣어 볼 수 있어야 해서 이렇게 둔다.
        /// </summary>
        /// <summary>
        /// 걷기 그림을 쓰지 않는 종. 지금은 비어 있다.
        ///
        /// `medium` 이 한때 여기 있었다 — 로브 종이라 걷기를 "로브가 벌어지는 것" 으로
        /// 그려서 실루엣이 통째로 바뀌었다. 재작업분(큐 38)이 실루엣을 그대로 두고
        /// 아랫단만 물결치게 고쳐 와서 뺐다.
        ///
        /// 같은 사고가 또 나면 여기에 넣고 재작업을 걸면 된다 — 그림 하나 때문에
        /// 그 캐릭터를 통째로 못 쓰게 두지 않기 위한 자리다.
        /// </summary>
        private static readonly HashSet<string> NoWalkFrames = new();

        private void ApplyFacingSprites(Unit u, string key)
        {
            var sets = new Sprite[Unit.FrameSuffix.Length][];
            for (int f = 0; f < sets.Length; f++)
                sets[f] = FrameSet(key, Unit.FrameSuffix[f]);
            if (sets[Unit.FrameIdle] == null) return;   // 방향 그림이 없는 종은 지금 그림 그대로 둔다

            if (NoWalkFrames.Contains(key))
                sets[Unit.FrameWalk1] = sets[Unit.FrameWalk2] = null;

            u.SetFacingSprites(sets);
        }

        /// <summary>한 동작의 방향 5장. 하나라도 없으면 null — 반쪽짜리는 안 쓴다.</summary>
        private Sprite[] FrameSet(string key, string frame)
        {
            var set = new Sprite[Unit.FacingSuffix.Length];
            for (int i = 0; i < set.Length; i++)
            {
                var suffix = frame == null
                    ? Unit.FacingSuffix[i]
                    : $"{Unit.FacingSuffix[i]}_{frame}";
                set[i] = UnitGet(key, suffix);
                if (set[i] == null) return null;
            }
            return set;
        }

        /// <summary>HUD 초상용. 아틀라스를 들고 있는 쪽이 하나뿐이라 여기서 내준다.</summary>
        public Sprite UnitSprite(string hostKey) => UnitGet(hostKey);

        /// <summary>
        /// 그 몸의 얼티밋 아이콘. 인게임 버튼이 21종 다 같은 그림을 쓰고 있어서
        /// 어떤 얼티밋을 들고 있는지가 화면에 안 보였다.
        ///
        /// 아이콘은 호스트 선택 화면 아틀라스에 있다. 인게임에서 쓰려면 그 아틀라스를
        /// 함께 올려야 하므로 여기서 늦게 한 번만 불러온다.
        /// </summary>
        public Sprite UltimateIcon(string hostKey)
            => _panelAtlas != null && hostKey != null
                ? _panelAtlas.GetSprite($"ultimateicon_{hostKey}") : null;

        private SpriteAtlas _panelAtlas;

        private async UniTask LoadPanelAtlasAsync(IResourceManager res)
        {
            if (_panelAtlas != null) return;
            try { _panelAtlas = await res.LoadAsync<SpriteAtlas>("atlas/hostselectpanel"); }
            catch (Exception e)
            {
                // 없어도 게임은 돈다 — 버튼이 기본 그림으로 남을 뿐이다
                Debug.LogWarning($"[Battle] 얼티밋 아이콘 아틀라스 로드 실패 — {e.Message}");
            }
        }

        /// <summary>
        /// 이 스테이지가 어떤 방인가 (기획서 A 06 ROOM TYPE).
        ///
        /// 마지막은 항상 보스다. 그 앞은 한 챕터 안에서 같은 방만 반복되지 않게
        /// 스테이지 번호로 갈라 준다 — 지금은 3스테이지라 경우의 수가 적다.
        /// 챕터가 길어지면 방 구성표를 데이터로 빼야 한다.
        /// </summary>
        /// <summary>
        /// 이 챕터의 방 수. 정본 경로를 따라가면 CH1 은 10방이다 —
        /// `StagesPerChapter`(3) 는 절차적 생성 시절의 값이라 진행 표시가 어긋난다.
        /// </summary>
        private int RoomTotal
        {
            get
            {
                if (_rooms == null) return _config.StagesPerChapter;
                int n = 0;
                var id = FirstCanonRoom;
                while (!string.IsNullOrEmpty(id) && n < 64)
                {
                    var r = _rooms.Get(id);
                    if (r == null) break;
                    n++;
                    id = r.Exits.Count > 0 ? r.Exits[0].NextRoomId : null;
                }
                return n > 0 ? n : _config.StagesPerChapter;
            }
        }

        /// <summary>정본 경로의 끝(보스를 잡은 방)인가.</summary>
        private bool IsLastRoom =>
            _canonRoom != null ? _canonRoom.IsChapterEnd
                               : _roomIndex >= _config.StagesPerChapter - 1;

        private RoomKind KindOf(int index)
        {
            int last = _config.StagesPerChapter - 1;
            if (index >= last) return RoomKind.Boss;
            if (index == 0) return RoomKind.Normal;        // 첫 방은 늘 평범하게 연다
            // 보스 직전은 정예로 조인다. 다만 Ghost HP 가 바닥이면 쉬어 가게 한다.
            if (index == last - 1)
                return _ghostHp <= GhostHpMax / 3 ? RoomKind.Rest : RoomKind.Elite;
            return RoomKind.Normal;
        }

        private void EnterRoom(int index)
        {
            _roomIndex = index;
            DespawnExit();
            ClearFields();   // 안 지우면 새 방 바닥에 지난 방 장판이 남는다
            ApplyRoomFloor();
            _bloodDebtUsed = 0;   // 피의 부채는 방마다 다시 센다
            ClearDeployables();   // 포탑도 방을 따라오지 않는다
            _emergencyUsedThisRoom = false;   // 긴급 호스트는 방마다 한 번 (기획서 A 8-3)
            // 정본 방이 있으면 그것이 이긴다. 없으면 예전 절차적 생성으로 돌아간다 —
            // 34방을 한 번에 갈아 끼우지 않고 한 방씩 옮겨 붙이기 위해서다.
            _canonRoom = _rooms != null ? _rooms.Get(_canonRoomId) : null;
            _roomKind = _canonRoom != null ? KindOfCanon(_canonRoom) : KindOf(index);
            bool isBoss = _roomKind == RoomKind.Boss;

            // 정본은 보스방만 세로가 16 m 다. 방마다 높이가 달라질 수 있어 여기서 정한다.
            SetRoomSize(_canonRoom != null ? _canonRoom.Height
                      : isBoss ? BossRoomMeterHeight : RoomMeterHeight);

            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null) Destroy(_enemies[i].gameObject);
            _enemies.Clear();

            // 이전 방에서 쓰러지던 몸은 여기서 끊는다. 안 그러면 새 방 바닥에
            // 앞 방 시체가 남아 페이드된다.
            for (int i = 0; i < _dying.Count; i++)
                if (_dying[i] != null) Destroy(_dying[i].gameObject);
            _dying.Clear();

            for (int i = 0; i < _damageTexts.Count; i++) _damageTexts[i].Despawn();
            for (int i = 0; i < _impacts.Count; i++) _impacts[i].gameObject.SetActive(false);

            // 빙의가 도는 중에 방이 바뀌면 빼앗기던 몸이 그대로 남는다.
            // 적 목록에서는 이미 빠져 있어서 아무도 치워 주지 않는다.
            if (_channelBody != null) { Destroy(_channelBody.gameObject); _channelBody = null; }
            _channel = 0f;
            _channelEntry = null;
            if (_ghost != null)
            {
                _ghost.transform.localScale = Vector3.one;
                _ghost.SetSpriteOverride(null);
            }

            _boss = null;
            _wave = 1;
            _waveDelay = -1f;
            ClearObstacles();
            if (_canonRoom != null) SpawnObstacles(_canonRoom);

            // 이전 룸의 탄이 다음 룸까지 날아가 첫 적을 때리는 일을 막는다
            for (int i = 0; i < _shots.Count; i++) _shots[i].Despawn();

            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0)
            {
                Debug.LogError("[Battle] 호스트 테이블이 비어 있어 적을 만들 수 없다.");
                return;
            }

            if (isBoss)
            {
                int chapter = _player != null ? _player.CurrentChapter : 1;
                var def = _bossTable != null ? _bossTable.ForChapter(chapter) : null;

                // 정본 보스가 있으면 이름·체력·공격력·이동속도를 그대로 쓴다.
                // 우리 BossTable 은 배율표라 절대값이 없다 — 정본 쪽이 단일 출처다.
                bool canon = _canonRoom != null && _canonRoom.IsBoss;
                string bossName = canon ? _canonRoom.BossName : def?.NameEn ?? "BOSS";

                var boss = NewUnit("Boss");
                boss.Setup(UnitSide.Enemy,
                           canon ? _canonRoom.BossId.ToLowerInvariant() : def?.BossKey ?? "boss",
                           canon ? _canonRoom.BossName : def?.NameKr ?? "BOSS",
                           UnitGet(UnitKeyOf(def?.SpriteName ?? "unit_boss")) ?? UnitGet("boss"),
                           canon ? _canonRoom.BossHp
                                 : Mathf.RoundToInt(_config.BossHp(chapter) * (def?.HpMul ?? 1f)),
                           canon ? _canonRoom.BossAtk
                                 : Mathf.RoundToInt(_config.BossAtk * (def?.AtkMul ?? 1f)),
                           canon ? _canonRoom.BossMoveSpeed * _pxPerMeter
                                 : _config.BossMoveSpeed * (def?.MoveSpeedMul ?? 1f),
                           _config.BossAttackRange, _config.BossAttackInterval,
                           // 보스 그림은 256×256 캔버스다(닿는 선 y=232). 160 상자에 넣으면
                           // 캔버스 여백까지 함께 줄어 보스가 잡몹보다 작아진다.
                           // 캔버스 크기를 그대로 쓴다 — 방 폭 720 의 약 1/3 이다.
                           new Vector2(256f, 256f), isBoss: true);
                boss.Position = canon ? ToPixels(_canonRoom.BossAt)
                                      : new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.14f);
                _enemies.Add(boss);
                _boss = boss;
                _brain.Setup(def);
                if (canon)
                {
                    // 문턱과 예고 시간은 보스마다 다르다 — 정본 값을 그대로 넣는다
                    var tel = new List<float>();
                    for (int i = 0; i < _canonRoom.BossPhases.Count; i++)
                        tel.Add(_canonRoom.BossPhases[i].TelegraphSeconds);
                    _brain.SetCanonPhases(_canonRoom.BossPhaseGates, tel);
                }
                _bus.Publish(new BossHpChangedEvent
                {
                    BossHp = boss.Hp, BossHpMax = boss.HpMax,
                    BossName = bossName, Phase = 1,
                });
            }
            else if (_roomKind == RoomKind.Rest)
            {
                // 회복 방 — 적이 없다. 들어서는 순간 Ghost HP 를 돌려주고 출구를 연다.
                // 유령 상태의 시계가 계속 도는 게임이라, 쉬어 가는 방이 곧 보상이다.
                _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + _config.RestGhostHeal);
                PublishHp();
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }
            else if (_canonRoom != null)
            {
                SpawnWave(_canonRoom, 1);
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }
            else
            {
                bool elite = _roomKind == RoomKind.Elite;
                int count = elite ? _config.EliteEnemyCount : _config.EnemiesPerRoom(index);
                for (int i = 0; i < count; i++)
                {
                    // 방마다 등장 조합이 달라지도록 룸 인덱스를 섞어 넣는다
                    var e = EnemyAt(hosts, index, i);
                    var u = NewUnit($"{(elite ? "Elite" : "Enemy")}_{e.HostKey}_{i}");
                    // 적도 호스트다 — 같은 공격 방식을 쓴다. 방마다 교전 양상이 달라진다.
                    // 정예는 수가 적은 대신 하나하나가 세다 — 빙의 대상이 귀해진다.
                    u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, UnitGet(e.SpriteKey),
                            Mathf.RoundToInt(EnemyHpOf(e) * (elite ? _config.EliteHpMul : 1f)),
                            Mathf.RoundToInt(EnemyAtkOf(e) * (elite ? _config.EliteAtkMul : 1f)),
                            EnemySpeedOf(e),
                            EnemyRangeOf(e),
                            EnemyIntervalOf(e),
                            new Vector2(84f, 78f), isBoss: false, profile: e);
                    u.Position = SpawnSlot(i, count);
                    // 기획서 A 4-3 — 빙의 우선순위·사거리는 적마다 다를 수 있다.
                    // 사거리 0 은 "전역 기본값을 쓴다"는 뜻이다.
                    u.PossessPriority = e.PossessPriority;
                    u.PossessRange = 0f;
                    u.SetState(EnemyState.Idle);
                    ApplyFacingSprites(u, e.SpriteKey);
                    _enemies.Add(u);
                }
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }

            // 정본 방은 들어서는 자리가 정해져 있다(layout.playerSpawns).
            // 방마다 입구 위치가 달라 여기서 옮겨 놓지 않으면 벽 속에서 시작한다.
            var avatar = Avatar;
            if (avatar != null && _canonRoom != null)
                avatar.Position = ToPixels(_canonRoom.PlayerSpawn);
            SnapCamera();

            _bus.Publish(new RoomEnteredEvent
            {
                RoomIndex = index, RoomTotal = RoomTotal,
                IsBossRoom = isBoss, Kind = _roomKind,
            });
        }

        /// <summary>필드 상단 절반에 고르게 흩어 놓는다. 플레이어 시작 위치와 겹치지 않게 한다.</summary>
        /// <summary>
        /// 방 위쪽에 넓게 흩어 배치한다.
        ///
        /// 좁게 모아두면 첫 프레임부터 한 덩어리로 보이고, 전부 같은 지점을 향해 움직여
        /// 끝까지 뭉쳐 다닌다. 가로는 거의 꽉 채우고 세로도 벌린 뒤 행마다 어긋나게 민다.
        /// 플레이어 시작 위치와는 탐지 거리보다 멀게 띄운다 — 들어가야 반응하게 하기 위함.
        /// </summary>
        private Vector2 SpawnSlot(int i, int count)
        {
            float w = _roomSize.x, h = _roomSize.y;
            int cols = Mathf.Min(3, Mathf.Max(1, count));
            int rows = Mathf.CeilToInt(count / (float)cols);

            int row = i / cols;
            int colInRow = i - row * cols;
            int inRow = Mathf.Min(cols, count - row * cols);

            float fx = inRow <= 1 ? 0.5f : (float)colInRow / (inRow - 1);
            float fy = rows <= 1 ? 0.35f : (float)row / (rows - 1);

            // 격자로 딱 맞으면 대형처럼 보인다. 행마다 반 칸씩 어긋나게 민다.
            float stagger = row % 2 == 0 ? 0.07f : -0.07f;
            float x = w * Mathf.Lerp(0.10f, 0.90f, Mathf.Clamp01(fx + stagger));
            float y = -h * Mathf.Lerp(EnemyBandTop, EnemyBandBottom, fy);
            return new Vector2(x, y);
        }

        /// <summary>필드 밖으로 나가지 않게 잘라낸다. 밀림·돌진이 벽을 넘지 않게.</summary>
        /// <summary>탄이 엄폐물에 막히는가.</summary>
        private bool BlockedByCover(Vector2 at)
        {
            for (int i = 0; i < _obstacles.Count; i++)
                if (_obstacles[i].BlocksShot && _obstacles[i].Bounds.Contains(at)) return true;
            return false;
        }

        private void ClampToField(Unit u)
        {
            var half = ((RectTransform)u.transform).sizeDelta * 0.5f;
            var p = u.Position;
            p.x = Mathf.Clamp(p.x, half.x, _roomSize.x - half.x);
            p.y = Mathf.Clamp(p.y, -_roomSize.y + half.y, -half.y);
            u.Position = p;
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_running || _config == null) return;
            if (_awaitingBuff) return;   // 3택1 선택 대기 — 적이 없는 상태라 멈춰도 안전하다
            float dt = Time.deltaTime;

            // 빙의가 들어가는 중이면 다른 것은 멈추지 않되 조작만 잠근다 —
            // 0.35 초 동안 영혼이 몸으로 빨려 들어가는 것을 보여 준다.
            TickPossessChannel(dt);

            TickGhostState(dt);
            if (!_running) return;       // 자연 감소로 소멸했을 수 있다

            _ultimateCharge = Mathf.Min(_ultimateCharge + dt * _buffs.UltimateChargeMul,
                                        _config.UltimateChargeSeconds);

            TickPlayer(dt);
            SyncFireRing();
            TickEnemies(dt);
            TickShots(dt);
            TickFields(dt);
            TickSynergy(dt);
            TickDeploy(dt);
            TickDeployables(dt);
            TickUltimate(dt);
            CleanupDead();
            // CleanupDead 다음에 돈다 — 이번 프레임에 죽은 몸도 바로 쓰러지기 시작한다.
            TickDying(dt);
            for (int i = 0; i < _enemies.Count; i++) _enemies[i]?.TickMark(dt);
            TickHazards(dt);
            SortDepth();          // 이동이 끝난 뒤에 앞뒤를 다시 정한다
            TickDamageTexts(dt);
            for (int i = 0; i < _impacts.Count; i++) _impacts[i].Tick(dt);
            // 모든 이동이 끝난 뒤에 화면을 옮긴다. 중간에 옮기면 한 프레임 늦게 따라온다.
            TickCamera(dt);
            RefreshPossessTarget();
            TickEmergency(dt);
            if (!_running) return;      // 긴급 호스트를 못 써서 졌을 수 있다
            TickExit();

            // 출구가 이미 열려 있으면 다시 클리어 처리하지 않는다
            // 웨이브가 남아 있으면 방을 비운 것이 아니다.
            bool waveLeft = TickWave(dt);
            // 빙의 중에는 방을 닫지 않는다 — 빼앗기는 몸은 이미 적 목록에서 빠져 있어서
            // 마지막 한 마리를 빼앗는 순간 방이 클리어된 것으로 보인다.
            if (!waveLeft && _enemies.Count == 0 && _exits.Count == 0
                && !_awaitingBuff && !IsChanneling) OnRoomCleared();
        }

        private Unit Avatar => _host != null ? _host : _ghost;

        /// <summary>무적 중인가. 빙의 직후와 호스트 상실 직후의 보호 시간을 함께 본다.</summary>
        private bool IsInvulnerable => _invuln > 0f || _ghostProtect > 0f;

        /// <summary>
        /// 유령 상태의 시간 규칙 (기획서 A 1-2 · 1-3).
        ///
        /// Ghost HP 는 체력이 아니라 **남은 시간**이다. 유령으로 떠 있는 동안 초당 깎이므로,
        /// "안전한 곳에서 기다린다"가 공짜가 아니게 된다. 호스트가 살아 있으면 멈춘다 —
        /// 몸을 얻은 상태가 곧 시계를 멈춘 상태다.
        ///
        /// 호스트를 잃은 직후에는 보호 시간이 붙는다. 그 순간은 적 한복판이라,
        /// 보호가 없으면 다시 빙의할 틈 없이 연쇄로 죽는다.
        /// </summary>
        private void TickGhostState(float dt)
        {
            if (_invuln > 0f) _invuln = Mathf.Max(0f, _invuln - dt);

            // 전술 빙의 쿨다운은 몸 안에 있든 밖에 있든 흐른다.
            // 유령일 때 멈추면 죽고 나서 기다리는 것이 이득이 된다.
            if (_tacticalCooldown > 0f)
            {
                _tacticalCooldown = Mathf.Max(0f, _tacticalCooldown - dt);
                // 매 프레임 발행하지 않는다 — 0.1초 눈금이 바뀔 때만. 표시는 그걸로 충분하다.
                if (_tacticalCooldown == 0f ||
                    Mathf.FloorToInt(_tacticalCooldown * 10f) != _tacticalShown)
                {
                    _tacticalShown = Mathf.FloorToInt(_tacticalCooldown * 10f);
                    _bus.Publish(new TacticalCooldownEvent
                    {
                        Remain = _tacticalCooldown, Total = _config.TacticalCooldownSeconds,
                    });
                }
                if (_tacticalCooldown == 0f) RefreshPossessTarget();
            }

            if (_ghostProtect > 0f)
            {
                _ghostProtect = Mathf.Max(0f, _ghostProtect - dt);
                return;                       // 보호 중에는 자연 감소도 멈춘다
            }
            if (_host != null) return;        // 몸이 있으면 시계가 멈춘다

            _drainCarry += _config.GhostDrainPerSecond * dt;
            int whole = Mathf.FloorToInt(_drainCarry);
            if (whole <= 0) return;

            _drainCarry -= whole;
            _ghostHp = Mathf.Max(0, _ghostHp - whole);
            PublishHp();
            if (_ghostHp == 0) Finish(false);
        }

        /// <summary>
        /// 긴급 호스트 (기획서 A 8-3).
        ///
        /// 빙의할 몸이 하나도 없으면 시계만 도는 상태가 된다 — 특히 보스방은 보스가
        /// 빙의 대상이 아니라(A 1-4) 손쓸 방법이 아예 없다. 1초를 기다린 뒤 몸을 하나
        /// 만들어 준다. 대신 값이 비싸다 — Ghost HP 를 추가로 깎고, 시작 체력이 30%다.
        /// **구제책이지 선택지가 아니다.** 방마다 한 번뿐이고, 못 쓰면 그대로 패배다.
        /// </summary>
        private void TickEmergency(float dt)
        {
            if (_host != null || _awaitingBuff) { _emergencyWait = 0f; return; }
            if (_possessTarget != null || HasPossessableTarget()) { _emergencyWait = 0f; return; }

            _emergencyWait += dt;
            if (_emergencyWait < _config.EmergencyDelaySeconds) return;
            _emergencyWait = 0f;

            // 기획서 A 8-1 — 빙의 대상이 없고 긴급 호스트도 못 쓰면 그 자리에서 진다.
            if (_emergencyUsedThisRoom || _ghostHp <= _config.EmergencyGhostCost)
            {
                Finish(false);
                return;
            }

            var entry = PickPlayerHost();
            if (entry == null) { Finish(false); return; }

            _emergencyUsedThisRoom = true;
            _ghostHp = Mathf.Max(1, _ghostHp - _config.EmergencyGhostCost);

            EnterHost(entry, entry.HostKey, entry.NameKr, _ghost.Position,
                      _config.EmergencyHostHpPercent);
            _bus.Publish(new EmergencyHostEvent
            {
                HostKey = entry.HostKey, GhostCost = _config.EmergencyGhostCost,
            });
        }

        /// <summary>살아 있는 빙의 가능 적이 방에 남아 있는가. 사거리는 보지 않는다.</summary>
        private bool HasPossessableTarget()
        {
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null && _enemies[i].IsPossessable) return true;
            return false;
        }

        /// <summary>
        /// 플레이어가 데려온 몸. 로비에서 고른 호스트를 우선한다.
        /// 런 시작 몸과 긴급 투입 몸이 같은 것을 쓴다 — 고른 캐릭터가 곧 내 캐릭터다.
        /// </summary>
        private HostEntry PickPlayerHost()
        {
            if (_player == null || !_player.IsReady) return null;
            var picked = _player.GetHost(_player.SelectedHostId);
            if (picked != null) return picked;
            var all = _player.AllHosts;
            return all != null && all.Count > 0 ? all[0] : null;
        }

        private void TickPlayer(float dt)
        {
            var me = Avatar;
            if (me == null) return;
            me.TickFlash(dt);
            me.TickAnim(dt);

            // 빙의가 들어가는 중에는 조작을 받지 않는다. 안 막으면 조이스틱이
            // 빨려 들어가는 연출과 서로 자리를 다툰다.
            if (IsChanneling) return;

            // ⚠️ 이것이 없으면 **갇힌다.** `SlideMove` 는 막힌 곳에 "들어가지 않게" 막는
            //    방식이라, 어쩌다 안에 들어간 뒤에는 어느 쪽으로도 못 나온다 —
            //    모든 후보 위치가 똑같이 막힌 것으로 판정되어 제자리를 돌려준다.
            //    적과 소환물에는 이 구제책이 걸려 있었는데 플레이어만 빠져 있었다.
            //    빙의 교체·밀림·방 진입 스폰으로 겹치면 그 판이 끝난다.
            ResolveObstacles(me);

            // ⚠️ 궁수의 전설 규칙 — **움직이는 동안에는 쏘지 않는다.**
            //    이동과 공격이 배타적이어야 "자리를 잡을까 딜을 넣을까"의 긴장이 생긴다.
            //    이걸 없애면 조작이 그냥 산책이 된다.
            bool moving = MoveInput.sqrMagnitude > 0.0001f;
            if (!moving) me.SetMoving(false);
            if (moving)
            {
                // 막힌 것을 타고 미끄러진다. 밀어 넣고 빼내면 벽에서 캐릭터가 떨린다.
                var before = me.Position;
                var p = SlideMove(me, me.Position,
                                  MoveInput * (me.MoveSpeed * _buffs.MoveMul) * dt);
                var half = me.GetComponent<RectTransform>().sizeDelta * 0.5f;
                p.x = Mathf.Clamp(p.x, half.x, _roomSize.x - half.x);
                p.y = Mathf.Clamp(p.y, -_roomSize.y + half.y, -half.y);
                me.Position = p;
                var moved = p - before;

                // 걷는 쪽을 바라본다. 아래 `return` 때문에 이동 중에는 조준 쪽
                // 방향 전환에 도달하지 못하므로, 여기서 돌려 주지 않으면
                // 이동 중에는 방향이 통째로 멈춘다.
                // 이동 중 사격이 되는 호스트는 아래에서 조준 방향이 덮어쓴다 —
                // 겨누는 쪽이 걷는 쪽보다 우선이다.
                // 민 방향이 아니라 **실제로 간 방향**으로 돈다. 벽에 스쳐 미끄러질 때
                // 민 쪽을 보면 벽을 향해 옆걸음질하는 그림이 된다.
                me.SetFacing(moved.sqrMagnitude > 0.0001f ? moved : MoveInput);
                me.SetMoving(true);

                // 기획서 A 3-3 Move Attack — 이동 중 사격은 **예외 호스트에만** 허용한다.
                // 전부 허용하면 멈출 이유가 없어져 위 규칙이 죽는다.
                bool moveAttack = _host != null && _host.Profile != null && _host.Profile.MoveAttack;
                if (!moveAttack)
                {
                    _stopTimer = 0f;
                    IsFiring = false;
                    return;
                }
            }

            // 멈춘 직후 아주 짧게 준비 시간을 둔다. 없으면 톡톡 끊어 눌러도 손해가 없어
            // 멈춤의 대가가 사라진다.
            _stopTimer += dt;
            if (_stopTimer < Mathf.Max(0.02f, _config.AttackResumeSeconds - _buffs.StopDelayCut))
            { IsFiring = false; return; }

            // 고스트는 공격하지 않는다 — 빙의해야 싸울 수 있다(핵심 동사)
            if (_host == null) { IsFiring = false; return; }

            // 시너지가 평소 사격보다 먼저다. 앞 몸이 남긴 표식이 있으면
            // 그것을 쓰는 것이 이 조합을 만든 이유다.
            if (TryBlinkExecution(_host, dt)) { IsFiring = true; return; }

            var target = Nearest(_host.Position);

            // 노리는 쪽을 바라본다. 사거리 밖이라 아직 안 쏘더라도 몸은 돌려 둔다 —
            // 조준이 먼저 보이고 사격이 뒤따라야 "겨눈다"는 느낌이 난다.
            if (target != null) _host.SetFacing(target.Position - _host.Position);

            // 근접은 붙어야 때린다 — 적과 같은 규칙이다. 전역 사거리를 900 으로
            // 올려 두어서 그대로 두면 아마존 주먹이 방 건너편까지 닿는다.
            bool inRange = target != null &&
                           Vector2.Distance(_host.Position, target.Position)
                               <= EffectiveRange(_host) * _buffs.RangeMul;
            IsFiring = inRange;
            if (!inRange) return;
            // 버프는 유닛 스탯을 덮어쓰지 않고 발사 시점에 곱한다 (빙의로 몸이 바뀌어도 유지)
            if (!_host.TickAttack(dt, _buffs.IntervalMul)) return;
            PerformAttack(_host, target, true);
        }

        private void SyncFireRing()
        {
            if (_host != null) _host.SetFiring(IsFiring);
        }

        private void TickEnemies(float dt)
        {
            if (_burnSpreadTimer > 0f) _burnSpreadTimer -= dt;
            var me = Avatar;
            if (me == null) return;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                e.TickFlash(dt);
                e.TickAnim(dt);
                e.TickSlow(dt);

                // 화상 피해는 유닛이 스스로 깎지 않는다 — 죽음 처리·보상·피해 숫자가
                // 전부 여기 있어서, 저쪽에서 깎으면 죽어도 아무 일도 안 일어난다.
                int burn = e.TickStatus(dt);
                if (burn > 0)
                {
                    // 정본 BUF_T02 — 3단계 화상이 주변으로 옮는다.
                    // 화상 틱은 3단계에서 0.06초마다 떨어지므로 그대로 두면 초당 열몇 번
                    // 옮는다. 초에 한 번으로 묶는다.
                    if (_buffs.BurnSpreads && e.BurnStack >= Unit.StatusMaxStack)
                        SpreadBurn(e);

                    ShowDamage(e.Position, burn, toEnemy: true);
                    if (e.TakeDamage(burn)) { KillEnemy(e); continue; }
                    if (e.IsBoss)
                        _bus.Publish(new BossHpChangedEvent { BossHp = e.Hp, BossHpMax = e.HpMax });
                }

                // 기획서 A 1-1 — 유령은 **적과 충돌하지 않고 표적도 되지 않는다.**
                // 몸이 없는 동안에는 적도 보스도 쫓거나 때리지 않는다. 유령 상태의
                // 압박은 맞아 죽는 것이 아니라 초당 깎이는 시계(A 1-2)에서 온다.
                // 이미 날아가고 있는 탄은 그대로 맞는다 — 그건 조준이 아니라 잔탄이다.
                //
                // 보스방에서 특히 중요하다. 보스는 빙의 대상이 아니라(A 1-4) 몸을 잃으면
                // 반격 수단이 없다. 계속 맞으면 전투가 아니라 처형이 된다.
                if (_host == null)
                {
                    e.IsAggro = false;
                    e.SetState(EnemyState.Idle);
                    if (!e.IsBoss) Separate(e, i, dt);
                    continue;
                }

                // 보스는 쿨다운으로 여러 패턴을 돌린다 — 잡몹 AI 를 태우지 않는다
                if (e.IsBoss) { TickBoss(e, me, dt); continue; }

                float d = Vector2.Distance(e.Position, me.Position);

                // 탐지 — 들어오기 전에는 제자리에서 기다린다.
                // 처음부터 전부 달려들면 방이 통째로 한 덩어리가 되어 몰려다닌다.

                if (!e.IsAggro)
                {
                    // 화면 밖에서는 깨어나지 않는다. 정본의 `NO_OFFSCREEN_TELEGRAPH` —
                    // 보이지도 않는 곳에서 예고 없이 날아오는 공격은 피할 방법이 없다.
                    //
                    // 거리는 더 이상 보지 않는다. 화면에 보이면 곧 싸움이다 —
                    // 탐지 거리를 두면 방에 들어가서 한참을 걸어가야 교전이 시작되고,
                    // 그 사이가 그냥 빈 시간이 된다.
                    if (!IsOnScreen(e))
                    {
                        e.SetState(EnemyState.Idle);
                        Separate(e, i, dt);
                        continue;
                    }
                    e.IsAggro = true;
                    e.SetState(EnemyState.Detect);
                }

                // 기획서 A 4-1 — Detect → Approach → Attack → Cooldown.
                // 상태를 이름으로 들고 있어야 AI 타입별 분기를 넣을 자리가 생긴다.
                e.SetFacing(me.Position - e.Position);   // 적도 플레이어를 바라본다

                // 근접과 원거리는 다르게 움직인다.
                //
                //   근접  붙어야 때린다. 사거리가 짧으므로 계속 쫓는다
                //   원거리 **두 번 쏘고 한 번 자리를 옮긴다.** 가만히 서서 계속 쏘면
                //          한 자리에 붙박여 있어 피하기만 하면 되는 과녁이 되고,
                //          계속 쫓아오면 붙어 버려 사거리의 의미가 없다
                float reach = EffectiveRange(e);

                if (d > reach)
                {
                    e.CancelWindup();   // 사거리 밖으로 밀려났으면 자세를 푼다
                    e.SetState(EnemyState.Approach);
                    e.Position = SlideMove(e, e.Position, e.StepToward(me.Position, dt));
                    e.SetMoving(true);
                }
                // 자세를 잡는 중 — 아직 안 때린다. 이 틈이 피하거나 파고들 시간이다
                else if (e.IsWindingUp)
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.TickWindup(dt))
                    {
                        PerformAttack(e, me, false);
                        if (!IsMelee(e) && e.CountShotAndNeedsMove(ShotsBeforeMove))
                            e.BeginReposition(PickRepositionSpot(e, me));
                    }
                }
                else if (e.IsRepositioning)
                {
                    // 자리를 옮기는 중 — 쏘지 않는다. 이 틈이 곧 반격할 틈이다
                    e.SetState(EnemyState.Cooldown);
                    e.Position = SlideMove(e, e.Position, e.StepToward(e.RepositionTarget, dt));
                    e.SetMoving(true);
                    if (Vector2.Distance(e.Position, e.RepositionTarget) < 24f) e.EndReposition();
                }
                else if (e.TickAttack(dt))
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);

                    float windup = e.Profile?.CanonTelegraph ?? 0f;
                    if (windup > 0f && CanStartAttack(e))
                    {
                        e.BeginWindup(windup);
                    }
                    else if (windup <= 0f)
                    {
                        // 정본에 예고가 없는 배우는 예전처럼 바로 때린다
                        PerformAttack(e, me, false);
                        if (!IsMelee(e) && e.CountShotAndNeedsMove(ShotsBeforeMove))
                            e.BeginReposition(PickRepositionSpot(e, me));
                    }
                    // 동시 공격 수가 찼으면 이번 차례는 거른다 — 다음 간격에 다시 본다
                }
                else
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                }

                Separate(e, i, dt);
            }
        }

        // ── 적 행동 거리 ──────────────────────────────────────────

        /// <summary>
        /// 지금 때리기 시작해도 되는가.
        ///
        /// 정본은 배우마다 **동시에 때릴 수 있는 마릿수**(MaxConcurrent 1~4)를 정해 둔다.
        /// 제한이 없으면 방 안 전원이 같은 순간에 쏴서, 근접으로는 들어갈 틈이 아예 없다.
        /// 순서를 정하지 않고 먼저 준비를 시작한 쪽이 자리를 차지한다 — 나머지는 다음 간격에
        /// 다시 본다. 그래야 사격이 한 덩어리가 아니라 물결처럼 나뉜다.
        ///
        /// 마릿수가 한 자릿수라 매번 세도 된다. 목록을 따로 들고 있으면 죽거나 빙의로
        /// 편이 바뀔 때마다 어긋난다.
        /// </summary>
        private bool CanStartAttack(Unit e)
        {
            int cap = e.Profile?.CanonMaxConcurrent ?? 0;
            if (cap <= 0) return true;

            int busy = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var o = _enemies[i];
                if (o == e || o == null || !o.IsAlive) continue;
                if (o.Key == e.Key && o.IsWindingUp && ++busy >= cap) return false;
            }
            return true;
        }

        /// <summary>몇 발 쏘고 자리를 옮기는가 (원거리).</summary>
        private const int ShotsBeforeMove = 2;
        private const float RepositionDistance = 190f;

        private static bool IsMelee(Unit e)
        {
            var k = e.Profile?.Kind;
            return k == AttackKind.Melee || k == AttackKind.Pulse;
        }

        /// <summary>
        /// 실제로 때릴 수 있는 거리.
        ///
        /// 정본 사거리는 근접도 1.2~1.8m(103~155px)라 그대로 쓰면 된다. 다만 그림 크기
        /// 때문에 맞붙어도 중심 사이가 90px 이라, 그보다 짧은 값이 들어오면 영영 닿지
        /// 않는다 — 근접에만 바닥을 깔아 준다.
        /// </summary>
        private float EffectiveRange(Unit e)
            => IsMelee(e) ? Mathf.Max(e.AttackRange, _config.MeleeAttackRange) : e.AttackRange;

        // ─────────────────────────────────────────
        // 정본 실수치 해석
        //
        // 정본(CH01_03_RUNTIME_DATA)은 배우마다 HP·공격력·이동속도·사거리·간격을
        // 실제 값으로 준다. 그 값이 있으면 **그대로** 쓴다.
        // 정본에 없는 창작 배우만 예전의 "표시 스탯 × 배율" 공식으로 되돌아간다.
        //
        // 거리·속도는 정본이 미터 단위다. 방 크기에서 얻은 _pxPerMeter 로 환산한다.
        // ─────────────────────────────────────────

        private int EnemyHpOf(HostEntry e)
            => e.HasCanon ? e.CanonHp : _config.EnemyHp(e.Hp);

        private int EnemyAtkOf(HostEntry e)
            => e.HasCanon ? e.CanonAtk
                          : Mathf.RoundToInt(_config.EnemyAtk(e.Atk) * e.DamageMul);

        // 적은 늘 싸우러 오는 중이다 — 정본의 EngageSpeed 를 쓴다.
        // MoveSpeed(1.0~2.5m/s)는 교전 전의 걸음이라, 그걸 쓰면 내(3.4~5.2)가 뒤로 걷기만 해도
        // 영영 안 잡힌다. 근접 적은 한 번도 닿지 못하고 과녁이 된다.
        private float EnemySpeedOf(HostEntry e)
            => e.HasCanon ? e.CanonEngageSpeed * _pxPerMeter : _config.EnemySpeed(e.Spd);

        private float EnemyRangeOf(HostEntry e)
            => e.HasCanon ? e.CanonRange * _pxPerMeter : _config.EnemyAttackRange * e.RangeMul;

        private float EnemyIntervalOf(HostEntry e)
            => e.HasCanon ? e.CanonInterval : _config.EnemyAttackInterval * e.IntervalMul;

        // 내가 탄 몸. 정본은 같은 배우라도 **적일 때와 내가 탔을 때 교전값을 따로** 준다
        // (attacks 의 AP_E### / AP_H##). 체력·공격력은 몸 자체의 것이라 적일 때와 같다.

        private int HostHpOf(HostEntry e)
            => e == null ? 100 : e.HasCanon ? e.CanonHp : _config.HostHp(e.Hp);

        private int HostAtkOf(HostEntry e)
            => e == null ? 10 : e.HasCanon ? e.CanonAtk
                                           : Mathf.RoundToInt(_config.HostAtk(e.Atk) * e.DamageMul);

        // 이동속도만은 교전 프로필이 안 맞아도 호스트 값을 쓴다 — 근접이냐 원거리냐와
        // 상관없는 값이라, 적 걸음으로 조종하게 두면 그 몸만 못 쓰게 된다.
        private float HostSpeedOf(HostEntry e)
            => e == null ? 180f
             : e.CanonHostMoveSpeed > 0f ? e.CanonHostMoveSpeed * _pxPerMeter
             : e.HasCanon                ? e.CanonMoveSpeed * _pxPerMeter
             : _config.HostSpeed(e.Spd);

        // 호스트 프로필이 없으면 그 배우가 적일 때 쓰던 값을 그대로 쓴다.
        // 예전 공식(전역 900 × 배율)으로 돌아가면 정본 배우들과 격이 어긋난다.
        private float HostRangeOf(HostEntry e)
            => e == null ? _config.HostAttackRange
             : e.HasCanonHost ? e.CanonHostRange * _pxPerMeter
             : e.HasCanon     ? e.CanonRange * _pxPerMeter
             : _config.HostAttackRange * e.RangeMul;

        /// <summary>
        /// 탄속. 정본은 배우마다 다르게 준다 — 닌자 탄이 12.5m/s, 잡몹이 8.5m/s 다.
        /// 전부 같은 속도로 날면 "저 탄은 빠르니 지금 피해야 한다" 는 판단이 안 생긴다.
        /// </summary>
        private float ShotSpeedOf(HostEntry e, bool fromPlayer)
        {
            float mps = e == null ? 0f
                      : fromPlayer ? (e.CanonHostShotSpeed > 0f ? e.CanonHostShotSpeed
                                                                : e.CanonShotSpeed)
                      : e.CanonShotSpeed;
            return mps > 0f ? mps * _pxPerMeter
                            : fromPlayer ? _config.ShotSpeedPlayer : _config.ShotSpeedEnemy;
        }

        private float HostIntervalOf(HostEntry e)
            => e == null ? _config.HostAttackInterval
             : e.HasCanonHost ? e.CanonHostInterval
             : e.HasCanon     ? e.CanonInterval
             : _config.HostAttackInterval * e.IntervalMul;

        /// <summary>
        /// 옮겨 갈 자리. 플레이어를 계속 사거리 안에 두되 **옆으로** 돈다 —
        /// 뒤로 물러나면 도망으로 보이고, 앞으로 가면 근접과 다를 바가 없다.
        /// </summary>
        private Vector2 PickRepositionSpot(Unit e, Unit me)
        {
            var toMe = me.Position - e.Position;
            var side = new Vector2(-toMe.y, toMe.x).normalized;
            if (((e.GetInstanceID() + _roomIndex) & 1) == 0) side = -side;

            var spot = e.Position + side * RepositionDistance;
            var half = FootHalf(e);
            spot.x = Mathf.Clamp(spot.x, half.x, _roomSize.x - half.x);
            spot.y = Mathf.Clamp(spot.y, -_roomSize.y + half.y, -half.y);
            return spot;
        }

        /// <summary>
        /// 서로 겹치지 않게 밀어낸다.
        ///
        /// 전부 같은 목표(플레이어)로 달려가면 사거리가 비슷한 개체끼리 같은 지점에 겹쳐
        /// 한 마리처럼 보인다. 가까운 개체끼리만 반대로 밀어 덩어리를 푼다.
        /// </summary>
        private void Separate(Unit e, int index, float dt)
        {
            float r = _config.EnemySeparation;
            if (r <= 0f) return;

            Vector2 push = Vector2.zero;
            for (int j = 0; j < _enemies.Count; j++)
            {
                if (j == index) continue;
                var o = _enemies[j];
                if (o == null || !o.IsAlive) continue;

                var diff = e.Position - o.Position;
                float dist = diff.magnitude;
                if (dist >= r) continue;
                // 겹쳐 있으면 방향이 없다 — 인덱스로 갈라 서로 반대로 민다
                if (dist < 0.01f) { push += new Vector2((index % 2 == 0) ? 1f : -1f, 0.3f); continue; }
                push += diff / dist * (1f - dist / r);
            }
            if (push.sqrMagnitude < 0.0001f) return;

            e.Position += push.normalized * (e.MoveSpeed * SeparationSpeedRatio) * dt;
            ClampToField(e);
            ResolveObstacles(e);   // 스폰이나 밀림으로 갇힌 경우의 구제책
        }

        // ── 보스 ─────────────────────────────────────────────────

        /// <summary>
        /// 페이즈가 바뀌는 순간에 하는 일.
        ///
        /// 정본이 가장 세게 못박은 것이 "페이즈마다 **행동이** 바뀐다"는 것이다.
        /// 수치만 오르는 것은 페이즈가 아니라고 적혀 있다. 지금 우리가 데이터에서
        /// 그대로 살릴 수 있는 것은 둘이다 —
        ///   · **잡몹 소환**(MinionPool). 정본은 이것을 "교체 창"이라고 부른다.
        ///     보스는 빙의할 수 없으니, 몸을 갈아탈 기회는 이때 부르는 잡몹뿐이다.
        ///   · **예고 시간**. 페이즈마다 다르고, 피할 수 있느냐를 가르는 값이다.
        ///
        /// 이름 붙은 패턴(BurrowTrack·ConveyorReverse 등)은 그림과 함께 와야 해서
        /// 아직 우리 볼리로 흉내낸다. 페이즈마다 탄 수·확산·간격이 갈리게 해 뒀다.
        /// </summary>
        private void EnterBossPhase(Unit boss, int phase)
        {
            var def = _canonRoom != null ? _canonRoom.BossPhase(phase) : null;
            if (def == null) return;

            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || def.MinionPool.Count == 0) return;

            // 보스 좌우로 벌려 세운다. 보스 위에 겹치면 누가 누군지 안 보인다.
            for (int i = 0; i < def.MinionPool.Count; i++)
            {
                var e = ActorProfile(def.MinionPool[i], hosts);
                if (e == null) continue;

                var u = NewUnit($"Minion_{def.MinionPool[i]}_P{phase}");
                u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, UnitGet(e.SpriteKey),
                        EnemyHpOf(e),
                        EnemyAtkOf(e),
                        EnemySpeedOf(e),
                        EnemyRangeOf(e),
                        EnemyIntervalOf(e),
                        new Vector2(84f, 78f), isBoss: false, profile: e);

                float side = i % 2 == 0 ? -1f : 1f;
                float row = i / 2 * 90f;
                u.Position = new Vector2(
                    Mathf.Clamp(boss.Position.x + side * 210f, 60f, _roomSize.x - 60f),
                    Mathf.Clamp(boss.Position.y - 130f - row, -_roomSize.y + 60f, -60f));
                u.PossessPriority = e.PossessPriority;
                u.IsAggro = true;          // 불러낸 것들은 기다리지 않는다
                u.SetState(EnemyState.Detect);
                ApplyFacingSprites(u, e.SpriteKey);
                _enemies.Add(u);
            }

            _bus.Publish(new BossPhaseEvent
            {
                Phase = phase, Pattern = def.Pattern, MinionCount = def.MinionPool.Count,
            });
        }

        private void TickBoss(Unit boss, Unit me, float dt)
        {
            int before = _brain.Phase;
            _brain.UpdatePhase((float)boss.Hp / boss.HpMax);
            if (_brain.Phase != before)
            {
                _bus.Publish(new BossHpChangedEvent
                {
                    BossHp = boss.Hp, BossHpMax = boss.HpMax, Phase = _brain.Phase,
                });
                EnterBossPhase(boss, _brain.Phase);
            }

            // 돌진 중에는 다른 행동을 하지 않는다. 접촉하면 피해를 주고 멈춘다.
            if (_brain.ChargeLeft > 0f)
            {
                boss.Position += _brain.ChargeDir * (boss.MoveSpeed * ChargeSpeedMul) * dt;
                if (Vector2.Distance(boss.Position, me.Position) <= _config.ShotHitRadius * 1.6f)
                {
                    DamagePlayer(Mathf.RoundToInt(boss.Atk * _chargeDamageMul));
                    _brain.BeginCharge(Vector2.zero, 0f);
                }
                return;
            }

            TickBossPending(dt);

            var move = _brain.Tick(dt);

            // 예고 중에는 제자리에서 번쩍인다. 피할 시간을 주지 않으면 패턴이 아니라 사고다.
            if (_brain.IsTelegraphing) { _telegraphPulse += dt; PulseTelegraph(boss); return; }

            if (move == null)
            {
                // 쿨다운 대기 중에는 천천히 접근만 한다
                if (Vector2.Distance(boss.Position, me.Position) > boss.AttackRange)
                    boss.MoveToward(me.Position, dt);
                boss.SetTelegraph(false);
                return;
            }

            boss.SetTelegraph(false);
            ExecuteBossMove(boss, me, move);
        }

        private void PulseTelegraph(Unit boss)
        {
            boss.SetTelegraph(Mathf.Repeat(_telegraphPulse, 0.16f) < 0.08f);
        }

        // ── 정본이 이름 붙인 보스 패턴 셋 ─────────────────────────
        //
        // 지금까지는 셋 다 "부채꼴·돌진·링" 으로 흉내만 냈다. 수치만 다른 같은 패턴은
        // 정본이 못박은 "페이즈마다 행동이 바뀐다" 를 만족하지 못한다.

        private const int LaneCount = 3;
        private const float CrackSeconds = 0.85f;   // 정본 B01 예고 0.85초
        private const float BeamSeconds = 0.7f;
        private const float SlamSeconds = 1.0f;     // 정본 B02 예고 1.0초
        private const float ShieldSeconds = 4.0f;
        private const float VenomSeconds = 5.0f;

        private int _laneParity;                    // 번갈아 — 쓸 때마다 줄이 바뀐다
        private float _pendingTimer;
        private int _pendingDamage;
        private Vector2 _pendingAt;
        private bool _pendingIsBeam;
        private int _pendingLane = -1;

        /// <summary>보스 실드가 남은 시간. 0 보다 크면 받는 피해가 줄어든다.</summary>
        private float _bossShield;
        private const float BossShieldDamageMul = 0.45f;

        /// <summary>
        /// 번갈아 솟는 레이저. 바닥에 **금이 먼저 간다** — 어디서 솟는지 안 보이면
        /// 예고가 아니라 사고다. 금이 사라지는 순간 그 자리에서 광선이 솟는다.
        /// </summary>
        private void PopupLaser(int damage)
        {
            _laneParity = 1 - _laneParity;
            float laneW = _roomSize.x / LaneCount;
            for (int i = _laneParity; i < LaneCount; i += 2)
            {
                float x = laneW * (i + 0.5f);
                SpawnField(new Vector2(x, -_roomSize.y * 0.5f), laneW * 0.42f,
                           CrackSeconds, FieldEffect.Damage, 0, fromPlayer: false);
            }
            _pendingIsBeam = true;
            _pendingLane = _laneParity;
            _pendingDamage = damage;
            _pendingTimer = CrackSeconds;
        }

        /// <summary>
        /// 실드를 두르고 내려찍는다. 실드 동안 피해가 줄어드는 것이 핵심이다 —
        /// "지금은 때릴 때가 아니라 피할 때" 라는 구간을 만든다.
        /// </summary>
        private void ShieldCycle(Unit boss, Unit me, int damage)
        {
            _bossShield = ShieldSeconds;
            boss.SetTelegraph(true);
            SpawnField(me.Position, 150f, SlamSeconds, FieldEffect.Damage, 0, fromPlayer: false);
            _pendingIsBeam = false;
            _pendingAt = me.Position;
            _pendingDamage = damage;
            _pendingTimer = SlamSeconds;
        }

        /// <summary>독구름. 플레이어가 선 자리를 물들여 그 자리를 못 쓰게 만든다.</summary>
        private void VenomCloud(Unit me, int count)
        {
            int n = Mathf.Clamp(count, 1, 2);
            for (int i = 0; i < n; i++)
            {
                var off = i == 0 ? Vector2.zero
                                 : new Vector2(UnityEngine.Random.Range(-160f, 160f), UnityEngine.Random.Range(-160f, 160f));
                SpawnField(me.Position + off, 150f, VenomSeconds,
                           FieldEffect.Curse, 4, fromPlayer: false);
            }
        }

        private void TickBossPending(float dt)
        {
            if (_bossShield > 0f) _bossShield -= dt;
            if (_pendingTimer <= 0f) return;
            _pendingTimer -= dt;
            if (_pendingTimer > 0f) return;

            if (_pendingIsBeam)
            {
                float laneW = _roomSize.x / LaneCount;
                for (int i = _pendingLane; i < LaneCount; i += 2)
                    SpawnField(new Vector2(laneW * (i + 0.5f), -_roomSize.y * 0.5f),
                               laneW * 0.42f, BeamSeconds, FieldEffect.Damage,
                               _pendingDamage, fromPlayer: false);
            }
            else
            {
                SpawnField(_pendingAt, 150f, 0.35f, FieldEffect.Damage,
                           _pendingDamage, fromPlayer: false);
            }
            _pendingLane = -1;
        }

        private void ExecuteBossMove(Unit boss, Unit me, BossMove m)
        {
            int dmg = Mathf.RoundToInt(boss.Atk * m.DamageMul);
            switch (m.Pattern)
            {
                case BossPattern.Volley:
                    FireFan(boss, me.Position, m.ShotCount, m.SpreadDegrees, dmg);
                    break;

                case BossPattern.Ring:
                    // 사방 360° — 붙어 있으면 피하기 어렵다. 거리를 벌리게 만드는 패턴.
                    FireFan(boss, me.Position, m.ShotCount, 360f - 360f / m.ShotCount, dmg);
                    break;

                case BossPattern.AimedBurst:
                    FireFan(boss, me.Position, m.ShotCount, m.SpreadDegrees, dmg);
                    break;

                case BossPattern.Charge:
                    _chargeDamageMul = m.DamageMul;
                    _brain.BeginCharge(me.Position - boss.Position, ChargeSeconds);
                    break;

                case BossPattern.Summon:
                    SummonMinions(boss, m.ShotCount);
                    break;

                case BossPattern.PopupLaser:  PopupLaser(dmg); break;
                case BossPattern.ShieldCycle: ShieldCycle(boss, me, dmg); break;
                case BossPattern.VenomCloud:  VenomCloud(me, m.ShotCount); break;
            }
        }

        /// <summary>
        /// 부채꼴로 흩뿌린다. 보스 패턴과 플레이어 얼티밋이 함께 쓴다.
        ///
        /// ⚠ <paramref name="fromPlayer"/> 를 반드시 넘긴다. 이 값이 틀리면 **내가 쏜 탄이
        ///    나를 때린다** — 부채꼴의 시작점이 곧 내 자리라 16발이 그 자리에서 전부
        ///    나에게 꽂힌다. 실제로 얼티밋을 붙이다가 한 번 그렇게 만들었다.
        /// </summary>
        private void FireFan(Unit from, Vector2 at, int count, float spanDeg, int damage,
                             bool fromPlayer = false)
        {
            // 탄은 **화면 끝까지 나가야 한다.** 수명이 짧으면 중간에 사라져
            // 멀찍이 떨어진 곳이 안전지대가 되고, 탄막을 피할 이유가 없어진다.
            float speed = fromPlayer
                ? _config.ShotSpeedPlayer * _buffs.ShotSpeedMul
                : _config.ShotSpeedEnemy;
            float reach = _roomSize.magnitude;
            float life = reach / Mathf.Max(1f, speed) + 0.25f;

            for (int i = 0; i < count; i++)
            {
                float off = count == 1 ? 0f : -spanDeg * 0.5f + spanDeg * i / (count - 1);
                var shot = RentShot();
                if (shot == null) return;
                shot.SetSprite(ShotSpriteOf(from), ShotKindOf(from));
                shot.Fire(from.Position, at, speed, damage,
                          fromPlayer, null, _config.ShotSize * 1.15f,
                          fromPlayer ? ShotPlayerColor : ShotBossColor,
                          life, angleOffsetDeg: off,
                          bounces: fromPlayer ? _buffs.Bounces : 0);
            }
        }

        /// <summary>보스만 노리다 둘러싸이게 만든다. 방 상한을 넘지 않게 막는다.</summary>
        private void SummonMinions(Unit boss, int count)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;
            if (_enemies.Count > MaxRoomUnits) return;

            for (int i = 0; i < count; i++)
            {
                var e = hosts[(_enemies.Count * 3 + i * 7) % hosts.Count];
                var u = NewUnit($"Minion_{e.HostKey}_{_enemies.Count}");
                u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, UnitGet(e.SpriteKey),
                        Mathf.Max(1, Mathf.RoundToInt(EnemyHpOf(e) * 0.6f)),
                        EnemyAtkOf(e),
                        EnemySpeedOf(e),
                        EnemyRangeOf(e),
                        EnemyIntervalOf(e),
                        new Vector2(78f, 72f), isBoss: false, profile: e);

                // 보스(160px)와 겹치지 않게 바깥에 원형으로 흩는다
                float a = (i / (float)count) * Mathf.PI * 2f + _enemies.Count * 0.7f;
                u.Position = boss.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * SummonRadius;
                u.IsAggro = true;   // 불러낸 것들은 기다리지 않는다
                ClampToField(u);
                ResolveObstacles(u);
                _enemies.Add(u);
            }
        }

        // ── 공격 방식 ────────────────────────────────────────────
        // 호스트마다 교전 거리·탄 수·발사 간격이 달라야 "어떤 몸을 뺏었는가"에 의미가 생긴다.
        // 사거리·간격·피해 배율은 Setup 시점에 이미 반영돼 있다(Unit.Atk/AttackRange/AttackInterval).

        private void PerformAttack(Unit attacker, Unit target, bool fromPlayer)
        {
            // 공격 방식과 무관하게 몸은 똑같이 쏘는 동작을 한다.
            // 여기 한 곳에서 켜야 근접·원거리·보스가 따로 놀지 않는다.
            attacker.PlayAttack();

            var p = attacker.Profile;
            var kind = p?.Kind ?? AttackKind.Single;

            switch (kind)
            {
                case AttackKind.Melee:
                case AttackKind.Pulse:
                    MeleeStrike(attacker, target, fromPlayer, hitAll: kind == AttackKind.Pulse);
                    break;

                default:
                {
                    // 다중 사격 버프는 확산이 아닌 방식에도 탄을 더한다 (플레이어 한정)
                    int extra = fromPlayer ? _buffs.ExtraShots : 0;
                    // 정본 projectiles 는 확산이 아닌 몸에도 탄 수를 준다 (갱스터 3발).
                    // 확산일 때만 세면 그 값이 버려진다.
                    int shots = p == null ? 1 : fromPlayer ? p.ShotCount : p.EnemyShotCount;
                    int n = shots + extra;
                    float span = kind == AttackKind.Spread ? p.SpreadDegrees : 0f;
                    // 확산이 아닌데 탄이 여럿이면 좁게 흩는다 — 0 이면 전부 겹쳐 한 발로 보인다
                    if (span <= 0f && n > 1) span = 8f * (n - 1);
                    if (extra > 0) span = Mathf.Max(span, 10f * (n - 1));

                    // 정본은 공격력을 **한 번의 공격**에 준다. 탄 수는 따로 적혀 있으므로
                    // 나눠 실어야 탄 수가 그대로 화력 배수가 되지 않는다 — 확산은 맞히기
                    // 쉬운 대신 한 발이 약한 것이 맞다.
                    int split = Mathf.Max(1, shots);

                    // 정본 BUF_A01 마지막 탄창 — 확산의 **마지막 한 발**이 더 아프다.
                    // 마지막 발만 강하면 "다 맞히는 것" 이 아니라 "끝까지 붙어 있는 것" 이 이득이 된다.
                    for (int i = 0; i < n; i++)
                    {
                        float off = n == 1 ? 0f : -span * 0.5f + span * i / (n - 1);
                        FireShot(attacker, target, fromPlayer, off,
                                 lastShot: fromPlayer && i == n - 1, split: split);
                    }
                    break;
                }
            }
        }

        /// <summary>근접·광역은 탄을 쓰지 않고 즉시 판정한다. 대신 타격 위치에 섬광만 남긴다.</summary>
        /// <summary>근접으로 마무리했다. 시너지 계기이자 회복 시점이다.</summary>
        private void OnMeleeFinish(Unit attacker)
        {
            FireSynergy(SynergyTrigger.OnMeleeFinish, attacker.Key);
            if (!SynergyOn(SynergyKind.HealFinisher) || _host == null) return;
            _host.Heal(Mathf.Max(1, Mathf.RoundToInt(_host.HpMax * HealFinisherPercent / 100f)));
            PublishHp();
        }

        private void MeleeStrike(Unit attacker, Unit target, bool fromPlayer, bool hitAll)
        {
            var p = attacker.Profile;
            float reach = attacker.AttackRange * (fromPlayer ? _buffs.RangeMul : 1f);

            // 슬러거 "탄환 반사" — 휘두르는 범위 안의 적 탄을 지운다
            if (p != null && p.ReflectsShots)
            {
                for (int i = 0; i < _shots.Count; i++)
                {
                    var s = _shots[i];
                    if (!s.IsActive || s.FromPlayer == fromPlayer) continue;
                    if (Vector2.Distance(s.Position, attacker.Position) > reach) continue;
                    // 지우지 말고 **되받아친다.** 도탄이 생기기 전에는 지우는 수밖에
                    // 없었지만, 이제 방향을 뒤집으면 진짜 반사가 된다.
                    if (!s.Bounce((s.Position - attacker.Position).normalized)) s.Despawn();
                }
            }

            if (fromPlayer)
            {
                for (int i = _enemies.Count - 1; i >= 0; i--)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsAlive) continue;
                    if (Vector2.Distance(e.Position, attacker.Position) > reach) continue;
                    Burst(e.Position, true);
                    bool wasAlive = e.IsAlive;
                    HitEnemyWith(e, Mathf.RoundToInt(attacker.Atk * _buffs.AttackMul * MaintainDamageMul), p);
                    // 정본 S04 흡혈 마무리 — 근접으로 끝냈을 때만 회복이 터진다.
                    // 흡혈을 쌓는 몸과 터뜨리는 몸이 달라 **갈아타야만** 성립한다.
                    if (wasAlive && !e.IsAlive) OnMeleeFinish(attacker);
                    if (!hitAll) break;
                }
                return;
            }

            Burst(target.Position, false);
            DamagePlayer(attacker.Atk);
        }

        /// <summary>피해 없는 시각 효과. 근접 공격이 화면에서 아무 일도 없어 보이는 것을 막는다.</summary>
        private void Burst(Vector2 at, bool fromPlayer)
        {
            var v = RentShot();
            if (v == null) return;
            v.Fire(at, at + Vector2.up, 0f, 0, fromPlayer, null,
                   _config.ShotSize * 2.2f,
                   fromPlayer ? ShotPlayerColor : ShotEnemyColor, 0.12f);
        }

        /// <summary>
        /// 무기 계열이 곧 원소다. 불을 뿜는 캐릭터가 화상을 걸고, 냉기가 빙결을,
        /// 마법이 저주를 건다. 탄 그림을 나눈 그 표(`ShotKind`)를 그대로 쓴다 —
        /// 표를 둘로 두면 "레이저인데 화상이 걸리는" 어긋남이 반드시 생긴다.
        /// </summary>
        private static readonly Dictionary<string, string> StatusOfKind = new()
        {
            { "flame", "burn" }, { "frost", "freeze" }, { "magic", "curse" },
        };

        private void InflictStatus(Unit victim, HostEntry p)
        {
            var key = p?.HostKey;
            if (key == null || !ShotKind.TryGetValue(key, out var kind)) return;

            // 수류탄은 상태이상 대신 **터진 자리에 장판**을 남긴다 — 정본의 "지뢰 네트워크".
            // BUF_S04 를 고르면 그 지뢰가 빙결 룬이 된다.
            if (kind == "grenade")
            {
                bool freeze = _buffs.MinesFreeze || SynergyOn(SynergyKind.FreezeRune);
                SpawnField(victim.Position, MineRadius * _buffs.AoeMul, MineSeconds,
                           freeze ? FieldEffect.Freeze : FieldEffect.Damage,
                           freeze ? 0 : MineDamagePerTick, fromPlayer: true);
                return;
            }

            if (!StatusOfKind.TryGetValue(kind, out var status)) return;

            float sec = _config.SlowSeconds;   // 둔화와 같은 지속시간을 쓴다
            switch (status)
            {
                case "burn":   victim.ApplyBurn(sec); break;
                case "freeze": victim.ApplyFreeze(sec); break;
                case "curse":
                    victim.ApplyCurse(sec);
                    // 마법사는 발밑에 둔화 장판을 남긴다 — 정본의 "장판 제어" 훅이다.
                    // 이것이 있어야 BUF_T03(가장자리 피해)이 걸 곳을 갖는다.
                    SpawnField(victim.Position, SlowFieldRadius * _buffs.AoeMul, SlowFieldSeconds,
                               FieldEffect.Slow, 0, fromPlayer: true);
                    break;
            }
        }

        private const float MineRadius = 110f;
        private const float MineSeconds = 3.0f;
        private const int MineDamagePerTick = 6;
        private const float SlowFieldRadius = 130f;
        private const float SlowFieldSeconds = 2.5f;

        // ── 설치물(자동 포탑) ──────────────────────────────────────
        //
        // 정본에서 마지막까지 남아 있던 구멍. 로봇의 정체성("배치 네트워크")이자
        // 버프 2종·시너지 2종이 여기 매여 있었다.

        private const int MaxDeployables = 4;
        private const float DeploySeconds = 8f;
        private const float DeployRange = 420f;
        private const float DeployFireInterval = 0.85f;
        private const float DeployCooldown = 4.5f;
        private const string DeployHostKey = "robot";

        private readonly List<Deployable> _deployables = new();
        private float _deployCooldown;

        /// <summary>
        /// 로봇의 몸일 때만 포탑이 나간다. 쿨다운으로 저절로 깔린다 —
        /// 버튼을 하나 더 두면 조작이 늘고, 이 게임의 조작은 이동과 사격 둘뿐이다.
        /// </summary>
        /// <summary>
        /// 보스방은 제 바닥을 쓴다. 원작에서도 보스마다 아레나가 따로 있다 —
        /// 같은 던전 바닥 위에서 싸우면 "여기가 그 방" 이라는 느낌이 안 난다.
        /// 전용 바닥이 아직 없으면 기본 바닥으로 돌아간다.
        /// </summary>
        private void ApplyRoomFloor()
        {
            if (_floorImage == null) return;
            var key = _canonRoom != null && _canonRoom.IsBoss
                ? $"roomfloor_{BossKeyOfChapter()}"
                : null;
            var art = key != null ? GetSprite(key) : null;
            _floorImage.sprite = art ?? _defaultFloor;
        }

        private string BossKeyOfChapter()
        {
            int chapter = _player != null ? _player.CurrentChapter : 1;
            var def = _bossTable != null ? _bossTable.ForChapter(chapter) : null;
            return def != null ? def.BossKey : "robot_snakes";
        }

        private void TickDeploy(float dt)
        {
            if (_deployCooldown > 0f) _deployCooldown -= dt;

            var me = Avatar;
            if (me == null || _host == null || _host.Key != DeployHostKey) return;
            if (_deployCooldown > 0f) return;

            int alive = 0;
            for (int i = 0; i < _deployables.Count; i++) if (_deployables[i].IsActive) alive++;
            if (alive >= 2) return;   // 화면이 포탑으로 덮이면 무엇이 적인지 안 보인다

            _deployCooldown = DeployCooldown;
            SpawnDeployable(me.Position);
        }

        private void SpawnDeployable(Vector2 at)
        {
            Deployable d = null;
            for (int i = 0; i < _deployables.Count; i++)
                if (!_deployables[i].IsActive) { d = _deployables[i]; break; }

            if (d == null)
            {
                if (_deployables.Count >= MaxDeployables) { d = _deployables[0]; d.Despawn(); }
                else
                {
                    var go = new GameObject("Deployable", typeof(RectTransform));
                    go.transform.SetParent(_unitLayer, false);
                    d = go.AddComponent<Deployable>();
                    d.Init(null, new Vector2(56f, 56f));
                    _deployables.Add(d);
                }
            }

            // 전용 그림이 아직 없다. 로봇을 줄여 쓴다 — 무엇이 놓았는지는 읽힌다.
            d.SetSprite(UnitGet(DeployHostKey, "s") ?? UnitGet(DeployHostKey));

            // 정본 BUF_T06 스마트 배치 — 재조준이 빨라진다(= 발사 간격이 준다)
            float interval = DeployFireInterval * (1f - _buffs.DeployRetargetCut);
            d.Spawn(at, DeploySeconds, DeployRange,
                    Mathf.RoundToInt(_host.Atk * _buffs.AttackMul * 0.6f),
                    Mathf.Max(0.25f, interval));
        }

        private void TickDeployables(float dt)
        {
            for (int i = 0; i < _deployables.Count; i++)
            {
                var d = _deployables[i];
                if (!d.IsActive || !d.Tick(dt)) continue;

                var target = NearestEnemy(d.Position, d.Range);
                if (target == null) continue;

                var shot = RentShot();
                if (shot == null) continue;
                shot.SetSprite(GetSprite("shot_pulse") ?? GetSprite("shot"));
                shot.Fire(d.Position, target.Position, _config.ShotSpeedPlayer * _buffs.ShotSpeedMul, d.Damage,
                          fromPlayer: true, target, _config.ShotSize, ShotPlayerColor,
                          _config.ShotLifeSeconds,
                          // 정본 S08 도탄 터렛 — 포탑 탄도 튕긴다
                          bounces: SynergyOn(SynergyKind.BounceTurret) ? 1 : 0);

                // 정본 S01/S03 — 로봇 포탑이 불을 물려받는다. 쏜 자리에 불장판이 남는다.
                if (SynergyOn(SynergyKind.NapalmTurret) || _buffs.DeployablesBurn)
                    SpawnField(target.Position, 90f * _buffs.AoeMul, 2f,
                               FieldEffect.Burn, 3, fromPlayer: true);
            }
        }

        private Unit NearestEnemy(Vector2 from, float range)
        {
            Unit best = null;
            float bestD = range;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                float d = Vector2.Distance(from, e.Position);
                if (d > bestD) continue;
                bestD = d; best = e;
            }
            return best;
        }

        private void ClearDeployables()
        {
            for (int i = 0; i < _deployables.Count; i++) _deployables[i]?.Despawn();
        }

        // ── 장판 ──────────────────────────────────────────────────
        //
        // 정본에서 이것 하나가 여러 갈래를 막고 있었다 — 버프 4종과 보스 패턴 둘이
        // 전부 "바닥에 깔린 지속 영역" 을 전제한다.

        private const int MaxFields = 24;
        private const int FieldSlowPercent = 45;
        private readonly List<Field> _fields = new();

        private static readonly Color FieldColorDamage = new(1f, 0.45f, 0.20f, 0.32f);
        private static readonly Color FieldColorSlow   = new(0.45f, 0.70f, 1f, 0.28f);
        private static readonly Color FieldColorBurn   = new(1f, 0.55f, 0.18f, 0.34f);
        private static readonly Color FieldColorFreeze = new(0.60f, 0.90f, 1f, 0.32f);
        private static readonly Color FieldColorCurse  = new(0.66f, 0.35f, 1f, 0.32f);

        /// <summary>
        /// 효과별 장판 그림 이름. 전용 그림이 없으면 흰 원판(`field`)에 색만 입혀 쓴다 —
        /// 그림이 오는 대로 저절로 갈아 끼워진다.
        /// </summary>
        private static string FieldSpriteOf(FieldEffect e) => e switch
        {
            FieldEffect.Slow   => "field_slow",
            FieldEffect.Burn   => "field_burn",
            FieldEffect.Freeze => "field_freeze",
            FieldEffect.Curse  => "field_curse",
            _                  => "field_damage",
        };

        private static Color ColorOf(FieldEffect e) => e switch
        {
            FieldEffect.Slow   => FieldColorSlow,
            FieldEffect.Burn   => FieldColorBurn,
            FieldEffect.Freeze => FieldColorFreeze,
            FieldEffect.Curse  => FieldColorCurse,
            _                  => FieldColorDamage,
        };

        /// <summary>장판을 깐다. 자리가 없으면 가장 오래된 것을 밀어낸다.</summary>
        private Field SpawnField(Vector2 at, float radius, float seconds,
                                 FieldEffect effect, int damagePerTick, bool fromPlayer)
        {
            Field f = null;
            for (int i = 0; i < _fields.Count; i++)
                if (!_fields[i].IsActive) { f = _fields[i]; break; }

            if (f == null)
            {
                if (_fields.Count >= MaxFields) { f = _fields[0]; f.Despawn(); }
                else
                {
                    var go = new GameObject("Field", typeof(RectTransform));
                    go.transform.SetParent(_fieldLayer, false);
                    f = go.AddComponent<Field>();
                    f.Init(null);
                    _fields.Add(f);
                }
            }

            // ⚠ 그림은 **깔 때마다** 정한다. 장판도 풀에서 돌려 쓰므로 태어날 때 정하면
            //    직전 효과의 그림이 그대로 남는다(탄·포탑에서 이미 두 번 겪었다).
            var art = GetSprite(FieldSpriteOf(effect));
            // 전용 그림이 없으면 흰 원판에 색을 입힌다. 색까지 없으면 그리지 않는다 —
            // 흰 네모가 바닥에 깔리는 것보다 아무것도 없는 편이 낫다.
            bool generic = art == null;
            f.SetSprite(art ?? GetSprite("field"));

            // 정본 BUF_A02 — 장판이 더 오래 남는다
            f.Spawn(at, radius, seconds + _buffs.FieldExtraSeconds,
                    effect, damagePerTick, fromPlayer,
                    generic ? ColorOf(effect) : Color.white);
            return f;
        }

        private void TickFields(float dt)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                var f = _fields[i];
                if (!f.IsActive || !f.Tick(dt)) continue;

                if (f.FromPlayer)
                {
                    for (int j = 0; j < _enemies.Count; j++)
                    {
                        var e = _enemies[j];
                        if (e == null || !e.IsAlive || !f.Contains(e.Position)) continue;
                        ApplyField(f, e, toEnemy: true);
                    }
                }
                else
                {
                    var me = Avatar;
                    if (me != null && me.IsAlive && f.Contains(me.Position))
                        ApplyField(f, me, toEnemy: false);
                }
            }
        }

        private void ApplyField(Field f, Unit u, bool toEnemy)
        {
            switch (f.Effect)
            {
                case FieldEffect.Slow:
                    u.ApplySlow(FieldSlowPercent, 0.8f);
                    // 정본 BUF_T03 — 둔화 장판 가장자리가 피해를 준다.
                    // 가장자리로 한정하는 이유는 "안에 있으면 아프다" 가 아니라
                    // "들어오고 나갈 때 아프다" 라야 자리를 잡을 이유가 생기기 때문이다.
                    if (_buffs.SlowFieldEdgeDamage > 0 && toEnemy)
                    {
                        float d = (u.Position - f.Center).magnitude;
                        if (d > f.Radius * 0.7f) HurtByField(u, _buffs.SlowFieldEdgeDamage, toEnemy);
                    }
                    break;
                case FieldEffect.Burn:   u.ApplyBurn(1.2f); break;
                case FieldEffect.Freeze: u.ApplyFreeze(1.2f); break;
                case FieldEffect.Curse:  u.ApplyCurse(1.2f); break;
            }
            if (f.DamagePerTick > 0) HurtByField(u, f.DamagePerTick, toEnemy);
        }

        private void HurtByField(Unit u, int damage, bool toEnemy)
        {
            if (damage <= 0) return;
            if (toEnemy)
            {
                ShowDamage(u.Position, damage, toEnemy: true);
                if (u.TakeDamage(damage)) { KillEnemy(u); return; }
                if (u.IsBoss) _bus.Publish(new BossHpChangedEvent { BossHp = u.Hp, BossHpMax = u.HpMax });
            }
            else
            {
                DamagePlayer(damage);   // 감소·유령 전환까지 이쪽이 다 처리한다
            }
        }

        private void ClearFields()
        {
            for (int i = 0; i < _fields.Count; i++) _fields[i]?.Despawn();
        }

        // ── 정본 BUF_T02 화상 전이 ────────────────────────────────
        private const float BurnSpreadRadius = 150f;
        private const float BurnSpreadInterval = 1.0f;
        private float _burnSpreadTimer;

        private void SpreadBurn(Unit source)
        {
            if (_burnSpreadTimer > 0f) return;
            _burnSpreadTimer = BurnSpreadInterval;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var o = _enemies[i];
                if (o == null || o == source || !o.IsAlive) continue;
                if ((o.Position - source.Position).sqrMagnitude > BurnSpreadRadius * BurnSpreadRadius) continue;
                o.ApplyBurn(_config.SlowSeconds);
            }
        }

        // ── 정본 BUF_U04 집중한 영혼 ──────────────────────────────
        //
        // "같은 표적 4회 명중마다 피해 +6%, 3단계까지". 표적을 바꾸면 처음부터다.
        // 누구를 몇 번 때렸는지만 들고 있으면 되므로 유닛 참조 하나와 정수 하나로 족하다 —
        // 적마다 카운터를 달면 죽을 때마다 정리해야 한다.
        private const int FocusHitsPerStack = 4;
        private Unit _focusTarget;
        private int _focusHits;

        private float FocusMul(Unit victim)
        {
            if (_buffs.FocusPerStack <= 0f) return 1f;
            if (_focusTarget != victim) { _focusTarget = victim; _focusHits = 0; }
            _focusHits++;
            int stack = Mathf.Min(Unit.StatusMaxStack, _focusHits / FocusHitsPerStack);
            return 1f + stack * _buffs.FocusPerStack;
        }

        /// <summary>
        /// 튕긴 뒤의 탄이 주는 피해. 정본 BUF_A03 은 50% 로 못박았고,
        /// 버프가 없으면 튕긴 탄은 **피해가 없다** — 그냥 다 때리면 벽이 공짜 이득이 된다.
        /// </summary>
        private int BouncedDamage(Projectile shot, int damage)
        {
            if (!shot.HasBounced) return damage;
            if (_buffs.ReturnDamagePercent <= 0) return 0;
            return Mathf.Max(1, damage * _buffs.ReturnDamagePercent / 100);
        }

        private void HitEnemyWith(Unit victim, int damage, HostEntry p)
        {
            victim.IsAggro = true;
            victim.SetState(EnemyState.Hit);
            AddMaintain();
            TryMark(victim);
            // 저주는 받는 피해를 늘린다. 표시되는 숫자도 늘어난 값이어야 —
            // 저주를 걸어 놓고 숫자가 그대로면 걸린 줄 모른다.
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * victim.CurseDamageMul));
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * FocusMul(victim)));
            // 실드 순환(정본 B02) — 이 구간에는 때려도 잘 안 들어간다.
            // 그래야 "지금은 피할 때" 라는 구간이 생긴다.
            if (victim.IsBoss && _bossShield > 0f)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * BossShieldDamageMul));
            ShowDamage(victim.Position, damage, toEnemy: true);
            bool dead = victim.TakeDamage(damage);
            InflictStatus(victim, p);
            int slow = (p?.SlowPercent ?? 0) + _buffs.SlowPercent;
            int steal = (p?.LifestealPercent ?? 0) + _buffs.LifestealPercent;
            if (slow > 0) victim.ApplySlow(slow, _config.SlowSeconds);
            if (steal > 0 && _host != null)
                Leech(Mathf.Max(1, damage * steal / 100));

            if (dead) { KillEnemy(victim); return; }
            if (victim.IsBoss)
                _bus.Publish(new BossHpChangedEvent { BossHp = victim.Hp, BossHpMax = victim.HpMax });
        }

        // ── 투사체 ────────────────────────────────────────────────
        private static readonly Color ShotPlayerColor = new(1f, 0.72f, 0.24f, 1f);
        private static readonly Color ShotEnemyColor = new(0.55f, 0.78f, 1f, 1f);
        // 보스 탄은 잡몹과 색을 나눈다 — 화면이 탄으로 덮이면 무엇을 피해야 할지 안 보인다
        private static readonly Color ShotBossColor = new(1f, 0.36f, 0.30f, 1f);

        private void FireShot(Unit attacker, Unit target, bool fromPlayer, float angleOffsetDeg,
                              bool lastShot = false, int split = 1)
        {
            var shot = RentShot();
            if (shot == null) return;
            shot.SetSprite(ShotSpriteOf(attacker), ShotKindOf(attacker));
            var p = attacker.Profile;
            bool snipe = p != null && p.Kind == AttackKind.Snipe;

            float speed = ShotSpeedOf(p, fromPlayer)
                          * (snipe ? 1.6f : 1f)
                          * (fromPlayer ? _buffs.ShotSpeedMul : 1f);

            // 몸 중심이 아니라 총구에서 나간다. 탄이 배에서 튀어나오면
            // 방향 스프라이트를 그린 의미가 없다.
            shot.Fire(attacker.MuzzlePosition, target.Position, speed,
                      fromPlayer
                          ? Mathf.Max(1, Mathf.RoundToInt(
                                attacker.Atk * _buffs.AttackMul * MaintainDamageMul
                                * (lastShot ? 1f + _buffs.LastShotBonus : 1f) / split))
                          : Mathf.Max(1, Mathf.RoundToInt(attacker.Atk / (float)split)),
                      fromPlayer, target, _config.ShotSize,
                      fromPlayer ? ShotPlayerColor : ShotEnemyColor,
                      _config.ShotLifeSeconds,
                      pierce: (p != null && p.Kind == AttackKind.Pierce) || (fromPlayer && _buffs.Pierce),
                      slowPercent: (p?.SlowPercent ?? 0) + (fromPlayer ? _buffs.SlowPercent : 0),
                      lifestealPercent: (p?.LifestealPercent ?? 0) + (fromPlayer ? _buffs.LifestealPercent : 0),
                      angleOffsetDeg: angleOffsetDeg,
                      // 슬러거는 탄을 되받아치는 것이 정체성이다(`ReflectsShots`).
                      // 그 몸에 들어가면 쏘는 탄도 튕긴다 — 버프 없이도 한 번은 튕긴다.
                      bounces: fromPlayer
                          ? _buffs.Bounces + ((p != null && p.ReflectsShots) ? 1 : 0)
                          : 0);
        }

        /// <summary>풀에서 하나 꺼낸다. 매 발마다 GameObject 를 만들면 교전 중 GC 가 튄다.</summary>
        /// <summary>
        /// 캐릭터 키 → 탄 그림 이름. 무기가 다른데 탄이 같으면 화면에서 무엇이
        /// 날아오는지 읽히지 않는다 — 레이저도 수류탄도 서리도 노란 총알이었다.
        ///
        /// 캐릭터마다 한 장씩 두지 않고 **무기 계열로 묶는다.** 21종이면 21장을
        /// 그려야 하지만 계열로 묶으면 8장이면 되고, 그래도 읽히는 데는 충분하다.
        /// 근접(amazon·amazon_elite·baseball·vampire)은 탄이 없어 여기 없다.
        /// </summary>
        private static readonly Dictionary<string, string> ShotKind = new()
        {
            { "gangster", "bullet" }, { "thug", "bullet" }, { "hopper", "bullet" },
            { "hopper_smg", "bullet" }, { "commando_mg", "bullet" },
            { "commando_laser", "laser" },
            { "commando_grenade", "grenade" },
            { "salamander", "flame" }, { "dragoon", "flame" },
            { "dragon_blue", "frost" }, { "snowwoman", "frost" },
            { "ninja", "shuriken" }, { "ninja_chain", "shuriken" },
            { "white_wizard", "magic" }, { "medium", "magic" },
            { "guru", "pulse" }, { "robot", "pulse" },
        };

        /// <summary>캐릭터별 탄 그림. 아직 안 온 것은 기본 탄으로 떨어진다.</summary>
        private readonly Dictionary<string, Sprite> _shotSprite = new();

        /// <summary>탄 종류 이름. 없는 배우는 null — 기본 그림을 쓴다.</summary>
        private static string ShotKindOf(Unit u)
            => u != null && u.Key != null && ShotKind.TryGetValue(u.Key, out var k) ? k : null;

        /// <summary>
        /// 맞은 자리에서 터뜨린다. 그림이 없으면 아무것도 하지 않는다 —
        /// 8종을 한 번에 받지 못해도 받은 것부터 보이게 한다.
        /// </summary>
        private void SpawnImpact(Vector2 at, string kind)
        {
            var first = GetSprite(kind != null ? $"impact_{kind}_1" : "impact_1")
                        ?? GetSprite("impact_1");
            if (first == null) return;
            var second = GetSprite(kind != null ? $"impact_{kind}_2" : "impact_2")
                         ?? GetSprite("impact_2");

            for (int i = 0; i < _impacts.Count; i++)
                if (!_impacts[i].IsActive) { _impacts[i].Play(at, first, second); return; }

            if (_impacts.Count >= MaxImpacts) return;   // 화면이 터짐으로 덮이지 않게 상한을 둔다
            var go = new GameObject($"Impact_{_impacts.Count}", typeof(RectTransform));
            var im = go.AddComponent<Impact>();
            im.Cache(_shotLayer, _config.ShotSize * 2.4f);
            _impacts.Add(im);
            im.Play(at, first, second);
        }

        private Sprite ShotSpriteOf(Unit u)
        {
            var key = u != null ? u.Key : null;
            if (key == null) return GetSprite("shot");
            if (_shotSprite.TryGetValue(key, out var cached)) return cached;

            var s = ShotKind.TryGetValue(key, out var kind) ? GetSprite($"shot_{kind}") : null;
            s ??= GetSprite("shot");
            _shotSprite[key] = s;
            return s;
        }

        private Projectile RentShot()
        {
            for (int i = 0; i < _shots.Count; i++)
                if (!_shots[i].IsActive) return _shots[i];

            if (_shots.Count >= MaxShots) return null;   // 폭주 방지 상한
            var go = new GameObject("Shot", typeof(RectTransform));
            go.transform.SetParent(_shotLayer, false);
            var p = go.AddComponent<Projectile>();
            p.Init(GetSprite("shot"));
            _shots.Add(p);
            return p;
        }

        /// <summary>
        /// 방 네 벽에서 튕긴다. 튕겼으면 true.
        /// 벽 안쪽으로 한 칸 밀어 넣어 같은 프레임에 두 번 튕기는 것을 막는다.
        /// </summary>
        private bool BounceOffWalls(Projectile p)
        {
            var q = p.Position;
            Vector2 n = Vector2.zero;
            if (q.x <= 0f) n = Vector2.right;
            else if (q.x >= _roomSize.x) n = Vector2.left;
            else if (q.y >= 0f) n = Vector2.down;
            else if (q.y <= -_roomSize.y) n = Vector2.up;
            if (n == Vector2.zero) return false;
            return p.Bounce(n);
        }

        /// <summary>부딪힌 엄폐물에서 되튕길 방향. 얕게 겹친 축으로 튕겨야 자연스럽다.</summary>
        private Vector2 BounceNormalFromCover(Vector2 at)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.BlocksShot || !o.Bounds.Contains(at)) continue;
                float dx = at.x - o.Bounds.center.x;
                float dy = at.y - o.Bounds.center.y;
                float ox = o.Bounds.width * 0.5f - Mathf.Abs(dx);
                float oy = o.Bounds.height * 0.5f - Mathf.Abs(dy);
                return ox < oy ? new Vector2(Mathf.Sign(dx), 0f) : new Vector2(0f, Mathf.Sign(dy));
            }
            return Vector2.up;
        }

        private void TickShots(float dt)
        {
            var me = Avatar;
            for (int i = 0; i < _shots.Count; i++)
            {
                var p = _shots[i];
                if (!p.IsActive) continue;

                if (!p.Tick(dt)) { p.Despawn(); continue; }

                // 방 벽에서 튕긴다. 도탄이 없는 탄은 그냥 밖으로 나가 수명으로 사라진다 —
                // 벽에서 없애 버리면 화면 끝에서 탄이 뚝 끊겨 어색하다.
                if (p.BouncesLeft > 0 && !BounceOffWalls(p)) { }

                // 엄폐물에 막힌다. 이게 없으면 기둥이 그림일 뿐이라
                // 뒤에 숨는 것이 아무 의미가 없다.
                if (BlockedByCover(p.Position))
                {
                    // 도탄이 남아 있으면 기둥에서도 튕긴다. 정본 "도탄 벽" 지형지물이
                    // 이 경로를 쓴다 — 기둥이 막기만 하는 것이 아니라 되돌려 준다.
                    if (!p.Bounce(BounceNormalFromCover(p.Position))) { p.Despawn(); continue; }
                }

                if (p.Damage <= 0) continue;   // 근접 타격 섬광 — 수명만 흘려보낸다

                if (p.FromPlayer)
                {
                    var hit = HitEnemy(p.Position, p);
                    if (hit == null) continue;
                    if (p.Pierce) p.MarkHit(hit); else p.Despawn();
                    SpawnImpact(p.Position, p.Kind);
                    ApplyShotHit(hit, p);
                }
                else
                {
                    if (me == null) { p.Despawn(); continue; }
                    if (Vector2.Distance(p.Position, me.Position) > _config.ShotHitRadius) continue;
                    p.Despawn();
                    SpawnImpact(p.Position, p.Kind);
                    DamagePlayer(p.Damage);
                }
            }
        }

        /// <summary>탄이 닿은 적. 관통탄은 이미 때린 대상을 건너뛴다.</summary>
        private Unit HitEnemy(Vector2 at, Projectile shot)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (shot.Pierce && shot.HasHit(e)) continue;
                if (Vector2.Distance(at, e.Position) <= _config.ShotHitRadius) return e;
            }
            return null;
        }

        private void ApplyShotHit(Unit victim, Projectile shot)
        {
            // 맞았으면 무조건 반응한다. 사거리가 탐지 거리보다 긴 호스트(히트맨 357)로
            // 저격하면 적이 맞고도 가만히 있는 그림이 된다.
            victim.IsAggro = true;
            victim.SetState(EnemyState.Hit);
            AddMaintain();
            TryMark(victim);
            int dmg = BouncedDamage(shot, shot.Damage);
            if (dmg <= 0) return;   // 버프 없이 튕긴 탄은 스쳐 지나간다
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * victim.CurseDamageMul));
            ShowDamage(victim.Position, dmg, toEnemy: true);
            bool dead = victim.TakeDamage(dmg);
            if (shot.SlowPercent > 0) victim.ApplySlow(shot.SlowPercent, _config.SlowSeconds);
            if (shot.LifestealPercent > 0 && _host != null)
                Leech(Mathf.Max(1, shot.Damage * shot.LifestealPercent / 100));

            if (dead) { KillEnemy(victim); return; }
            if (victim.IsBoss)
                _bus.Publish(new BossHpChangedEvent { BossHp = victim.Hp, BossHpMax = victim.HpMax });
        }

        private void DamagePlayer(int amount)
        {
            if (IsInvulnerable) return;
            // 받는 피해 감소(정본 BUF_A04). 0 이 되지 않게 최소 1 은 남긴다 —
            // 무적이 되어 버리면 버프가 아니라 버그로 보인다.
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * _buffs.DamageTakenMul));
            // 정본 S06 수호 돌진 — 구루의 가드 오라를 대시에 실어 나른다
            if (SynergyOn(SynergyKind.ArmoredDash))
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * ArmoredDashDamageMul));
            var hitAt = Avatar != null ? Avatar.Position : Vector2.zero;
            if (_host != null)
            {
                ShowDamage(hitAt, amount, toEnemy: false);
                if (_host.TakeDamage(amount)) LoseHost();
                else PublishHp();
                return;
            }
            // 유령은 싸울 수 없다. 원피해를 그대로 받으면 빙의하기 전에 소멸한다.
            int reduced = _config.GhostDamage(amount);
            ShowDamage(hitAt, reduced, toEnemy: false);   // 감소 후 값이라야 체력바와 맞는다
            _ghostHp = Mathf.Max(0, _ghostHp - reduced);
            _ghost.TakeDamage(reduced);
            PublishHp();
            if (_ghostHp == 0) Finish(false);
        }

        /// <summary>보호 시간 동안 근처 적을 늦춘다. 범위는 빙의 사거리의 두 배로 잡는다.</summary>
        private void SlowNearbyEnemies(Vector2 center)
        {
            float r = _config.PossessRange * 2f;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (Vector2.Distance(e.Position, center) > r) continue;
                e.ApplySlow(_config.ProtectSlowPercent, _config.GhostProtectSeconds);
            }
        }

        private void LoseHost()
        {
            var pos = _host.Position;
            var key = _host.Key;
            Retire(_host);       // 몸은 쓰러진다 — 유령이 그 자리에서 빠져나온다
            _host = null;

            _ghost.gameObject.SetActive(true);
            _ghost.Position = pos;

            // 기획서 A 1-3 — 호스트를 잃은 자리는 적 한복판이다. 보호가 없으면
            // 다시 빙의할 틈 없이 연쇄로 죽는다. 무적과 함께 주변을 늦춘다.
            _ghostProtect = _config.GhostProtectSeconds;
            _drainCarry = 0f;
            _buffs.SetHost(null);
            ResetMaintain();
            SlowNearbyEnemies(pos);

            _bus.Publish(new HostLostEvent { LostHostKey = key });
            PublishHp();
        }

        /// <summary>
        /// 사격 대상 선택 (기획서 A 3-3 Target Type).
        /// 호스트마다 "누구를 먼저 때리는가"가 다르면 같은 화력도 다른 교전이 된다.
        /// 체력 기준으로 고를 때도 사거리 밖은 후보가 아니므로 거리를 함께 본다.
        /// </summary>
        private Unit Nearest(Vector2 from)
        {
            var rule = _host != null && _host.Profile != null
                ? _host.Profile.Targeting : TargetType.Nearest;

            Unit best = null;
            float bestD = float.MaxValue;
            int bestHp = rule == TargetType.LowestHp ? int.MaxValue : int.MinValue;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                // 화면 밖은 겨누지 않는다. 방이 화면보다 길어진 뒤로 안 보이는 적을 향해
                // 쏘는 일이 생겼다 — 플레이어에게는 허공에 대고 쏘는 것으로 보인다.
                if (!IsOnScreen(e)) continue;
                float d = Vector2.Distance(from, e.Position);

                bool better;
                switch (rule)
                {
                    case TargetType.LowestHp:
                        better = e.Hp < bestHp || (e.Hp == bestHp && d < bestD);
                        break;
                    case TargetType.HighestHp:
                        better = e.Hp > bestHp || (e.Hp == bestHp && d < bestD);
                        break;
                    default:
                        better = d < bestD;
                        break;
                }
                if (!better) continue;
                best = e; bestD = d; bestHp = e.Hp;
            }
            return best;
        }

        private void KillEnemy(Unit u)
        {
            u.SetState(EnemyState.Dead);
            TryFirePillar(u);   // 저주가 걸린 채 죽으면 그 자리에서 불기둥 (S05)
            if (u.IsBoss) _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = u.HpMax });
            _enemies.Remove(u);
            if (u == _possessTarget) _possessTarget = null;
            Retire(u);

            GainExp(u.IsBoss ? _config.ExpPerBoss : _config.ExpPerEnemy);
        }

        // ── 런 레벨 ──────────────────────────────────────────────
        // 기획서 A 5-2 — EXP 가 차면 전투를 멈추고 버프 3택1 을 띄운다.
        // 방을 비워야 버프가 나오던 것과 달리, **잡는 만큼** 성장한다.
        // 방 하나가 곧 스테이지인 지금 구조에서는 이게 없으면 성장이 스테이지당 한 번뿐이다.

        private void GainExp(int amount)
        {
            if (amount <= 0) return;
            _exp += amount;

            int need = _config.ExpToNext(_level);
            if (_exp < need)
            {
                PublishExp(need);
                return;
            }

            _exp -= need;
            _level++;
            PublishExp(_config.ExpToNext(_level));
            OfferBuff();
        }

        private void PublishExp(int need)
            => _bus.Publish(new RunExpChangedEvent { Level = _level, Exp = _exp, ExpToNext = need });

        /// <summary>버프 3택1 을 연다. 이미 열려 있으면 아무것도 하지 않는다.</summary>
        private void OfferBuff()
        {
            if (_awaitingBuff) return;
            _buffTable?.Draw(_offer, 3, _buffs.ExcludedKeys, _rng, _host?.Profile,
                             _player != null ? _player.CurrentChapter : 1);
            if (_offer.Count == 0) return;

            _awaitingBuff = true;
            var keys = new string[_offer.Count];
            for (int i = 0; i < _offer.Count; i++) keys[i] = _offer[i].BuffKey;
            _bus.Publish(new BuffOfferEvent { OfferedKeys = keys, Level = _level });
        }

        /// <summary>
        /// 몸을 화면에서 물린다. 사망 그림이 있으면 쓰러지는 연출을 돌리고,
        /// 없으면 예전처럼 바로 없앤다 — 캐릭터를 한 종씩 채워 넣는 중이라
        /// 그림이 없는 종이 멈춰 있으면 안 된다.
        /// </summary>
        private void Retire(Unit u)
        {
            if (u == null) return;
            if (u.BeginDeath()) _dying.Add(u);
            else Destroy(u.gameObject);
        }

        /// <summary>쓰러지는 중인 몸을 진행시키고, 다 사라진 것을 치운다.</summary>
        private void TickDying(float dt)
        {
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                var u = _dying[i];
                if (u == null) { _dying.RemoveAt(i); continue; }
                if (!u.TickDeath(dt)) continue;
                _dying.RemoveAt(i);
                Destroy(u.gameObject);
            }
        }

        // ── 피해 수치 ──────────────────────────────────────────────
        // 색은 "누가 맞았나"로 나눈다. 내가 때린 것과 내가 맞은 것이 같은 색이면
        // 화면이 숫자로 덮였을 때 상황 판단이 안 된다.
        private static readonly Color DamageToEnemy = new(1f, 0.95f, 0.75f, 1f);
        private static readonly Color DamageToPlayer = new(1f, 0.42f, 0.38f, 1f);
        // 유령 HP 색(#5AC8F0)과 같은 계열. 피해 숫자와 섞이면 안 된다 — 성격이 다른 값이다.
        private static readonly Color GhostCostColor = new(0.35f, 0.78f, 0.94f, 1f);

        private const int MaxDamageTexts = 24;

        private DamageText RentDamageText()
        {
            for (int i = 0; i < _damageTexts.Count; i++)
                if (!_damageTexts[i].IsActive) return _damageTexts[i];

            if (_damageTexts.Count >= MaxDamageTexts) return null;   // 폭주 방지 상한
            var go = new GameObject("DamageText", typeof(RectTransform));
            go.transform.SetParent(_textLayer, false);
            var t = go.AddComponent<DamageText>();
            t.Init(TMP_Settings.defaultFontAsset);
            _damageTexts.Add(t);
            return t;
        }

        /// <summary>맞은 자리에 피해 수치를 띄운다. 풀이 다 차면 조용히 넘어간다.</summary>
        private void ShowDamage(Vector2 at, int damage, bool toEnemy)
        {
            if (damage <= 0) return;
            var t = RentDamageText();
            if (t == null) return;
            t.Show(at, damage, toEnemy ? DamageToEnemy : DamageToPlayer);
        }

        /// <summary>전술 빙의로 나간 Ghost HP. 유령 색으로 띄워 피해 숫자와 구분한다.</summary>
        private void ShowGhostCost(Vector2 at, int cost)
        {
            if (cost <= 0) return;
            var t = RentDamageText();
            if (t == null) return;
            t.Show(at, $"-{cost}", GhostCostColor);
        }

        private void TickDamageTexts(float dt)
        {
            for (int i = 0; i < _damageTexts.Count; i++) _damageTexts[i].Tick(dt);
        }

        private void CleanupDead()
        {
            _dead.Clear();
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] == null || !_enemies[i].IsAlive) _dead.Add(_enemies[i]);
            for (int i = 0; i < _dead.Count; i++)
            {
                _enemies.Remove(_dead[i]);
                if (_dead[i] != null) Retire(_dead[i]);
            }
            _dead.Clear();
        }

        /// <summary>
        /// 빙의 가능 대상 갱신. 유령일 때와 몸을 입고 있을 때 **둘 다** 의미가 있다.
        ///
        /// 유령이면 공짜다 — 몸이 없으니 다른 선택지가 없다.
        /// 몸이 있으면 전술 빙의다 — Ghost HP 를 내고 살아 있는 몸을 버린다.
        /// 기준점도 다르다. 유령은 유령 자리에서, 호스트는 호스트 자리에서 잰다.
        /// </summary>
        private void RefreshPossessTarget()
        {
            _possessTarget = null;
            var from = Avatar;
            if (from != null && !_awaitingBuff)
            {
                // 기획서 A 4-3 — 우선순위가 높은 적을 먼저 잡는다. 같으면 가까운 쪽.
                // 사거리는 적마다 다를 수 있다(PossessRange 0 이면 전역 기본값).
                int bestPri = int.MinValue;
                float bestD = float.MaxValue;
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsPossessable) continue;

                    // 사거리가 유령과 호스트에서 다르다. 유령은 몸에 달라붙어야 하지만,
                    // 호스트는 265 밖에서 쏘고 있어 유령 사거리(110)로는 버튼이 영영 안 켜진다.
                    // 전술 빙의가 실제로 눌리는 선택지가 되려면 교전 거리에서 닿아야 한다.
                    float range = _host != null ? _config.TacticalPossessRange
                                : e.PossessRange > 0f ? e.PossessRange
                                : _config.PossessRange;
                    float d = Vector2.Distance(from.Position, e.Position);
                    if (d > range) continue;

                    if (e.PossessPriority < bestPri) continue;
                    if (e.PossessPriority == bestPri && d >= bestD) continue;

                    bestPri = e.PossessPriority; bestD = d; _possessTarget = e;
                }
            }
            // 표식은 대상에만 찍지 않는다. 조건부 적은 **잠긴 것도 보여야** 어느 놈을
            // 먼저 두들겨야 하는지 알 수 있다.
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                if (e == _possessTarget) e.SetPossessMark(Unit.PossessMark.Ready);
                else if (e.HasPossessCondition && e.IsAlive && !e.IsDying)
                    e.SetPossessMark(Unit.PossessMark.Progress, e.PossessProgress);
                else e.SetPossessMark(Unit.PossessMark.None);
            }

            bool has = _possessTarget != null;
            int cost = _host != null ? TacticalCost : 0;
            bool blocked = has && _host != null && !CanSwitch;
            if (has == _hadPossessTarget && blocked == _hadPossessBlocked) return;

            _hadPossessTarget = has;
            _hadPossessBlocked = blocked;
            _bus.Publish(new PossessTargetChangedEvent
            {
                HasTarget = has, GhostCost = cost, Blocked = blocked,
            });
        }

        /// <summary>
        /// 지금 전술 빙의를 낼 수 있는가 (정본 TC_POS_D 의 선행 조건).
        /// 대상 유무는 보지 않는다 — 그건 부르는 쪽이 따로 본다.
        /// </summary>
        /// <summary>유지 단계로 얻는 피해 배율. 몸을 갈아타면 1로 돌아간다.</summary>
        private float MaintainDamageMul => 1f + _maintainStack * MaintainDamagePerStack;

        /// <summary>
        /// 갱스터가 때린 적에 표식을 남긴다. 갱스터의 유지 훅이 "표식 릴레이" 인 것과
        /// 같은 뿌리다 — 이 몸이 세상에 남기는 흔적이 곧 다음 몸의 재료가 된다.
        /// </summary>
        // ── 시너지 (정본 SYNERGY 표) ──────────────────────────────
        //
        // 시너지는 한 캐릭터가 잘나서가 아니라 **무엇을 버리고 무엇으로 갈아탔는가**에서
        // 나온다. 그래야 몸을 바꾸는 것이 손해 계산이 아니라 선택이 된다.

        private const float ArmoredDashDamageMul = 0.5f;
        private const float HealFinisherPercent = 25f;

        private string _prevHostKey;
        private SynergyKind _synergyKind;
        private float _synergyTimer;

        private bool SynergyOn(SynergyKind k) => _synergyTimer > 0f && _synergyKind == k;

        private void TickSynergy(float dt)
        {
            if (_synergyTimer > 0f) _synergyTimer -= dt;
        }

        /// <summary>계기가 왔을 때 짝이 맞는 시너지를 켠다.</summary>
        private void FireSynergy(SynergyTrigger trigger, string toKey)
        {
            if (_prevHostKey == null || toKey == null) return;
            var rules = SynergyTable.Rules;
            for (int i = 0; i < rules.Length; i++)
            {
                var r = rules[i];
                if (r.Trigger != trigger || r.From != _prevHostKey || r.To != toKey) continue;
                // 개방 조건이 걸린 시너지는 그 버프를 뽑아야 열린다(정본 BUFF_GATED)
                if (r.GateBuff != null && !_buffs.Has(r.GateBuff)) continue;

                _synergyKind = r.Kind;
                _synergyTimer = r.Seconds;
                _bus.Publish(new SynergyTriggeredEvent
                {
                    SynergyId = r.Id,
                    Name = SynergyTable.NameOf(r.Kind),
                    FromHostKey = r.From,
                    ToHostKey = r.To,
                    FirstTime = _synergySeen.Add(r.Id),
                });
                return;
            }
        }

        /// <summary>
        /// 저주가 걸린 채 죽으면 그 자리에서 불기둥이 솟는다 (정본 S05 저주 화염).
        /// 저주를 거는 것과 태우는 것이 서로 다른 몸이라 **갈아타야만** 성립한다.
        /// </summary>
        private void TryFirePillar(Unit victim)
        {
            if (!SynergyOn(SynergyKind.FirePillarCircuit) || victim.CurseStack <= 0) return;
            SpawnField(victim.Position, 140f * _buffs.AoeMul, 3f, FieldEffect.Burn, 5, fromPlayer: true);
        }

        /// <summary>
        /// 흡혈. 정본 BUF_T04 피의 부채는 **넘치는 만큼을 고스트 체력으로** 돌린다.
        /// 방마다 횟수를 막는 이유는 정본 그대로다 — 안 막으면 잡몹 많은 방에서
        /// 고스트가 무한정 회복되어 유령 시계의 압박이 사라진다.
        /// </summary>
        private void Leech(int amount)
        {
            if (_host == null || amount <= 0) return;
            int room = _host.HpMax - _host.Hp;
            _host.Heal(amount);
            int over = amount - room;
            if (over <= 0) return;
            if (_buffs.BloodDebtPerRoom <= 0 || _bloodDebtUsed >= _buffs.BloodDebtPerRoom) return;
            _bloodDebtUsed++;
            _ghostHp = Mathf.Min(_config.GhostHpMax, _ghostHp + over);
            PublishHp();
        }

        private int _bloodDebtUsed;

        private void TryMark(Unit victim)
        {
            if (victim == null || _host == null) return;
            if (_host.Key != SynergyMarkSource) return;
            victim.SetMark(MarkSeconds);
        }

        /// <summary>
        /// 닌자로 갈아탄 뒤 표식이 남은 적이 있으면 순간이동 처형이 나간다.
        /// 발동했으면 true — 그 프레임의 평소 사격은 건너뛴다.
        /// </summary>
        private bool TryBlinkExecution(Unit me, float dt)
        {
            if (_blinkCooldown > 0f) { _blinkCooldown -= dt; return false; }
            if (me == null || _host == null || _host.Key != SynergyMarkReceiver) return false;

            Unit target = null;
            float best = float.MaxValue;
            float reach = _host.AttackRange * BlinkRangeMul;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                // 정본 S07 — 빙결 연쇄. 표식뿐 아니라 얼어붙은 적도 처형 지점이 된다.
                bool anchor = e.IsMarked
                              || (SynergyOn(SynergyKind.FrozenBlinkChain) && e.IsFrozen);
                if (e == null || !e.IsAlive || e.IsDying || !anchor) continue;
                if (!IsOnScreen(e)) continue;
                float d = Vector2.Distance(me.Position, e.Position);
                if (d > reach || d >= best) continue;
                best = d; target = e;
            }
            if (target == null) return false;

            _blinkCooldown = BlinkCooldown;
            target.ClearMark();

            // 대상 바로 앞으로 붙는다. 겹쳐 서면 누가 누군지 안 보인다.
            var dir = (me.Position - target.Position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            me.Position = target.Position + dir * 70f;
            me.SetFacing(target.Position - me.Position);
            me.PlayAttack();

            int dmg = Mathf.RoundToInt(me.Atk * _buffs.AttackMul * MaintainDamageMul * BlinkDamageMul);
            HitEnemyWith(target, dmg, _host.Profile);

            // 표식 폭발 — 주변까지 함께 맞는다. 정본 "추가 피해 및 범위 데미지".
            // 정본 BUF_T01 표식 탄두는 여기서 **번지는 수와 반경**을 늘린다.
            float aoe = BlinkAoeRadius * _buffs.AoeMul * (1f + _buffs.MarkPayload * 0.25f);
            int spread = 0;
            int spreadMax = 3 + _buffs.MarkPayload;
            for (int i = _enemies.Count - 1; i >= 0 && spread < spreadMax; i--)
            {
                var e = _enemies[i];
                if (e == null || e == target || !e.IsAlive || e.IsDying) continue;
                if (Vector2.Distance(e.Position, target.Position) > aoe) continue;
                HitEnemyWith(e, Mathf.RoundToInt(dmg * 0.5f), _host.Profile);
                spread++;
            }

            _bus.Publish(new SynergyTriggeredEvent
            {
                SynergyId = SynergyS01,
                Name = "마크 폭발",
                FromHostKey = SynergyMarkSource,
                ToHostKey = SynergyMarkReceiver,
                FirstTime = _synergySeen.Add(SynergyS01),
            });
            return true;
        }

        /// <summary>명중을 쌓는다. 단계가 오르면 알린다.</summary>
        private void AddMaintain()
        {
            if (_host == null || _maintainStack >= MaintainMaxStack) return;
            _maintainHits++;
            if (_maintainHits < MaintainHitsPerStack) { PublishMaintain(); return; }
            _maintainHits = 0;
            _maintainStack++;
            PublishMaintain();
        }

        /// <summary>몸이 바뀌면 쌓은 것이 사라진다 (정본 OnSwitchRule — Switch clears active meter).</summary>
        private void ResetMaintain()
        {
            _maintainHits = 0;
            _maintainStack = 0;
            PublishMaintain();
        }

        private void PublishMaintain()
        {
            _bus.Publish(new MaintainChangedEvent
            {
                HookName = _host != null && _host.Profile != null ? _host.Profile.MaintainHook : null,
                Stack = _maintainStack,
                MaxStack = MaintainMaxStack,
                Progress = _maintainStack >= MaintainMaxStack
                    ? 1f : (float)_maintainHits / MaintainHitsPerStack,
            });
        }

        /// <summary>지금 전술 빙의에 나갈 Ghost HP. 버프로 깎일 수 있다(정본 BUF_U06).</summary>
        private int TacticalCost =>
            Mathf.Max(1, _config.TacticalGhostCost - _buffs.TacticalCostCut);

        private bool CanSwitch => _tacticalCooldown <= 0f && _ghostHp > TacticalCost;

        // ─────────────────────────────────────────────────────────
        /// <summary>
        /// 몸을 빼앗는다. 두 갈래다.
        ///
        /// **유령일 때** — 공짜다. 몸이 없으니 다른 선택지가 없고, 여기에 값을 매기면
        /// 죽은 뒤에 벌을 두 번 주는 셈이 된다.
        ///
        /// **몸을 입고 있을 때(전술 빙의)** — Ghost HP 를 내고 쿨다운을 문다.
        /// 이 게임이 묻는 질문이 여기서 나온다. 지금 이 몸을 계속 굴릴 것인가,
        /// 값을 내고 저 몸으로 갈아탈 것인가. 값이 없으면 항상 갈아타는 것이 정답이 되고,
        /// 쿨다운이 없으면 적을 만날 때마다 갈아타는 것이 정답이 된다. 둘 다 있어야
        /// 유지와 교체가 같이 성립한다.
        /// </summary>
        // ── 빙의 연출 ────────────────────────────────────────────
        //
        // 정본 `PossessionChannel` 0.35 초. 원작도 영혼이 **작아지면서 몸으로 빨려 들어간다**.
        // 즉시 갈아타면 몸을 빼앗았다는 감각이 없다 — 화면이 그냥 바뀔 뿐이다.

        private float _channel;                 // 남은 시간
        private float _channelTotal;
        private Vector2 _channelFrom, _channelTo;
        private Game.Character.HostEntry _channelEntry;
        private string _channelKey, _channelName;

        /// <summary>
        /// 빼앗기는 중인 몸. 채널이 끝날 때까지 **지우지 않고** 그 자리에 세워 둔다 —
        /// 지워 버리면 영혼이 빈 바닥으로 빨려 들어가는 그림이 되고,
        /// 몸이 저항하는 자세를 보여 줄 자리가 없다.
        /// </summary>
        private Unit _channelBody;

        /// <summary>빙의가 들어가는 중인가. 이 동안은 조작을 받지 않는다.</summary>
        public bool IsChanneling => _channel > 0f;

        /// <summary>축소 그림이 없을 때 스케일로 줄이는 끝값.</summary>
        private const float ShrinkEnd = 0.15f;

        /// <summary>
        /// 지금 진행도에 맞는 영혼 축소 그림. 아직 안 들어왔으면 null —
        /// 그때는 부르는 쪽이 스케일로 줄인다.
        /// </summary>
        private Sprite ShrinkFrame(float t)
            => UnitGet("ghost", t < 0.45f ? "shrink1" : t < 0.75f ? "shrink2" : "shrink3");

        private void TickPossessChannel(float dt)
        {
            if (_channel <= 0f) return;

            _channel -= dt;
            float t = _channelTotal <= 0f ? 1f : 1f - Mathf.Clamp01(_channel / _channelTotal);

            if (_ghost != null)
            {
                _ghost.Position = Vector2.Lerp(_channelFrom, _channelTo, t * t);

                // 앞의 1/4 동안 몸에서 빠져나오며 부풀고, 나머지에서 빨려 들어가며 줄어든다.
                // 처음부터 줄기만 하면 "나왔다"가 안 보이고 그냥 사라지는 것으로 읽힌다.
                float swell = _config.PossessGhostSwell;
                float scale = t < 0.25f
                    ? Mathf.Lerp(1f, swell, t / 0.25f)
                    : Mathf.Lerp(swell, ShrinkEnd, (t - 0.25f) / 0.75f);

                // 정본 축소 그림(3장)이 있으면 그것으로 줄인다 — 스케일로 줄이면
                // 픽셀이 뭉개져 도트가 아니라 흐릿한 얼룩이 된다.
                var frame = ShrinkFrame(t);
                if (frame != null)
                {
                    _ghost.SetSpriteOverride(frame);
                    // 그림이 크기를 이미 담고 있으므로 부푸는 것만 남기고 축소는 그림에 맡긴다
                    scale = t < 0.25f ? scale : swell;
                }
                _ghost.transform.localScale = Vector3.one * scale;
            }

            if (_channel > 0f) return;

            _channel = 0f;
            if (_ghost != null)
            {
                _ghost.transform.localScale = Vector3.one;
                _ghost.SetSpriteOverride(null);
            }

            // 이제야 옛 몸을 치운다. 그 자리에 내 몸이 선다.
            if (_channelBody != null) Destroy(_channelBody.gameObject);
            _channelBody = null;

            EnterHost(_channelEntry, _channelKey, _channelName, _channelTo,
                      _config.HostStartHpPercent);
            _channelEntry = null;
        }

        public void TryPossess()
        {
            if (!_running || _possessTarget == null || _awaitingBuff || IsChanneling) return;

            bool tactical = _host != null;
            if (tactical && !CanSwitch) return;

            var target = _possessTarget;
            var entry = _player.GetHost(target.Key);
            var pos = target.Position;

            // ⚠ 영혼이 출발하는 자리는 **지금 내가 서 있는 자리**다.
            //   `_ghost` 는 몸을 탄 동안 꺼져 있어 좌표가 마지막으로 유령이었던 곳
            //   (방에 들어온 자리) 에 멈춰 있다. 그걸 그대로 쓰면 몸을 갈아탈 때마다
            //   영혼이 방 입구 바닥에서 날아온다.
            var from = Avatar != null ? Avatar.Position : _ghost.Position;

            // 적 목록에서만 빼고 **지우지는 않는다.** 채널이 도는 동안 그 자리에서
            // 빼앗기는 자세로 굳어 있어야 한다. 총알·AI 는 목록을 보므로 더는 안 건드린다.
            _enemies.Remove(target);
            target.CancelWindup();
            target.HoldPossessed(true);
            _channelBody = target;
            _possessTarget = null;

            if (tactical)
            {
                _ghostHp = Mathf.Max(1, _ghostHp - TacticalCost);
                _tacticalCooldown = _config.TacticalCooldownSeconds;
                _tacticalShown = -1;

                // 값을 치렀다는 것이 화면에서 보여야 한다. 상단 숫자만 바뀌면
                // 100 중 6이라 눈치채지 못한다 — 버린 몸 자리에 띄운다.
                ShowGhostCost(_host.Position, TacticalCost);

                // 버린 몸은 그 자리에 쓰러진다. 경험치는 주지 않는다 —
                // 죽인 것이 아니라 놓아준 것이고, 값을 치른 쪽은 나다.
                var old = _host;
                _host = null;
                Retire(old);

                _bus.Publish(new TacticalCooldownEvent
                {
                    Remain = _tacticalCooldown, Total = _config.TacticalCooldownSeconds,
                });
            }

            // 몸에 들어가는 것은 채널이 끝난 뒤다. 지금은 영혼이 빨려 들어가기 시작한다.
            //
            // 전술 빙의였다면 위에서 이미 옛 몸을 놓아주었으므로 지금 나는 영혼이다.
            // 처음 빙의라면 원래 영혼이었다 — 어느 쪽이든 여기서 영혼을 켜고 날려 보낸다.
            _ghost.gameObject.SetActive(true);
            _ghost.transform.localScale = Vector3.one;
            _ghost.Position = from;      // 켜기 전에 옛 좌표를 버린다
            _ghost.SetFacing(pos - from);

            _channelFrom = from;
            _channelTo = pos;
            _channelEntry = entry;
            _channelKey = target.Key;
            _channelName = target.DisplayName;
            _channelTotal = _channel = _config.PossessChannelSeconds;

            // 채널 시간이 0 이면 예전처럼 즉시 들어간다
            if (_channel <= 0f) TickPossessChannel(0f);
        }

        /// <summary>
        /// 몸을 입는다. 일반 빙의와 긴급 호스트가 같은 길을 쓴다 —
        /// 시작 체력만 다르고 나머지(무적·버프 재계산·표시)는 똑같아야 한다.
        /// </summary>
        private void EnterHost(HostEntry entry, string key, string fallbackName,
                               Vector2 pos, int startHpPercent)
        {
            _ghost.gameObject.SetActive(false);

            _host = NewUnit($"Host_{key}");
            _host.Setup(UnitSide.Player, key, entry != null ? entry.NameKr : fallbackName,
                        UnitGet(key),
                        Mathf.RoundToInt(HostHpOf(entry) * _buffs.HostHpMul),
                        HostAtkOf(entry),
                        HostSpeedOf(entry),
                        HostRangeOf(entry),
                        HostIntervalOf(entry),
                        new Vector2(96f, 92f), isBoss: false, profile: entry);
            _host.Position = pos;
            ApplyFacingSprites(_host, key);

            // 이전 몸이 무엇이었는지가 시너지의 전부다. 새 몸을 세운 **뒤에** 던져야
            // 시너지가 새 몸의 능력치를 보고 켜진다.
            FireSynergy(SynergyTrigger.OnSwitch, key);
            _prevHostKey = key;

            // 기획서 A 3-3 — 빼앗은 몸은 온전하지 않다. 최대 체력의 70%로 시작한다.
            // 이게 없으면 교체가 곧 완전 회복이라, 몸을 갈아타는 데 대가가 없어진다.
            _host.SetHpPercent(startHpPercent);

            // 기획서 A 02 — 빙의 직후 무적(0.35) + 호스트 진입 무적(0.5). 몸을 얻는 순간이
            // 가장 취약한 지점이라, 여기서 맞으면 빙의 자체가 손해가 된다.
            _invuln = _config.PossessInvulnSeconds + _buffs.SwitchShieldSeconds;
            _ghostProtect = 0f;
            _emergencyWait = 0f;
            // 태그형·전용 버프는 쓰는 몸에 따라 켜지고 꺼진다 (기획서 A 5-4)
            _buffs.SetHost(entry);
            ResetMaintain();     // 새 몸에는 앞 몸에서 쌓은 것이 따라오지 않는다

            _bus.Publish(new PossessedEvent
            {
                PossessedHostKey = key,
                // 어떤 몸을 뺏었는지가 곧 빌드다 — 교전 스타일을 함께 보여준다
                DisplayName = entry != null ? $"{entry.NameEn}  ·  {entry.AttackText}" : fallbackName,
                HostHpMax = _host.HpMax,
            });
            PublishHp();
        }

        // ── 얼티밋 ────────────────────────────────────────────────
        //
        // 지금까지는 **21종이 전부 같은 전체 광역**이었다. 얼티밋은 그 몸을 고른
        // 이유가 가장 크게 드러나는 자리인데, 다 같으면 몸이 아니라 게이지를 쓰는 것이 된다.
        // 정본 11종에 각각 제 행동을 준다 — 전부 이미 있는 시스템(장판·상태이상·
        // 설치물·도탄·무적) 위에 세웠다.

        private string _ultKey;          // 지금 도는 지속형 얼티밋
        private float _ultTimer;
        private float _ultTick;

        private const float UltRegenInterval = 0.5f;

        public void TryUltimate()
        {
            if (!_running || _ultimateCharge < _config.UltimateChargeSeconds) return;
            // 유령은 싸우지 않는다. 자동 사격은 막혀 있었는데 얼티밋은 뚫려 있어서,
            // 몸이 없는 상태로 화면 전체를 쓸어버릴 수 있었다.
            // 게이지는 그대로 둔다 — 몸을 얻으면 그때 쓴다.
            var me = _host;
            if (me == null) return;

            _ultimateCharge = 0f;
            int dmg = _config.UltimateDamage;
            string key = _host?.Profile?.UltimateKey;

            switch (key)
            {
                // 광각 확산 + 경직. 정면을 쓸어버리는 대신 등 뒤는 그대로 열려 있다.
                case "tommy_barrage":
                    FireFan(me, me.Position + me.Facing * 400f, 16, 150f, dmg / 3, fromPlayer: true);
                    foreach (var e in EnemiesInRange(me.Position, 520f)) e.ApplySlow(60, 2.0f);
                    break;

                // 지속형 — 끝날 때까지 자리를 지키면 이득이 커진다.
                case "bullet_hell":   BeginUltimate(key, 5f); break;
                case "laser_storm":   BeginUltimate(key, 4f); break;
                case "dragon_breath": BeginUltimate(key, 3f); break;
                case "blood_tornado": BeginUltimate(key, 4f); break;

                // 위상 이탈 — 때리는 것이 아니라 버티는 얼티밋이다.
                case "astral_form":
                    BeginUltimate(key, 8f);
                    _invuln = Mathf.Max(_invuln, 8f);
                    break;

                // 자동조준 터렛 배치. 설치물이 생겼으니 정본 그대로 나온다.
                case "system_override":
                    SpawnDeployable(me.Position + new Vector2(-90f, 0f));
                    SpawnDeployable(me.Position + new Vector2(90f, 0f));
                    break;

                // 연속 돌진. 가까운 넷을 차례로 찍고 마지막 일격에 경직을 남긴다.
                case "rush_combo":  BlinkStrikes(me, 4, dmg, slowLast: true); break;

                // 순간이동 연격 + 무적 프레임.
                case "shadow_burst":
                    BlinkStrikes(me, 3, Mathf.RoundToInt(dmg * 1.2f), slowLast: false);
                    _invuln = Mathf.Max(_invuln, 1.2f);
                    break;

                // 360도 광역. 보스에게 두 배 — 정본이 못박은 유일한 보스 특효다.
                case "elemental_nova":
                {
                    var list = EnemiesInRange(me.Position, 9999f);
                    for (int i = 0; i < list.Count; i++)
                        HitEnemyWith(list[i], list[i].IsBoss ? dmg * 2 : dmg, _host?.Profile);
                    break;
                }

                // 모든 적 탄을 3배 피해로 되받아친다.
                case "grand_slam":
                {
                    int n = 0;
                    for (int i = 0; i < _shots.Count; i++)
                    {
                        var sh = _shots[i];
                        if (!sh.IsActive || sh.FromPlayer) continue;
                        sh.TurnFriendly(3f);
                        n++;
                    }
                    // 되받을 탄이 없으면 아무 일도 안 일어난다 — 그때는 근처를 후려친다
                    if (n == 0)
                    {
                        var list = EnemiesInRange(me.Position, 420f);
                        for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, _host?.Profile);
                    }
                    break;
                }

                default:
                {
                    var list = EnemiesInRange(me.Position, 9999f);
                    for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, _host?.Profile);
                    break;
                }
            }
        }

        private void BeginUltimate(string key, float seconds)
        {
            _ultKey = key;
            _ultTimer = seconds;
            _ultTick = 0f;
        }

        /// <summary>지속형 얼티밋을 흘린다.</summary>
        private void TickUltimate(float dt)
        {
            if (_ultTimer <= 0f) return;
            _ultTimer -= dt;
            var me = Avatar;
            if (me == null) { _ultTimer = 0f; return; }

            _ultTick -= dt;
            if (_ultTick > 0f) return;

            int dmg = _config.UltimateDamage;
            switch (_ultKey)
            {
                case "bullet_hell":
                    _ultTick = 0.22f;
                    FireFan(me, me.Position + me.Facing * 400f, 10, 360f, dmg / 4, fromPlayer: true);
                    break;

                case "laser_storm":
                    _ultTick = 0.18f;
                    FireFan(me, me.Position + me.Facing * 400f, 3, 14f, dmg / 3, fromPlayer: true);
                    break;

                case "dragon_breath":
                    // 원뿔로 불장판을 세 겹 깐다. 화상은 장판이 알아서 건다.
                    _ultTick = 0.35f;
                    for (int i = -1; i <= 1; i++)
                    {
                        var dir = (Vector2)(Quaternion.Euler(0f, 0f, i * 22f) * (Vector3)me.Facing);
                        SpawnField(me.Position + dir * 190f, 110f * _buffs.AoeMul,
                                   1.6f, FieldEffect.Burn, Mathf.Max(1, dmg / 8), fromPlayer: true);
                    }
                    break;

                case "blood_tornado":
                {
                    _ultTick = 0.4f;
                    var list = EnemiesInRange(me.Position, 260f * _buffs.AoeMul);
                    for (int i = 0; i < list.Count; i++)
                    {
                        HitEnemyWith(list[i], Mathf.Max(1, dmg / 5), _host?.Profile);
                        Leech(Mathf.Max(1, dmg / 10));
                    }
                    break;
                }

                case "astral_form":
                    _ultTick = UltRegenInterval;
                    if (_host != null)
                    {
                        _host.Heal(Mathf.Max(1, _host.HpMax / 40));
                        PublishHp();
                    }
                    break;
            }
        }

        /// <summary>가까운 적을 차례로 찍으며 순간이동한다.</summary>
        private void BlinkStrikes(Unit me, int count, int damage, bool slowLast)
        {
            for (int n = 0; n < count; n++)
            {
                var target = NearestEnemy(me.Position, 9999f);
                if (target == null) return;
                var dir = (me.Position - target.Position).normalized;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
                me.Position = target.Position + dir * 70f;
                me.SetFacing(target.Position - me.Position);
                me.PlayAttack();
                if (n == count - 1 && slowLast) target.ApplySlow(70, 2.0f);
                HitEnemyWith(target, damage, _host?.Profile);
            }
        }

        /// <summary>반경 안의 살아 있는 적. 순회 중 죽어도 안전하도록 버퍼에 담아 준다.</summary>
        private readonly List<Unit> _rangeBuffer = new();

        private List<Unit> EnemiesInRange(Vector2 at, float radius)
        {
            _rangeBuffer.Clear();
            float r2 = radius * radius;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                if ((e.Position - at).sqrMagnitude > r2) continue;
                _rangeBuffer.Add(e);
            }
            return _rangeBuffer;
        }

        private void OnRoomCleared()
        {
            // 마지막 스테이지 = 보스방. 보스를 잡으면 **챕터 클리어**로 끝난다.
            if (_roomKind == RoomKind.Elite) _eliteRoomsCleared++;
            bool isLast = IsLastRoom;
            _bus.Publish(new RoomClearedEvent { ClearedRoomIndex = _roomIndex, IsLastRoom = isLast });
            if (isLast) { Finish(true); return; }

            // 버프는 이제 **레벨업**에서 나온다(기획서 A 5-2). 방을 비운 것만으로는
            // 주지 않는다 — 잡는 만큼 성장하는 쪽이 교전을 피하지 않게 만든다.
            SpawnExit();
        }

        /// <summary>제시된 3장 중 하나를 고른다. UI 가 호출한다.</summary>
        public void ChooseBuff(string buffKey)
        {
            if (!_awaitingBuff) return;

            var e = _buffTable?.Get(buffKey);
            if (e == null) return;

            _buffs.Apply(e);

            // 즉발 효과 — 누적 배율이 아니라 그 자리에서 끝나는 것들
            if (e.Kind == BuffKind.GhostHp)
            {
                _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + e.Value);
                PublishHp();
            }
            else if (e.Kind == BuffKind.Heal && _host != null)
            {
                _host.Heal(Mathf.Max(1, _host.HpMax * e.Value / 100));
                PublishHp();
            }

            _awaitingBuff = false;
            _offer.Clear();
            _bus.Publish(new BuffChosenEvent { ChosenKey = buffKey, TotalBuffCount = _buffs.Count });
            // 레벨업 중에도 방이 이미 비었을 수 있다 — 그때는 고른 뒤에 출구를 연다.
            if (_enemies.Count == 0 && _exits.Count == 0 && !IsLastRoom
                && (_canonRoom == null || _wave >= _canonRoom.LastWave))
                SpawnExit();
        }

        // ── 출구 ─────────────────────────────────────────────────
        // 방을 비우면 자동으로 다음 방으로 넘어가는 게 아니라 **출구가 열린다.**
        // 걸어서 통과해야 넘어가므로, 다 잡은 뒤에도 한 번 더 판단할 여지가 생긴다
        // (남은 Ghost HP 를 보고 쉬어 갈지 바로 갈지 — 시계가 계속 도는 상태다).

        private void SpawnExit()
        {
            DespawnExit();

            // 갈림길 방은 문이 둘이다(CH2_N03 · CH3_N03). 어느 문으로 걸어 나가느냐가
            // 곧 선택이므로, 팝업을 띄우지 않고 문을 둘 다 세운다 — 걸어서 고른다.
            if (_canonRoom != null && _canonRoom.Exits.Count > 0)
            {
                for (int i = 0; i < _canonRoom.Exits.Count; i++)
                {
                    var x = _canonRoom.Exits[i];
                    _exits.Add(NewExit(ToPixels(x.At), x.NextRoomId));
                }
            }
            else
            {
                _exits.Add(NewExit(new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.05f), null));
            }

            _bus.Publish(new ExitOpenedEvent { StageIndex = _roomIndex });
        }

        private ExitGate NewExit(Vector2 at, string nextRoomId)
        {
            var go = new GameObject($"Exit_{nextRoomId ?? "next"}",
                                    typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 132f);
            rt.anchoredPosition = at;

            var img = go.GetComponent<Image>();
            img.sprite = GetSprite("exitportal");
            img.raycastTarget = false;
            img.preserveAspect = true;
            return new ExitGate { View = rt, NextRoomId = nextRoomId };
        }

        private void DespawnExit()
        {
            for (int i = 0; i < _exits.Count; i++)
                if (_exits[i].View != null) Destroy(_exits[i].View.gameObject);
            _exits.Clear();
        }

        /// <summary>출구에 닿았으면 다음 스테이지로 넘어간다.</summary>
        private void TickExit()
        {
            if (_exits.Count == 0) return;
            var me = Avatar;
            if (me == null) return;

            for (int i = 0; i < _exits.Count; i++)
            {
                var gate = _exits[i];
                if (gate.View == null) continue;
                if (Vector2.Distance(me.Position, gate.View.anchoredPosition)
                    > _config.ExitTouchRadius) continue;

                // 어느 문으로 나갔는지가 곧 경로 선택이다.
                if (_canonRoom != null) _canonRoomId = gate.NextRoomId;
                DespawnExit();
                // 도달 스테이지를 갱신한다 — 호스트 해금 조건이 이 값을 본다.
                if (_player != null)
                    _player.SetProgress(_player.CurrentChapter, _roomIndex + 2);
                EnterRoom(_roomIndex + 1);
                return;
            }
        }

        private int GhostHpMax => _config.GhostHpMax + _buffs.GhostHpBonus;

        private void Finish(bool cleared)
        {
            if (!_running) return;
            _running = false;
            // 보상은 **통과한 스테이지 수** 기준. 챕터를 끝냈으면 전부 통과한 것이다.
            int stages = cleared ? RoomTotal : Mathf.Max(0, _roomIndex);

            // 정본 REWARD_DB — 방마다 골드가 조금씩 붙고, 정예방은 스피릿 코어와
            // 호스트 메모리를 준다. 챕터를 끝내면 큰 몫이 따로 온다.
            //   R_STD  방당 Gold 10
            //   R_ELITE 정예방 Gold 25 · Core 2 · Memory 1
            //   R_CH1  챕터 클리어 Gold 120 · Core 4 · EXP 5
            int gold = stages * 10 + _eliteRoomsCleared * 15;
            int core = _eliteRoomsCleared * 2;
            int memory = _eliteRoomsCleared;
            int gem = 0;
            if (cleared)
            {
                int ch = Mathf.Clamp(_player != null ? _player.CurrentChapter : 1, 1, 3);
                gold += ch == 1 ? 120 : ch == 2 ? 180 : 260;
                core += ch == 1 ? 4 : ch == 2 ? 6 : 9;
                memory += ch == 1 ? 0 : ch == 2 ? 2 : 4;
                gem += ch == 3 ? 20 : 0;
            }

            _bus.Publish(new StageFinishedEvent
            {
                IsCleared = cleared,
                RewardGold = gold,
                RewardGhostExp = _config.RewardGhostExp(stages),
                RewardSpiritCore = core,
                RewardHostMemory = memory,
                RewardGem = gem,
            });
        }

        private void PublishHp()
        {
            _bus.Publish(new CombatHpChangedEvent
            {
                GhostHp = _ghostHp,
                GhostHpMax = GhostHpMax,
                HostHp = _host != null ? _host.Hp : 0,
                HostHpMax = _host != null ? _host.HpMax : 0,
                HasHost = _host != null,
            });
        }
    }
}
