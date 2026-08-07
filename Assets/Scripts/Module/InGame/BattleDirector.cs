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

        private float _stopTimer;

        private BuffTable _buffTable;
        private readonly RunBuffs _buffs = new();
        private readonly List<BuffEntry> _offer = new();
        private readonly System.Random _rng = new();
        private bool _awaitingBuff;

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

            try { _buffTable = await res.LoadAsync<BuffTable>("TableData/BuffTable"); }
            catch (Exception e) { Debug.LogError($"[Battle] BuffTable 로드 실패 — {e.Message}"); }
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

            SpawnGhost();
            EnterRoom(0);
            _running = true;
        }

        // ─────────────────────────────────────────────────────────
        private void SpawnGhost()
        {
            _ghostHp = GhostHpMax;
            _ghost = NewUnit("Ghost");
            _ghost.Setup(UnitSide.Player, "ghost", "GHOST", GetSprite("unit_ghost"),
                         GhostHpMax, 0, _config.GhostMoveSpeed, 0f, 1f,
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

        /// <summary>런타임에 붙이는 UI 스프라이트(버프 카드 등). 이름으로 아틀라스에서 꺼낸다.</summary>
        public Sprite AtlasSprite(string spriteName) => GetSprite(spriteName);

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
                    // 적도 호스트다 — 같은 공격 방식을 쓴다. 방마다 교전 양상이 달라진다.
                    u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, GetSprite($"unit_{e.HostKey}"),
                            _config.EnemyHp(e.Hp),
                            Mathf.RoundToInt(_config.EnemyAtk(e.Atk) * e.DamageMul),
                            _config.EnemySpeed(e.Spd),
                            _config.EnemyAttackRange * e.RangeMul,
                            _config.EnemyAttackInterval * e.IntervalMul,
                            new Vector2(84f, 78f), isBoss: false, profile: e);
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
            if (_awaitingBuff) return;   // 3택1 선택 대기 — 적이 없는 상태라 멈춰도 안전하다
            float dt = Time.deltaTime;

            _ultimateCharge = Mathf.Min(_ultimateCharge + dt * _buffs.UltimateChargeMul,
                                        _config.UltimateChargeSeconds);

            TickPlayer(dt);
            SyncFireRing();
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

            // ⚠️ 궁수의 전설 규칙 — **움직이는 동안에는 쏘지 않는다.**
            //    이동과 공격이 배타적이어야 "자리를 잡을까 딜을 넣을까"의 긴장이 생긴다.
            //    이걸 없애면 조작이 그냥 산책이 된다.
            bool moving = MoveInput.sqrMagnitude > 0.0001f;
            if (moving)
            {
                var p = me.Position + MoveInput * (me.MoveSpeed * _buffs.MoveMul) * dt;
                var half = me.GetComponent<RectTransform>().sizeDelta * 0.5f;
                p.x = Mathf.Clamp(p.x, half.x, _field.rect.width - half.x);
                p.y = Mathf.Clamp(p.y, -_field.rect.height + half.y, -half.y);
                me.Position = p;
                _stopTimer = 0f;
                IsFiring = false;
                return;
            }

            // 멈춘 직후 아주 짧게 준비 시간을 둔다. 없으면 톡톡 끊어 눌러도 손해가 없어
            // 멈춤의 대가가 사라진다.
            _stopTimer += dt;
            if (_stopTimer < _config.AttackResumeSeconds) { IsFiring = false; return; }

            // 고스트는 공격하지 않는다 — 빙의해야 싸울 수 있다(핵심 동사)
            if (_host == null) { IsFiring = false; return; }

            var target = Nearest(_host.Position);
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
                e.TickSlow(dt);

                float d = Vector2.Distance(e.Position, me.Position);
                if (d > e.AttackRange) { e.MoveToward(me.Position, dt); continue; }
                if (!e.TickAttack(dt)) continue;
                PerformAttack(e, me, false);
            }
        }

        // ── 공격 방식 ────────────────────────────────────────────
        // 호스트마다 교전 거리·탄 수·발사 간격이 달라야 "어떤 몸을 뺏었는가"에 의미가 생긴다.
        // 사거리·간격·피해 배율은 Setup 시점에 이미 반영돼 있다(Unit.Atk/AttackRange/AttackInterval).

        private void PerformAttack(Unit attacker, Unit target, bool fromPlayer)
        {
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

        private void FireShot(Unit attacker, Unit target, bool fromPlayer, float angleOffsetDeg)
        {
            var shot = RentShot();
            if (shot == null) return;
            var p = attacker.Profile;
            bool snipe = p != null && p.Kind == AttackKind.Snipe;

            float speed = (fromPlayer ? _config.ShotSpeedPlayer : _config.ShotSpeedEnemy)
                          * (snipe ? 1.6f : 1f)
                          * (fromPlayer ? _buffs.ShotSpeedMul : 1f);

            shot.Fire(attacker.Position, target.Position, speed,
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
                        entry != null ? Mathf.RoundToInt(_config.HostAtk(entry.Atk) * entry.DamageMul) : 10,
                        entry != null ? _config.HostSpeed(entry.Spd) : 180f,
                        _config.HostAttackRange * (entry?.RangeMul ?? 1f),
                        _config.HostAttackInterval * (entry?.IntervalMul ?? 1f),
                        new Vector2(96f, 92f), isBoss: false, profile: entry);
            _host.Position = pos;

            _bus.Publish(new PossessedEvent
            {
                PossessedHostKey = target.Key,
                // 어떤 몸을 뺏었는지가 곧 빌드다 — 교전 스타일을 함께 보여준다
                DisplayName = entry != null ? $"{entry.NameEn}  ·  {entry.AttackText}" : target.DisplayName,
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

            // 로그라이크 축 — 룸마다 3택1 로 런 한정 빌드를 쌓는다.
            // 고를 때까지 전투를 멈춘다(적이 없는 상태라 안전하다).
            _buffTable?.Draw(_offer, 3, _buffs.ExcludedKeys, _rng);
            if (_offer.Count == 0) { EnterRoom(_roomIndex + 1); return; }

            _awaitingBuff = true;
            var keys = new string[_offer.Count];
            for (int i = 0; i < _offer.Count; i++) keys[i] = _offer[i].BuffKey;
            _bus.Publish(new BuffOfferEvent { OfferedKeys = keys });
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
            EnterRoom(_roomIndex + 1);
        }

        private int GhostHpMax => _config.GhostHpMax + _buffs.GhostHpBonus;

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
                GhostHpMax = GhostHpMax,
                HostHp = _host != null ? _host.Hp : 0,
                HostHpMax = _host != null ? _host.HpMax : 0,
                HasHost = _host != null,
            });
        }
    }
}
