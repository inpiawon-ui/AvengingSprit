using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 코만도(수류탄) 「C4」 — 던파 「C4」(붙이고 터뜨리기)를 옮긴 스킬 (PD 2026-10-14 「일정 시간 동안 애들한테 붙이고 터트리면 잼있을 것 같아」).
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_commando_grenade.png` · 기획 `AVSR_SkillRework_DNF.md`.
    ///
    /// 박자: 시작(폭탄 원반을 첫 적에게 던진다) → 한창(원반이 적 사이를 튀며 맞은 적마다 붉게 깜빡이는 폭탄을 붙인다,
    ///       지나간 길에 붉은 점선) → 마무리(붙인 순서대로 펑 · 펑 · 펑 연쇄 폭발, 마지막이 가장 크다).
    /// 그림: 받은 부품 3종(`fx_c4disc` 원반 · `fx_c4bomb` 붙은 폭탄 · `fx_c4trail` 점선) + 기존 큰 폭발(`fx_burst`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const int C4MaxTargets = 6;
        private const float C4HopMeters = 6f;          // 원반이 다음 적을 찾는 거리
        private const float C4HopSeconds = 0.16f;      // 한 번 튀는 시간 — 시안 「0.5~2.0초 도약 · 부착」
        private const float C4ArmSeconds = 0.45f;      // 다 붙인 뒤 깜빡이며 기다리는 시간 — 「곧 터진다」가 읽힌다
        private const float C4ChainEvery = 0.16f;      // 펑 · 펑 · 펑 간격
        private const float C4StickMul = 0.4f;
        private const float C4BlastMul = 2.0f;
        private const float C4LastMul = 3.0f;
        private const float C4BlastMeters = 1.6f;
        private const float C4LastMeters = 2.4f;
        private const float C4DiscSize = 40f;
        private const float C4BombSize = 34f;
        private const float C4TrailThickness = 28f;

        private struct C4Bomb
        {
            public Unit Target;
            public Vector2 LastAt;
            public Impact Fx;
        }

        private readonly List<C4Bomb> _c4Bombs = new();
        private readonly List<Unit> _c4Hit = new();
        private bool _c4Active;
        private int _c4Phase;           // 0 = 튀는 중 · 1 = 깜빡이며 기다림 · 2 = 연쇄 폭발
        private float _c4Timer;
        private float _c4HopT;
        private Vector2 _c4From;
        private Unit _c4Next;
        private int _c4Detonated;
        private Impact _c4Disc;

        private void CommandoStickyBomb(Unit me)
        {
            StopStickyBomb();
            _c4Active = true;
            _c4Phase = 0;
            _c4From = me.MuzzlePosition;
            _c4Next = C4NextTarget(me.Position);
            _c4HopT = 0f;
            me.PlayAttack();
            if (_c4Next == null)
            {
                // 적이 없으면 앞에 그냥 한 발 — 헛누름이 아니다
                PlayFx("burst", me.Position + AimDirection(me) * Meters(2f), Meters(C4BlastMeters) * 2f, loop: false);
                _c4Active = false;
                return;
            }
            me.SetFacing(_c4Next.Position - me.Position);
            _c4Disc = TakeLoopFx("c4disc", _c4From, C4DiscSize);
            _c4Disc?.SetFrameSeconds(0.04f);
        }

        private void StopStickyBomb()
        {
            for (int i = 0; i < _c4Bombs.Count; i++) _c4Bombs[i].Fx?.Stop();
            _c4Bombs.Clear();
            _c4Hit.Clear();
            _c4Disc?.Stop();
            _c4Disc = null;
            _c4Active = false;
        }

        private Unit C4NextTarget(Vector2 from)
        {
            Unit best = null;
            float bestD = Meters(C4HopMeters);
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsHidden || _c4Hit.Contains(e) || !IsOnScreen(e)) continue;
                float d = Vector2.Distance(from, e.Position);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private void TickStickyBomb(float dt)
        {
            // 붙은 폭탄은 적을 따라 다닌다 — 죽으면 그 자리에 남는다
            for (int i = 0; i < _c4Bombs.Count; i++)
            {
                var b = _c4Bombs[i];
                if (b.Target != null && b.Target.IsAlive) b.LastAt = b.Target.Position + new Vector2(10f, 22f);
                b.Fx?.MoveTo(b.LastAt);
                _c4Bombs[i] = b;
            }
            if (!_c4Active) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "commando_grenade") { StopStickyBomb(); return; }

            if (_c4Phase == 0) { TickC4Hop(me, dt); return; }
            _c4Timer -= dt;
            if (_c4Timer > 0f) return;
            if (_c4Phase == 1) { _c4Phase = 2; _c4Detonated = 0; }
            C4Detonate(me, _c4Detonated);
            _c4Detonated++;
            _c4Timer = C4ChainEvery;
            if (_c4Detonated >= _c4Bombs.Count) StopStickyBomb();
        }

        private void TickC4Hop(Unit me, float dt)
        {
            if (_c4Next == null || !_c4Next.IsAlive)
            {
                // 가는 사이 다른 것이 잡았다 — 다음 적으로 돌린다
                _c4Next = C4NextTarget(_c4From);
                if (_c4Next == null) { C4Arm(); return; }
            }
            _c4HopT += dt / C4HopSeconds;
            var to = _c4Next.Position;
            // 낮게 포물선으로 튄다 — 곧게 날면 원반이 아니라 탄이다
            var at = Vector2.Lerp(_c4From, to, _c4HopT) + Vector2.up * (Mathf.Sin(Mathf.Clamp01(_c4HopT) * Mathf.PI) * 26f);
            _c4Disc?.MoveTo(at);
            if (_c4HopT < 1f) return;

            C4Stick(me, _c4Next);
            C4Trail(_c4From, to);
            _c4From = to;
            _c4HopT = 0f;
            _c4Next = _c4Hit.Count < C4MaxTargets ? C4NextTarget(to) : null;
            if (_c4Next == null) C4Arm();
        }

        private void C4Stick(Unit me, Unit target)
        {
            _c4Hit.Add(target);
            HitEnemyWith(target, SkillDamage(me, C4StickMul * BaseAxis(1f)), me.Profile);
            var at = target.Position + new Vector2(10f, 22f);
            var fx = TakeLoopFx("c4bomb", at, C4BombSize);
            fx?.SetFrameSeconds(0.1f);
            _c4Bombs.Add(new C4Bomb { Target = target, LastAt = at, Fx = fx });
            PlayFx("hit", target.Position, 64f, loop: false);
            HitStop(0.03f);
        }

        private void C4Trail(Vector2 from, Vector2 to)
        {
            var frames = FxFrames("c4trail");
            if (frames == null) return;
            var im = FreeImpact(C4TrailThickness);
            if (im == null) return;
            im.PlayBeam(from, to, frames, C4TrailThickness);
            im.SetFrameSeconds(0.12f);   // 다 붙일 때까지 길이 남아 보인다
        }

        private void C4Arm()
        {
            _c4Disc?.Stop();
            _c4Disc = null;
            _c4Phase = 1;
            _c4Timer = C4ArmSeconds;
            // 기다리는 동안은 빨리 깜빡인다
            for (int i = 0; i < _c4Bombs.Count; i++) _c4Bombs[i].Fx?.SetFrameSeconds(0.05f);
            if (_c4Bombs.Count == 0) StopStickyBomb();
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
            _c4Bombs[index] = b;
            bool last = index == _c4Bombs.Count - 1;
            var at = b.Target != null && b.Target.IsAlive ? b.Target.Position : b.LastAt;
            float r = Meters(last ? C4LastMeters : C4BlastMeters) * _buffs.AoeMul;
            int dmg = SkillDamage(me, (last ? C4LastMul : C4BlastMul) * BaseAxis(1f));
            var list = EnemiesInRange(at, r);
            for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, me.Profile);
            // 연쇄는 작은 도트 폭발, 마지막만 큰 폭발 — 둘 다 반경 × 2.2 로 띄웠더니 연기가 화면 3분의 1을 덮었다(녹화 rw_c4_1)
            // 연쇄 폭발은 알갱이(연기 · 파편)를 얹지 않는다 — 여섯 번 겹치면 연기 구름이 화면을 덮었다(녹화 rw_c4_2). 마지막 한 방만 얹는다
            var boom = last ? PlayFx("burst", at, r * 1.6f, loop: false) : C4QuietBoom(at, r * 1.5f);
            boom?.SetFrameSeconds(0.06f);
            Shake(last ? 12f : 5f);
            HitStop(last ? 0.1f : 0.03f);
        }
    }
}
