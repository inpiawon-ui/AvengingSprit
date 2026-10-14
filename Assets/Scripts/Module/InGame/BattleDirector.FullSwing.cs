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
    /// 그림: 받은 부품 3종(`fx_slwind` 회오리 · `fx_sltendril` 바람 팔 · `fx_slswing` 금빛 호 · `fx_slball` 금빛 탄) + 원작 몸 잔상 · 히트스톱.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float FsSeconds = 2.2f;
        private const float FsPullMeters = 5.0f;        // 끌어당기는 반경 — 회오리 그림 끝과 맞춘다
        private const float FsHoldMeters = 1.6f;        // 몸 둘레 이 고리에 붙잡혀 돈다 — 시안처럼 둘레에 둥글게(1.1 은 한 덩어리로 겹쳤다)
        private const float FsOrbitMeters = 3.2f;       // 초당 둘레로 도는 거리 — 곧장 끌려오면 「빨려 든다」가 아니라 「걸어온다」로 보였다(PD 「빨아들이는 느낌이 구려」)
        private const float FsPullSpeedMeters = 7f;     // 초당 끌려오는 거리
        private const float FsTickEvery = 0.4f;         // 회오리에 갈리는 피해 간격
        private const float FsTickMul = 0.12f;       // 갈리는 피해는 얕게 — 마무리 풀스윙에 날아갈 몸이 남아야 한다(0.3 은 다 죽어 헛스윙이었다)
        private const float FsSwingMul = 4.0f;
        private const float FsSwingMeters = 2.6f;       // 풀스윙이 닿는 거리(끌려온 적은 모두 안쪽)
        private const float FsLaunchMeters = 7f;        // 날아가는 거리
        private const float FsLaunchSeconds = 0.42f;
        private const float FsReflectMul = 2f;
        private const float FsWindHeightPerPull = 1.25f; // 새 회오리(`slwind`, 2:1 납작한 타원) 높이 = 끌어당기는 반경 × 1.25 → 화면 폭의 약 60%(시안 실측 430px / 720)
        private const float FsWindSquash = 1.25f;        // 시안 회오리는 가로 : 세로가 약 1.6 : 1 — 그림(2 : 1)을 세로로 조금 늘린다
        private const float FsDebrisEvery = 0.04f;
        // 회오리 바깥으로 뻗어 적을 감아 끌고 오는 바람 팔(`fx_sltendril`) — PD 2026-10-14
        // 「가운데는 맞는데, 상대편을 끌고 오는 느낌이 드는 이펙트가 회오리 바깥쪽에 더 있어 · 저런 디테일이 중요한 거야」
        private const float FsTendrilEvery = 0.2f;     // 0.12 는 팔이 열 가닥 넘게 겹쳐 문어처럼 보였다(녹화 rw_sl_7) — 시안은 3~5가닥
        private const int FsTendrilPerTick = 2;           // 한 번에 끌려오는 적 둘까지 팔을 뻗는다
        private const float FsTendrilThickness = 72f;
        private const float FsTendrilCurlDeg = 35f;       // 팔 안쪽 끝을 회오리 도는 쪽으로 틀어 나선으로 감긴다
        private static readonly Color FsWindColor = new(0.85f, 0.96f, 1f, 0.9f);
        private static readonly Color FsDebrisColor = new(0.62f, 0.56f, 0.5f, 1f);
        private static readonly Color FsTrailTint = new(0.8f, 0.92f, 1f, 1f);
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
        private Impact _fsVortexInner;
        private Impact _fsVortexMid;
        private float _fsDebrisTick;
        private float _fsTrailTick;
        private float _fsTendrilTick;
        private int _fsTendrilAmbient;

        private void SluggerFullSwing(Unit me)
        {
            _fsActive = true;
            _fsLeft = FsSeconds;
            _fsTick = FsTickEvery;
            _fsVortex?.Stop();
            _fsVortexInner?.Stop();
            // 시안 회오리 — 납작한 타원의 가는 바람 줄기 + 안쪽에 더 빨리 도는 작은 겹(PD 「회오리가 시안이랑 달라」 → 새 그림)
            float h = Meters(FsPullMeters) * FsWindHeightPerPull;
            _fsVortexMid?.Stop();
            _fsVortex = TakeLoopFx("slwind", me.Position, h);
            // 세 겹 — 바깥(느리게) · 가운데 · 안쪽(빠르게). 한 겹만으론 시안보다 성기고 어두웠다
            _fsVortex?.SetFrameSeconds(0.07f);
            _fsVortex?.SetSquash(FsWindSquash);
            _fsVortexMid = TakeLoopFx("slwind", me.Position, h * 0.75f);
            _fsVortexMid?.SetFrameSeconds(0.05f);
            _fsVortexMid?.SetSquash(FsWindSquash);
            _fsVortexInner = TakeLoopFx("slwind", me.Position, h * 0.45f);
            _fsVortexInner?.SetFrameSeconds(0.035f);
            _fsVortexInner?.SetSquash(FsWindSquash);
        }

        private void StopFullSwing()
        {
            _fsActive = false;
            _fsVortex?.Stop();
            _fsVortex = null;
            _fsVortexInner?.Stop();
            _fsVortexInner = null;
            _fsVortexMid?.Stop();
            _fsVortexMid = null;
        }

        private void TickFullSwing(float dt)
        {
            TickFsFlyers(dt);
            if (!_fsActive) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "baseball") { StopFullSwing(); return; }
            _fsVortex?.MoveTo(me.Position);
            _fsVortexInner?.MoveTo(me.Position);
            _fsVortexMid?.MoveTo(me.Position);

            // 소용돌이로 빨아들인다 — 안쪽으로 끌리면서 둘레로 돈다(나선). 고리에 닿으면 그 둘레를 계속 돈다. 보스는 안 끌린다
            float pull = Meters(FsPullMeters), hold = Meters(FsHoldMeters);
            float step = Meters(FsPullSpeedMeters) * dt, orbit = Meters(FsOrbitMeters) * dt;
            _fsTrailTick -= dt;
            bool trail = _fsTrailTick <= 0f;
            if (trail) _fsTrailTick = 0.07f;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss || e.IsHidden) continue;
                var d = me.Position - e.Position;
                float dist = d.magnitude;
                if (dist > pull || dist < 1f) continue;
                var inward = d / dist;
                var tangent = new Vector2(-inward.y, inward.x);   // 시계 반대 — 회오리 그림과 같은 쪽
                var move = tangent * orbit + inward * Mathf.Min(step, Mathf.Max(0f, dist - hold));
                e.Position = ClampedInField(e, e.Position + move);
                e.SetState(EnemyState.Hit);   // 끌려가는 동안은 걷지도 치지도 못한다
                if (trail) FsTrail(e, 0.45f);
            }
            FsDebris(me, dt, pull);
            FsTendrils(me, dt, pull, hold);

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

        /// <summary>돌 파편 · 바람 줄기가 바깥에서 나선으로 빨려 든다(파티클) — 시안의 날리는 돌멩이.</summary>
        private void FsDebris(Unit me, float dt, float pull)
        {
            if (_pfx == null) return;
            _fsDebrisTick -= dt;
            if (_fsDebrisTick > 0f) return;
            _fsDebrisTick = FsDebrisEvery;
            for (int k = 0; k < 2; k++)
            {
                float a = Random.value * Mathf.PI * 2f;
                // 바닥을 비스듬히 본 납작한 원 — 위아래는 반만
                var rim = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f) * pull * Random.Range(0.75f, 1f);
                var inward = -rim.normalized;
                var spiral = (inward * 0.55f + new Vector2(-inward.y, inward.x) * 0.85f).normalized;
                bool rock = k == 0;
                _pfx.Spray(rock ? ParticleFxKind.Shard : ParticleFxKind.Streak, me.Position + rim, spiral, 8f, 1,
                           pull * 1.2f, pull * 1.6f,
                           rock ? new Vector2(16f, 16f) : new Vector2(54f, 6f), 0.45f,
                           rock ? FsDebrisColor : FsWindColor);
            }
        }

        /// <summary>
        /// 바람 팔 — 끌려오는 적마다 바깥(적)에서 회오리 안쪽으로 휘어 감기는 바람 줄기를 뻗는다.
        /// 끌 적이 없어도 둘레로 두 팔이 돌며 뻗어 「빨아들인다」가 늘 보인다.
        /// </summary>
        private void FsTendrils(Unit me, float dt, float pull, float hold)
        {
            _fsTendrilTick -= dt;
            if (_fsTendrilTick > 0f) return;
            _fsTendrilTick = FsTendrilEvery;
            var frames = FxFrames("sltendril");
            if (frames == null) return;
            int n = 0;
            for (int i = 0; i < _enemies.Count && n < FsTendrilPerTick; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss || e.IsHidden) continue;
                float dist = Vector2.Distance(me.Position, e.Position);
                if (dist > pull * 1.1f || dist < hold * 1.3f) continue;
                FsTendril(me, frames, e.Position + (e.Position - me.Position).normalized * 20f, hold);
                n++;
            }
            // 둘레로 도는 빈 팔 — 납작한 원 위 두 자리
            _fsTendrilAmbient++;
            float baseAng = _fsTendrilAmbient * 0.9f;
            for (int k = 0; k < 1; k++)
            {
                float a = baseAng + k * Mathf.PI;
                var rim = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.55f) * pull * 1.15f;
                FsTendril(me, frames, me.Position + rim, hold);
            }
        }

        private void FsTendril(Unit me, Sprite[] frames, Vector2 outer, float hold)
        {
            var d = me.Position - outer;
            if (d.sqrMagnitude < 1f) return;
            // 안쪽 끝은 가운데가 아니라 몸 둘레 고리 위, 회오리 도는 쪽으로 틀어서 — 나선으로 감겨 들어간다
            float r = FsTendrilCurlDeg * Mathf.Deg2Rad;
            var inward = -d.normalized;   // 몸 → 바깥
            var rot = new Vector2(inward.x * Mathf.Cos(r) - inward.y * Mathf.Sin(r), inward.x * Mathf.Sin(r) + inward.y * Mathf.Cos(r));
            var inner = me.Position + rot * hold;
            var im = FreeImpact(FsTendrilThickness);
            if (im == null) return;
            im.PlayBeam(inner, outer, frames, FsTendrilThickness);
            im.SetFrameSeconds(0.05f);
            im.SetTint(new Color(1f, 1f, 1f, 0.8f));   // 회오리(주인공)보다 한 단계 옅게 — 곁가지다
        }

        private void FsTrail(Unit e, float alpha)
        {
            var sprite = e.BodySprite;
            var img = sprite != null ? RentAfterimage() : null;
            img?.Play(sprite, e.Position, e.GetComponent<RectTransform>().sizeDelta, e.BodyFlipX, alpha, 0.18f, FsTrailTint);
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
