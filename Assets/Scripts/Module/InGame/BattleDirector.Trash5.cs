using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 2단계 몬스터 조합(PD 2026-10-22)의 새 Lv2 행동 둘 — 그림 없이 이동만 바뀐다. 모두 **예고 → 실행 → 빈틈**.
    ///
    ///   구르는 아르마딜로(`armadillo_2`)  몸을 말아(0.5초 · 띠 예고) 점점 빨라지며 굴러온다 — 구르는 몸에 닿으면 아프다.
    ///                                    벽 · 물건에 박거나 끝까지 구르면 **1.2초 휘청** — 이때는 정면도 안 막는다(배가 드러남)
    ///   튕기는 박쥐(`bat_bounce`)        나를 겨눠 날다 벽 · 물건에 튕길 때마다 다시 겨눈다 — 세 번 튕기고 1.2초 내려앉는다.
    ///                                    날아다닐 때 닿으면 아프다(돌진처럼 예고는 없다 — 겨눈 방향이 몸 방향으로 보인다)
    ///
    /// ⚠ 구르는 모습 그림은 아직 없다 — 말린 몸 그림이 오기 전엔 서 있는 그림으로 구른다(미검).
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 구르기 ──
        private const float RollTriggerMeters = 6.5f;
        private const float RollMinMeters = 2.2f;
        private const float RollCurlSeconds = 0.5f;
        private const float RollMaxSeconds = 2.2f;
        private const float RollStartMps = 3.0f;
        private const float RollTopMps = 7.5f;
        private const float RollAccelMps2 = 6f;
        private const float RollWidthMeters = 1.0f;
        private const float RollStunSeconds = 1.2f;
        private const float RollCooldownSeconds = 2.2f;

        // ── 튕기는 박쥐 ──
        private const float BounceMps = 4.2f;
        private const int BounceTimes = 3;
        private const float BounceRestSeconds = 1.2f;
        private const float BounceHitMeters = 0.95f;   // 0.6 은 몸을 스쳐도 안 맞았다(2026-10-22 녹화) — 돌진 박쥐 띠 폭(0.9)과 맞춘다
        private const float BounceHitEverySeconds = 0.6f;

        private readonly Dictionary<Unit, float> _rollSpeed = new();
        private readonly Dictionary<Unit, float> _contactClock = new();

        /// <summary>구르는 아르마딜로 한 프레임. 구를 차례가 아니면 false — 평소처럼 다가와 박치기.</summary>
        private bool TickArmadilloRoll(Unit e, Unit me, float distance, float dt)
        {
            e.PatternTimer -= dt;
            switch (e.PatternPhase)
            {
                case 0:
                {
                    bool ready = e.PatternTimer <= 0f && distance <= Meters(RollTriggerMeters)
                              && distance >= Meters(RollMinMeters) && EnemyLineClear(e.Position, me.Position);
                    if (!ready) return false;
                    var dir = (me.Position - e.Position).normalized;
                    e.SetFacing(dir);
                    e.PatternFrom = dir;   // 구를 방향 — 몸을 마는 순간 굳는다
                    float len = Meters(RollTopMps * RollMaxSeconds * 0.6f);
                    StartWarn(BandShape(e.Position, dir, Meters(RollWidthMeters), len), RollCurlSeconds, 0, owner: e);
                    StartRushFx(e, e.Position, e.Position + dir * len, Meters(RollWidthMeters), RollCurlSeconds, dust: true);
                    e.SetTelegraph(true);
                    e.PatternPhase = 1;
                    e.PatternTimer = RollCurlSeconds;
                    return true;
                }

                case 1:   // 몸을 만다
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    e.PlayAttack();
                    e.PatternPhase = 2;
                    e.PatternTimer = RollMaxSeconds;
                    _rollSpeed[e] = RollStartMps;
                    _contactClock[e] = 0f;
                    return true;

                case 2:   // 구른다 — 점점 빨라진다
                {
                    e.SetMoving(true);
                    e.SetState(EnemyState.Approach);
                    _rollSpeed.TryGetValue(e, out float v);
                    v = Mathf.Min(RollTopMps, v + RollAccelMps2 * dt);
                    _rollSpeed[e] = v;
                    var want = e.Position + e.PatternFrom * (Meters(v) * dt);
                    var got = ClampedInField(e, SlideMove(e, e.Position, want - e.Position));
                    bool crashed = (got - want).sqrMagnitude > 4f;   // 벽 · 물건에 걸려 덜 갔다
                    e.Position = got;
                    TickBodyContact(e, me, RollWidthMeters * 0.5f + BoarHitReachMeters, dt);
                    if (!crashed && e.PatternTimer > 0f) return true;
                    if (crashed) { Shake(ShakeOnKill * 0.5f); _pfx?.Puff(e.Position, ParticleElement.Dust, 0.8f); }
                    e.ApplyStun(RollStunSeconds);   // 휘청 — 배가 드러나 정면도 안 막는다(`GuardedDamage`)
                    e.PlayHit();
                    e.PatternPhase = 0;
                    e.PatternTimer = RollStunSeconds + RollCooldownSeconds;
                    return true;
                }
            }
            e.PatternPhase = 0;
            return false;
        }

        /// <summary>튕기는 박쥐 한 프레임. 늘 true — 쫓아와 무는 평소 흐름으로 내려가지 않는다.</summary>
        private bool TickBatBounce(Unit e, Unit me, float dt)
        {
            e.PatternTimer -= dt;
            if (e.PatternPhase == 1)   // 내려앉아 쉰다 — 칠 틈
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Cooldown);
                if (e.PatternTimer > 0f) return true;
                e.PatternPhase = 0;
                e.PatternAngle = 0f;
                e.PatternFrom = Vector2.zero;
                return true;
            }

            // 처음 — 나를 겨눠 날아간다(대각선 45° 로만 날면 그 선 위에 서 있어야만 맞았다 — 2026-10-22 녹화)
            if (e.PatternFrom == Vector2.zero)
            {
                var to = me.Position - e.Position;
                e.PatternFrom = to.sqrMagnitude < 0.0001f ? Vector2.one.normalized : to.normalized;
                e.PatternAngle = 0f;   // 튕긴 수
                _contactClock[e] = 0f;
            }

            e.SetMoving(true);
            e.SetState(EnemyState.Approach);
            var dir = e.PatternFrom;
            var want = e.Position + dir * (Meters(BounceMps) * dt);
            var got = ClampedInField(e, SlideMove(e, e.Position, want - e.Position));
            bool hitX = Mathf.Abs(got.x - want.x) > 1f, hitY = Mathf.Abs(got.y - want.y) > 1f;
            if (hitX || hitY)
            {
                // 튕기면 다시 나를 겨눈다 — 다만 부딪힌 벽 쪽으로는 못 가게 그 축은 벽에서 나오는 쪽으로
                var to = me.Position - e.Position;
                var aim = to.sqrMagnitude < 0.0001f ? -dir : to.normalized;
                if (hitX && Mathf.Sign(aim.x) == Mathf.Sign(dir.x)) aim.x = -aim.x;
                if (hitY && Mathf.Sign(aim.y) == Mathf.Sign(dir.y)) aim.y = -aim.y;
                dir = aim.normalized;
                e.PatternFrom = dir;
                e.PatternAngle += 1f;
                _pfx?.Puff(got, ParticleElement.Dust, 0.35f);
            }
            e.Position = got;
            e.SetFacing(dir);
            TickBodyContact(e, me, BounceHitMeters, dt);

            if (e.PatternAngle >= BounceTimes)
            {
                e.PatternPhase = 1;
                e.PatternTimer = BounceRestSeconds;
            }
            return true;
        }

        /// <summary>움직이는 몸에 닿으면 친다 — 같은 몸은 0.6초에 한 번만(구르기 · 튕기기).</summary>
        private void TickBodyContact(Unit e, Unit me, float reachMeters, float dt)
        {
            _contactClock.TryGetValue(e, out float c);
            c -= dt;
            if (c <= 0f && _host != null && me != null
                && Vector2.Distance(e.Position, me.Position) <= Meters(reachMeters))
            {
                c = BounceHitEverySeconds;
                DamagePlayer(Mathf.Max(1, e.Atk), e.Key);
                ApplyElementToPlayer(ElementOf(e));
            }
            _contactClock[e] = c;
        }

        /// <summary>방을 나갈 때 — `ClearTrash4` 가 부른다.</summary>
        private void ClearTrash5()
        {
            _rollSpeed.Clear();
            _contactClock.Clear();
            _snipeSecond.Clear();
            ClearNewShapes();
        }
    }
}
