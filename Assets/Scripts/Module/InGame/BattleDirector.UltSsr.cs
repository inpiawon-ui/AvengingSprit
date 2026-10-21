using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// SSR 필살기 시안 다듬기(PD 2026-10-15 「SSR 부터 시안 퀄로 · 연출이 별로면 새로 다 발주」).
    /// 시안: `Projects/AVSR/_exchange/in/mock_skill_{amazon_elite_v2, dragoon_v2, medium_golem, vampire}.png`
    /// 부품: `_exchange/in/part_{el,dg,md,vp}_*.png` → `fx_{elaura, elburst, elcrack, dgdragon, dgbreath, dgblast, dgrise,
    ///        mdrune, mdslam, mddust, vpclaw, vporb, vpburst}` (자르기 `Tools/cut_skill_rework_parts.py`)
    /// 부품이 없으면 예전 그림으로 떨어진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // 크기는 몸(약 90) 기준 배수 — 시안 실측(시안 몸 높이 대비)으로 맞춘다
        private const float SsrBody = 90f;

        // ═══════════════════════════════════════════════════════════
        //  아마존 정예 — 역린 (흡수 틱은 BattleDirector.Ultimates `TickWrath`) · 붉은 기운 · 균열 · 방사 섬광
        // ═══════════════════════════════════════════════════════════

        private void SsrWrathOpen(Unit me)
        {
            var aura = UltLoop("elaura", me.Position, SsrBody * 2f) ?? UltLoop("rpaura", me.Position, 140f);
            aura?.SetFrameSeconds(0.06f);
        }

        private void SsrWrathBurst(Unit me, float mul)
        {
            var crack = PlayFloorFx("elcrack", me.Position, SsrBody * 4.6f, loop: true);
            crack?.SetLife(1.8f, 0.6f);
            var burst = PlayFx("elburst", me.Position, SsrBody * 5.6f, loop: false);
            burst?.SetFrameSeconds(0.05f);
            UltAoe(me, me.Position, 3f, mul, burst == null ? "slam" : null, 2.2f, knockMeters: 2f);
            _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, me.Position, 18, 420f, 34f, 0.8f, 360f);
            _pfx?.Spray(ParticleFxKind.Streak, me.Position, Vector2.up, 180f, 24, 300f, 520f, new Vector2(46f, 5f), 0.35f,
                        new Color(1f, 0.3f, 0.35f, 1f));
        }
    }
}
