using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 코만도(수류탄) 「C4」 — 던파 「C4」(붙이고 터뜨리기)를 옮긴 스킬.
    /// PD 2026-10-14 「폭탄을 몹들한테 설치하는 느낌이 하나도 안 나 — 스킬을 켜면 중복되지 않게 주변에 장착하고 3초 정도 있다가 설치된 게 터지는 느낌」.
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_commando_grenade.png`.
    ///
    /// 박자: 시작(주변 적 최대 6명에게 폭탄을 한 개씩 휙휙 던져 붙인다 — 한 명에 하나, 붙는 순간 「탁」)
    ///       → 한창(3초 동안 붙은 폭탄이 적을 따라다니며 깜빡인다, 터질 때가 다가올수록 빨라진다)
    ///       → 마무리(붙인 순서대로 펑 · 펑 · 펑, 마지막이 가장 크다).
    /// 그림: 받은 부품(`fx_c4bomb` 붙은 폭탄) + 기존 폭발(`fx_grenade_burst` · `fx_burst`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const int C4MaxTargets = 6;
        private const float C4RangeMeters = 5.5f;      // 이 안의 화면 속 적에게 붙인다
        private const float C4ThrowEvery = 0.08f;      // 한 개씩 휙 · 휙 — 여섯 개가 0.5초 안에 다 붙는다
        private const float C4ThrowSeconds = 0.2f;     // 날아가는 시간
        private const float C4ThrowArc = 70f;          // 던지는 포물선 높이
        private const float C4FuseSeconds = 3f;        // 붙이고 터질 때까지(PD 「3초 정도 있다가」)
        private const float C4ChainEvery = 0.08f;      // 펑 · 펑 · 펑 간격
        private const float C4StickMul = 0.3f;
        private const float C4BlastMul = 2.2f;
        private const float C4LastMul = 3.0f;
        private const float C4BlastMeters = 1.6f;
        private const float C4LastMeters = 2.4f;
        private const float C4BombSize = 96f;          // 화면 배율(약 0.45) 뒤 약 40px — 64 도 녹화에서 15px 남짓이라 「붙였다」가 안 읽혔다(rw_c4_4)
        private static readonly Vector2 C4StickOffset = new(0f, 14f);   // 몸통 한가운데(머리 위가 아니라 몸에 붙는다)

        private sealed class C4Bomb
        {
            public Unit Target;
            public Vector2 LastAt;
            public Vector2 From;
            public float Delay;      // 던지기 전 기다림
            public float FlyT;       // 0 → 1 날아가는 중
            public bool Thrown;
            public bool Stuck;
            public Impact Fx;
        }

        private readonly List<C4Bomb> _c4Bombs = new();
        private bool _c4Active;
        private float _c4Fuse;
        private int _c4Detonated;
        private float _c4ChainTimer;

        private void CommandoStickyBomb(Unit me)
        {
            StopStickyBomb();
            var targets = C4PickTargets(me);
            me.PlayAttack();
            if (targets.Count == 0)
            {
                // 적이 없으면 앞에 그냥 한 발 — 헛누름이 아니다
                PlayFx("burst", me.Position + AimDirection(me) * Meters(2f), Meters(C4BlastMeters) * 2f, loop: false);
                return;
            }
            _c4Active = true;
            _c4Fuse = C4FuseSeconds;
            _c4Detonated = -1;
            for (int i = 0; i < targets.Count; i++)
                _c4Bombs.Add(new C4Bomb { Target = targets[i], LastAt = targets[i].Position + C4StickOffset, Delay = i * C4ThrowEvery });
        }

        /// <summary>가까운 순으로 최대 6명 — 한 명에 하나씩(중복 없음).</summary>
        private List<Unit> C4PickTargets(Unit me)
        {
            var list = new List<Unit>();
            float range = Meters(C4RangeMeters);
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsHidden || !IsOnScreen(e)) continue;
                if (Vector2.Distance(me.Position, e.Position) > range) continue;
                list.Add(e);
            }
            var from = me.Position;
            list.Sort((a, b) => Vector2.SqrMagnitude(a.Position - from).CompareTo(Vector2.SqrMagnitude(b.Position - from)));
            if (list.Count > C4MaxTargets) list.RemoveRange(C4MaxTargets, list.Count - C4MaxTargets);
            return list;
        }

        private void StopStickyBomb()
        {
            for (int i = 0; i < _c4Bombs.Count; i++) _c4Bombs[i].Fx?.Stop();
            _c4Bombs.Clear();
            _c4Active = false;
        }

        private void TickStickyBomb(float dt)
        {
            if (!_c4Active) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "commando_grenade") { StopStickyBomb(); return; }

            _c4Fuse -= dt;
            // 깜빡임이 터질 때가 다가올수록 빨라진다 — 0.25초 → 0.04초
            float blink = Mathf.Lerp(0.04f, 0.25f, Mathf.Clamp01(_c4Fuse / C4FuseSeconds));
            for (int i = 0; i < _c4Bombs.Count; i++)
            {
                var b = _c4Bombs[i];
                if (b.Target != null && b.Target.IsAlive) b.LastAt = b.Target.Position + C4StickOffset;
                if (!b.Stuck) { TickC4Throw(me, b, dt); continue; }
                b.Fx?.MoveTo(b.LastAt);
                b.Fx?.SetFrameSeconds(blink);
            }

            if (_c4Fuse > 0f) return;
            if (_c4Detonated < 0) { _c4Detonated = 0; _c4ChainTimer = 0f; }
            _c4ChainTimer -= dt;
            if (_c4ChainTimer > 0f) return;
            _c4ChainTimer = C4ChainEvery;
            C4Detonate(me, _c4Detonated);
            _c4Detonated++;
            if (_c4Detonated >= _c4Bombs.Count) StopStickyBomb();
        }

        private void TickC4Throw(Unit me, C4Bomb b, float dt)
        {
            if (!b.Thrown)
            {
                b.Delay -= dt;
                if (b.Delay > 0f) return;
                b.Thrown = true;
                b.From = me.MuzzlePosition;
                b.Fx = TakeLoopFx("c4bomb", b.From, C4BombSize);
                b.Fx?.SetFrameSeconds(0.05f);
                me.SetFacing(b.LastAt - me.Position);
                me.PlayAttack();
            }
            b.FlyT += dt / C4ThrowSeconds;
            float k = Mathf.Clamp01(b.FlyT);
            var at = Vector2.Lerp(b.From, b.LastAt, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * C4ThrowArc);
            b.Fx?.MoveTo(at);
            if (k < 1f) return;
            // 붙었다 — 「탁」
            b.Stuck = true;
            if (b.Target != null && b.Target.IsAlive)
                HitEnemyWith(b.Target, SkillDamage(me, C4StickMul * BaseAxis(1f)), me.Profile);
            PlayFx("hit", b.LastAt, 70f, loop: false);
            HitStop(0.02f);
        }

        private Impact C4QuietBoom(Vector2 at, float size)
        {
            var frames = FxFrames("grenade_burst") ?? FxFrames("burst");
            var im = frames != null ? FreeImpact(size) : null;
            im?.Play(at, frames, size, false);
            return im;
        }

        private void C4Detonate(Unit me, int index)
        {
            if (index < 0 || index >= _c4Bombs.Count) return;
            var b = _c4Bombs[index];
            b.Fx?.Stop();
            b.Fx = null;
            bool last = index == _c4Bombs.Count - 1;
            var at = b.LastAt - C4StickOffset;
            float r = Meters(last ? C4LastMeters : C4BlastMeters) * _buffs.AoeMul;
            int dmg = SkillDamage(me, (last ? C4LastMul : C4BlastMul) * BaseAxis(1f));
            var list = EnemiesInRange(at, r);
            for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, me.Profile);
            // 연쇄는 알갱이(연기 · 파편)를 얹지 않는다 — 여섯 번 겹치면 연기 구름이 화면을 덮었다(녹화 rw_c4_2). 마지막 한 방만 얹는다
            var boom = last ? PlayFx("burst", at, r * 1.6f, loop: false) : C4QuietBoom(at, r * 1.5f);
            boom?.SetFrameSeconds(0.06f);
            Shake(last ? 12f : 5f);
            HitStop(last ? 0.1f : 0.03f);
        }
    }
}
