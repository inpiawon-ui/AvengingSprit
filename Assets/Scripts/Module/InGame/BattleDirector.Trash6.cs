using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 2단계 몬스터 조합(PD 2026-10-22)의 새 모양 여섯 — 라인업 3차 시안(`_exchange/in/mock_lineup_v3.png`).
    /// 모두 **예고 → 실행 → 빈틈** 세 박자이고, 빈틈에 붙어 칠 수 있다(패턴은 근접 틈을 남긴다).
    ///
    ///   감시 눈(무기 · 비행)     떠다니며 거리를 둔다 · 조준선 0.8초(굳음) → 레이저 세 줄기 → 1.5초 선다
    ///   그림자 살쾡이(마법)      멀면 연기로 사라져(0.4초) 내 옆에 나타날 자리를 보이고(0.5초) 나타나 할퀸다 → 1초 선다
    ///   톱날 원숭이(파워)        거리를 둔다 · 톱날을 감아(0.6초) 던진다 — 끝까지 가서 **되돌아온다**(갈 때 · 올 때 둘 다 아프다)
    ///                            톱날이 날아가 있는 동안은 빈손이라 안 던진다
    ///   미믹 상자(파워)          상자인 척 가만히 — 2.5 m 안에 오거나 맞으면 깨어나(0.5초) 쫓아와 문다 · 4초마다 8발 고리
    ///   수호 등불(마법)          뒤로 숨는다 · 5초마다 곁(5 m)의 적 하나에 방패 4초(받는 피해 90% 줄임) — 등불부터 끊어야 한다
    ///   혼 사냥개(무기)          몸이 있을 땐 느리게 쫓고, 내가 유령이 되면 빨라져 쫓아와 **붙잡는다**(1.2초 느려짐 — 피해는 없다,
    ///                            유령은 적 공격을 받지 않는다는 규칙은 지킨다). 다음 몸으로 서둘러 가야 한다
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string TrashWatcherKey = "watcher_eye";
        private const string TrashShadowCatKey = "shadow_cat";
        private const string TrashSawMonkeyKey = "saw_monkey";
        private const string TrashMimicKey = "mimic";
        private const string TrashLanternKey = "lantern";
        private const string TrashHoundKey = "soul_hound";

        // ── 감시 눈 ──
        private const float WatcherKeepMeters = 5.0f;
        private const float WatcherAimSeconds = 0.8f;
        private const float WatcherRestSeconds = 1.5f;
        private const float WatcherEverySeconds = 3.0f;
        private const float WatcherBeamWidthMeters = 0.5f;
        private const int WatcherBeamPulses = 3;
        private const float WatcherPulseGap = 0.08f;
        private const float WatcherBeamDamageMul = 1.4f;

        // ── 그림자 살쾡이 ──
        private const float CatBlinkMinMeters = 4.0f;
        private const float CatVanishSeconds = 0.4f;
        private const float CatMarkSeconds = 0.5f;
        private const float CatSlashSeconds = 0.25f;
        private const float CatStandSeconds = 1.0f;
        private const float CatCooldownSeconds = 4.0f;
        private const float CatSideMeters = 1.1f;
        private const float CatSlashMeters = 1.3f;

        // ── 톱날 원숭이 ──
        private const float MonkeyKeepMeters = 5.0f;
        private const float MonkeyWindSeconds = 0.6f;
        private const float MonkeyThrowMeters = 7.5f;
        private const float MonkeySawSpeedMul = 0.8f;
        private const string MonkeySawKind = "saw";

        // ── 미믹 상자 ──
        private const float MimicWakeMeters = 2.5f;
        private const float MimicWakeSeconds = 0.5f;
        private const float MimicRingEvery = 4.0f;
        private const int MimicRingShots = 8;

        // ── 수호 등불 ──
        private const float LanternKeepMeters = 6.0f;
        private const float LanternEverySeconds = 5.0f;
        private const float LanternReachMeters = 5.0f;
        private const float LanternShieldSeconds = 4.0f;
        private const float LanternCastSeconds = 0.6f;

        // ── 혼 사냥개 ──
        private const float HoundGhostMoveMul = 2.2f;    // 유령이 되면 내달린다(몸이 있을 땐 표의 느린 걸음)
        private const float HoundHoldSeconds = 1.2f;
        private const float HoundHoldSlow = 0.5f;
        private const float HoundHoldMeters = 0.7f;

        private readonly Dictionary<Unit, float> _lanternShield = new();
        private readonly List<Unit> _lanternShieldDone = new();
        private readonly HashSet<Unit> _mimicAwake = new();
        private readonly Dictionary<Unit, float> _sideClock = new();
        private float _ghostHeldLeft;

        private struct SawFlight { public Unit Owner; public Vector2 Turn; public float Left; }
        private readonly List<SawFlight> _saws = new();

        /// <summary>혼 사냥개에게 붙잡힌 유령의 이동 배율.</summary>
        private float GhostHeldMoveMul => _host == null && _ghostHeldLeft > 0f ? 1f - HoundHoldSlow : 1f;

        /// <summary>새 모양 여섯의 한 프레임. 처리했으면 true(`TickPatterns3` 가 부른다).</summary>
        private bool TickNewShapes(Unit e, Unit me, float distance, float dt)
        {
            switch (e.Key)
            {
                case TrashWatcherKey: return TickWatcher(e, me, distance, dt);
                case TrashShadowCatKey: return TickShadowCat(e, me, distance, dt);
                case TrashSawMonkeyKey: return TickSawMonkey(e, me, distance, dt);
                case TrashMimicKey: return TickMimic(e, me, distance, dt);
                case TrashLanternKey: return TickLantern(e, me, distance, dt);
                case TrashHoundKey: return TickHound(e, me, distance, dt);
            }
            return false;
        }

        // ═══ 감시 눈 ═══════════════════════════════════════════════

        private bool TickWatcher(Unit e, Unit me, float distance, float dt)
        {
            e.PatternTimer -= dt;
            switch (e.PatternPhase)
            {
                case 0:
                    if (e.PatternTimer <= 0f && distance <= EffectiveRange(e) && EnemyLineClear(e.Position, me.Position))
                    {
                        var dir = (me.Position - e.Position).normalized;
                        e.SetFacing(dir);
                        e.SetTelegraph(true);
                        e.PatternTo = e.Position + dir * _roomSize.magnitude;   // 조준은 긋는 순간 굳는다
                        StartWarn(BandShape(e.Position, dir, Meters(WatcherBeamWidthMeters), _roomSize.magnitude),
                                  WatcherAimSeconds, 0, owner: e);
                        e.PatternPhase = 1;
                        e.PatternTimer = WatcherAimSeconds;
                        return true;
                    }
                    KeepDistance(e, me, distance, WatcherKeepMeters, dt);
                    return true;

                case 1:   // 조준
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    e.PlayAttack();
                    e.PatternPhase = 2;
                    e.PatternAngle = 0f;   // 쏜 줄기 수
                    e.PatternTimer = 0f;
                    return true;

                case 2:   // 레이저 세 줄기 — 굳은 선을 따라
                    e.SetMoving(false);
                    if (e.PatternTimer > 0f) return true;
                    FireAimed(e, e.PatternTo, 3f, "laser",
                              damage: Mathf.Max(1, Mathf.RoundToInt(e.Atk * WatcherBeamDamageMul / WatcherBeamPulses)),
                              sizeMul: 1.8f);
                    e.PatternAngle += 1f;
                    e.PatternTimer = WatcherPulseGap;
                    if (e.PatternAngle < WatcherBeamPulses) return true;
                    e.PatternPhase = 3;
                    e.PatternTimer = WatcherRestSeconds;
                    return true;

                default:  // 쉰다 — 칠 틈
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 0;
                    e.PatternTimer = WatcherEverySeconds - WatcherAimSeconds - WatcherRestSeconds;
                    return true;
            }
        }

        // ═══ 그림자 살쾡이 ═════════════════════════════════════════

        private bool TickShadowCat(Unit e, Unit me, float distance, float dt)
        {
            e.PatternTimer -= dt;
            switch (e.PatternPhase)
            {
                case 0:
                    if (e.PatternTimer > 0f || distance < Meters(CatBlinkMinMeters)) return false;   // 가까우면 평소처럼 쫓아와 벤다
                    e.SetHidden(true);
                    e.SetMoving(false);
                    _pfx?.Puff(e.Position, ParticleElement.Curse, 0.9f);
                    SetPose(e, "vanish");
                    e.PatternPhase = 1;
                    e.PatternTimer = CatVanishSeconds;
                    return true;

                case 1:   // 사라짐 → 나타날 자리를 보인다
                {
                    if (e.PatternTimer > 0f) return true;
                    var side = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.Range(-0.4f, 0.4f)).normalized;
                    e.PatternTo = ClampedInField(e, me.Position + side * Meters(CatSideMeters));
                    StartWarn(DiscShape(e.PatternTo, Meters(0.6f)), CatMarkSeconds, 0, owner: e);
                    e.PatternPhase = 2;
                    e.PatternTimer = CatMarkSeconds;
                    return true;
                }

                case 2:   // 나타나 할퀸다 — 앞 띠 안에 있으면 아프다
                {
                    if (e.PatternTimer > 0f) return true;
                    e.Position = e.PatternTo;
                    e.SetHidden(false);
                    SetPose(e, null);
                    FaceAt(e, me);
                    _pfx?.Puff(e.Position, ParticleElement.Curse, 0.7f);
                    e.SetTelegraph(true);
                    StartWarn(BandShape(e.Position, e.Facing, Meters(1.0f), Meters(CatSlashMeters)),
                              CatSlashSeconds, Mathf.Max(1, e.Atk), owner: e);
                    e.PatternPhase = 3;
                    e.PatternTimer = CatSlashSeconds;
                    return true;
                }

                case 3:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    e.PlayAttack();
                    e.PatternPhase = 4;
                    e.PatternTimer = CatStandSeconds;
                    return true;

                default:  // 선다 — 칠 틈
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 0;
                    e.PatternTimer = CatCooldownSeconds;
                    return true;
            }
        }

        // ═══ 톱날 원숭이 ═══════════════════════════════════════════

        private bool TickSawMonkey(Unit e, Unit me, float distance, float dt)
        {
            bool sawOut = SawOutOf(e);
            SetPose(e, sawOut ? "empty" : null);
            e.PatternTimer -= dt;
            if (e.PatternPhase == 1)
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Attack);
                if (e.PatternTimer > 0f) return true;
                e.SetTelegraph(false);
                ThrowSaw(e, me);
                e.PatternPhase = 0;
                e.PatternTimer = e.AttackInterval;
                return true;
            }
            if (!sawOut && e.PatternTimer <= 0f && distance <= EffectiveRange(e) && EnemyLineClear(e.Position, me.Position))
            {
                FaceAt(e, me);
                e.SetTelegraph(true);
                e.PlayAttack();
                e.PatternPhase = 1;
                e.PatternTimer = MonkeyWindSeconds;
                return true;
            }
            KeepDistance(e, me, distance, MonkeyKeepMeters, dt);
            return true;
        }

        private bool SawOutOf(Unit e)
        {
            for (int i = 0; i < _saws.Count; i++) if (_saws[i].Owner == e) return true;
            return false;
        }

        /// <summary>톱날 — 겨눈 쪽으로 7.5 m 를 날아가 거기서 되돌아온다. 돌아오는 길은 그때 원숭이 자리로.</summary>
        private void ThrowSaw(Unit e, Unit me)
        {
            var dir = (me.Position - e.Position).normalized;
            var turn = ClampedInField(e, e.Position + dir * Meters(MonkeyThrowMeters));
            float speed = _config.ShotSpeedEnemy * MonkeySawSpeedMul;
            float out1 = Vector2.Distance(e.Position, turn) / Mathf.Max(1f, speed);
            FireAimed(e, turn, MonkeySawSpeedMul, SawKind());
            _saws.Add(new SawFlight { Owner = e, Turn = turn, Left = out1 });
        }

        /// <summary>날아간 톱날이 끝에 닿으면 원숭이 쪽으로 되돌린다. `TickElements` 옆에서 매 프레임.</summary>
        private void TickSaws(float dt)
        {
            for (int i = _saws.Count - 1; i >= 0; i--)
            {
                var s = _saws[i];
                s.Left -= dt;
                if (s.Left > 0f) { _saws[i] = s; continue; }
                _saws.RemoveAt(i);
                if (s.Owner == null || !s.Owner.IsAlive || s.Owner.IsDying) continue;
                // 되돌아오는 톱날 — 끝 자리에서 원숭이를 향해(지나쳐 방 끝까지 가지 않게 거리만큼 산다)
                var shot = RentShot();
                if (shot == null) continue;
                float speed = _config.ShotSpeedEnemy * MonkeySawSpeedMul;
                float back = Vector2.Distance(s.Turn, s.Owner.Position);
                string kind = SawKind();
                shot.SetSprite(ShotFrames(kind), kind, LoopsFrames(kind));
                shot.SourceKey = s.Owner.Key; shot.Element = ElementOf(s.Owner);
                shot.Fire(s.Turn, s.Owner.Position, speed, Mathf.Max(1, s.Owner.Atk), false, null,
                          _config.ShotSize, ShotEnemyColor, back / Mathf.Max(1f, speed) + 0.05f);
            }
        }

        /// <summary>톱날 탄 그림 — 받기 전엔 회오리 탄으로 난다(미검).</summary>
        private string SawKind() => GetSprite($"shot_{MonkeySawKind}_1") != null ? MonkeySawKind : "spiral";

        // ═══ 미믹 상자 ═════════════════════════════════════════════

        private bool TickMimic(Unit e, Unit me, float distance, float dt)
        {
            if (!_mimicAwake.Contains(e))
            {
                e.SetMoving(false);
                e.IsAggro = false;
                if (e.PatternPhase == 0)
                {
                    SetPose(e, "sleep");   // 방 상자와 같은 모습
                    // 깨는 때 — 가까이 왔거나 맞았다(체력이 줄었다)
                    if (distance > Meters(MimicWakeMeters) && e.Hp >= e.HpMax) return true;
                    e.PatternPhase = 1;
                    e.PatternTimer = MimicWakeSeconds;
                    e.SetTelegraph(true);
                    _pfx?.Puff(e.Position, ParticleElement.Dust, 0.6f);
                    return true;
                }
                e.PatternTimer -= dt;
                if (e.PatternTimer > 0f) return true;
                e.SetTelegraph(false);
                SetPose(e, null);
                e.PlayAttack();
                e.IsAggro = true;
                _mimicAwake.Add(e);
                e.PatternPhase = 0;
                e.PatternTimer = MimicRingEvery;
                return true;
            }
            // 깨어났다 — 쫓아와 문다(평소 흐름). 몇 초마다 이빨 고리
            e.PatternTimer -= dt;
            if (e.PatternTimer > 0f) return false;
            e.PatternTimer = MimicRingEvery;
            e.PlayAttack();
            FireRadial(e.Position, MimicRingShots, 22.5f, DeathShotKind, Mathf.Max(1, e.Atk / 2), 0.7f);
            return false;
        }

        // ═══ 수호 등불 ═════════════════════════════════════════════

        private bool TickLantern(Unit e, Unit me, float distance, float dt)
        {
            e.PatternTimer -= dt;
            if (e.PatternPhase == 1)
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Attack);
                if (e.PatternTimer > 0f) return true;
                e.SetTelegraph(false);
                var ally = NearestAlly(e, Meters(LanternReachMeters));
                if (ally != null)
                {
                    _lanternShield[ally] = LanternShieldSeconds;
                    _pfx?.Dart(e.Position, ally.Position, ParticleElement.Ice, 0.25f, 4);
                    _pfx?.Ring(ally.Position, ParticleElement.Ice, Meters(1.2f));
                }
                e.PatternPhase = 0;
                e.PatternTimer = LanternEverySeconds;
                return true;
            }
            if (e.PatternTimer <= 0f && NearestAlly(e, Meters(LanternReachMeters)) != null)
            {
                e.SetTelegraph(true);
                e.PlayAttack();
                e.PatternPhase = 1;
                e.PatternTimer = LanternCastSeconds;
                return true;
            }
            KeepDistance(e, me, distance, LanternKeepMeters, dt);
            return true;   // 직접 치지 않는다 — 곁의 몸을 지키는 것이 하는 일이다
        }

        private Unit NearestAlly(Unit self, float reach)
        {
            Unit best = null;
            float bd = reach * reach;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var u = _enemies[i];
                if (u == null || u == self || !u.IsAlive || u.IsDying || u.IsBoss || u.Key == TrashLanternKey) continue;
                if (_lanternShield.ContainsKey(u)) continue;
                float d = (u.Position - self.Position).sqrMagnitude;
                if (d <= bd) { bd = d; best = u; }
            }
            return best;
        }

        /// <summary>등불 방패를 흘린다 — 시간이 다 되거나 몸이 죽으면 벗긴다.</summary>
        private void TickLanternShields(float dt)
        {
            if (_lanternShield.Count == 0) return;
            _lanternShieldDone.Clear();
            foreach (var k in _lanternShield.Keys) _lanternShieldDone.Add(k);
            for (int i = 0; i < _lanternShieldDone.Count; i++)
            {
                var u = _lanternShieldDone[i];
                float left = _lanternShield[u] - dt;
                if (u == null || !u.IsAlive || u.IsDying || left <= 0f) _lanternShield.Remove(u);
                else _lanternShield[u] = left;
            }
            _lanternShieldDone.Clear();
        }

        // ═══ 혼 사냥개 ═════════════════════════════════════════════

        private bool TickHound(Unit e, Unit me, float distance, float dt)
        {
            if (_host != null) return false;   // 몸이 있을 땐 평소처럼 쫓아와 문다 — 표의 걸음이 느리다
            // 유령을 쫓는다 — 닿으면 붙잡는다(피해 없음)
            var to = me.Position - e.Position;
            var dir = to.sqrMagnitude < 0.0001f ? e.Facing : to.normalized;
            e.SetFacing(dir);
            e.SetState(EnemyState.Approach);
            e.SetMoving(true);
            e.IsAggro = true;
            e.Position = SlideMove(e, e.Position, dir * (e.MoveSpeed * HoundGhostMoveMul * dt));
            _sideClock.TryGetValue(e, out float c);
            c -= dt;
            if (c <= 0f && distance <= Meters(HoundHoldMeters))
            {
                c = HoundHoldSeconds;
                _ghostHeldLeft = HoundHoldSeconds;
                e.PlayAttack();
                _pfx?.Puff(me.Position, ParticleElement.Ice, 0.6f);
            }
            _sideClock[e] = c;
            return true;
        }

        // ═══ 공용 ══════════════════════════════════════════════════

        /// <summary>알맞은 거리를 지키며 천천히 — 너무 가까우면 물러난다(미라와 같은 걸음).</summary>
        private void KeepDistance(Unit e, Unit me, float distance, float keepMeters, float dt)
        {
            var to = me.Position - e.Position;
            var dir = to.sqrMagnitude < 0.0001f ? -e.Facing : to.normalized;
            e.SetFacing(dir);
            e.SetState(EnemyState.Approach);
            float want = distance - Meters(keepMeters);
            if (Mathf.Abs(want) < Meters(0.5f)) { e.SetMoving(false); return; }
            StepWithDetour(e, me, dir * (Mathf.Sign(want) * e.MoveSpeed * dt), dt);
            e.SetMoving(true);
        }

        /// <summary>몸 그림 대신 아홉째 칸 그림(사라짐 · 빈손 · 잠든 상자). 그림이 없으면 원래 그림 그대로.</summary>
        private void SetPose(Unit e, string act)
        {
            if (act == null) { e.SetSpriteOverride(null); return; }
            var dir = e.Facing.x < -0.3f ? "w" : "e";
            var s = UnitGet(e.Profile != null ? e.Profile.SpriteKey : e.Key, $"{dir}_{act}")
                 ?? UnitGet(e.Profile != null ? e.Profile.SpriteKey : e.Key, $"e_{act}");
            e.SetSpriteOverride(s);
        }

        /// <summary>매 프레임 — 톱날 되돌리기 · 등불 방패 · 붙잡힘. `TickGimmicks` 가 부른다.</summary>
        private void TickNewShapeFlights(float dt)
        {
            TickSaws(dt);
            TickLanternShields(dt);
            if (_ghostHeldLeft > 0f) _ghostHeldLeft -= dt;
        }

        /// <summary>방을 나갈 때.</summary>
        private void ClearNewShapes()
        {
            _lanternShield.Clear();
            _mimicAwake.Clear();
            _sideClock.Clear();
            _saws.Clear();
            _ghostHeldLeft = 0f;
        }
    }
}
