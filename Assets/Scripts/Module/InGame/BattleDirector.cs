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

        private const int MaxShots = 64;
        private readonly List<Projectile> _shots = new();
        private RectTransform _shotLayer;

        private int _roomIndex = -1;
        private int _ghostHp;
        /// <summary>유령 자연 감소의 소수점 이월. 프레임마다 반올림하면 3/초가 안 맞는다.</summary>
        private float _drainCarry;
        private float _invuln;
        private float _ghostProtect;
        private RectTransform _exit;
        private float _ultimateCharge;
        private bool _running;
        private Unit _possessTarget;
        private bool _hadPossessTarget;

        private float _stopTimer;

        private const float ChargeSeconds = 0.9f;
        private const float ChargeSpeedMul = 5.5f;
        private const int MaxRoomUnits = 14;
        private const float SummonRadius = 200f;

        /// <summary>적 배치 띠 — 필드 높이 대비. 아래쪽은 플레이어 시작 위치를 위해 비운다.</summary>
        private const float EnemyBandTop = 0.06f;
        private const float EnemyBandBottom = 0.44f;
        /// <summary>플레이어 시작 높이. 적 띠 끝과 탐지 거리보다 멀어야 첫 프레임에 안 달려든다.</summary>
        private const float PlayerStartY = 0.80f;
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
            _bus = CoreModule.Get<IEventBus>();
            CoreModule.TryGet(out _player);

            var res = CoreModule.Get<IResourceManager>();
            try { _atlas = await res.LoadAsync<SpriteAtlas>(AtlasAddress); }
            catch (Exception e) { Debug.LogError($"[Battle] 아틀라스 로드 실패 — {e.Message}"); }
            try { _config = await res.LoadAsync<GameConfig>("TableData/GameConfig"); }
            catch (Exception e) { Debug.LogError($"[Battle] GameConfig 로드 실패 — {e.Message}"); }
            if (_config == null) return;

            try { _bossTable = await res.LoadAsync<BossTable>("TableData/BossTable"); }
            catch (Exception e) { Debug.LogError($"[Battle] BossTable 로드 실패 — {e.Message}"); }
            try { _buffTable = await res.LoadAsync<BuffTable>("TableData/BuffTable"); }
            catch (Exception e) { Debug.LogError($"[Battle] BuffTable 로드 실패 — {e.Message}"); }

            // 스폰은 동기 코드다. 테이블이 다 올라온 뒤에 이 런이 쓸 캐릭터를 먼저 올린다.
            await LoadUnitAtlasesAsync(res, RunUnitKeys());
            _buffs.Clear();   // 버프는 런 한정 — 스테이지 진입마다 초기화한다

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
            _ghost = NewUnit("Ghost");
            _ghost.Setup(UnitSide.Player, "ghost", "GHOST", UnitGet("ghost"),
                         GhostHpMax, 0, _config.GhostMoveSpeed, 0f, 1f,
                         new Vector2(72f, 90f));
            _ghost.Position = new Vector2(_field.rect.width * 0.5f, -_field.rect.height * PlayerStartY);
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
            return atlas.GetSprite(suffix == null ? $"unit_{key}" : $"unit_{key}_{suffix}");
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

            // 빙의로 몸을 갈아타도 로비에서 고른 호스트는 긴급 투입으로 나올 수 있다.
            var emergency = PickPlayerHost();
            if (emergency != null) keys.Add(emergency.HostKey);

            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts != null && hosts.Count > 0)
            {
                // 방 종류(일반·정예)에 따라 마릿수가 달라지므로 둘 중 많은 쪽까지 훑는다.
                for (int index = 0; index < _config.StagesPerChapter; index++)
                {
                    int count = Mathf.Max(_config.EliteEnemyCount, _config.EnemiesPerRoom(index));
                    for (int i = 0; i < count; i++)
                    {
                        var key = EnemyAt(hosts, index, i).HostKey;
                        if (!keys.Contains(key)) keys.Add(key);
                    }
                }
            }
            return keys;
        }

        /// <summary>
        /// 방향 스프라이트 5장을 찾아 붙인다. 하나라도 없으면 붙이지 않는다 —
        /// 없는 방향만 원래 그림으로 나오면 캐릭터가 방향마다 바뀌어 보인다.
        /// 12종을 한 번에 만들지 않고 한 종씩 넣어 볼 수 있어야 해서 이렇게 둔다.
        /// </summary>
        private void ApplyFacingSprites(Unit u, string key)
        {
            var sets = new Sprite[Unit.FrameSuffix.Length][];
            for (int f = 0; f < sets.Length; f++)
                sets[f] = FrameSet(key, Unit.FrameSuffix[f]);
            if (sets[Unit.FrameIdle] == null) return;   // 방향 그림이 없는 종은 지금 그림 그대로 둔다
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
        /// 이 스테이지가 어떤 방인가 (기획서 A 06 ROOM TYPE).
        ///
        /// 마지막은 항상 보스다. 그 앞은 한 챕터 안에서 같은 방만 반복되지 않게
        /// 스테이지 번호로 갈라 준다 — 지금은 3스테이지라 경우의 수가 적다.
        /// 챕터가 길어지면 방 구성표를 데이터로 빼야 한다.
        /// </summary>
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
            _emergencyUsedThisRoom = false;   // 긴급 호스트는 방마다 한 번 (기획서 A 8-3)
            _roomKind = KindOf(index);
            bool isBoss = _roomKind == RoomKind.Boss;

            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null) Destroy(_enemies[i].gameObject);
            _enemies.Clear();

            // 이전 방에서 쓰러지던 몸은 여기서 끊는다. 안 그러면 새 방 바닥에
            // 앞 방 시체가 남아 페이드된다.
            for (int i = 0; i < _dying.Count; i++)
                if (_dying[i] != null) Destroy(_dying[i].gameObject);
            _dying.Clear();

            for (int i = 0; i < _damageTexts.Count; i++) _damageTexts[i].Despawn();

            _boss = null;

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

                var boss = NewUnit("Boss");
                boss.Setup(UnitSide.Enemy, def?.BossKey ?? "boss", def?.NameKr ?? "BOSS",
                           UnitGet(UnitKeyOf(def?.SpriteName ?? "unit_boss")),
                           Mathf.RoundToInt(_config.BossHp(chapter) * (def?.HpMul ?? 1f)),
                           Mathf.RoundToInt(_config.BossAtk * (def?.AtkMul ?? 1f)),
                           _config.BossMoveSpeed * (def?.MoveSpeedMul ?? 1f),
                           _config.BossAttackRange, _config.BossAttackInterval,
                           new Vector2(160f, 160f), isBoss: true);
                boss.Position = new Vector2(_field.rect.width * 0.5f, -_field.rect.height * 0.2f);
                _enemies.Add(boss);
                _boss = boss;
                _brain.Setup(def);
                _bus.Publish(new BossHpChangedEvent
                {
                    BossHp = boss.Hp, BossHpMax = boss.HpMax,
                    BossName = def != null ? $"{def.NameEn}" : "BOSS",
                    Phase = 1,
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
                    u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, UnitGet(e.HostKey),
                            Mathf.RoundToInt(_config.EnemyHp(e.Hp) * (elite ? _config.EliteHpMul : 1f)),
                            Mathf.RoundToInt(_config.EnemyAtk(e.Atk) * e.DamageMul * (elite ? _config.EliteAtkMul : 1f)),
                            _config.EnemySpeed(e.Spd),
                            _config.EnemyAttackRange * e.RangeMul,
                            _config.EnemyAttackInterval * e.IntervalMul,
                            new Vector2(84f, 78f), isBoss: false, profile: e);
                    u.Position = SpawnSlot(i, count);
                    // 기획서 A 4-3 — 빙의 우선순위·사거리는 적마다 다를 수 있다.
                    // 사거리 0 은 "전역 기본값을 쓴다"는 뜻이다.
                    u.PossessPriority = e.PossessPriority;
                    u.PossessRange = 0f;
                    u.SetState(EnemyState.Idle);
                    ApplyFacingSprites(u, e.HostKey);
                    _enemies.Add(u);
                }
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }

            _bus.Publish(new RoomEnteredEvent
            {
                RoomIndex = index, RoomTotal = _config.StagesPerChapter,
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
            float w = _field.rect.width, h = _field.rect.height;
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
        private void ClampToField(Unit u)
        {
            var half = ((RectTransform)u.transform).sizeDelta * 0.5f;
            var p = u.Position;
            p.x = Mathf.Clamp(p.x, half.x, _field.rect.width - half.x);
            p.y = Mathf.Clamp(p.y, -_field.rect.height + half.y, -half.y);
            u.Position = p;
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_running || _config == null) return;
            if (_awaitingBuff) return;   // 3택1 선택 대기 — 적이 없는 상태라 멈춰도 안전하다
            float dt = Time.deltaTime;

            TickGhostState(dt);
            if (!_running) return;       // 자연 감소로 소멸했을 수 있다

            _ultimateCharge = Mathf.Min(_ultimateCharge + dt * _buffs.UltimateChargeMul,
                                        _config.UltimateChargeSeconds);

            TickPlayer(dt);
            SyncFireRing();
            TickEnemies(dt);
            TickShots(dt);
            CleanupDead();
            // CleanupDead 다음에 돈다 — 이번 프레임에 죽은 몸도 바로 쓰러지기 시작한다.
            TickDying(dt);
            TickDamageTexts(dt);
            RefreshPossessTarget();
            TickEmergency(dt);
            if (!_running) return;      // 긴급 호스트를 못 써서 졌을 수 있다
            TickExit();

            // 출구가 이미 열려 있으면 다시 클리어 처리하지 않는다
            if (_enemies.Count == 0 && _exit == null && !_awaitingBuff)
            {
                // ⚠ 진단용 — "적이 남았는데 클리어가 뜬다"는 제보를 추적한다.
                //    화면에 보이는데 목록에서 빠진 몸이 있는지 함께 남긴다.
                int alive = 0;
                var layer = _unitLayer;
                for (int i = 0; i < layer.childCount; i++)
                {
                    var u = layer.GetChild(i).GetComponent<Unit>();
                    if (u != null && u.Side == UnitSide.Enemy && u.IsAlive) alive++;
                }
                Debug.Log($"[Battle] 룸 클리어 판정 — 방 {_roomIndex}({_roomKind}) "
                          + $"목록 {_enemies.Count} / 화면에 살아있는 적 {alive} / 쓰러지는 중 {_dying.Count}");
                OnRoomCleared();
            }
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

            // ⚠️ 궁수의 전설 규칙 — **움직이는 동안에는 쏘지 않는다.**
            //    이동과 공격이 배타적이어야 "자리를 잡을까 딜을 넣을까"의 긴장이 생긴다.
            //    이걸 없애면 조작이 그냥 산책이 된다.
            bool moving = MoveInput.sqrMagnitude > 0.0001f;
            if (!moving) me.SetMoving(false);
            if (moving)
            {
                var p = me.Position + MoveInput * (me.MoveSpeed * _buffs.MoveMul) * dt;
                var half = me.GetComponent<RectTransform>().sizeDelta * 0.5f;
                p.x = Mathf.Clamp(p.x, half.x, _field.rect.width - half.x);
                p.y = Mathf.Clamp(p.y, -_field.rect.height + half.y, -half.y);
                me.Position = p;

                // 걷는 쪽을 바라본다. 아래 `return` 때문에 이동 중에는 조준 쪽
                // 방향 전환에 도달하지 못하므로, 여기서 돌려 주지 않으면
                // 이동 중에는 방향이 통째로 멈춘다.
                // 이동 중 사격이 되는 호스트는 아래에서 조준 방향이 덮어쓴다 —
                // 겨누는 쪽이 걷는 쪽보다 우선이다.
                me.SetFacing(MoveInput);
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
            if (_stopTimer < _config.AttackResumeSeconds) { IsFiring = false; return; }

            // 고스트는 공격하지 않는다 — 빙의해야 싸울 수 있다(핵심 동사)
            if (_host == null) { IsFiring = false; return; }

            var target = Nearest(_host.Position);

            // 노리는 쪽을 바라본다. 사거리 밖이라 아직 안 쏘더라도 몸은 돌려 둔다 —
            // 조준이 먼저 보이고 사격이 뒤따라야 "겨눈다"는 느낌이 난다.
            if (target != null) _host.SetFacing(target.Position - _host.Position);

            bool inRange = target != null &&
                           Vector2.Distance(_host.Position, target.Position)
                               <= _host.AttackRange * _buffs.RangeMul;
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
            var me = Avatar;
            if (me == null) return;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                e.TickFlash(dt);
                e.TickAnim(dt);
                e.TickSlow(dt);

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
                    if (d > _config.EnemyDetectRange)
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

                if (d > e.AttackRange)
                {
                    e.SetState(EnemyState.Approach);
                    e.MoveToward(me.Position, dt);
                    e.SetMoving(true);
                }
                else if (e.TickAttack(dt))
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    PerformAttack(e, me, false);
                }
                else
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                }

                Separate(e, i, dt);
            }
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
        }

        // ── 보스 ─────────────────────────────────────────────────
        private void TickBoss(Unit boss, Unit me, float dt)
        {
            int before = _brain.Phase;
            _brain.UpdatePhase((float)boss.Hp / boss.HpMax);
            if (_brain.Phase != before)
                _bus.Publish(new BossHpChangedEvent
                {
                    BossHp = boss.Hp, BossHpMax = boss.HpMax, Phase = _brain.Phase,
                });

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
            }
        }

        private void FireFan(Unit from, Vector2 at, int count, float spanDeg, int damage)
        {
            // 보스 탄은 **화면 끝까지 나가야 한다.** 수명이 짧으면 중간에 사라져
            // 보스에게서 멀찍이 떨어진 곳이 안전지대가 되고, 탄막을 피할 이유가 없어진다.
            float speed = _config.ShotSpeedEnemy;
            float reach = new Vector2(_field.rect.width, _field.rect.height).magnitude;
            float life = reach / Mathf.Max(1f, speed) + 0.25f;

            for (int i = 0; i < count; i++)
            {
                float off = count == 1 ? 0f : -spanDeg * 0.5f + spanDeg * i / (count - 1);
                var shot = RentShot();
                if (shot == null) return;
                shot.Fire(from.Position, at, speed, damage,
                          false, null, _config.ShotSize * 1.15f, ShotBossColor,
                          life, angleOffsetDeg: off);
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
                u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, UnitGet(e.HostKey),
                        Mathf.Max(1, Mathf.RoundToInt(_config.EnemyHp(e.Hp) * 0.6f)),
                        Mathf.RoundToInt(_config.EnemyAtk(e.Atk) * e.DamageMul),
                        _config.EnemySpeed(e.Spd),
                        _config.EnemyAttackRange * e.RangeMul,
                        _config.EnemyAttackInterval * e.IntervalMul,
                        new Vector2(78f, 72f), isBoss: false, profile: e);

                // 보스(160px)와 겹치지 않게 바깥에 원형으로 흩는다
                float a = (i / (float)count) * Mathf.PI * 2f + _enemies.Count * 0.7f;
                u.Position = boss.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * SummonRadius;
                u.IsAggro = true;   // 불러낸 것들은 기다리지 않는다
                ClampToField(u);
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
                    int n = (kind == AttackKind.Spread ? p.ShotCount : 1) + extra;
                    float span = kind == AttackKind.Spread ? p.SpreadDegrees : 0f;
                    if (extra > 0) span = Mathf.Max(span, 10f * (n - 1));

                    for (int i = 0; i < n; i++)
                    {
                        float off = n == 1 ? 0f : -span * 0.5f + span * i / (n - 1);
                        FireShot(attacker, target, fromPlayer, off);
                    }
                    break;
                }
            }
        }

        /// <summary>근접·광역은 탄을 쓰지 않고 즉시 판정한다. 대신 타격 위치에 섬광만 남긴다.</summary>
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
                    if (Vector2.Distance(s.Position, attacker.Position) <= reach) s.Despawn();
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
                    HitEnemyWith(e, Mathf.RoundToInt(attacker.Atk * _buffs.AttackMul), p);
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

        private void HitEnemyWith(Unit victim, int damage, HostEntry p)
        {
            victim.IsAggro = true;
            victim.SetState(EnemyState.Hit);
            ShowDamage(victim.Position, damage, toEnemy: true);
            bool dead = victim.TakeDamage(damage);
            int slow = (p?.SlowPercent ?? 0) + _buffs.SlowPercent;
            int steal = (p?.LifestealPercent ?? 0) + _buffs.LifestealPercent;
            if (slow > 0) victim.ApplySlow(slow, _config.SlowSeconds);
            if (steal > 0 && _host != null)
                _host.Heal(Mathf.Max(1, damage * steal / 100));

            if (dead) { KillEnemy(victim); return; }
            if (victim.IsBoss)
                _bus.Publish(new BossHpChangedEvent { BossHp = victim.Hp, BossHpMax = victim.HpMax });
        }

        // ── 투사체 ────────────────────────────────────────────────
        private static readonly Color ShotPlayerColor = new(1f, 0.72f, 0.24f, 1f);
        private static readonly Color ShotEnemyColor = new(0.55f, 0.78f, 1f, 1f);
        // 보스 탄은 잡몹과 색을 나눈다 — 화면이 탄으로 덮이면 무엇을 피해야 할지 안 보인다
        private static readonly Color ShotBossColor = new(1f, 0.36f, 0.30f, 1f);

        private void FireShot(Unit attacker, Unit target, bool fromPlayer, float angleOffsetDeg)
        {
            var shot = RentShot();
            if (shot == null) return;
            var p = attacker.Profile;
            bool snipe = p != null && p.Kind == AttackKind.Snipe;

            float speed = (fromPlayer ? _config.ShotSpeedPlayer : _config.ShotSpeedEnemy)
                          * (snipe ? 1.6f : 1f)
                          * (fromPlayer ? _buffs.ShotSpeedMul : 1f);

            // 몸 중심이 아니라 총구에서 나간다. 탄이 배에서 튀어나오면
            // 방향 스프라이트를 그린 의미가 없다.
            shot.Fire(attacker.MuzzlePosition, target.Position, speed,
                      fromPlayer ? Mathf.RoundToInt(attacker.Atk * _buffs.AttackMul) : attacker.Atk,
                      fromPlayer, target, _config.ShotSize,
                      fromPlayer ? ShotPlayerColor : ShotEnemyColor,
                      _config.ShotLifeSeconds,
                      pierce: (p != null && p.Kind == AttackKind.Pierce) || (fromPlayer && _buffs.Pierce),
                      slowPercent: (p?.SlowPercent ?? 0) + (fromPlayer ? _buffs.SlowPercent : 0),
                      lifestealPercent: (p?.LifestealPercent ?? 0) + (fromPlayer ? _buffs.LifestealPercent : 0),
                      angleOffsetDeg: angleOffsetDeg);
        }

        /// <summary>풀에서 하나 꺼낸다. 매 발마다 GameObject 를 만들면 교전 중 GC 가 튄다.</summary>
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

        private void TickShots(float dt)
        {
            var me = Avatar;
            for (int i = 0; i < _shots.Count; i++)
            {
                var p = _shots[i];
                if (!p.IsActive) continue;

                if (!p.Tick(dt)) { p.Despawn(); continue; }

                if (p.Damage <= 0) continue;   // 근접 타격 섬광 — 수명만 흘려보낸다

                if (p.FromPlayer)
                {
                    var hit = HitEnemy(p.Position, p);
                    if (hit == null) continue;
                    if (p.Pierce) p.MarkHit(hit); else p.Despawn();
                    ApplyShotHit(hit, p);
                }
                else
                {
                    if (me == null) { p.Despawn(); continue; }
                    if (Vector2.Distance(p.Position, me.Position) > _config.ShotHitRadius) continue;
                    p.Despawn();
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
            ShowDamage(victim.Position, shot.Damage, toEnemy: true);
            bool dead = victim.TakeDamage(shot.Damage);
            if (shot.SlowPercent > 0) victim.ApplySlow(shot.SlowPercent, _config.SlowSeconds);
            if (shot.LifestealPercent > 0 && _host != null)
                _host.Heal(Mathf.Max(1, shot.Damage * shot.LifestealPercent / 100));

            if (dead) { KillEnemy(victim); return; }
            if (victim.IsBoss)
                _bus.Publish(new BossHpChangedEvent { BossHp = victim.Hp, BossHpMax = victim.HpMax });
        }

        private void DamagePlayer(int amount)
        {
            if (IsInvulnerable) return;
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
            _buffTable?.Draw(_offer, 3, _buffs.ExcludedKeys, _rng, _host?.Profile);
            if (_offer.Count == 0) return;

            _awaitingBuff = true;
            var keys = new string[_offer.Count];
            for (int i = 0; i < _offer.Count; i++) keys[i] = _offer[i].BuffKey;
            _bus.Publish(new BuffOfferEvent { OfferedKeys = keys });
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

        /// <summary>빙의 가능 대상 갱신. 고스트 상태에서만 의미가 있다.</summary>
        private void RefreshPossessTarget()
        {
            _possessTarget = null;
            if (_host == null && _ghost != null)
            {
                // 기획서 A 4-3 — 우선순위가 높은 적을 먼저 잡는다. 같으면 가까운 쪽.
                // 사거리는 적마다 다를 수 있다(PossessRange 0 이면 전역 기본값).
                int bestPri = int.MinValue;
                float bestD = float.MaxValue;
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsPossessable) continue;

                    float range = e.PossessRange > 0f ? e.PossessRange : _config.PossessRange;
                    float d = Vector2.Distance(_ghost.Position, e.Position);
                    if (d > range) continue;

                    if (e.PossessPriority < bestPri) continue;
                    if (e.PossessPriority == bestPri && d >= bestD) continue;

                    bestPri = e.PossessPriority; bestD = d; _possessTarget = e;
                }
            }
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null) _enemies[i].SetPossessMark(_enemies[i] == _possessTarget);

            bool has = _possessTarget != null;
            if (has == _hadPossessTarget) return;
            _hadPossessTarget = has;
            _bus.Publish(new PossessTargetChangedEvent { HasTarget = has });
        }

        // ─────────────────────────────────────────────────────────
        public void TryPossess()
        {
            if (!_running || _host != null || _possessTarget == null) return;

            var target = _possessTarget;
            var entry = _player.GetHost(target.Key);
            var pos = target.Position;
            _enemies.Remove(target);
            Destroy(target.gameObject);
            _possessTarget = null;

            EnterHost(entry, target.Key, target.DisplayName, pos, _config.HostStartHpPercent);
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
                        entry != null ? _config.HostHp(entry.Hp) : 100,
                        entry != null ? Mathf.RoundToInt(_config.HostAtk(entry.Atk) * entry.DamageMul) : 10,
                        entry != null ? _config.HostSpeed(entry.Spd) : 180f,
                        _config.HostAttackRange * (entry?.RangeMul ?? 1f),
                        _config.HostAttackInterval * (entry?.IntervalMul ?? 1f),
                        new Vector2(96f, 92f), isBoss: false, profile: entry);
            _host.Position = pos;
            ApplyFacingSprites(_host, key);

            // 기획서 A 3-3 — 빼앗은 몸은 온전하지 않다. 최대 체력의 70%로 시작한다.
            // 이게 없으면 교체가 곧 완전 회복이라, 몸을 갈아타는 데 대가가 없어진다.
            _host.SetHpPercent(startHpPercent);

            // 기획서 A 02 — 빙의 직후 무적(0.35) + 호스트 진입 무적(0.5). 몸을 얻는 순간이
            // 가장 취약한 지점이라, 여기서 맞으면 빙의 자체가 손해가 된다.
            _invuln = _config.PossessInvulnSeconds;
            _ghostProtect = 0f;
            _emergencyWait = 0f;
            // 태그형·전용 버프는 쓰는 몸에 따라 켜지고 꺼진다 (기획서 A 5-4)
            _buffs.SetHost(entry);

            _bus.Publish(new PossessedEvent
            {
                PossessedHostKey = key,
                // 어떤 몸을 뺏었는지가 곧 빌드다 — 교전 스타일을 함께 보여준다
                DisplayName = entry != null ? $"{entry.NameEn}  ·  {entry.AttackText}" : fallbackName,
                HostHpMax = _host.HpMax,
            });
            PublishHp();
        }

        public void TryUltimate()
        {
            if (!_running || _ultimateCharge < _config.UltimateChargeSeconds) return;
            _ultimateCharge = 0f;

            // 화면 전체 광역. 오토어택 게임이라 위치를 고르는 조작을 넣지 않는다.
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null) continue;
                ShowDamage(e.Position, _config.UltimateDamage, toEnemy: true);
                if (e.TakeDamage(_config.UltimateDamage)) KillEnemy(e);
                else if (e.IsBoss)
                    _bus.Publish(new BossHpChangedEvent { BossHp = e.Hp, BossHpMax = e.HpMax });
            }
        }

        private void OnRoomCleared()
        {
            // 마지막 스테이지 = 보스방. 보스를 잡으면 **챕터 클리어**로 끝난다.
            bool isLast = _roomIndex >= _config.StagesPerChapter - 1;
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
            if (_enemies.Count == 0 && _exit == null && _roomIndex < _config.StagesPerChapter - 1)
                SpawnExit();
        }

        // ── 출구 ─────────────────────────────────────────────────
        // 방을 비우면 자동으로 다음 방으로 넘어가는 게 아니라 **출구가 열린다.**
        // 걸어서 통과해야 넘어가므로, 다 잡은 뒤에도 한 번 더 판단할 여지가 생긴다
        // (남은 Ghost HP 를 보고 쉬어 갈지 바로 갈지 — 시계가 계속 도는 상태다).

        private void SpawnExit()
        {
            DespawnExit();
            var go = new GameObject("Exit", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);

            _exit = (RectTransform)go.transform;
            _exit.anchorMin = _exit.anchorMax = new Vector2(0f, 1f);
            _exit.pivot = new Vector2(0.5f, 0.5f);
            _exit.sizeDelta = new Vector2(120f, 132f);
            _exit.anchoredPosition = new Vector2(_field.rect.width * 0.5f, -_field.rect.height * 0.16f);

            var img = go.GetComponent<Image>();
            img.sprite = GetSprite("exitportal");
            img.raycastTarget = false;
            img.preserveAspect = true;

            _bus.Publish(new ExitOpenedEvent { StageIndex = _roomIndex });
        }

        private void DespawnExit()
        {
            if (_exit == null) return;
            Destroy(_exit.gameObject);
            _exit = null;
        }

        /// <summary>출구에 닿았으면 다음 스테이지로 넘어간다.</summary>
        private void TickExit()
        {
            if (_exit == null) return;
            var me = Avatar;
            if (me == null) return;
            if (Vector2.Distance(me.Position, _exit.anchoredPosition) > _config.ExitTouchRadius) return;

            DespawnExit();
            // 도달 스테이지를 갱신한다 — 호스트 해금 조건이 이 값을 본다.
            if (_player != null)
                _player.SetProgress(_player.CurrentChapter, _roomIndex + 2);
            EnterRoom(_roomIndex + 1);
        }

        private int GhostHpMax => _config.GhostHpMax + _buffs.GhostHpBonus;

        private void Finish(bool cleared)
        {
            if (!_running) return;
            _running = false;
            // 보상은 **통과한 스테이지 수** 기준. 챕터를 끝냈으면 전부 통과한 것이다.
            int stages = cleared ? _config.StagesPerChapter : Mathf.Max(0, _roomIndex);
            _bus.Publish(new StageFinishedEvent
            {
                IsCleared = cleared,
                RewardGold = _config.RewardGold(stages),
                RewardGhostExp = _config.RewardGhostExp(stages),
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
