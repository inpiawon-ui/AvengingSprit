using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 폭력배 「난사」 — 던파 레인저 「난사」를 옮긴 스킬 (PD 2026-10-14).
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_thug.png` · 기획 `AVSR_SkillRework_DNF.md`.
    ///
    /// 박자: 시작(두 손 권총을 들고 돌기 시작) → 한창(2.5초 동안 몸이 돌며 양손으로 사방 연사, 총구 불꽃이 원을 그리고 탄피가 튄다,
    ///       이동 스틱은 그대로 먹는다) → 마무리(회전을 멈추며 사방으로 한 발씩 + 흰 섬광 고리, 가장 세게).
    /// 그림: 받은 부품 3종(`fx_thspin` 회전 궤적 · `fx_thring` 마무리 섬광 · `fx_thshell` 탄피) + 원작 8방향 사격 자세(돌면서 방향만 바꾼다).
    /// 부품이 없으면 기존 그림(`muzzle` · `crit`)으로 떨어진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float SfSeconds = 2.5f;
        private const float SfTurnsPerSecond = 1.4f;   // 초당 1.4바퀴 — 8방향 자세가 또렷이 넘어가 「돈다」가 읽히는 빠르기
        private const float SfShotEvery = 0.06f;       // 양손이 번갈아 — 초당 약 33발
        private const float SfShotMul = 0.35f;         // 한 발 × 표 성장축 — 도는 동안 한 적이 맞는 것은 일부다
        private const float SfFinaleMul = 1.2f;
        private const int SfFinaleShots = 16;
        private const float SfShellEvery = 0.07f;
        private const float SfTrailEvery = 0.09f;
        private const float SfSpinSize = 190f;          // 몸(약 96) 둘레를 감는 고리
        private const float SfRingSize = 330f;
        private const float SfMuzzleSize = 44f;
        private const float SfShellSize = 22f;
        private static readonly Color SfTrailTint = new(0.82f, 0.78f, 1f, 1f);

        private bool _sfActive;
        private float _sfLeft;
        private float _sfAngle;
        private float _sfShotTick;
        private float _sfShellTick;
        private float _sfTrailTick;
        private bool _sfLeftHand;
        private Impact _sfSpinFx;

        private void ThugSpinFire(Unit me)
        {
            _sfActive = true;
            _sfLeft = SfSeconds;
            var f = me.Facing;
            _sfAngle = Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg;
            _sfShotTick = _sfShellTick = _sfTrailTick = 0f;
            _sfSpinFx?.Stop();
            _sfSpinFx = TakeLoopFx("thspin", me.Position, SfSpinSize);
            _sfSpinFx?.SetFrameSeconds(0.05f);
        }

        private void StopSpinFire()
        {
            _sfActive = false;
            _sfSpinFx?.Stop();
            _sfSpinFx = null;
        }

        private void TickSpinFire(float dt)
        {
            if (!_sfActive) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "thug") { StopSpinFire(); return; }
            _sfSpinFx?.MoveTo(me.Position);

            _sfAngle += 360f * SfTurnsPerSecond * dt;
            float rad = _sfAngle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            me.SetFacing(dir);

            _sfShotTick -= dt;
            if (_sfShotTick <= 0f)
            {
                _sfShotTick += SfShotEvery;
                // 두 손 권총 — 앞 · 뒤로 번갈아. 한 손씩 쏘아야 총구 불꽃이 원을 그린다
                var d = _sfLeftHand ? -dir : dir;
                _sfLeftHand = !_sfLeftHand;
                SfShoot(me, d, UltDamage(me, SfShotMul * BaseAxis(1f)));
            }

            _sfShellTick -= dt;
            if (_sfShellTick <= 0f)
            {
                _sfShellTick += SfShellEvery;
                // 탄피는 몸 옆으로 튄다 — 발밑 둘레 아무 자리
                var side = new Vector2(-dir.y, dir.x) * Random.Range(18f, 40f) + Random.insideUnitCircle * 10f;
                PlayFx("thshell", me.Position + side - new Vector2(0f, 20f), SfShellSize, loop: false)?.SetFrameSeconds(0.06f);
            }

            _sfTrailTick -= dt;
            if (_sfTrailTick <= 0f)
            {
                _sfTrailTick += SfTrailEvery;
                var sprite = me.BodySprite;
                var img = sprite != null ? RentAfterimage() : null;
                img?.Play(sprite, me.Position, me.GetComponent<RectTransform>().sizeDelta, me.BodyFlipX,
                          0.45f, 0.22f, SfTrailTint);
            }

            _sfLeft -= dt;
            if (_sfLeft > 0f) return;
            SfFinale(me);
            StopSpinFire();
        }

        private void SfShoot(Unit me, Vector2 dir, int dmg)
        {
            FireFan(me, me.Position + dir * 2000f, 1, 0f, dmg, fromPlayer: true);
            PlayFx("muzzle", me.Position + dir * 34f, SfMuzzleSize, loop: false);
        }

        /// <summary>회전을 멈추며 사방으로 한 발씩 — 흰 섬광 고리.</summary>
        private void SfFinale(Unit me)
        {
            int dmg = UltDamage(me, SfFinaleMul * BaseAxis(1f));
            FireFan(me, me.Position + me.Facing * 2000f, SfFinaleShots, 360f - 360f / SfFinaleShots, dmg, fromPlayer: true);
            var ring = PlayFx("thring", me.Position, SfRingSize, loop: false);
            if (ring != null) ring.SetFrameSeconds(0.05f);
            else PlayFx("crit", me.Position, SfRingSize * 0.6f, loop: false);
            me.PlayAttack();
            Shake(8f);
            HitStop(0.08f);
        }
    }
}
