using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 영매 「원혼 강림」 — 던파 「흑염의 칼라」(따라다니는 혼)를 옮긴 스킬 (PD 2026-10-14
    /// 「캐릭터 뒤에 영혼이 생기고 평타를 치면 불꽃이 하나씩 모이다가 3마리가 모이면 터지면서 추가 데미지」).
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_medium_v2.png`.
    ///
    /// 박자: 시작(등 뒤에 푸른 원혼이 내려앉는다) → 한창(6초 동안 평타가 맞을 때마다 원혼이 낫을 휘둘러 그 적에게 혼불 1 · 2 · 3,
    ///       3이 되면 그 자리에서 펑) → 마무리(원혼이 떠나며 남은 혼불도 터진다).
    /// 그림: 받은 부품 3종(`fx_mdwraith` 원혼 · `fx_mdpop` 혼불 폭발 · `fx_mdslash` 낫 궤적) + 기존 혼불(`fx_soulflame`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float WfSeconds = 6f;
        private const int WfMaxMarked = 6;            // 혼불을 다는 적 수 — 넘으면 새 적은 안 단다(화면이 혼불로 덮인다)
        private const int WfStacksToPop = 3;
        private const float WfPopMul = 2.5f;
        private const float WfPopMeters = 1.3f;
        private const float WfWraithSize = 145f;     // PD 2026-10-14 「악마가 너무 커 · 절반으로」 — 290 → 145
        private const float WfFlameSize = 60f;       // 34 · 46 은 녹화에서 평타 구슬과 구별이 안 됐다
        private const float WfStackSize = 64f;       // 화면 배율(약 0.45) 때문에 36 은 12px 로 찍혀 안 보였다(녹화 rw_md_4)
        private const float WfPopSize = 250f;
        private const float WfSlashThickness = 110f;
        private static readonly Vector2[] WfFlameSlots = { new(-30f, 74f), new(0f, 86f), new(30f, 74f) };

        private sealed class WfMark
        {
            public Unit Target;
            public int Stacks;
            public bool PopPending;   // 탄이 맞는 도중(적 목록을 도는 중)에 터뜨리지 않는다 — 다음 틱에 터진다
            public readonly Impact[] Flames = new Impact[WfStacksToPop];
        }

        private readonly List<WfMark> _wfMarks = new();
        private bool _wfActive;
        private float _wfLeft;
        private Impact _wfWraith;

        private void MediumWraithFlame(Unit me)
        {
            StopWraithFlame(popRemaining: false);
            _wfActive = true;
            _wfLeft = WfSeconds;
            // 몸 **뒤**(장판 레이어)에 띄운다 — 탄 레이어에 올렸더니 원혼이 영매를 통째로 덮었다(녹화 rw_md_1)
            _wfWraith = PlayFloorFx("mdwraith", WfWraithAt(me), WfWraithSize, loop: true);
            _wfWraith?.SetFrameSeconds(0.09f);
            PlayFx("mdpop", WfWraithAt(me), WfWraithSize, loop: false)?.SetFrameSeconds(0.05f);   // 내려앉는 순간
        }

        /// <summary>등 뒤 — 바라보는 반대쪽으로 조금, 머리 위로.</summary>
        private static Vector2 WfWraithAt(Unit me) => me.Position - me.Facing * 12f + new Vector2(0f, 58f);

        private void StopWraithFlame(bool popRemaining)
        {
            var me = _host;
            for (int i = 0; i < _wfMarks.Count; i++)
            {
                var m = _wfMarks[i];
                if (popRemaining && me != null && m.Target != null && m.Target.IsAlive && m.Stacks > 0)
                    WfPop(me, m, (float)m.Stacks / WfStacksToPop);
                WfClearFlames(m);
            }
            _wfMarks.Clear();
            if (popRemaining && _wfWraith != null && me != null)
                PlayFx("mdpop", WfWraithAt(me), WfWraithSize, loop: false)?.SetFrameSeconds(0.06f);   // 원혼이 떠난다
            _wfWraith?.Stop();
            _wfWraith = null;
            _wfActive = false;
        }

        private void TickWraithFlame(float dt)
        {
            if (!_wfActive) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "medium") { StopWraithFlame(popRemaining: false); return; }
            _wfWraith?.MoveTo(WfWraithAt(me));
            for (int i = _wfMarks.Count - 1; i >= 0; i--)
            {
                var m = _wfMarks[i];
                if (m.Target == null || !m.Target.IsAlive) { WfClearFlames(m); _wfMarks.RemoveAt(i); continue; }
                if (m.PopPending) { WfPop(me, m, 1f); WfClearFlames(m); _wfMarks.RemoveAt(i); continue; }
                for (int k = 0; k < m.Stacks && k < m.Flames.Length; k++)
                    m.Flames[k]?.MoveTo(m.Target.Position + WfFlameSlots[k]);
            }
            _wfLeft -= dt;
            if (_wfLeft <= 0f) StopWraithFlame(popRemaining: true);
        }

        /// <summary>평타(탄)가 적에게 맞았다. 원혼이 낫을 휘둘러 혼불을 하나 단다.</summary>
        private void WfOnShotHit(Unit victim)
        {
            if (!_wfActive || victim == null || !victim.IsAlive) return;
            var me = _host;
            if (me == null || me.Key != "medium") return;
            WfMark mark = null;
            for (int i = 0; i < _wfMarks.Count; i++) if (_wfMarks[i].Target == victim) { mark = _wfMarks[i]; break; }
            if (mark == null)
            {
                if (_wfMarks.Count >= WfMaxMarked) return;
                mark = new WfMark { Target = victim };
                _wfMarks.Add(mark);
            }
            WfSlash(me, victim);
            int k = mark.Stacks;
            mark.Stacks++;
            if (k < mark.Flames.Length)
            {
                // 통통한 혼불 아이콘(`fx_mdstack`) — 기존 혼불(`soulflame`)은 가는 불줄기라 머리 위에서 개수가 안 읽혔다
                mark.Flames[k] = TakeLoopFx("mdstack", victim.Position + WfFlameSlots[k], WfStackSize)
                              ?? TakeLoopFx("soulflame", victim.Position + WfFlameSlots[k], WfFlameSize);
                mark.Flames[k]?.SetFrameSeconds(0.08f);
            }
            if (mark.Stacks >= WfStacksToPop) mark.PopPending = true;
        }

        private void WfSlash(Unit me, Unit victim)
        {
            var frames = FxFrames("mdslash");
            if (frames == null) return;
            var from = WfWraithAt(me);
            var d = victim.Position - from;
            var dir = d.sqrMagnitude > 1f ? d.normalized : Vector2.right;
            var im = FreeImpact(WfSlashThickness);
            if (im == null) return;
            im.PlayBeam(victim.Position - dir * 70f, victim.Position + dir * 50f, frames, WfSlashThickness);
            im.SetFrameSeconds(0.05f);
        }

        private void WfPop(Unit me, WfMark mark, float scale)
        {
            var at = mark.Target.Position;
            float r = Meters(WfPopMeters) * _buffs.AoeMul;
            int dmg = SkillDamage(me, WfPopMul * scale * BaseAxis(1f));
            var list = EnemiesInRange(at, r);
            for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, me.Profile);
            var pop = PlayFx("mdpop", at, WfPopSize * Mathf.Lerp(0.6f, 1f, scale), loop: false);
            if (pop != null) pop.SetFrameSeconds(0.05f);
            else PlayFx("soulripple", at, WfPopSize, loop: false);
            Shake(scale >= 1f ? 5f : 2f);
            if (scale >= 1f) HitStop(0.05f);
        }

        private static void WfClearFlames(WfMark m)
        {
            for (int k = 0; k < m.Flames.Length; k++)
            {
                m.Flames[k]?.Stop();
                m.Flames[k] = null;
            }
        }
    }
}
