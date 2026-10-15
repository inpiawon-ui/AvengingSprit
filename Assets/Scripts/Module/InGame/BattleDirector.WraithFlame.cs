using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 영매 「원혼 강림」 — 던파 「흑염의 칼라」(따라다니는 혼)를 옮긴 스킬 (PD 2026-10-14
    /// 「캐릭터 뒤에 영혼이 생기고 평타를 치면 불꽃이 하나씩 모이다가 3마리가 모이면 터지면서 추가 데미지」).
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_medium_v2.png`.
    ///
    /// PD 2026-10-14(3차) 「영매가 공격할 때 소환된 유령도 같이 공격하는 느낌이 들어가야 유령이 저걸 붙이는구나가 든다 ·
    /// 기승전결 — 아 이래서 이렇게 되는구나를 표현」. 예전엔 영매 탄이 맞는 순간 혼불이 그냥 생겨서 누가 붙였는지 안 읽혔다.
    ///
    /// 기 — 등 뒤에 푸른 원혼이 내려앉는다.
    /// 승 — 영매가 칠 때마다 원혼도 **같이** 친다: 표적 쪽으로 몸을 내밀며 낫을 휘두르고, 혼불 하나를 던진다.
    ///      혼불이 날아가(빛 꼬리) 적에게 「탁」 붙는다 — 머리 위 혼불 1 · 2.
    /// 전 — 세 번째 혼불이 붙으면 머리 위 혼불 셋이 적 몸으로 빨려 든다(잠깐 멈칫).
    /// 결 — 「펑」 푸른 혼불 폭발 + 둘레 피해. 6초가 끝나면 원혼이 떠나며 남은 혼불도 터진다.
    /// 그림: `fx_mdwraith` 원혼 · `fx_mdslash` 낫 궤적 · `fx_mdstack` 혼불 · `fx_mdpop` 혼불 폭발.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float WfSeconds = 6f;
        private const int WfMaxMarked = 6;            // 혼불을 다는 적 수 — 넘으면 새 적은 안 단다(화면이 혼불로 덮인다)
        private const int WfStacksToPop = 3;
        private const float WfPopMul = 2.5f;
        private const float WfPopMeters = 1.3f;
        private const float WfWraithSize = 145f;     // PD 2026-10-14 「악마가 너무 커 · 절반으로」 — 290 → 145
        private const float WfStackSize = 64f;       // 화면 배율(약 0.45) 때문에 36 은 12px 로 찍혀 안 보였다(녹화 rw_md_4)
        private const float WfOrbSize = 56f;         // 날아가는 혼불
        private const float WfOrbSeconds = 0.3f;     // 원혼 → 적 — 눈으로 따라갈 수 있는 빠르기
        private const float WfOrbArc = 50f;          // 위로 휘어 날아간다 — 곧게 날면 평타 탄과 구별이 안 된다
        private const float WfLungeSeconds = 0.22f;  // 원혼이 표적 쪽으로 몸을 내밀었다 돌아오는 시간
        private const float WfLungePixels = 34f;
        private const float WfGatherSeconds = 0.2f;  // 혼불 셋이 몸으로 빨려 드는 시간(「전」 — 멈칫)
        private const float WfPopSize = 250f;
        private const float WfSlashThickness = 120f;
        private static readonly Vector2[] WfFlameSlots = { new(-30f, 74f), new(0f, 86f), new(30f, 74f) };
        private static readonly Vector2 WfBodyCenter = new(0f, 20f);

        private sealed class WfMark
        {
            public Unit Target;
            public int Stacks;
            public int Incoming;      // 날아가는 중인 혼불 — 셋이 넘게 날아가지 않게
            public float Gather = -1f; // 0 이상이면 「전」 — 혼불이 몸으로 빨려 드는 중
            public readonly Impact[] Flames = new Impact[WfStacksToPop];
        }

        private struct WfOrb
        {
            public Impact Fx;
            public WfMark Mark;
            public Vector2 From;
            public float T;
        }

        private readonly List<WfMark> _wfMarks = new();
        private readonly List<WfOrb> _wfOrbs = new();
        private bool _wfActive;
        private float _wfLeft;
        private Impact _wfWraith;
        private float _wfLunge;
        private Vector2 _wfLungeDir;

        private void MediumWraithFlame(Unit me)
        {
            StopWraithFlame(popRemaining: false);
            _wfActive = true;
            _wfLeft = WfSeconds;
            _wfLunge = 0f;
            // 몸 **뒤**(장판 레이어)에 띄운다 — 탄 레이어에 올렸더니 원혼이 영매를 통째로 덮었다(녹화 rw_md_1)
            _wfWraith = PlayFloorFx("mdwraith", WfWraithAt(me), WfWraithSize, loop: true);
            _wfWraith?.SetFrameSeconds(0.09f);
            _wfWraith?.SetSnap(1.6f, 0.25f);   // 크게 내려앉아 제자리로 조여든다
            PlayFx("mdpop", WfWraithAt(me), WfWraithSize, loop: false)?.SetFrameSeconds(0.05f);   // 내려앉는 순간
        }

        /// <summary>등 뒤 — 바라보는 반대쪽으로 조금, 머리 위로.</summary>
        private static Vector2 WfWraithAt(Unit me) => me.Position - me.Facing * 12f + new Vector2(0f, 58f);

        /// <summary>원혼이 지금 있는 자리(표적 쪽으로 내밀었으면 그만큼).</summary>
        private Vector2 WfWraithNow(Unit me)
        {
            float k = _wfLunge > 0f ? Mathf.Sin(Mathf.Clamp01(1f - _wfLunge / WfLungeSeconds) * Mathf.PI) : 0f;
            return WfWraithAt(me) + _wfLungeDir * (WfLungePixels * k);
        }

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
            for (int i = 0; i < _wfOrbs.Count; i++) _wfOrbs[i].Fx?.Stop();
            _wfOrbs.Clear();
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
            if (_wfLunge > 0f) _wfLunge -= dt;
            _wfWraith?.MoveTo(WfWraithNow(me));

            TickWfOrbs(me, dt);

            for (int i = _wfMarks.Count - 1; i >= 0; i--)
            {
                var m = _wfMarks[i];
                if (m.Target == null || !m.Target.IsAlive) { WfClearFlames(m); _wfMarks.RemoveAt(i); continue; }
                var body = m.Target.Position + WfBodyCenter;
                if (m.Gather >= 0f)
                {
                    // 「전」 — 머리 위 혼불 셋이 몸으로 빨려 든다
                    m.Gather += dt;
                    float g = Game.Module.Common.UI.Ease.OutCubic(Mathf.Clamp01(m.Gather / WfGatherSeconds));
                    for (int k = 0; k < m.Flames.Length; k++)
                        m.Flames[k]?.MoveTo(Vector2.Lerp(m.Target.Position + WfFlameSlots[k], body, g));
                    if (m.Gather < WfGatherSeconds) continue;
                    WfPop(me, m, 1f);   // 「결」
                    WfClearFlames(m);
                    _wfMarks.RemoveAt(i);
                    continue;
                }
                for (int k = 0; k < m.Stacks && k < m.Flames.Length; k++)
                    m.Flames[k]?.MoveTo(m.Target.Position + WfFlameSlots[k]);
            }
            _wfLeft -= dt;
            if (_wfLeft <= 0f) StopWraithFlame(popRemaining: true);
        }

        /// <summary>
        /// 영매가 평타를 쳤다 — 원혼도 같이 친다(「승」). 표적 쪽으로 몸을 내밀며 낫을 휘두르고 혼불을 던진다.
        /// 혼불은 날아가서 닿는 순간에 붙는다(<see cref="WfOrbLand"/>).
        /// </summary>
        private void WfOnHostAttack(Unit target)
        {
            if (!_wfActive || target == null || !target.IsAlive) return;
            var me = _host;
            if (me == null || me.Key != "medium") return;
            WfMark mark = null;
            for (int i = 0; i < _wfMarks.Count; i++) if (_wfMarks[i].Target == target) { mark = _wfMarks[i]; break; }
            if (mark == null)
            {
                if (_wfMarks.Count >= WfMaxMarked) return;
                mark = new WfMark { Target = target };
                _wfMarks.Add(mark);
            }
            if (mark.Gather >= 0f || mark.Stacks + mark.Incoming >= WfStacksToPop) return;   // 이미 셋이 찼거나 날아가는 중

            var from = WfWraithAt(me);
            var d = target.Position - from;
            _wfLungeDir = d.sqrMagnitude > 1f ? d.normalized : Vector2.right;
            _wfLunge = WfLungeSeconds;
            _wfWraith?.SetSnap(1.18f, 0.15f);   // 휘두르는 순간 한 번 부푼다

            // 원혼 바로 앞에서 휘두르는 낫 — 표적 쪽으로
            var slash = FxFrames("mdslash");
            var im = slash != null ? FreeImpact(WfSlashThickness) : null;
            if (im != null)
            {
                var side = new Vector2(-_wfLungeDir.y, _wfLungeDir.x);
                var c = from + _wfLungeDir * 46f;
                im.PlayBeam(c - side * 55f, c + side * 55f, slash, WfSlashThickness);
                im.SetFrameSeconds(0.04f);
            }

            var orb = TakeLoopFx("mdstack", from + _wfLungeDir * 40f, WfOrbSize);
            orb?.SetFrameSeconds(0.06f);
            mark.Incoming++;
            _wfOrbs.Add(new WfOrb { Fx = orb, Mark = mark, From = from + _wfLungeDir * 40f, T = 0f });
        }

        private void TickWfOrbs(Unit me, float dt)
        {
            for (int i = _wfOrbs.Count - 1; i >= 0; i--)
            {
                var o = _wfOrbs[i];
                var m = o.Mark;
                if (m == null || m.Target == null || !m.Target.IsAlive)
                {
                    o.Fx?.Stop();
                    _wfOrbs.RemoveAt(i);
                    continue;
                }
                o.T += dt / WfOrbSeconds;
                float t = Mathf.Clamp01(o.T);
                var to = m.Target.Position + WfBodyCenter;
                var at = Vector2.Lerp(o.From, to, t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * WfOrbArc);
                o.Fx?.MoveTo(at);
                // 지나간 자리에 푸른 빛 알갱이 — 「날아간다」가 읽힌다
                _pfx?.Drift(ParticleFxKind.Ember, ParticleElement.Ice, at, new Vector2(6f, 6f), Vector2.zero, 22f, 0.25f, 1);
                if (t < 1f) { _wfOrbs[i] = o; continue; }
                o.Fx?.Stop();
                _wfOrbs.RemoveAt(i);
                WfOrbLand(me, m);
            }
        }

        /// <summary>혼불이 닿았다 — 「탁」 붙어 머리 위 혼불이 하나 는다. 셋이면 「전」으로.</summary>
        private void WfOrbLand(Unit me, WfMark m)
        {
            m.Incoming = Mathf.Max(0, m.Incoming - 1);
            var body = m.Target.Position + WfBodyCenter;
            PlayFx("mdpop", body, 70f, loop: false)?.SetFrameSeconds(0.04f);   // 붙는 순간 작은 「탁」
            HitEnemyWith(m.Target, UltDamage(me, 0.3f * BaseAxis(1f)), me.Profile);
            int k = m.Stacks;
            m.Stacks++;
            if (k < m.Flames.Length)
            {
                m.Flames[k] = TakeLoopFx("mdstack", m.Target.Position + WfFlameSlots[k], WfStackSize);
                m.Flames[k]?.SetFrameSeconds(0.08f);
                m.Flames[k]?.SetSnap(1.9f, 0.18f);   // 크게 붙었다 제자리로 — 「하나 늘었다」가 보인다
            }
            if (m.Stacks >= WfStacksToPop) m.Gather = 0f;
        }

        private void WfPop(Unit me, WfMark mark, float scale)
        {
            var at = mark.Target.Position + WfBodyCenter;
            float r = Meters(WfPopMeters) * _buffs.AoeMul;
            int dmg = UltDamage(me, WfPopMul * scale * BaseAxis(1f));
            var list = EnemiesInRange(mark.Target.Position, r);
            for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, me.Profile);
            var pop = PlayFx("mdpop", at, WfPopSize * Mathf.Lerp(0.6f, 1f, scale), loop: false);
            if (pop != null) pop.SetFrameSeconds(0.05f);
            else PlayFx("soulripple", at, WfPopSize, loop: false);
            Shake(scale >= 1f ? 6f : 2f);
            if (scale >= 1f) HitStop(0.07f);
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
