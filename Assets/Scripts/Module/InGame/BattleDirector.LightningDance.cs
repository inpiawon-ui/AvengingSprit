using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 아마존 「번개 무희」 — 던파 스트라이커 「라이트닝 댄스」를 옮긴 스킬 (PD 2026-10-14).
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_amazon.png` · 기획 `AVSR_SkillRework_DNF.md`.
    ///
    /// 박자: 시작(번개를 두르고 첫 적에게 순간 돌진) → 한창(주변 적 최대 6명 사이를 번개 줄기로 옮겨 다니며 1타씩,
    ///       맞을 때마다 짧은 히트스톱) → 마무리(마지막 자리에서 하늘 번개 내려찍기 + 충격파, 가장 세게).
    /// 시전 내내 무적이다 — 몸이 적 사이를 날아다니는데 그 사이 맞으면 답답하다.
    /// 그림은 시안을 기준으로 받은 부품 4종(`fx_ldbolt` 줄기 · `fx_ldaura` 몸 오라 · `fx_ldspark` 타격 섬광 · `fx_ldslam` 내려찍기,
    /// 납품 `_exchange/in/part_ld_*.png`) + 원작 몸 잔상 · 히트스톱. 부품이 없으면 예전 번개 그림으로 떨어진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const int LdMaxTargets = 6;
        private const float LdRadiusMeters = 12f;       // 화면 안의 적이면 간다 — 시안처럼 아래에서 위쪽 적들까지 건너간다(6m 로는 첫 녹화에서 헛돌았다)
        private const float LdLeadSeconds = 0.12f;      // 번개를 두르는 순간
        private const float LdDashSeconds = 0.11f;      // 한 명에게 미끄러져 가는 시간 — 순간이동이 아니라 잔상을 끌며 간다(PD 2026-10-14)
        private const float LdPauseSeconds = 0.09f;     // 친 뒤 숨 — 「탁 · 탁 · 탁」 이 읽히는 간격(돌진 + 숨 = 0.2초)
        private const float LdTrailEvery = 0.035f;      // 한 번 건너는 동안 잔상 3장 — 촘촘하면 번진 띠가 되고 자세가 안 읽힌다(시안은 또렷한 몸 2~3개)
        private const float LdTrailAlpha = 0.75f;
        private const float LdTrailLife = 0.5f;
        private static readonly Color LdTrailTint = new(0.78f, 0.88f, 1f, 1f);   // 몸 색이 비치는 옅은 번개빛 — 청록 실루엣은 시안보다 탁했다
        private const float LdSlamDelay = 0.22f;        // 마지막 타 뒤 숨 한 번, 그리고 내려찍기
        private const float LdStandOffMeters = 0.7f;    // 적 바로 앞에 선다(겹치면 누가 누군지 안 보인다)
        private const float LdHitMul = 1.6f;            // 한 타 × 표 성장축 — 6타 + 마무리 합이 예전 도약 강타(6.0)의 약 2배
        private const float LdSlamMul = 3.0f;
        private const float LdSlamRadiusMeters = 2.5f;
        private const float LdSlamKnockMeters = 1.8f;
        private const float LdHitStopSeconds = 0.04f;
        private const float LdSlamHitStopSeconds = 0.12f;
        private const float LdBoltThickness = 130f;   // 84 는 시안보다 줄기가 가늘었다(시안 대조 v3)
        private const float LdAuraSize = 100f;          // 몸(약 96) 둘레만 — 120 · 150 은 둥근 덩어리가 몸을 덮어 차기 자세가 안 읽혔다(녹화 대조)
        private const float LdSparkSize = 160f;       // 시안의 흰 별 섬광은 적 몸보다 크다
        private const float LdSlamHeightPerRadius = 2.6f;  // 내려찍기 그림(1:2) 높이 = 반경 × 5 → 바닥 고리 폭 ≈ 지름
        private const float LdSlamPoseHold = 0.25f;
        private const float LdSlamBurstPerRadius = 2.4f;   // 바닥 번개 섬광 지름 = 반경 × 2.4 — 기둥 그림의 고리만으론 시안 충격파의 절반이었다
        private const float LdSlamHopPixels = 64f;      // 내려찍기 전에 뛰어오르는 높이 — 시안 오른쪽 컷처럼 공중에서 내리꽂는다
        private const float LdBoltFrameSeconds = 0.08f; // 번개 줄기가 두세 번 건너는 동안 남아 길이 이어져 보인다(시안 가운데 컷의 번개 그물)     // 내려찍은 자세로 잠깐 멈춘다 — 바로 서면 「찍었다」가 안 읽힌다

        // 스킬 자세(원작 시트를 앵커로 받은 새 프레임, `Unit/amazon/unit_amazon_{s|e|n}_ld{1|2|3}`)
        private const string LdPoseDash = "ld1";        // 돌진 날아차기
        private const string LdPoseHit = "ld2";         // 돌려차기
        private const string LdPoseSlam = "ld3";        // 뛰어올라 내려찍기

        private readonly List<Unit> _ldHit = new();
        private bool _ldActive;
        private bool _ldSlamPending;
        private int _ldStep;
        private float _ldTimer;
        private bool _ldDashing;
        private float _ldDashT;
        private float _ldTrailTick;
        private Vector2 _ldFrom, _ldTo;
        private Unit _ldTarget;
        private Impact _ldAura;
        private Unit _ldBody;           // 자세 그림을 쥐여 준 몸 — 몸이 바뀌어도 옛 몸의 자세를 풀어 준다
        private float _ldPoseHold;

        /// <summary>스킬 시작. 실제 움직임은 <see cref="TickLightningDance"/> 가 박자마다 낸다.</summary>
        private void AmazonLightningDance(Unit me)
        {
            _ldHit.Clear();
            _ldActive = true;
            _ldSlamPending = false;
            _ldStep = 0;
            _ldDashing = false;
            _ldTarget = null;
            _ldTimer = LdLeadSeconds;
            _invuln = Mathf.Max(_invuln, LdLeadSeconds + (LdDashSeconds + LdPauseSeconds) * LdMaxTargets + LdSlamDelay + 0.2f);
            // 번개를 두른다 — 스킬 동안 몸을 따라다니는 전기 오라(시안 왼쪽 컷)
            _ldAura?.Stop();
            _ldAura = TakeLoopFx("ldaura", me.Position, LdAuraSize);
            _ldAura?.SetFrameSeconds(0.06f);
            if (_ldAura == null) PlayFx("castburst", me.Position, Meters(2f), loop: false);
        }

        private void StopLightningDance()
        {
            LdReleasePose();
            _ldActive = false;
            _ldDashing = false;
            _ldTarget = null;
            _ldHit.Clear();
            _ldAura?.Stop();
            _ldAura = null;
        }

        private void TickLightningDance(float dt)
        {
            if (_ldPoseHold > 0f)
            {
                _ldPoseHold -= dt;
                if (_ldPoseHold <= 0f) LdReleasePose();
            }
            if (!_ldActive) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "amazon") { StopLightningDance(); return; }
            _ldAura?.MoveTo(me.Position);
            if (_ldDashing) { TickLdDash(me, dt); return; }
            _ldTimer -= dt;
            if (_ldSlamPending) LdHop(me, 1f - Mathf.Clamp01(_ldTimer / LdSlamDelay));
            if (_ldTimer > 0f) return;

            if (!_ldSlamPending && _ldStep < LdMaxTargets)
            {
                var next = NextLdTarget(me);
                if (next != null)
                {
                    BeginLdDash(me, next);
                    _ldStep++;
                    return;
                }
            }
            // 주변에 적이 없으면 숨 고르지 않고 바로 — 헛돌지 않는다. 쳤으면 숨 한 번 쉬고 내려찍는다
            if (!_ldSlamPending && _ldStep > 0)
            {
                _ldSlamPending = true;
                _ldTimer = LdSlamDelay;
                LdPose(me, LdPoseSlam);
                return;
            }
            LdSlam(me);
            StopLightningDance();
            // 멈춘 자세는 스킬이 끝난 뒤에도 잠깐 남긴다(StopLightningDance 가 풀었으니 다시 쥔다)
            if (LdPose(me, LdPoseSlam)) _ldPoseHold = LdSlamPoseHold;
        }

        /// <summary>
        /// 바라보는 쪽에 맞는 스킬 자세를 몸에 쥐여 준다. 옆이면 오른쪽 그림(왼쪽은 뒤집기), 아니면 앞 · 뒤.
        /// 그림이 없으면 false — 원작 공격 동작 그대로 간다.
        /// </summary>
        private bool LdPose(Unit me, string pose)
        {
            var f = me.Facing;
            string dir = Mathf.Abs(f.x) >= Mathf.Abs(f.y) * 0.7f ? "e" : (f.y > 0f ? "n" : "s");
            var sprite = UnitGet(me.Key, $"{dir}_{pose}");
            if (sprite == null) return false;
            me.SetSpriteOverride(sprite, dir == "e" && f.x < 0f);
            _ldBody = me;
            _ldPoseHold = 0f;
            return true;
        }

        /// <summary>뛰어올라(빠르게) · 잠깐 떠 있다가 · 내리꽂는다. k = 0 → 1.</summary>
        private void LdHop(Unit me, float k)
        {
            float lift = k < 0.7f
                ? Game.Module.Common.UI.Ease.OutCubic(k / 0.7f)
                : 1f - (k - 0.7f) / 0.3f * ((k - 0.7f) / 0.3f);
            me.SetSpriteLift(LdSlamHopPixels * lift);
            _ldBody = me;
        }

        private void LdReleasePose()
        {
            _ldPoseHold = 0f;
            if (_ldBody != null) { _ldBody.SetSpriteOverride(null); _ldBody.SetSpriteLift(0f); }
            _ldBody = null;
        }

        /// <summary>아직 안 친 적 중 가장 가까운 놈. 다 쳤으면 null.</summary>
        private Unit NextLdTarget(Unit me)
        {
            Unit best = null;
            float bestD = Meters(LdRadiusMeters);
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || _ldHit.Contains(e) || !IsOnScreen(e)) continue;
                float d = Vector2.Distance(me.Position, e.Position);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        /// <summary>다음 적에게 미끄러져 가기 시작한다. 번개 줄기는 출발하는 순간 길을 먼저 긋는다.</summary>
        private void BeginLdDash(Unit me, Unit target)
        {
            _ldFrom = me.Position;
            var dir = target.Position - _ldFrom;
            _ldTo = ClampedInField(me, target.Position - (dir.sqrMagnitude > 0.01f ? dir.normalized : me.Facing)
                                         * Meters(LdStandOffMeters));
            _ldTarget = target;
            _ldDashT = 0f;
            _ldTrailTick = 0f;
            _ldDashing = true;
            _ldHit.Add(target);
            me.SetFacing(target.Position - _ldFrom);
            LdPose(me, LdPoseDash);
            LdBolt(_ldFrom, _ldTo);
        }

        private void TickLdDash(Unit me, float dt)
        {
            _ldDashT += dt;
            float k = Mathf.Clamp01(_ldDashT / LdDashSeconds);
            // 처음에 확 나가고 끝에서 멈춘다 — 등속이면 미끄러지는 것이 아니라 끌려가 보인다
            me.Position = Vector2.Lerp(_ldFrom, _ldTo, Game.Module.Common.UI.Ease.OutCubic(k));
            _ldTrailTick -= dt;
            if (_ldTrailTick <= 0f)
            {
                _ldTrailTick = LdTrailEvery;
                LdTrail(me);
            }
            if (k < 1f) return;
            _ldDashing = false;
            _ldTimer = LdPauseSeconds;
            LdArrive(me, _ldTarget);
        }

        /// <summary>몸 그림 그대로 하늘빛 잔상을 한 장 남긴다(새 그림이 아니라 원작 몸의 실루엣).</summary>
        private void LdTrail(Unit me)
        {
            var sprite = me.BodySprite;
            if (sprite == null) return;
            var img = RentAfterimage();
            if (img == null) return;
            img.Play(sprite, me.Position, me.GetComponent<RectTransform>().sizeDelta, me.BodyFlipX,
                     LdTrailAlpha, LdTrailLife, LdTrailTint);
        }

        private void LdArrive(Unit me, Unit target)
        {
            if (target != null && target.IsAlive) me.SetFacing(target.Position - me.Position);
            if (!LdPose(me, LdPoseHit)) me.PlayAttack();
            if (target == null || !target.IsAlive) return;   // 가는 사이 다른 것이 잡았다 — 헛치지 않고 다음으로
            HitEnemyWith(target, SkillDamage(me, LdHitMul * BaseAxis(1f)), me.Profile);
            var spark = PlayFx("ldspark", target.Position, LdSparkSize, loop: false);
            if (spark != null) spark.SetFrameSeconds(0.045f);
            else PlayFx("bolt", target.Position, 72f, loop: false);
            HitStop(LdHitStopSeconds);
        }

        private void LdSlam(Unit me)
        {
            me.SetSpriteLift(0f);
            var at = me.Position;
            float r = Meters(LdSlamRadiusMeters) * _buffs.AoeMul;
            int dmg = SkillDamage(me, LdSlamMul * BaseAxis(1f));
            var list = EnemiesInRange(at, r);
            for (int i = 0; i < list.Count; i++)
            {
                HitEnemyWith(list[i], dmg, me.Profile);
                // 시안처럼 둘레 적이 바깥으로 튕겨 나간다 — 보스는 안 민다
                if (list[i] != null && list[i].IsAlive && !list[i].IsBoss) Knockback(list[i], LdSlamKnockMeters);
            }
            // 하늘 번개 기둥 + 바닥 번개 고리(시안 오른쪽 컷) — 고리가 칸 아래 1/4 에 있어 그만큼 올려 발밑에 맞춘다.
            // ⚠ 예전엔 흙먼지 충격파(SpawnShockwave)를 썼다 — 번개 스킬 끝에 흙구름이 나와 시안과 반대였다
            float h = r * LdSlamHeightPerRadius;
            var slam = PlayFx("ldslam", at + new Vector2(0f, h * 0.35f), h, loop: false);
            if (slam != null) slam.SetFrameSeconds(0.06f);
            // 발밑에 크게 터지는 번개 섬광 — 시안 오른쪽 컷의 바닥 충격파
            var burst = PlayFx("ldspark", at, r * LdSlamBurstPerRadius, loop: false);
            if (burst != null) burst.SetFrameSeconds(0.06f);
            else { LdBolt(at + new Vector2(0f, Meters(5f)), at); SpawnShockwave(at, r * 2f); }
            if (!LdPose(me, LdPoseSlam)) me.PlayAttack();
            Shake(10f);
            HitStop(LdSlamHitStopSeconds);
        }

        /// <summary>번개 줄기 — 청룡 퀄업의 지그재그 번개를 빌려 쓴다. 그림이 없으면 기본 줄기.</summary>
        private void LdBolt(Vector2 from, Vector2 to)
        {
            var frames = FxFrames("ldbolt") ?? FxFrames("dsbolt") ?? FxFrames("boltbeam");
            if (frames == null) { PlayFx("bolt", to, 64f, loop: false); return; }
            var im = FreeImpact(LdBoltThickness);
            im?.PlayBeam(from, to, frames, LdBoltThickness);
            im?.SetFrameSeconds(LdBoltFrameSeconds);
        }
    }
}
