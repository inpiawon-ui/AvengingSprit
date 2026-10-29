using System.Collections.Generic;
using Game.Module.Common.UI;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 호스트 스킬 다시 잡기 (PD 2026-10-29) — 기획 `Projects/AVSR/_exchange/plan_party_skills.md` 「23명 새 스킬」.
    ///
    /// 빙의 파티에서 액티브는 **보조가 자동으로도 쓰는 스킬**이다. 자기 몸에만 거는 액티브 7개
    /// (아마존 정예 · 호퍼 · 코만도(기관총) · 구루 · 닌자 · 설녀 · 슬러거)는 보조가 쓰면 아무 일도 없어서
    /// **적에게 닿는 스킬**로 바꿨다. 메인에게만 붙는 덤(쉴드 · 무적)은 `me == _host` 로 가른다.
    /// 여기 있는 것은 바뀐 액티브 · 코만도(기관총) 필살기 「탄막 요새」다. 바뀐 패시브는 `BattleDirector.HostPassives.cs`.
    ///
    /// 그림은 새로 받지 않았다 — 있는 부품(`njslash` · `dbstrike` · `lzbeam` · `castring` · `guard` · `freeze` …)과 파티클을 다시 쓴다.
    ///
    /// 성급 성장: 액티브 표 `_scaling` 기본 축은 **예전 스킬 값**이 그대로 들어 있다(예: 정조준 5 → 7.5초).
    /// 새 스킬은 그 축의 **오른 비율**(`GrowRatio`, Lv1 = 1)만 빌려 제 수치에 곱한다 — 표를 다시 굽지 않아도 된다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ═══════════════════════════════════════════════════════════
        //  수치 — 전부 PD 2026-10-29 시험값
        // ═══════════════════════════════════════════════════════════

        // 갱스터 · 일제 표식 — 체력 높은 적 셋 · 4초 · 받는 피해 +30%
        private const int GangMarkTargets = 3;
        private const float GangMarkSeconds = 4f;
        private const int GangMarkPercent = 30;
        private const float GangMarkTableBase = 3f;          // 표 축(예전 표식 3초)

        // 호퍼 · 조준 사격 — 가장 먼 적에게 확정 치명 3발
        private const int AimShotCount = 3;
        private const int AimShotSpecCount = 4;              // 각성 옵션 — 4발
        private const float AimShotMul = 1.2f;               // 한 발(치명 배율은 따로 곱한다)
        private const float AimShotGap = 0.14f;
        private const float AimShotTravel = 0.1f;
        private const float AimShotTableBase = 5f;           // 표 축(예전 정조준 5초)
        private static readonly Color AimMarkTint = new(1f, 0.35f, 0.3f, 1f);

        // 호퍼(기관단총) · 백스텝 사격의 덤 — 쏜 쪽 부채꼴의 적을 밀어낸다
        private const float SmgKnockMeters = 1.5f;
        private const float SmgKnockRangeMeters = 7f;
        private const float SmgKnockDelay = 0.1f;            // 탄이 닿을 무렵에 민다
        private const float SmgKnockHoldSeconds = 0.25f;

        // 코만도(기관총) · 제압 사격 — 앞쪽 부채꼴 2초 연사 · 맞은 적 둔화 30%
        private const float SuppressSeconds = 2f;
        private const float SuppressGap = 0.08f;
        private const float SuppressConeDeg = 50f;
        private const float SuppressRangeMeters = 7f;
        private const float SuppressShotMul = 0.3f;          // 한 발 — 25발이 부채꼴로 흩어진다
        private const int SuppressSlowPercent = 30;
        private const int SuppressSpecSlowPercent = 50;      // 각성 옵션
        private const float SuppressSlowSeconds = 1f;
        private const int SuppressSlowEvery = 5;             // 다섯 발(0.4초)마다 부채꼴 안을 다시 늦춘다
        private const float SuppressTableBase = 1f;          // 표 축(예전 방벽 쉴드 100%)

        // 코만도(기관총) · 필살기 탄막 요새 — 3초 제자리 고정 · 받는 피해 −70% · 사방 연사
        private const float FortressSeconds = 3f;
        private const float FortressDamageCut = 0.7f;
        // 거치 기관포 — 앞 60°(±30°)를 1.2초에 한 번 왕복 · 0.07초마다 관통 탄줄(3초 ≈ 43줄)
        private const float FortressVolleyGap = 0.07f;
        private const float FortressHalfDeg = 30f;
        private const float FortressSweepSeconds = 1.2f;
        private const float FortressRangeMeters = 9f;
        private const float FortressWidthMeters = 0.45f;
        private const float FortressShotMul = 0.32f;
        private const int FortressSlowPercent = 35;
        private const float FortressSlowSeconds = 0.8f;
        private const float FortressTracerDraw = 46f;
        private const float FortressMuzzleSize = 120f;
        private const float FortressHitSize = 84f;
        private const float FortressDustSize = 220f;
        private const float FortressShellEvery = 0.09f;
        private const float FortressShellSize = 26f;

        // 코만도(레이저) · 충전 레이저 — 1초 모은 뒤 일직선 관통
        private const float ChargeLaserSeconds = 1f;
        private const float ChargeLaserMeters = 12f;
        private const float ChargeLaserWidthMeters = 1f;
        private const float ChargeLaserSpecWidthMeters = 1.5f;   // 각성 옵션 — 폭 1.5배
        private const float ChargeLaserMul = 3.5f;
        private const float ChargeLaserTableBase = 3f;      // 표 축(예전 연쇄 방전 3초)

        // 아마존 정예 · 전장의 함성 — 둘레 밀쳐내기 + 1.5초 기절 · 메인이면 쉴드(최대 체력 20%)
        private const float WarCryMeters = 3f;
        private const float WarCryPushMeters = 1.5f;
        private const float WarCryStunSeconds = 1.5f;
        private const float WarCrySpecStunSeconds = 2.5f;    // 각성 옵션
        private const float WarCryShieldPercent = 0.2f;
        private const float WarCryTableBase = 2.5f;         // 표 축(예전 불굴 2.5초)

        // 닌자 · 그림자 베기 — 가까운 적 셋 사이를 순간이동하며 벤다
        private const int ShadowCutTargets = 3;
        private const int ShadowCutSpecTargets = 5;          // 각성 옵션
        private const float ShadowCutRangeMeters = 7f;
        private const float ShadowCutGap = 0.14f;
        private const float ShadowCutBehindMeters = 0.7f;    // 적의 등 뒤로 나타난다
        private const float ShadowCutMul = 2f;
        private const float ShadowCutTableBase = 0.5f;      // 표 축(예전 몸놀림)

        // 슬러거 · 라인 드라이브 — 관통 강타구 · 맞은 적을 멀리 날리고, 날아간 적에 부딪힌 적도 피해
        private const float LineDriveMeters = 9f;
        private const float LineDriveWidthMeters = 1.2f;
        private const float LineDriveMul = 2.5f;
        private const float LineDriveFlyMeters = 3f;
        private const float LineDriveSpecFlyMeters = 4.5f;   // 각성 옵션 — 1.5배 멀리
        private const float LineDriveFlySeconds = 0.3f;
        private const int LineDriveFlySteps = 6;
        private const float LineDriveBumpMeters = 1f;
        private const float LineDriveBumpMul = 1f;
        private const float LineDriveTableBase = 5f;        // 표 축(예전 전탄 반사 5초)

        // 청룡 · 뇌전 — 둘레 적 다섯에게 낙뢰 한 번씩
        private const int ThunderTargets = 5;
        private const int ThunderSpecTargets = 8;            // 각성 옵션
        private const float ThunderRangeMeters = 6f;
        private const float ThunderGap = 0.07f;
        private const float ThunderMul = 1.5f;
        private const float ThunderTableBase = 2f;          // 표 축(예전 뇌전 폭주 2초)

        // 구루 · 진압 결계 — 둘레 3 m 적을 2초 묶는다
        private const float BindWardMeters = 3f;
        private const float BindWardSeconds = 2f;
        private const float BindWardSpecSeconds = 3f;        // 각성 옵션
        private const float BindWardTableBase = 0.5f;       // 표 축(예전 수호 결계 −50%)
        private static readonly Color BindWardTint = new(1f, 0.85f, 0.35f, 1f);

        // 설녀 · 눈보라 — 둘레 적 1.5초 빙결
        private const float BlizzardMeters = 3.5f;
        private const float BlizzardSeconds = 1.5f;
        private const float BlizzardSpecSeconds = 2.5f;      // 각성 옵션
        private const float BlizzardTableBase = 1.25f;      // 표 축(예전 얼음 감옥 1.25초)

        // 아마존 · 패시브 질주 — 도약 강타로 처치하면 한 번 더 도약
        private const float AmazonSprintDelay = 0.18f;

        // ═══════════════════════════════════════════════════════════
        //  공용
        // ═══════════════════════════════════════════════════════════

        /// <summary>표 성장 축이 Lv1 에서 얼마나 올랐나(Lv1 = 1). 표가 비었으면 1.</summary>
        private float GrowRatio(float tableBase) => tableBase > 0f ? BaseAxis(tableBase) / tableBase : 1f;

        /// <summary>시전자가 아직 싸우는가 — 메인이거나 빙의 파티의 보조. 예약된 타격이 이것을 보고 멈춘다.</summary>
        private bool IsLiveCaster(Unit me)
        {
            if (me == null || !me.IsAlive) return false;
            if (me == _host) return true;
            for (int i = 0; i < _subs.Count; i++) if (_subs[i].U == me) return true;
            return false;
        }

        /// <summary>화면 안에서 체력이 높은 적부터 <paramref name="count"/> 명(시전 순간 한 번 — 새 목록).</summary>
        private List<Unit> ToughestEnemies(int count)
        {
            var list = new List<Unit>();
            for (int i = 0; i < _enemies.Count; i++)
                if (Targetable(_enemies[i]) && IsOnScreen(_enemies[i])) list.Add(_enemies[i]);
            list.Sort((a, b) => b.Hp.CompareTo(a.Hp));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        /// <summary>화면 안에서 가장 먼 적.</summary>
        private Unit FarthestEnemy(Vector2 from)
        {
            Unit far = null;
            float best = -1f;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || !IsOnScreen(e)) continue;
                float d = (e.Position - from).sqrMagnitude;
                if (d > best) { best = d; far = e; }
            }
            return far;
        }

        /// <summary><paramref name="from"/> 에서 밀어낸다. 보스는 안 밀린다.</summary>
        private void PushFrom(Unit e, Vector2 from, float meters, float holdSeconds)
        {
            if (e == null || !e.IsAlive || e.IsBoss) return;
            var away = e.Position - from;
            if (away.sqrMagnitude < 0.01f) return;
            e.Position = ClampedInField(e, e.Position + away.normalized * Meters(meters));
            e.CancelWindup();
            if (holdSeconds > 0f) e.HoldHit(holdSeconds);
        }

        private void TickSkills1029(float dt)
        {
            TickFortress(dt);
            TickInyeonSkills(dt);   // 인연 — 레이저 포탑(BattleDirector.SkillsInyeon)
        }

        /// <summary>몸을 갈아입거나 방을 나갈 때(`ClearSkillState`).</summary>
        private void ClearSkills1029()
        {
            EndFortress();
            _wrapUntil.Clear();
            _amazonSprintReady = false;
            ClearInyeonSkills();
        }

        // ═══════════════════════════════════════════════════════════
        //  호퍼 · 조준 사격
        // ═══════════════════════════════════════════════════════════

        private void HopperAimedShot(Unit me)
        {
            var far = FarthestEnemy(me.Position);
            if (far == null) { PlayFx("crit", me.Position, 96f, loop: false); return; }
            int shots = SpecOpen ? AimShotSpecCount : AimShotCount;
            if (InyeonOn(me)) shots *= IyAimShotMul;   // 인연 「쌍권총」 — 3 → 6발
            // 확정 치명 — 치명 배율(+ 급소 패시브)을 미리 곱한다. 숫자 색은 일반 피해로 뜬다(스킬 피해 길)
            int dmg = Mathf.Max(1, Mathf.RoundToInt(SkillDamage(me, AimShotMul * GrowRatio(AimShotTableBase))
                                                   * (CritMultiplier + CritDamageBonus)));
            var mark = PlayFloorFx("castring", far.Position, 120f, loop: false);
            if (mark != null) { mark.SetTint(AimMarkTint); mark.SetFrameSeconds(0.05f); }
            for (int i = 0; i < shots; i++)
            {
                UltAfter(i * AimShotGap, () =>
                {
                    if (!IsLiveCaster(me)) return;
                    var t = far.IsAlive ? far : FarthestEnemy(me.Position);
                    if (t == null) return;
                    me.SetFacing(t.Position - me.Position);
                    me.PlayAttack();
                    _pfx?.Dart(me.MuzzlePosition, t.Position, ParticleElement.Fire, AimShotTravel, 2);
                    PlayFx("muzzle", me.MuzzlePosition, 56f, loop: false);
                    UltAfter(AimShotTravel, () =>
                    {
                        if (t == null || !t.IsAlive) return;
                        HitEnemyWith(t, dmg, me.Profile);
                        PlayFx("crit", t.Position, 110f, loop: false);
                        Shake(2.5f);
                    });
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  호퍼(기관단총) · 백스텝 사격의 넉백
        // ═══════════════════════════════════════════════════════════

        private void SmgKnockFan(Unit me, Vector2 from, Vector2 dir, float spanDeg)
        {
            UltAfter(SmgKnockDelay, () =>
            {
                if (!IsLiveCaster(me)) return;
                var cone = new List<Unit>(EnemiesInCone(from, dir, spanDeg, Meters(SmgKnockRangeMeters)));
                for (int i = 0; i < cone.Count; i++) PushFrom(cone[i], me.Position, SmgKnockMeters, SmgKnockHoldSeconds);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(기관총) · 제압 사격
        // ═══════════════════════════════════════════════════════════

        private void SuppressFire(Unit me)
        {
            var dir = AimDirection(me);
            int steps = Mathf.RoundToInt(SuppressSeconds / SuppressGap);
            int dmg = SkillDamage(me, SuppressShotMul * GrowRatio(SuppressTableBase));
            int slow = SpecOpen ? SuppressSpecSlowPercent : SuppressSlowPercent;
            float half = SuppressConeDeg * 0.5f;
            bool iy = InyeonOn(me);   // 인연 「엄호 화망」 — 맞는 적 발밑에 수류탄
            int iyGrenade = iy ? SkillDamage(me, IyCoverGrenadeMul) : 0;
            for (int s = 0; s < steps; s++)
            {
                bool slowTick = s % SuppressSlowEvery == 0;
                UltAfter(s * SuppressGap, () =>
                {
                    if (!IsLiveCaster(me)) return;
                    // 앞의 적을 따라 총구를 돌린다 — 적이 없으면 처음 겨눈 쪽
                    var near = NearestEnemy(me.Position, Meters(SuppressRangeMeters));
                    var aim = near != null ? near.Position - me.Position : dir;
                    aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
                    me.SetFacing(aim);
                    me.PlayAttack();
                    var shot = Rotate(aim, Random.Range(-half, half));
                    FireFan(me, me.Position + shot * Meters(SuppressRangeMeters), 1, 0f, dmg, fromPlayer: true);
                    PlayFx("muzzle", me.MuzzlePosition, 56f, loop: false);
                    if (!slowTick) return;
                    var cone = EnemiesInCone(me.Position, aim, SuppressConeDeg, Meters(SuppressRangeMeters));
                    for (int i = 0; i < cone.Count; i++) cone[i].ApplySlow(slow, SuppressSlowSeconds);
                    if (iy && cone.Count > 0)
                        ThrowSkillGrenade(me, cone[Random.Range(0, cone.Count)].Position, iyGrenade, Meters(IyCoverGrenadeMeters));
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(기관총) · 필살기 거치 기관포 (PD 2026-10-29 시안 1-A `_exchange/in/mock1029_ult_mg_a.png`)
        //  바닥에 거치하고 **앞쪽 60° 부채꼴만** 좌우로 훑으며 굵은 관통 탄줄을 3초 — 맞은 적은 둔해진다.
        //  폭력배 「회전 난사」(제자리 사방)와 한눈에 달라야 한다 — 사방 · 원형 탄막을 쓰지 않는다.
        //  그림: 탄줄 `mgtracer` · 총구 `mgmuzzle` · 맞은 자리 `mghit` · 흙먼지 `mgdust` · 탄피 `mgshell`(없으면 지금 그림으로 돈다)
        // ═══════════════════════════════════════════════════════════

        private float _fortressSeconds;
        private float _fortressVolley;
        private float _fortressAngle;     // 훑는 가운데(처음 겨눈 쪽)
        private float _fortressSweep;     // 훑기 시계
        private float _fortressShell;
        private Vector2 _fortressAt;
        private Impact _fortressFx;

        private void MgFortress(Unit me)
        {
            _fortressSeconds = FortressSeconds;
            _fortressAt = me.Position;
            _fortressVolley = 0f;
            _fortressSweep = 0f;
            _fortressShell = 0f;
            // 가장 가까운 적 쪽을 가운데로 — 없으면 바라보던 쪽
            var t = NearestEnemy(me.Position, Meters(FortressRangeMeters));
            var f = t != null ? t.Position - me.Position : me.Facing;
            if (f.sqrMagnitude < 0.0001f) f = Vector2.up;
            _fortressAngle = Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg;
            _fortressFx?.Stop();
            _fortressFx = null;
            // 거치(강) — 발밑 흙먼지가 확 퍼지고 화면이 한 번 크게 흔들린다
            if (PlayFx("mgdust", me.Position + new Vector2(0f, -18f), FortressDustSize, loop: false) is Impact dust) dust.SetFrameSeconds(0.08f);
            _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, me.Position, 10, 180f, 36f, 0.55f, 0f);
            Shake(5f);
        }

        /// <summary>거치한 동안 — 걷는 입력을 되돌려 제자리에 박고, 부채꼴을 좌우로 훑으며 0.07초마다 관통 탄줄 한 줄.</summary>
        private void TickFortress(float dt)
        {
            if (_fortressSeconds <= 0f) return;
            var me = _host;
            if (me == null || !me.IsAlive || me.Key != "commando_mg") { EndFortress(); return; }
            _fortressSeconds -= dt;
            // 걷기(`TickPlayer`)가 이보다 먼저 돈다 — 옮겨진 자리를 그 프레임 안에 되돌린다
            me.Position = _fortressAt;

            _fortressSweep += dt;
            float ang = _fortressAngle + FortressHalfDeg * Mathf.Sin(_fortressSweep * Mathf.PI * 2f / FortressSweepSeconds);
            float rad = ang * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            me.SetFacing(dir);

            _fortressVolley -= dt;
            if (_fortressVolley <= 0f)
            {
                _fortressVolley += FortressVolleyGap;
                me.PlayAttack();
                var from = me.MuzzlePosition;
                float length = Meters(FortressRangeMeters);
                var to = from + dir * length;
                var frames = FxFrames("mgtracer");
                if (frames != null)
                {
                    var beam = FreeImpact(FortressTracerDraw);
                    beam?.PlayBeam(from, to, frames, FortressTracerDraw);
                    beam?.SetLoop(true);   // 한 장짜리라 돌리지 않으면 바로 끝났다 — 수명(SetLife)이 끝을 정한다(레이저 빔과 같은 방식)
                    // 시안처럼 탄줄 네댓 줄이 부채꼴로 겹쳐 보이게 — 한 줄씩만 보이면 레이저 하나처럼 읽혔다(녹화 검수 2026-10-29)
                    beam?.SetLife(FortressVolleyGap * 5f, 0.18f);
                }
                else UltBeam("laser_wide", from, to, FortressTracerDraw * 0.4f, 0.04f);
                (PlayFx("mgmuzzle", from + dir * 18f, FortressMuzzleSize, loop: false)
                    ?? PlayFx("muzzle", from + dir * 18f, FortressMuzzleSize * 0.6f, loop: false))?.SetFrameSeconds(0.03f);
                int dmg = UltDamage(me, FortressShotMul);
                var hit = new List<Unit>(EnemiesInPath(from, dir, length, Meters(FortressWidthMeters)));
                for (int i = 0; i < hit.Count; i++)
                {
                    var e = hit[i];
                    if (e == null || !e.IsAlive) continue;
                    var at = e.Position;
                    HitEnemyWith(e, dmg, me.Profile);
                    e.ApplySlow(FortressSlowPercent, FortressSlowSeconds);
                    (PlayFx("mghit", at, FortressHitSize, loop: false) ?? PlayFx("hit", at, FortressHitSize * 0.7f, loop: false))?.SetFrameSeconds(0.04f);
                }
                Shake(1.2f);
            }

            // 탄피 — 몸 옆으로 쉴 새 없이 튄다(약 · 계속)
            _fortressShell -= dt;
            if (_fortressShell <= 0f)
            {
                _fortressShell += FortressShellEvery;
                var side = new Vector2(-dir.y, dir.x) * Random.Range(14f, 34f) + Random.insideUnitCircle * 8f;
                (PlayFx("mgshell", me.Position + side - new Vector2(0f, 16f), FortressShellSize, loop: false)
                    ?? PlayFx("thshell", me.Position + side - new Vector2(0f, 16f), FortressShellSize, loop: false))?.SetFrameSeconds(0.06f);
            }
            if (_fortressSeconds <= 0f) EndFortress();
        }

        private void EndFortress()
        {
            _fortressSeconds = 0f;
            _fortressFx?.Stop();
            _fortressFx = null;
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(레이저) · 충전 레이저
        // ═══════════════════════════════════════════════════════════

        private void ChargeLaser(Unit me)
        {
            var t = NearestEnemy(me.Position, Meters(ChargeLaserMeters));
            var dir = t != null ? t.Position - me.Position : AimDirection(me);
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            me.SetFacing(dir);
            var charge = PlayFx("lzcharge", me.MuzzlePosition, SsrBody * 1.2f, loop: false)
                      ?? PlayFx("wfgather", me.MuzzlePosition, 120f, loop: false);
            charge?.SetFrameSeconds(0.085f);
            _pfx?.Spray(ParticleFxKind.Streak, me.MuzzlePosition + dir * 60f, -dir, 70f, 10, 120f, 260f,
                        new Vector2(30f, 4f), ChargeLaserSeconds * 0.8f, new Color(0.5f, 0.9f, 1f, 1f));
            float width = Meters(SpecOpen ? ChargeLaserSpecWidthMeters : ChargeLaserWidthMeters) * _buffs.AoeMul;
            int dmg = SkillDamage(me, ChargeLaserMul * GrowRatio(ChargeLaserTableBase));
            bool iy = InyeonOn(me);   // 인연 「과부하」 — 레이저 두 줄
            UltAfter(ChargeLaserSeconds, () =>
            {
                if (!IsLiveCaster(me)) return;
                // 모으는 사이 적이 움직였다 — 처음 겨눈 적이 살아 있으면 그쪽으로 다시 맞춘다
                var aim = dir;
                if (t != null && t.IsAlive)
                {
                    var d = t.Position - me.Position;
                    if (d.sqrMagnitude > 1f) aim = d.normalized;
                }
                me.SetFacing(aim);
                me.PlayAttack();
                var from = me.MuzzlePosition;
                float length = Meters(ChargeLaserMeters);
                PrismBeam(from, from + aim * length, LzBeamDraw * (width / Meters(ChargeLaserWidthMeters)), 0.35f);
                var hit = new List<Unit>(EnemiesInPath(from, aim, length, width));
                for (int i = 0; i < hit.Count; i++)
                {
                    var at = hit[i].Position;
                    HitEnemyWith(hit[i], dmg, me.Profile);
                    PlayFx("lzhit", at, 90f, loop: false);
                }
                if (iy) IySecondLaser(me, from, aim, t, length, width, dmg);
                Shake(6f);
                HitStop(0.05f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  아마존 정예 · 전장의 함성
        // ═══════════════════════════════════════════════════════════

        private void EliteWarCry(Unit me)
        {
            float r = Meters(WarCryMeters) * _buffs.AoeMul;
            float stun = SpecOpen ? WarCrySpecStunSeconds : WarCryStunSeconds;
            if (InyeonOn(me))   // 인연 「불굴의 함성」 — 범위 1.5배 · 기절 2.5초
            {
                r *= IyWarCryRangeMul;
                stun = Mathf.Max(stun, IyWarCryStunSeconds);
            }
            var list = new List<Unit>(EnemiesInRange(me.Position, r));
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;
                PushFrom(e, me.Position, WarCryPushMeters, 0f);
                if (!e.IsBoss) e.ApplyStun(stun);
            }
            // 메인이면 쉴드 — 보조가 외치면 적만 밀린다
            if (me == _host)
            {
                int amount = Mathf.Max(1, Mathf.RoundToInt(me.HpMax * WarCryShieldPercent * GrowRatio(WarCryTableBase)));
                me.AddShield(amount, me.HpMax);
                PlayFx("guard", me.Position, Meters(1.6f), loop: false);
            }
            me.PlayAttack();
            PlayFx("slam", me.Position, r * 2f, loop: false);
            _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, me.Position, 10, 240f, 40f, 0.5f, 0f);
            Shake(6f);
            HitStop(0.05f);
        }

        // ═══════════════════════════════════════════════════════════
        //  닌자 · 그림자 베기
        // ═══════════════════════════════════════════════════════════

        private void NinjaShadowCut(Unit me)
        {
            var near = UltTargets(me.Position, Meters(ShadowCutRangeMeters));
            if (near.Count == 0) { PlayFx("njsmoke", me.Position, SsrBody * 1.4f, loop: false); return; }
            int n = Mathf.Min(SpecOpen ? ShadowCutSpecTargets : ShadowCutTargets, near.Count);
            int dmg = SkillDamage(me, ShadowCutMul * GrowRatio(ShadowCutTableBase));
            // 메인은 순간이동하는 동안 안 맞는다(스킬 무적 윤곽)
            if (me == _host) UltInvuln(n * ShadowCutGap + 0.2f);
            PlayFx("njsmoke", me.Position, SsrBody * 1.6f, loop: false);
            if (InyeonOn(me)) IyShadowClone(me, n * ShadowCutGap + 0.1f);   // 인연 「그림자 분신」 — 베기 끝에 분신 4초
            for (int i = 0; i < n; i++)
            {
                var e = near[i];
                UltAfter(i * ShadowCutGap, () =>
                {
                    if (!IsLiveCaster(me) || e == null || !e.IsAlive) return;
                    var from = me.Position;
                    var side = e.Position - from;
                    var behind = e.Position + (side.sqrMagnitude > 1f ? side.normalized : me.Facing)
                                 * Meters(ShadowCutBehindMeters);
                    var to = ClampedInField(me, behind);
                    SpawnAfterimages(me, from, to);
                    me.Position = to;
                    me.SetFacing(e.Position - to);
                    me.PlayAttack();
                    HitEnemyWith(e, dmg, me.Profile);
                    PlayFx("njslash", e.Position, SsrBody * 1.3f, loop: false)?.SetFrameSeconds(0.04f);
                    Shake(3f);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  슬러거 · 라인 드라이브
        // ═══════════════════════════════════════════════════════════

        private void SluggerLineDrive(Unit me)
        {
            var dir = AimDirection(me);
            me.SetFacing(dir);
            me.PlayAttack();
            var from = me.Position;
            float length = Meters(LineDriveMeters);
            float width = Meters(LineDriveWidthMeters) * _buffs.AoeMul;
            float grow = GrowRatio(LineDriveTableBase);
            int dmg = SkillDamage(me, LineDriveMul * grow);
            int bump = SkillDamage(me, LineDriveBumpMul * grow);
            float fly = SpecOpen ? LineDriveSpecFlyMeters : LineDriveFlyMeters;

            // 타구 — 흰 빛줄이 일직선으로 뻗는다
            PlayFx("rfswing", from, 120f, loop: false);
            _pfx?.Spray(ParticleFxKind.Streak, from, dir, 4f, 12, 900f, 1400f, new Vector2(40f, 6f), 0.25f,
                        new Color(1f, 0.95f, 0.85f, 1f));
            _pfx?.Dart(from, from + dir * length, ParticleElement.Fire, 0.18f, 4);

            var hit = new List<Unit>(EnemiesInPath(from, dir, length, width));
            var bumped = new HashSet<Unit>(hit);   // 타구에 맞은 적은 부딪혀 다시 맞지 않는다
            bool iy = InyeonOn(me);   // 인연 「얼음 장타」 — 맞은 적이 언다
            for (int i = 0; i < hit.Count; i++)
            {
                var e = hit[i];
                HitEnemyWith(e, dmg, me.Profile);
                PlayFx("crit", e.Position, 96f, loop: false);
                if (e.IsAlive && !e.IsBoss) LaunchDrive(me, e, dir, fly, bump, bumped);
                if (iy) IyIceDrive(e);
            }
            Shake(5f);
            HitStop(0.05f);
        }

        /// <summary>맞은 적이 날아간다. 날아가며 부딪힌 적도 맞는다(한 번씩).</summary>
        private void LaunchDrive(Unit me, Unit e, Vector2 dir, float meters, int bump, HashSet<Unit> bumped)
        {
            var start = e.Position;
            var end = ClampedInField(e, start + dir * Meters(meters));
            e.CancelWindup();
            for (int s = 1; s <= LineDriveFlySteps; s++)
            {
                float k = Ease.OutCubic(s / (float)LineDriveFlySteps);
                UltAfter(LineDriveFlySeconds * s / LineDriveFlySteps, () =>
                {
                    if (e == null || !e.IsAlive) return;
                    e.Position = Vector2.Lerp(start, end, k);
                    _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, e.Position, 1, 60f, 22f, 0.3f, 0f);
                    var near = new List<Unit>(EnemiesInRange(e.Position, Meters(LineDriveBumpMeters)));
                    for (int j = 0; j < near.Count; j++)
                    {
                        var o = near[j];
                        if (o == null || !o.IsAlive || !bumped.Add(o)) continue;
                        if (me != null) HitEnemyWith(o, bump, me.Profile);
                        PlayFx("crit", o.Position, 72f, loop: false);
                        Shake(2f);
                    }
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  청룡 · 뇌전
        // ═══════════════════════════════════════════════════════════

        private void DragonThunder(Unit me)
        {
            var list = UltTargets(me.Position, Meters(ThunderRangeMeters));
            if (list.Count == 0) { PlayFx("crit", me.Position, 96f, loop: false); return; }
            int n = Mathf.Min(SpecOpen ? ThunderSpecTargets : ThunderTargets, list.Count);
            int dmg = SkillDamage(me, ThunderMul * GrowRatio(ThunderTableBase));
            me.PlayAttack();
            PlayFx("dscoil", me.Position, SsrBody * 1.2f, loop: false);
            bool iy = InyeonOn(me);   // 인연 「번개 사슬」 — 묶인 적은 낙뢰를 두 번 맞는다
            for (int i = 0; i < n; i++)
            {
                var e = list[i];
                UltAfter(0.1f + i * ThunderGap, () =>
                {
                    if (!IsLiveCaster(me) || e == null || !e.IsAlive) return;
                    var strike = PlayFx("dbstrike", e.Position + new Vector2(0f, SsrBody * 1.1f), SsrBody * 2.2f, loop: false);
                    if (strike != null) strike.SetFrameSeconds(0.04f);
                    else PlayBolt(e.Position + new Vector2(0f, Meters(4f)), e.Position);
                    var at = e.Position;
                    HitEnemyWith(e, dmg, me.Profile);
                    if (!QxSurgeSpark(at)) SpawnImpact(at, "thunder", ChainImpactSize);
                    Shake(2.5f);
                    if (iy && e.IsAlive && (e.IsRooted || IsWrapped(e)))
                        UltAfter(IyThunderAgainGap, () =>
                        {
                            if (!IsLiveCaster(me) || e == null || !e.IsAlive) return;
                            var again = PlayFx("dbstrike", e.Position + new Vector2(0f, SsrBody * 1.1f), SsrBody * 2.2f, loop: false);
                            if (again != null) again.SetFrameSeconds(0.04f);
                            else PlayBolt(e.Position + new Vector2(0f, Meters(4f)), e.Position);
                            var p = e.Position;
                            HitEnemyWith(e, dmg, me.Profile);
                            SpawnImpact(p, "thunder", ChainImpactSize);
                            Shake(2.5f);
                        });
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  구루 · 진압 결계
        // ═══════════════════════════════════════════════════════════

        private void GuruBindWard(Unit me)
        {
            float r = Meters(BindWardMeters) * _buffs.AoeMul;
            float seconds = (SpecOpen ? BindWardSpecSeconds : BindWardSeconds) * GrowRatio(BindWardTableBase);
            var ring = PlayFloorFx("castring", me.Position, r * 2f, loop: false);
            if (ring != null) { ring.SetTint(BindWardTint); ring.SetFrameSeconds(0.05f); }
            PlayFx("guard", me.Position, WardFxSize * 1.6f, loop: false);
            _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Fire, me.Position, 10, 220f, 30f, 0.5f, 0f);
            me.PlayAttack();
            var list = new List<Unit>(EnemiesInRange(me.Position, r));
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;
                e.ApplyRoot(seconds);
                e.CancelWindup();
                PlayFx("guard", e.Position, 72f, loop: false);
            }
            if (InyeonOn(me)) IyBindSkullsNear(me, list);   // 인연 「결계 강령」 — 묶인 적 곁에 해골 2기
            Shake(3f);
        }

        // ═══════════════════════════════════════════════════════════
        //  설녀 · 눈보라
        // ═══════════════════════════════════════════════════════════

        private void SnowBlizzard(Unit me)
        {
            float r = Meters(BlizzardMeters) * _buffs.AoeMul;
            if (InyeonOn(me)) r *= IyBlizzardRangeMul;   // 인연 「얼음 진」 — 눈보라 범위 2배
            float seconds = (SpecOpen ? BlizzardSpecSeconds : BlizzardSeconds) * GrowRatio(BlizzardTableBase);
            me.PlayAttack();
            PlayFx("breath_ice", me.Position, r * 2f, loop: false);
            _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Ice, me.Position, 16, 300f, 30f, 0.7f, 0f);
            var list = new List<Unit>(EnemiesInRange(me.Position, r));
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;
                // 보스는 얼지 않는다(서릿발과 같은 규칙) — 대신 느려진다
                if (e.IsBoss) { e.ApplySlow(35, seconds); continue; }
                e.ApplyFreeze(seconds);
                PlayFx("freeze", e.Position, 72f, loop: false);
            }
            Shake(3f);
        }

        // ═══════════════════════════════════════════════════════════
        //  아마존 · 패시브 질주
        // ═══════════════════════════════════════════════════════════

        private bool _amazonSprintReady;

        /// <summary>도약 강타 — 메인이 쓰면 한 번 더 뛸 수 있게 장전한다(질주는 패시브라 메인만).</summary>
        private void AmazonLeapCast(Unit me)
        {
            _amazonSprintReady = me == _host;
            AmazonLeapStrike(me);
            if (InyeonOn(me)) IyLeapAgain(me);   // 인연 「번개 도약」 — 한 번 더 도약
        }

        /// <summary>도약 강타가 적을 잡았다 — 장전돼 있으면 가장 가까운 적에게 한 번 더 뛰어든다.</summary>
        private void AmazonSprint(Unit me)
        {
            if (!_amazonSprintReady || me != _host) return;
            _amazonSprintReady = false;
            UltAfter(AmazonSprintDelay, () =>
            {
                if (me != _host || !me.IsAlive || NearestEnemy(me.Position) == null) return;
                AmazonLeapStrike(me);
            });
        }
    }
}
