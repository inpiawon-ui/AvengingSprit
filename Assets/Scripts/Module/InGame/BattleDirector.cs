using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
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

        private RectTransform _field;
        private RectTransform _unitLayer;
        private GameConfig _config;
        private IPlayerDataService _player;
        private SpriteAtlas _atlas;
        private IEventBus _bus;

        private Unit _ghost;
        private Unit _host;                       // 빙의 중이 아니면 null
        private readonly List<Unit> _enemies = new();
        private readonly List<Unit> _dead = new();   // 정리용 재사용 버퍼 (hot path 할당 금지)

        private const int MaxShots = 64;
        private readonly List<Projectile> _shots = new();
        private RectTransform _shotLayer;

        private int _roomIndex = -1;
        private int _ghostHp;
        private float _ultimateCharge;
        private bool _running;
        private Unit _possessTarget;
        private bool _hadPossessTarget;

        public Vector2 MoveInput { get; set; }
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

            // 탄은 유닛보다 위에 그린다 — 유닛 뒤로 숨으면 피격 판단이 안 보인다
            var shotGo = new GameObject("ShotLayer", typeof(RectTransform));
            shotGo.transform.SetParent(_unitLayer.parent, false);
            _shotLayer = (RectTransform)shotGo.transform;
            _shotLayer.anchorMin = _unitLayer.anchorMin;
            _shotLayer.anchorMax = _unitLayer.anchorMax;
            _shotLayer.pivot = _unitLayer.pivot;
            _shotLayer.anchoredPosition = _unitLayer.anchoredPosition;
            _shotLayer.sizeDelta = _unitLayer.sizeDelta;

            SpawnGhost();
            EnterRoom(0);
            _running = true;
        }

        // ─────────────────────────────────────────────────────────
        private void SpawnGhost()
        {
            _ghostHp = _config.GhostHpMax;
            _ghost = NewUnit("Ghost");
            _ghost.Setup(UnitSide.Player, "ghost", "GHOST", GetSprite("unit_ghost"),
                         _config.GhostHpMax, 0, _config.GhostMoveSpeed, 0f, 1f,
                         new Vector2(72f, 90f));
            _ghost.Position = new Vector2(_field.rect.width * 0.5f, -_field.rect.height * 0.72f);
            PublishHp();
        }

        private Unit NewUnit(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_unitLayer, false);
            return go.AddComponent<Unit>();
        }

        private Sprite GetSprite(string n) => _atlas != null ? _atlas.GetSprite(n) : null;

        /// <summary>HUD 초상용. 아틀라스를 들고 있는 쪽이 하나뿐이라 여기서 내준다.</summary>
        public Sprite UnitSprite(string hostKey) => GetSprite($"unit_{hostKey}");

        private void EnterRoom(int index)
        {
            _roomIndex = index;
            bool isBoss = index == _config.RoomsPerStage - 1;

            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null) Destroy(_enemies[i].gameObject);
            _enemies.Clear();

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
                var boss = NewUnit("Boss");
                int chapter = _player != null ? _player.CurrentChapter : 1;
                boss.Setup(UnitSide.Enemy, "boss", "BOSS", GetSprite("unit_boss"),
                           _config.BossHp(chapter), _config.BossAtk, _config.BossMoveSpeed,
                           _config.BossAttackRange, _config.BossAttackInterval,
                           new Vector2(160f, 160f), isBoss: true);
                boss.Position = new Vector2(_field.rect.width * 0.5f, -_field.rect.height * 0.2f);
                _enemies.Add(boss);
                _bus.Publish(new BossHpChangedEvent { BossHp = boss.Hp, BossHpMax = boss.HpMax });
            }
            else
            {
                int count = _config.EnemiesPerRoom(index);
                for (int i = 0; i < count; i++)
                {
                    // 방마다 등장 조합이 달라지도록 룸 인덱스를 섞어 넣는다
                    var e = hosts[(i * 5 + index * 3 + 1) % hosts.Count];
                    var u = NewUnit($"Enemy_{e.HostKey}_{i}");
                    u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, GetSprite($"unit_{e.HostKey}"),
                            _config.EnemyHp(e.Hp), _config.EnemyAtk(e.Atk), _config.EnemySpeed(e.Spd),
                            _config.EnemyAttackRange, _config.EnemyAttackInterval,
                            new Vector2(84f, 78f));
                    u.Position = SpawnSlot(i, count);
                    _enemies.Add(u);
                }
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }

            _bus.Publish(new RoomEnteredEvent
            {
                RoomIndex = index, RoomTotal = _config.RoomsPerStage, IsBossRoom = isBoss,
            });
        }

        /// <summary>필드 상단 절반에 고르게 흩어 놓는다. 플레이어 시작 위치와 겹치지 않게 한다.</summary>
        private Vector2 SpawnSlot(int i, int count)
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int row = i / cols, col = i % cols;
            float w = _field.rect.width, h = _field.rect.height;
            float x = w * (0.18f + 0.64f * (cols == 1 ? 0.5f : (float)col / (cols - 1)));
            float y = -h * (0.14f + 0.30f * row);
            return new Vector2(x, y);
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_running || _config == null) return;
            float dt = Time.deltaTime;

            _ultimateCharge = Mathf.Min(_ultimateCharge + dt, _config.UltimateChargeSeconds);

            TickPlayer(dt);
            TickEnemies(dt);
            TickShots(dt);
            CleanupDead();
            RefreshPossessTarget();

            if (_enemies.Count == 0) OnRoomCleared();
        }

        private Unit Avatar => _host != null ? _host : _ghost;

        private void TickPlayer(float dt)
        {
            var me = Avatar;
            if (me == null) return;
            me.TickFlash(dt);

            // 이동 — 조이스틱 입력. 필드 밖으로 나가지 않게 잘라낸다.
            if (MoveInput.sqrMagnitude > 0.0001f)
            {
                var p = me.Position + MoveInput * me.MoveSpeed * dt;
                var half = me.GetComponent<RectTransform>().sizeDelta * 0.5f;
                p.x = Mathf.Clamp(p.x, half.x, _field.rect.width - half.x);
                p.y = Mathf.Clamp(p.y, -_field.rect.height + half.y, -half.y);
                me.Position = p;
            }

            // 고스트는 공격하지 않는다 — 빙의해야 싸울 수 있다(핵심 동사)
            if (_host == null) return;

            var target = Nearest(_host.Position);
            if (target == null) return;
            if (Vector2.Distance(_host.Position, target.Position) > _host.AttackRange) return;
            if (!_host.TickAttack(dt)) return;
            FireShot(_host.Position, target.Position, _host.Atk, true, target);
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

                float d = Vector2.Distance(e.Position, me.Position);
                if (d > e.AttackRange) { e.MoveToward(me.Position, dt); continue; }
                if (!e.TickAttack(dt)) continue;
                FireShot(e.Position, me.Position, e.Atk, false, me);
            }
        }

        // ── 투사체 ────────────────────────────────────────────────
        private static readonly Color ShotPlayerColor = new(1f, 0.72f, 0.24f, 1f);
        private static readonly Color ShotEnemyColor = new(0.55f, 0.78f, 1f, 1f);

        private void FireShot(Vector2 from, Vector2 to, int damage, bool fromPlayer, Unit target)
        {
            var p = RentShot();
            if (p == null) return;
            p.Fire(from, to,
                   fromPlayer ? _config.ShotSpeedPlayer : _config.ShotSpeedEnemy,
                   damage, fromPlayer, target, _config.ShotSize,
                   fromPlayer ? ShotPlayerColor : ShotEnemyColor,
                   _config.ShotLifeSeconds);
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

                if (p.FromPlayer)
                {
                    var hit = HitEnemy(p.Position);
                    if (hit == null) continue;
                    p.Despawn();
                    if (hit.TakeDamage(p.Damage)) KillEnemy(hit);
                    else if (hit.IsBoss)
                        _bus.Publish(new BossHpChangedEvent { BossHp = hit.Hp, BossHpMax = hit.HpMax });
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

        private Unit HitEnemy(Vector2 at)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (Vector2.Distance(at, e.Position) <= _config.ShotHitRadius) return e;
            }
            return null;
        }

        private void DamagePlayer(int amount)
        {
            if (_host != null)
            {
                if (_host.TakeDamage(amount)) LoseHost();
                else PublishHp();
                return;
            }
            // 유령은 싸울 수 없다. 원피해를 그대로 받으면 빙의하기 전에 소멸한다.
            int reduced = _config.GhostDamage(amount);
            _ghostHp = Mathf.Max(0, _ghostHp - reduced);
            _ghost.TakeDamage(reduced);
            PublishHp();
            if (_ghostHp == 0) Finish(false);
        }

        private void LoseHost()
        {
            var pos = _host.Position;
            var key = _host.Key;
            Destroy(_host.gameObject);
            _host = null;

            _ghost.gameObject.SetActive(true);
            _ghost.Position = pos;
            _bus.Publish(new HostLostEvent { LostHostKey = key });
            PublishHp();
        }

        private Unit Nearest(Vector2 from)
        {
            Unit best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                float d = Vector2.Distance(from, e.Position);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private void KillEnemy(Unit u)
        {
            if (u.IsBoss) _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = u.HpMax });
            _enemies.Remove(u);
            if (u == _possessTarget) _possessTarget = null;
            Destroy(u.gameObject);
        }

        private void CleanupDead()
        {
            _dead.Clear();
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] == null || !_enemies[i].IsAlive) _dead.Add(_enemies[i]);
            for (int i = 0; i < _dead.Count; i++)
            {
                _enemies.Remove(_dead[i]);
                if (_dead[i] != null) Destroy(_dead[i].gameObject);
            }
            _dead.Clear();
        }

        /// <summary>빙의 가능 대상 갱신. 고스트 상태에서만 의미가 있다.</summary>
        private void RefreshPossessTarget()
        {
            _possessTarget = null;
            if (_host == null && _ghost != null)
            {
                float best = _config.PossessRange;
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsPossessable) continue;
                    float d = Vector2.Distance(_ghost.Position, e.Position);
                    if (d <= best) { best = d; _possessTarget = e; }
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

            _ghost.gameObject.SetActive(false);

            _host = NewUnit($"Host_{target.Key}");
            _host.Setup(UnitSide.Player, target.Key, entry != null ? entry.NameKr : target.DisplayName,
                        GetSprite($"unit_{target.Key}"),
                        entry != null ? _config.HostHp(entry.Hp) : 100,
                        entry != null ? _config.HostAtk(entry.Atk) : 10,
                        entry != null ? _config.HostSpeed(entry.Spd) : 180f,
                        _config.HostAttackRange, _config.HostAttackInterval,
                        new Vector2(96f, 92f));
            _host.Position = pos;

            _bus.Publish(new PossessedEvent
            {
                PossessedHostKey = target.Key,
                DisplayName = entry != null ? $"{entry.NameEn}" : target.DisplayName,
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
                if (e.TakeDamage(_config.UltimateDamage)) KillEnemy(e);
                else if (e.IsBoss)
                    _bus.Publish(new BossHpChangedEvent { BossHp = e.Hp, BossHpMax = e.HpMax });
            }
        }

        private void OnRoomCleared()
        {
            bool isLast = _roomIndex >= _config.RoomsPerStage - 1;
            _bus.Publish(new RoomClearedEvent { ClearedRoomIndex = _roomIndex, IsLastRoom = isLast });
            if (isLast) { Finish(true); return; }
            EnterRoom(_roomIndex + 1);
        }

        private void Finish(bool cleared)
        {
            if (!_running) return;
            _running = false;
            int rooms = cleared ? _config.RoomsPerStage : Mathf.Max(0, _roomIndex);
            _bus.Publish(new StageFinishedEvent
            {
                IsCleared = cleared,
                RewardGold = _config.RewardGold(rooms),
                RewardGhostExp = _config.RewardGhostExp(rooms),
            });
        }

        private void PublishHp()
        {
            _bus.Publish(new CombatHpChangedEvent
            {
                GhostHp = _ghostHp,
                GhostHpMax = _config.GhostHpMax,
                HostHp = _host != null ? _host.Hp : 0,
                HostHpMax = _host != null ? _host.HpMax : 0,
                HasHost = _host != null,
            });
        }
    }
}
