using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 잡몹 패턴 3차 — 그림 없이 되는 것 (2026-10-07 PD 「근접은 그냥 가서 때리는 수준」 ·
    /// 기획 `AVSR_Content_EnemyGimmick.md` 3절). 모두 **예고 → 실행 → 빈틈** 세 박자다.
    ///
    ///   저격   폐품 사수(5단계+)  조준선을 1초 긋고 그 선을 한 번에 친다 → 2초 재장전
    ///   박격   코일 보행기(7단계+) 내 자리에 원을 깔고 1초 뒤 떨어진다 — 엄폐 너머로도 온다
    ///   링     해골(7단계+)       쫓아오면서 3.5초마다 몸 둘레로 탄을 고리처럼 뿌린다 — 한 칸이 빈다
    ///
    /// 새 잡몹 그림이 오면 저격 · 박격은 그 몸으로 옮긴다(저격수 · 땅굴 포수). 지금은 있는 몸으로 맛을 본다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const int SnipeFromStage = 5;
        private const int MortarFromStage = 7;
        private const int RingFromStage = 7;

        // ── 저격 ──────────────────────────────────────────────────
        private const float SnipeTriggerMeters = 7.5f;
        private const float SnipeTellSeconds = 1.0f;
        private const float SnipeWidthMeters = 0.45f;
        private const float SnipeReloadSeconds = 2.0f;
        private const float SnipeDamageMul = 1.6f;

        // ── 박격 ──────────────────────────────────────────────────
        private const float MortarRadiusMeters = 1.15f;
        private const float MortarTellSeconds = 1.0f;
        private const float MortarIntervalSeconds = 3.0f;

        // ── 링 ────────────────────────────────────────────────────
        private const float RingIntervalSeconds = 3.5f;
        private const float RingTellSeconds = 0.6f;
        private const int RingShots = 12;

        /// <summary>
        /// 이 몸에 3차 패턴이 붙는가 — 붙으면 그 패턴이 한 프레임을 맡았는지(true)를 돌려준다.
        /// `false` 면 아래 평소 흐름(쫓기 · 사거리 공격)으로 내려간다.
        /// </summary>
        private bool TickPatterns3(Unit e, Unit me, float distance, float dt)
        {
            int stage = PatternChapter;
            if (e.Key == TrashMoleKey) return TickMole(e, me, distance, dt);   // 드릴 두더지 — 숨었다 솟아 미사일 (BattleDirector.Mole)
            if (e.Key == TrashMantisKey) return TickMantis(e, me, distance, dt); // 외눈 사마귀 — 저격만 (BattleDirector.Mantis)
            if (e.Key == TrashGunnerKey && stage >= SnipeFromStage) return TickSnipe(e, me, distance, dt);
            if (e.Key == TrashCoilKey && stage >= MortarFromStage) return TickMortar(e, me, dt);
            if (e.Key == TrashSkeletonKey && stage >= RingFromStage) TickRing(e, dt);   // 쫓으면서 뿌린다 — 흐름을 안 막는다
            return false;
        }

        /// <summary>
        ///   0  쉰다 / 거리를 잰다 (사거리 밖이면 평소 흐름으로 다가온다)
        ///   1  조준선 1초 — 멈춰 선다. 선은 처음 그은 자리에 고정된다(따라오면 피할 수 없다)
        ///   2  재장전 2초 — 그 자리에 선다(빈틈)
        /// </summary>
        private bool TickSnipe(Unit e, Unit me, float distance, float dt)
        {
            switch (e.PatternPhase)
            {
                case 0:
                    e.PatternTimer -= dt;
                    if (e.PatternTimer > 0f || distance > Meters(SnipeTriggerMeters)) return false;
                    if (!EnemyLineClear(e.Position, me.Position)) return false;
                    {
                        var dir = (me.Position - e.Position).normalized;
                        e.SetFacing(dir);
                        e.SetTelegraph(true);
                        // 선이 곧 탄이다 — 조준선 안에 남아 있으면 맞는다
                        StartWarn(BandShape(e.Position, dir, Meters(SnipeWidthMeters), _roomSize.magnitude),
                                  SnipeTellSeconds, Mathf.Max(1, Mathf.RoundToInt(e.Atk * SnipeDamageMul)), owner: e);
                        // 공용 꺾쇠 — 탄이 날아올 쪽 (BattleDirector.Rush)
                        StartRushFx(e, e.Position, e.Position + dir * _roomSize.magnitude, Meters(SnipeWidthMeters),
                                    SnipeTellSeconds, dust: false);
                        e.PatternPhase = 1;
                        e.PatternTimer = SnipeTellSeconds;
                    }
                    return true;

                case 1:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    e.PatternTimer -= dt;
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    e.PlayAttack();
                    e.PatternPhase = 2;
                    e.PatternTimer = SnipeReloadSeconds;
                    return true;

                default:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                    e.PatternTimer -= dt;
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 0;
                    e.PatternTimer = 0.5f;
                    return true;
            }
        }

        /// <summary>
        /// 박격 — 자리를 지키고 3초마다 내 자리에 원을 깐다. 원은 깐 자리에 남는다(나를 따라오지 않는다).
        /// 원이 떠 있는 동안에만 멈춰 선다 — 그 1초가 다가가 칠 틈이다.
        /// </summary>
        private bool TickMortar(Unit e, Unit me, float dt)
        {
            e.PatternTimer -= dt;
            if (e.PatternPhase == 1)
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Attack);
                if (e.PatternTimer > 0f) return true;
                e.SetTelegraph(false);
                e.PatternPhase = 0;
                e.PatternTimer = MortarIntervalSeconds;
                return true;
            }
            if (e.PatternTimer > 0f) return false;   // 쉬는 동안은 평소처럼 움직이고 쏜다
            float r = Meters(MortarRadiusMeters);
            e.PlayAttack();
            e.SetTelegraph(true);
            StartWarn(DiscShape(me.Position, r), MortarTellSeconds, Mathf.Max(1, e.Atk), owner: e,
                      impactKind: "grenade", fxSize: r * 2f);
            e.PatternPhase = 1;
            e.PatternTimer = MortarTellSeconds;
            return true;
        }

        /// <summary>
        /// 링 — 쫓아오면서 3.5초마다. 0.6초 몸이 빛나고(예고) 12방향 중 11발을 뿌린다.
        /// 빈 한 칸은 **나를 향한 쪽**이다 — 서 있던 자리 그대로 피할 수 있어야 「읽는」 패턴이 된다.
        /// 0.6초 동안은 멈춰 선다.
        /// </summary>
        private void TickRing(Unit e, float dt)
        {
            // 링 박자는 따로 센다 — PatternTimer 는 매복이 쓴다. 처음 깨어난 순간에는 바로 쏘지 않는다
            if (e.PatternAngle == 0f) { e.PatternAngle = RingIntervalSeconds; return; }
            e.PatternAngle -= dt;
            if (e.PatternAngle > RingTellSeconds) return;
            if (e.PatternAngle > 0f)
            {
                e.SetTelegraph(true);
                e.SetMoving(false);
                return;
            }
            e.SetTelegraph(false);
            e.PatternAngle = RingIntervalSeconds;
            var me = Avatar;
            float gapDeg = me != null ? Mathf.Atan2(me.Position.y - e.Position.y, me.Position.x - e.Position.x) * Mathf.Rad2Deg : 0f;
            float step = 360f / RingShots;
            for (int i = 1; i < RingShots; i++)
            {
                float a = (gapDeg + i * step) * Mathf.Deg2Rad;
                FireAimed(e, e.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 100f, 0.7f, DeathShotKind);
            }
        }
    }
}
