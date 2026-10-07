using System.Collections.Generic;
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
    ///   예고 → 실행 → 빈틈 : 끝까지 달리면 0.6초 선다. **벽 · 물건에 박으면 1.6초 휘청인다** — 엄폐 앞에 서서
    ///   돌진을 끌어들이면 오래 때릴 수 있다(방의 물건이 「이용」 역할을 얻는다).
    ///
    ///   **돌진 → 돌진 → 돌진**(PD 2026-10-07 「한 번 돌진해서 갔으면 또 다른 곳으로 돌진해야 한다 — 붙어서 때리고만 있으면 의미가 없다」).
    ///   돌진 사이에 서 있거나 물지 않는다. 너무 가까우면 내 둘레를 비스듬히 돌아 거리를 벌리고(달릴 길을 만든다),
    ///   알맞은 거리면 옆걸음으로 각을 바꾸다가, 숨이 돌아오는 대로 **다른 방향에서** 다시 겨눈다.
    ///
    ///   **달릴 때 부딪혀야만 아프다**(PD 「가만히 있을 때는 안 아파야 근접 캐릭이 때릴 수 있다」).
    ///   띠는 보여 주기만 하고, 피해는 돌진 중 몸이 내게 닿는 순간 한 번 들어간다(띠가 끝날 때 한꺼번에 치지 않는다).
    ///   근접 몫 : 돌진 뒤 0.5초 그 자리에 선다(박으면 1.3초) · 돌아 나가는 걸음은 몸보다 느리다 — 쫓아가 칠 수 있다.
    ///   (PD 「패턴 딜레이가 너무 길다 — 0.5초쯤 줄여」 → 멈춤 1초 → 0.5초)
    ///
    ///   **화면 밖으로 나가지 않는다**(PD) — 돌진 끝점과 도는 걸음을 지금 보이는 화면 안으로 자른다.
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
                                  RecoverSeconds, WallStunSeconds, CooldownSeconds;

            public DiveSpec(float trigger, float min, float tell, float dash, float meters, float width,
                            float recover, float wallStun, float cooldown)
            {
                TriggerMeters = trigger; MinMeters = min; TellSeconds = tell; DashSeconds = dash;
                Meters = meters; WidthMeters = width; RecoverSeconds = recover; WallStunSeconds = wallStun;
                CooldownSeconds = cooldown;
            }
        }

        // 멧돼지 — 2.6 ~ 6 m 에서 겨눠 7 m 를 0.36초에 달린다(박쥐 5 m / 0.22초보다 길고 굵다). 예고 0.75초.
        //   멈춤 0.5초(근접이 치는 틈, 박으면 1.3초) · 다음 돌진까지 0.35초. 2.6 m 보다 가까우면 먼저 거리를 벌린다
        private static readonly DiveSpec BoarDive = new(6.0f, 2.6f, 0.75f, 0.36f, 7.0f, 1.1f, 0.5f, 1.3f, 0.35f);
        private const float BoarViewMarginPx = 48f;   // 화면 가장자리에서 이만큼 안쪽까지만 간다(몸 반쯤)
        private const float BoarRunMps = 2.0f;        // 거리를 벌리며 도는 빠르기 — 몸보다 느려 쫓아가 칠 수 있다
        private const float BoarStrafeMps = 1.4f;     // 알맞은 거리에서 각을 바꾸는 옆걸음
        private const float BoarHitReachMeters = 0.45f;   // 띠 반폭에 더하는 몸 반경 — 이만큼 닿으면 부딪힌 것
        private const float BoarOrbitDegrees = 55f;   // 내게서 멀어지는 방향을 이만큼 옆으로 꺾어 돈다 — 다음 돌진은 다른 각에서

        // ── 연속 돌진 (패턴 단계 7 부터 — 기획 6절 「7챕터 돌진형 연속 돌진」) ──
        //   한 번 달린 뒤 쉬지 않고 짧은 예고로 **한 번 더** 들이받는다. 두 번째가 끝나면 평소처럼 쉰다.
        //   벽 · 물건에 박으면 이어 달리지 않는다 — 박은 휘청이 근접 몸이 치는 틈이다(패턴은 근접 틈을 남긴다).
        //   이어 달리기는 붙어 있어도 건다(최소 거리 1.2 m) — 돌진을 피한 자리로 곧장 꺾어 오는 게 이 패턴이다.
        private const int BoarChainFromStage = 7;
        private const float BoarChainRecoverSeconds = 0.2f;   // 첫 돌진 뒤 숨 고르기 — 0.5초 멈춤 대신
        private const float BoarChainTellRatio = 0.65f;       // 두 번째 예고는 짧게(0.75 → 0.49초)
        private const float BoarChainMinMeters = 1.2f;
        private const float BoarChainWindowSeconds = 0.6f;    // 이만큼 안에 각이 안 나오면 이어 달리기를 버린다

        /// <summary>두 번째 돌진을 앞둔 · 달리는 멧돼지. 방을 나갈 때 비운다(`ClearRushFx`).</summary>
        private readonly HashSet<Unit> _boarChain = new();

        private bool BoarChaining(Unit e) => e.Key == TrashBoarKey && _boarChain.Contains(e);

        /// <summary>
        /// 돌진 하나가 끝났다(`TickDive` 단계 2 끝). 이어 달릴지 정하고 멈춤 시간을 돌려준다.
        /// </summary>
        private float AfterBoarDash(Unit e, bool crashed, in DiveSpec spec)
        {
            if (e.Key != TrashBoarKey || PatternChapter < BoarChainFromStage)
                return crashed ? spec.WallStunSeconds : spec.RecoverSeconds;
            if (crashed || _boarChain.Remove(e))   // 박았거나 두 번째였다 → 평소처럼 쉰다
                return crashed ? spec.WallStunSeconds : spec.RecoverSeconds;
            _boarChain.Add(e);
            return BoarChainRecoverSeconds;
        }

        /// <summary>달리는 동안 부딪혀야 아픈가(띠가 끝날 때 한꺼번에 치지 않는다). 지금은 멧돼지만.</summary>
        private static bool ChargeContact(Unit e) => e.Key == TrashBoarKey;

        /// <summary>
        /// 돌진 중(단계 2) 매 프레임 — 몸이 내게 닿으면 한 번 친다. 한 번 친 돌진은 다시 안 친다(`PatternAngle` 을 표시로 쓴다 —
        /// 지그재그 흔들기 각도인데 멧돼지는 흔들지 않는다).
        /// </summary>
        private void TickChargeContact(Unit e, Unit me, in DiveSpec spec)
        {
            if (e.PatternAngle > 0.5f || _host == null || me == null) return;
            if (Vector2.Distance(e.Position, me.Position) > Meters(spec.WidthMeters * 0.5f + BoarHitReachMeters)) return;
            e.PatternAngle = 1f;
            DamagePlayer(Mathf.Max(1, e.Atk));
        }

        private DiveSpec DiveSpecOf(Unit e)
            => e.Key == TrashBoarKey
                ? BoarDive
                : new DiveSpec(DiveTriggerMeters, DiveMinMeters, DiveTellSeconds, DiveDashSeconds,
                               DiveMeters, DiveWidthMeters, DiveRecoverSeconds, DiveRecoverSeconds, DiveCooldown);

        /// <summary>
        /// 겨누기 · 돌진 중에는 **피격 경직(0.1초)에 안 끊긴다** — 띠가 깔린 대로 정확히 달린다.
        ///
        /// 예전에는 맞을 때마다 패턴이 멈췄다. 띠는 제 시간에 사라지는데 몸은 그 뒤에야 달려 나가
        /// **띠 없는 돌진**이 됐다(2026-10-07 멧돼지 시험 — 로봇 탄에 맞으며 겨누다 2.2초 늦게 달렸다).
        /// 진짜 기절(스킬)은 막지 않는다 — 그때는 `InterruptCharge` 가 돌진을 거둔다.
        /// 박쥐 급강하 · 고릴라 돌진도 같은 길을 탄다.
        /// </summary>
        ///
        /// **멧돼지는 피격 경직을 아예 안 받는다**(2026-10-07 시험 — 로봇 탄에 맞을 때마다 굳어 멈춤 1초가 2.8초, 다음 돌진까지 7초가 됐다.
        /// 「계속 돌진」이 총 맞는 동안 사라졌다). 근접이 치는 틈은 정해진 멈춤(1초 · 박으면 1.8초)으로 준다.
        private bool ChargeArmored(Unit e)
            => !e.IsStunned && (e.Key == TrashBoarKey || e.Key == TrashMoleKey   // 두더지도 박자가 정해져 있다 — 맞는다고 오래 솟아 있지 않는다
                                || ((e.PatternPhase == 1 || e.PatternPhase == 2) && PatternOf(e) == EnemyPattern.Dive));

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
        /// 돌진 사이 — 멧돼지는 서 있지도 물지도 않는다. 처리했으면 true.
        ///   멀다        → 다가온다
        ///   너무 가깝다 → 내 둘레를 비스듬히 돌며 멀어진다(달릴 길을 만든다)
        ///   알맞다      → 옆걸음으로 각을 바꾼다 — 숨이 돌아오면 `TickDive` 가 곧장 겨눈다
        /// 도는 쪽은 몸마다 정해져 있다 — 둘이면 양쪽에서 갈라 들어온다.
        /// 박쥐 · 고릴라는 false — 평소 흐름(쫓아와 문다)으로 내려간다.
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

            var away = e.Position - me.Position;
            away = away.sqrMagnitude < 0.0001f ? -e.Facing : away.normalized;
            float side = (e.GetInstanceID() & 1) == 0 ? 1f : -1f;
            Vector2 dir;
            float mps;
            if (distance < Meters(BoarDive.MinMeters + 0.6f))
            {
                dir = Rotate(away, side * BoarOrbitDegrees);   // 멀어지며 옆으로 돈다
                mps = BoarRunMps;
            }
            else
            {
                dir = new Vector2(-away.y, away.x) * side;    // 거리는 두고 옆으로만
                mps = BoarStrafeMps;
            }
            e.SetState(EnemyState.Approach);
            e.SetFacing(dir);
            bool wasOnScreen = IsOnScreen(e);
            e.Position = SlideMove(e, e.Position, dir * (Meters(mps) * dt));
            if (wasOnScreen) e.Position = ClampToView(e.Position, BoarViewMarginPx);   // 보이던 몸은 화면 밖으로 안 나간다
            e.SetMoving(true);
            return true;
        }

        /// <summary>방 좌표를 지금 보이는 화면 안으로 자른다(가장자리에서 <paramref name="marginPx"/> 안쪽).</summary>
        private Vector2 ClampToView(Vector2 room, float marginPx)
        {
            if (_field == null || _zoom <= 0.0001f) return room;
            var v = RoomToView(room);
            float w = _field.rect.width, h = _field.rect.height;
            var c = new Vector2(Mathf.Clamp(v.x, marginPx, w - marginPx), Mathf.Clamp(v.y, -h + marginPx, -marginPx));
            if (c == v) return room;
            return room + (c - v) / _zoom;   // RoomToView 는 배율 _zoom 의 닮은꼴이다
        }

    }
}
