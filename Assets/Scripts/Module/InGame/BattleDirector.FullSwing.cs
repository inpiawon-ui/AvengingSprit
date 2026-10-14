using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 슬러거 「풀스윙 회오리」 — 던파 「대회전격」을 옮긴 스킬 (PD 2026-10-14
    /// 「일정 반경의 적을 끌어들이고 원거리 공격은 반사시키고 2~3초 있다가 끌어들인 적을 한 방에 날리는 거」).
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png`.
    ///
    /// 박자: 시작(몸 둘레에 회오리, 반경 안 적을 끌어당긴다) → 한창(2.2초 동안 끌려온 적이 몸 둘레에 붙잡히고,
    ///       날아온 적 탄은 금빛 탄으로 되받아쳐 가까운 적을 쫓는다) → 마무리(풀스윙 — 모인 적을 한쪽으로 한꺼번에 날린다, 가장 세게).
    /// 그림: 받은 부품 3종(`fx_slvortex` 회오리 · `fx_slswing` 금빛 호 · `fx_slball` 금빛 탄) + 원작 몸 잔상 · 히트스톱.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float FsSeconds = 2.2f;
        private const float FsPullMeters = 4.5f;        // 끌어당기는 반경
        private const float FsHoldMeters = 1.1f;        // 몸에서 이만큼 떨어진 자리에 붙잡힌다(겹치면 누가 누군지 안 보인다)
        private const float FsPullSpeedMeters = 7f;     // 초당 끌려오는 거리
        private const float FsTickEvery = 0.4f;         // 회오리에 갈리는 피해 간격
        private const float FsTickMul = 0.12f;       // 갈리는 피해는 얕게 — 마무리 풀스윙에 날아갈 몸이 남아야 한다(0.3 은 다 죽어 헛스윙이었다)
        private const float FsSwingMul = 4.0f;
        private const float FsSwingMeters = 2.6f;       // 풀스윙이 닿는 거리(끌려온 적은 모두 안쪽)
        private const float FsLaunchMeters = 7f;        // 날아가는 거리
        private const float FsLaunchSeconds = 0.42f;
        private const float FsReflectMul = 2f;
        private const float FsVortexSize = 340f;
        private const float FsSwingLength = 440f;   // 시안의 풀스윙은 화면 반을 가른다 — 300 은 몸 둘레에 그쳤다
        private const float FsSwingThickness = 280f;
        private static readonly Color FsGold = new(1f, 0.86f, 0.35f, 1f);

        private struct FsFlyer
        {
            public Unit Target;
            public Vector2 From, To;
            public float T;
            public float TrailTick;
        }

        private readonly List<FsFlyer> _fsFlyers = new();
        private bool _fsActive;
        private float _fsLeft;
        private float _fsTick;
        private Impact _fsVortex;

        private void SluggerFullSwing(Unit me)
        {
            _fsActive = true;
            _fsLeft = FsSeconds;
            _fsTick = FsTickEvery;
            _fsVortex?.Stop();
            _fsVortex = TakeLoopFx("slvortex", me.Position, FsVortexSize);
            _fsVortex?.SetFrameSeconds(0.05f);
            _fsVortex?.SetTint(new Color(1f, 1f, 1f, 0.55f));   // 회오리 너머로 몸이 보여야 한다 — 불투명하면 슬러거가 통째로 가려졌다
        }

        private void StopFullSwing()
        {
            _fsActive = false;
            _fsVortex?.Stop();
            _fsVortex = null;
        }

        private void TickFullSwing(float dt)
        {
            TickFsFlyers(dt);
            if (!_fsActive) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "baseball") { StopFullSwing(); return; }
            _fsVortex?.MoveTo(me.Position);

            // 회오리 쪽으로 끌어당긴다 — 보스는 안 끌린다
            float pull = Meters(FsPullMeters), hold = Meters(FsHoldMeters), step = Meters(FsPullSpeedMeters) * dt;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss || e.IsHidden) continue;
                var d = me.Position - e.Position;
                float dist = d.magnitude;
                if (dist > pull || dist <= hold) continue;
                e.Position = ClampedInField(e, e.Position + d / dist * Mathf.Min(step, dist - hold));
            }

            // 반경 안 적 탄 → 금빛 탄(가까운 적을 쫓는다)
            for (int i = 0; i < _shots.Count; i++)
            {
                var s = _shots[i];
                if (!s.IsActive || s.FromPlayer || s.IsLob) continue;   // 던진 수류탄은 되받아치지 않는다 — 내 것이 되면 큰 폭발로 터졌다
                if (Vector2.Distance(s.Position, me.Position) > pull) continue;
                s.TurnFriendly(FsReflectMul);
                var target = NearestEnemy(s.Position, Meters(12f));
                if (target != null) { s.Retarget(target); s.SetHoming(1.2f); }
                var gold = FxFrames("slball");
                if (gold != null) s.SetSprite(gold, s.Kind, loop: true);
                s.SetColor(gold != null ? Color.white : FsGold);
                PlayFx("hit", s.Position, 48f, loop: false);
            }

            _fsTick -= dt;
            if (_fsTick <= 0f)
            {
                _fsTick += FsTickEvery;
                int dmg = SkillDamage(me, FsTickMul * BaseAxis(1f));
                var list = EnemiesInRange(me.Position, hold * 1.8f);
                for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, me.Profile);
                me.PlayAttack();   // 배트를 휘두르며 돈다
            }
            // 회오리 안에서 몸이 돈다 — 8방향 자세가 넘어간다
            float a = (FsSeconds - _fsLeft) * 360f * 1.2f * Mathf.Deg2Rad;
            me.SetFacing(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));

            _fsLeft -= dt;
            if (_fsLeft > 0f) return;
            FsSwing(me);
            StopFullSwing();
        }

        /// <summary>풀스윙 — 모인 적이 가장 많은 쪽으로 휘둘러 한꺼번에 날린다.</summary>
        private void FsSwing(Unit me)
        {
            float reach = Meters(FsSwingMeters) * _buffs.AoeMul;
            var list = new List<Unit>(EnemiesInRange(me.Position, reach));
            // 날리는 쪽: 적이 없는 쪽(벽이 먼 쪽)보다 화면 가운데 쪽 — 시안처럼 옆으로 시원하게
            var dir = list.Count > 0 ? FsSwingDirection(me, list) : AimDirection(me);
            me.SetFacing(dir);
            me.PlayAttack();
            int dmg = SkillDamage(me, FsSwingMul * BaseAxis(1f));
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;
                // 날리고 나서 친다 — 이 한 방에 죽는 적도 날아가야 「한 방에 날렸다」가 보인다(녹화 rw_sl_4 는 그 자리에서 쓰러졌다)
                if (!e.IsBoss) FsLaunch(e, dir);
                HitEnemyWith(e, dmg, me.Profile);
            }
            // 금빛 호 — 몸에서 휘두른 쪽으로
            var frames = FxFrames("slswing");
            var im = frames != null ? FreeImpact(FsSwingThickness) : null;
            if (im != null)
            {
                im.PlayBeam(me.Position - dir * 40f, me.Position + dir * (FsSwingLength - 40f), frames, FsSwingThickness);
                im.SetFrameSeconds(0.05f);
            }
            else PlayFx("rfswing", me.Position, FsSwingLength, loop: false);
            PlayFx("crit", me.Position + dir * 60f, 140f, loop: false);
            Shake(12f);
            HitStop(0.14f);
        }

        /// <summary>몸 → 적 무리 평균 쪽. 평균이 몸 위면 바라보는 쪽.</summary>
        private static Vector2 FsSwingDirection(Unit me, List<Unit> list)
        {
            var sum = Vector2.zero;
            for (int i = 0; i < list.Count; i++) if (list[i] != null) sum += list[i].Position - me.Position;
            return sum.sqrMagnitude > 1f ? sum.normalized : me.Facing;
        }

        private void FsLaunch(Unit e, Vector2 dir)
        {
            // 같은 쪽으로 날리되 조금씩 벌린다 — 한 줄로 겹치면 몇 명이 날아갔는지 안 보인다
            float spread = Random.Range(-22f, 22f) * Mathf.Deg2Rad;
            var d = new Vector2(dir.x * Mathf.Cos(spread) - dir.y * Mathf.Sin(spread),
                                dir.x * Mathf.Sin(spread) + dir.y * Mathf.Cos(spread));
            var to = ClampedInField(e, e.Position + d * Meters(FsLaunchMeters));
            for (int i = 0; i < _fsFlyers.Count; i++) if (_fsFlyers[i].Target == e) return;
            _fsFlyers.Add(new FsFlyer { Target = e, From = e.Position, To = to, T = 0f });
        }

        private void TickFsFlyers(float dt)
        {
            for (int i = _fsFlyers.Count - 1; i >= 0; i--)
            {
                var f = _fsFlyers[i];
                // 죽은 몸도 날아간다(쓰러지는 동작 그대로) — 풀로 돌아간 몸만 놓는다
                if (f.Target == null || !f.Target.isActiveAndEnabled) { _fsFlyers.RemoveAt(i); continue; }
                f.T += dt / FsLaunchSeconds;
                float k = Game.Module.Common.UI.Ease.OutCubic(Mathf.Clamp01(f.T));
                f.Target.Position = Vector2.Lerp(f.From, f.To, k);
                if (f.Target.IsAlive) f.Target.SetState(EnemyState.Hit);
                f.TrailTick -= dt;
                if (f.TrailTick <= 0f)
                {
                    f.TrailTick = 0.04f;
                    var sprite = f.Target.BodySprite;
                    var img = sprite != null ? RentAfterimage() : null;
                    img?.Play(sprite, f.Target.Position, f.Target.GetComponent<RectTransform>().sizeDelta,
                              f.Target.BodyFlipX, 0.5f, 0.2f, Color.white);
                }
                if (f.T >= 1f) { _fsFlyers.RemoveAt(i); continue; }
                _fsFlyers[i] = f;
            }
        }
    }
}
