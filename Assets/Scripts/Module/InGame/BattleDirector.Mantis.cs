using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 외눈 사마귀 — 6챕터의 새 잡몹(무기 · 견제). 기획 `AVSR_Content_EnemyGimmick.md` 3-2 「저격」 · 4절(라인업 2차 5번).
    ///
    /// **평타가 없다.** 하는 일은 저격 하나 — 멀리 서서 외눈으로 조준선을 1초 긋고(빨간 띠 · 공용 꺾쇠) 빠른 한 발, 그리고 2초 재장전.
    /// 저격 자체는 고철 전갈(5챕터부터)과 같은 `TickSnipe` 를 쓴다. 다른 점은 처음부터 저격수라는 것과 쏘지 않을 때 걷는 법이다.
    ///
    ///   멀면 다가온다 · 알맞은 거리(사거리 안)면 옆걸음으로 사선을 바꾸며 숨을 고른다 — 물지도 쏘지도 않는다.
    ///   근접 몫(PD 「근거리도 감안해서」) : 재장전 2초 동안 그 자리에 선다. 조준선 밖으로 비켜 돌아 들어가 친다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string TrashMantisKey = "mantis";
        private static HostEntry s_mantis;

        private static HostEntry Mantis => s_mantis ??= HostEntry.CreateTrash(
            TrashMantisKey, "외눈 사마귀", AttackKind.Single,
            hp: 26, atk: 9, moveMps: 1.4f, engageMps: 2.0f,
            rangeMeters: 7.5f, interval: 2.4f, telegraph: 1.0f);

        private const float MantisStrafeMps = 1.2f;

        /// <summary>사마귀 한 프레임. 늘 true — 평소 흐름(쫓아와 쏜다)으로 내려가지 않는다.</summary>
        private bool TickMantis(Unit e, Unit me, float distance, float dt)
        {
            if (TickSnipe(e, me, distance, dt)) return true;
            // 사거리 안이면 평소처럼 쏜다 — 예전엔 평타가 없어 조준선만 긋고 쉬는 놈이었다(PD 2026-10-08 「일반적으로 쏘다가
            // 특정 구간에 워닝 띄우고 쏘는 것처럼」). 모아 쏘기는 위 `TickSnipe` 가 몇 초마다 끼어든다
            if (distance <= Meters(SnipeTriggerMeters * 0.9f)) return false;

            // 쏘지 않는 동안 — 멀면 다가오고, 사거리 안이면 옆걸음으로 사선을 바꾼다
            if (distance > Meters(SnipeTriggerMeters * 0.9f))
            {
                e.SetState(EnemyState.Approach);
                e.Position = SlideMove(e, e.Position, e.StepToward(me.Position, dt));
                e.SetMoving(true);
                return true;
            }
            var away = e.Position - me.Position;
            away = away.sqrMagnitude < 0.0001f ? -e.Facing : away.normalized;
            float side = (e.GetInstanceID() & 1) == 0 ? 1f : -1f;
            var dir = new Vector2(-away.y, away.x) * side;
            e.SetState(EnemyState.Approach);
            e.SetFacing(-away);
            e.Position = SlideMove(e, e.Position, dir * (Meters(MantisStrafeMps) * dt));
            e.SetMoving(true);
            return true;
        }
    }
}
