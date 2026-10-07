using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 쇠뿔 멧돼지 — 2챕터의 새 잡몹(2026-10-07 PD 「돌진해서 지나가며 피해를 입히는 애들 … 노티로 일직선 돌진을 알려 주면서」).
    /// 기획 `AVSR_Content_EnemyGimmick.md` 4절 「돌진 멧돼지형」.
    ///
    /// **평타가 없다.** 하는 일은 돌진 하나뿐이다 — 멈춰 겨누고(앞발 긁기) → 바닥에 띠 → 일직선으로 들이받으며 지나간다.
    /// 박쥐의 급강하(`TickDive`)와 같은 세 박자를 쓰되 더 멀리 · 더 굵게 · 더 길게 예고한다.
    ///
    ///   예고 → 실행 → 빈틈 : 끝까지 달리면 0.9초 선다. **벽 · 물건에 박으면 1.6초 휘청인다** — 엄폐 앞에 서서
    ///   돌진을 끌어들이면 오래 때릴 수 있다(방의 물건이 「이용」 역할을 얻는다).
    ///   돌진 사이에는 다가오기만 하고 물지 않는다 — 가까이 붙으면 그 자리에서 다시 겨눈다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string TrashBoarKey = "boar";
        private static HostEntry s_boar;

        private static HostEntry Boar => s_boar ??= HostEntry.CreateTrash(
            TrashBoarKey, "쇠뿔 멧돼지", AttackKind.Melee,
            hp: 30, atk: 8, moveMps: 1.6f, engageMps: 2.4f,
            rangeMeters: 1.2f, interval: 1.6f, telegraph: 0.7f);

        /// <summary>돌진 한 번의 크기 — 박쥐(급강하) · 돌 고릴라(돌진) · 멧돼지가 같은 세 박자를 다른 크기로 쓴다.</summary>
        private readonly struct DiveSpec
        {
            public readonly float TriggerMeters, MinMeters, TellSeconds, DashSeconds, Meters, WidthMeters,
                                  RecoverSeconds, WallStunSeconds;

            public DiveSpec(float trigger, float min, float tell, float dash, float meters, float width,
                            float recover, float wallStun)
            {
                TriggerMeters = trigger; MinMeters = min; TellSeconds = tell; DashSeconds = dash;
                Meters = meters; WidthMeters = width; RecoverSeconds = recover; WallStunSeconds = wallStun;
            }
        }

        // 멧돼지 — 6 m 앞에서 겨눠 7 m 를 0.36초에 달린다(박쥐 5 m / 0.22초보다 길고 굵다). 예고 0.75초
        private static readonly DiveSpec BoarDive = new(6.0f, 1.2f, 0.75f, 0.36f, 7.0f, 1.1f, 0.9f, 1.6f);

        private DiveSpec DiveSpecOf(Unit e)
            => e.Key == TrashBoarKey
                ? BoarDive
                : new DiveSpec(DiveTriggerMeters, DiveMinMeters, DiveTellSeconds, DiveDashSeconds,
                               DiveMeters, DiveWidthMeters, DiveRecoverSeconds, DiveRecoverSeconds);

        /// <summary>
        /// 겨누기 · 돌진 중에는 **피격 경직(0.1초)에 안 끊긴다** — 띠가 깔린 대로 정확히 달린다.
        ///
        /// 예전에는 맞을 때마다 패턴이 멈췄다. 띠는 제 시간에 사라지는데 몸은 그 뒤에야 달려 나가
        /// **띠 없는 돌진**이 됐다(2026-10-07 멧돼지 시험 — 로봇 탄에 맞으며 겨누다 2.2초 늦게 달렸다).
        /// 진짜 기절(스킬)은 막지 않는다 — 그때는 `InterruptCharge` 가 돌진을 거둔다.
        /// 박쥐 급강하 · 고릴라 돌진도 같은 길을 탄다.
        /// </summary>
        private bool ChargeArmored(Unit e)
            => !e.IsStunned && (e.PatternPhase == 1 || e.PatternPhase == 2) && PatternOf(e) == EnemyPattern.Dive;

        /// <summary>
        /// 겨누는 중에 기절하면 **돌진을 거둔다** — 띠도 같이 지운다. 풀리면 처음(다가오기)부터 다시 겨눈다.
        /// </summary>
        private void InterruptCharge(Unit e)
        {
            if (e.PatternPhase != 1 || PatternOf(e) != EnemyPattern.Dive) return;
            CancelWarnsOf(e);
            e.SetTelegraph(false);
            e.PatternPhase = 0;
            e.PatternTimer = 0.4f;
        }

        /// <summary>
        /// 돌진 사이 — 멧돼지는 물지 않는다. 겨눌 거리 밖이면 다가오고, 안이면 그 자리에서 숨을 고른다(앞발 긁기 전).
        /// 처리했으면 true. 박쥐 · 고릴라는 false — 평소 흐름(쫓아와 문다)으로 내려간다.
        /// </summary>
        private bool HoldBetweenCharges(Unit e, Unit me, float distance, float dt)
        {
            if (e.Key != TrashBoarKey) return false;
            if (distance > Meters(BoarDive.TriggerMeters * 0.85f))
            {
                e.SetState(EnemyState.Approach);
                e.Position = SlideMove(e, e.Position, e.StepToward(me.Position, dt));
                e.SetMoving(true);
                return true;
            }
            var to = me.Position - e.Position;
            if (to.sqrMagnitude > 0.0001f) e.SetFacing(to.normalized);
            e.SetMoving(false);
            e.SetState(EnemyState.Cooldown);
            return true;
        }
    }
}
