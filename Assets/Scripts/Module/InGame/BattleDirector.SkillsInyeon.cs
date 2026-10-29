using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 인연 — 메인일 때만 바뀌는 액티브 덤 (PD 2026-10-29).
    /// 표 · 문구는 `BattleDirector.PartyInyeon.cs` 의 `s_inyeon`(정본 `_exchange/plan_party_inyeon.md` + `plan_party_skills.md` 「인연 표와 맞추기」).
    ///
    /// 각 액티브 함수가 `bool iy = InyeonOn(me);` 로 갈라 아래 수치 · 도구를 쓴다. 새 스킬이 아니라 지금 액티브에 덤이다.
    /// `InyeonOn` 은 메인만 참이라 보조가 같은 스킬을 써도 인연형은 안 나온다.
    /// 그림은 새로 받지 않았다 — 있는 부품(`thunder` · `freeze` · `lzhit` · `burst` · `njsmoke` …)과 파티클을 다시 쓴다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ═══════════════════════════════════════════════════════════
        //  수치 — 전부 PD 2026-10-29 인연 시험값
        // ═══════════════════════════════════════════════════════════

        // 갱스터 「청부 표식」 — 표식이 박히면 보조가 그 적을 한 번씩 친다 · 보조 · 소환물 근접 피해 +50%
        private const float IyContractHitMul = 1.5f;          // 보조 한 방(보조 공격력 기준)
        private const float IyContractSubMul = 1.5f;          // 표식 적을 보조가 때릴 때
        private const float IyContractLead = 0.3f;            // 표식이 박힐 무렵

        // 폭력배 「뒷골목 난사」 — 탄이 가까운 적 쪽으로 살짝 휜다
        private const float IyThugHoming = 0.35f;

        // 아마존 「번개 도약」 — 한 번 더 도약 · 착지마다 번개 고리
        private const float IyLeapAgainDelay = 0.45f;
        private const float IyLeapRingMeters = 2.2f;
        private const float IyLeapRingMul = 1.2f;

        // 아마존 정예 「불굴의 함성」 — 범위 1.5배 · 기절 2.5초
        private const float IyWarCryRangeMul = 1.5f;
        private const float IyWarCryStunSeconds = 2.5f;

        // 호퍼 「쌍권총」 — 조준 사격 두 배(3 → 6발)
        private const int IyAimShotMul = 2;

        // 코만도(기관총) 「엄호 화망」 — 제압 사격 0.4초마다 맞는 적 발밑에 수류탄
        private const float IyCoverGrenadeMul = 0.8f;
        private const float IyCoverGrenadeMeters = 1.4f;

        // 코만도(레이저) 「과부하」 — 두 번째 줄은 다음 적, 없으면 이만큼 벌려 쏜다
        private const float IyLaserSplitDeg = 14f;

        // 코만도(수류탄) 「융단 폭격」 — 한 바퀴 더(바깥 원)
        private const float IyCarpetLapDelay = 0.45f;
        private const float IyCarpetLapRingMul = 1.6f;

        // 코만도(미사일) 「집속탄」 — 한 발마다 작은 폭발 셋
        private const int IyClusterCount = 3;
        private const float IyClusterSpreadMeters = 0.9f;
        private const float IyClusterBlastMeters = 0.8f;
        private const float IyClusterMul = 0.4f;
        private const float IyClusterDelay = 0.12f;
        private const float IyClusterTrackSeconds = 6f;       // 이보다 오래 안 터진 탄은 잊는다(풀에서 다시 쓴다)

        // 샐러맨더 「불독」 — 중독된 적이 쓰러지면 불꽃으로 터진다
        private const float IyVenomBurstMeters = 1.6f;
        private const float IyVenomBurstMul = 1.2f;

        // 드라군 「업화」 — 불바다 1.5배 · 밟은 적 둔화
        private const float IyFireFieldMul = 1.5f;
        private const int IyFireSlowPercent = 30;
        private const float IyFireSlowTick = 0.5f;
        private const float IyFireFieldSeconds = 6f;          // DragoonFireField 의 장판 시간과 같다

        // 청룡 「번개 사슬」 — 묶인 적은 낙뢰를 두 번 맞는다
        private const float IyThunderAgainGap = 0.18f;

        // 구루 「결계 강령」 — 묶인 적 곁에 해골 2기
        private const int IyBindSkulls = 2;
        private const float IyBindSkullSideMeters = 0.6f;

        // 화이트 위저드 「빛과 얼음」 — 끌어모은 적이 언 뒤 폭발(피해 +30%)
        private const float IyLightIceFreezeLead = 0.2f;
        private const float IyLightIceFreezeSeconds = 0.8f;
        private const float IyLightIceMul = 1.3f;

        // 닌자 「그림자 분신」 — 베기 끝에 분신 하나가 4초 남아 싸운다
        private const float IyCloneSeconds = 4f;
        private const float IyCloneMul = 0.6f;
        private const float IyCloneInterval = 0.4f;

        // 닌자(사슬) 「뇌전 사슬」 — 끌어올 때 사슬에 번개(두 번)
        private const float IyChainShockMul = 0.6f;
        private const float IyChainShockFirst = 0.05f;
        private const float IyChainShockSecond = 0.2f;

        // 로봇 「레이저 포탑」 — 포탑이 꿰뚫는 레이저를 쏜다
        private const float IyTurretLaserMeters = 8.2f;       // 포탑 사거리와 같다
        private const float IyTurretLaserWidthMeters = 0.8f;
        private const float IyTurretLaserInterval = 1.2f;
        private const float IyTurretLaserMul = 1.3f;          // 포탑 한 발 대비(관통이라 간격을 조금 늘렸다)

        // 슬러거 「얼음 장타」 — 라인 드라이브에 맞은 적이 언다
        private const float IyIceDriveFreezeSeconds = 1.5f;
        private const int IyIceDriveBossSlow = 35;

        // 설녀 「얼음 진」 — 눈보라 범위 2배
        private const float IyBlizzardRangeMul = 2f;

        // 흡혈귀 「피의 연회」 — 박쥐 두 배 · 중독된 적에게서 두 배로 빨아 온다
        private const float IyFeastBatMul = 1.5f;             // 적 하나당(박쥐 둘)
        private const float IyFeastPoisonMul = 2f;
        private const float IyFeastBatOffsetMeters = 0.3f;

        private static readonly Color IyCloneTint = new(0.78f, 0.55f, 1f, 0.82f);

        // ═══════════════════════════════════════════════════════════
        //  공용
        // ═══════════════════════════════════════════════════════════

        /// <summary>인연형 부채꼴 탄 — `FireFan`(내 탄)과 같고 관통 · 유도만 덧붙인다.</summary>
        private void IyFireFan(Unit from, Vector2 at, int count, float spanDeg, int damage, bool pierce, float homing)
        {
            float speed = _config.ShotSpeedPlayer * _buffs.ShotSpeedMul;
            float life = _roomSize.magnitude / Mathf.Max(1f, speed) + 0.25f;
            var seek = homing > 0f ? NearestEnemy(from.Position) : null;
            for (int i = 0; i < count; i++)
            {
                float off = count == 1 ? 0f : -spanDeg * 0.5f + spanDeg * i / (count - 1);
                var shot = RentShot();
                if (shot == null) return;
                shot.SetSprite(ShotSpriteOf(from), ShotKindOf(from), LoopsFrames(ShotKindOf(from)));
                shot.Fire(from.Position, at, speed, damage, true, seek, _config.ShotSize * 1.15f, ShotPlayerColor,
                          life, angleOffsetDeg: off,
                          slowPercent: SlowChanceOf(0, _buffs.SlowPercent),
                          lifestealPercent: LeechChanceOf(0, _buffs.LifestealPercent),
                          pierce: pierce || _buffs.Pierce,
                          bounces: _buffs.Bounces);
                ApplyCardShotRules(shot, innateBounce: false, innatePierce: pierce);
                if (seek != null) shot.SetHoming(homing);
            }
        }

        /// <summary>몸을 갈아입거나 방을 나갈 때(`ClearSkills1029`).</summary>
        private void ClearInyeonSkills()
        {
            _iyCluster.Clear();
            _iyLaserTurrets.Clear();
        }

        private void TickInyeonSkills(float dt)
        {
            TickIyLaserTurrets(dt);
        }

        // ═══════════════════════════════════════════════════════════
        //  갱스터 「청부 표식」
        // ═══════════════════════════════════════════════════════════

        /// <summary>표식이 박힐 무렵 보조마다 표식 적을 하나씩 맡아 친다(보조 표적 고르기는 `Party` 쪽이라 여기서 한 방으로 낸다).</summary>
        private void IyContractStrike(List<Unit> marked)
        {
            if (marked == null || marked.Count == 0) return;
            for (int s = 0; s < _subs.Count; s++)
            {
                var sub = _subs[s].U;
                if (sub == null || !sub.IsAlive) continue;
                var e = marked[s % marked.Count];
                UltAfter(IyContractLead + s * 0.12f, () =>
                {
                    if (!IsLiveCaster(sub) || e == null || !e.IsAlive) return;
                    sub.SetFacing(e.Position - sub.Position);
                    sub.PlayAttack();
                    _pfx?.Dart(sub.Position, e.Position, ParticleElement.Fire, 0.12f, 2);
                    UltAfter(0.12f, () =>
                    {
                        if (e == null || !e.IsAlive) return;
                        HitEnemyWith(e, SkillDamage(sub, IyContractHitMul), sub.Profile);
                        PlayFx("crit", e.Position, 96f, loop: false);
                        Shake(2f);
                    });
                });
            }
        }

        /// <summary>보조 · 소환물이 직접 때릴 때(`_summonStriking`) 표식 적이면 더 아프다 — 갱스터 인연이 켜졌을 때만.</summary>
        private float IyContractMul(Unit victim)
            => _summonStriking && victim != null && victim.IsMarked
               && _host != null && _host.Key == "gangster" && InyeonOn(_host) ? IyContractSubMul : 1f;

        // ═══════════════════════════════════════════════════════════
        //  아마존 「번개 도약」
        // ═══════════════════════════════════════════════════════════

        private void IyLeapAgain(Unit me)
        {
            UltAfter(IyLeapAgainDelay, () =>
            {
                if (me != _host || !me.IsAlive || NearestEnemy(me.Position) == null) return;
                AmazonLeapStrike(me);
            });
        }

        /// <summary>착지한 자리에 번개 고리 — 둘레 적에게 감전.</summary>
        private void IyLeapRing(Unit me)
        {
            float r = Meters(IyLeapRingMeters) * _buffs.AoeMul;
            int dmg = SkillDamage(me, IyLeapRingMul);
            PlayFx("dscoil", me.Position, r * 1.6f, loop: false);
            _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Fire, me.Position, 10, 240f, 24f, 0.4f, 0f);
            var list = new List<Unit>(EnemiesInRange(me.Position, r));
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;
                var at = e.Position;
                HitEnemyWith(e, dmg, me.Profile);
                SpawnImpact(at, "thunder", ChainImpactSize * 0.8f);
            }
            Shake(3f);
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(레이저) 「과부하」
        // ═══════════════════════════════════════════════════════════

        /// <summary>두 번째 레이저 — 첫 줄이 겨눈 적 말고 가장 가까운 적, 없으면 옆으로 벌려 쏜다.</summary>
        private void IySecondLaser(Unit me, Vector2 from, Vector2 aim, Unit first, float length, float width, int dmg)
        {
            Vector2 dir = Rotate(aim, IyLaserSplitDeg);
            Unit other = null;
            float best = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == first || !Targetable(e)) continue;
                float d = (e.Position - from).sqrMagnitude;
                if (d > length * length || d >= best) continue;
                best = d;
                other = e;
            }
            if (other != null && (other.Position - from).sqrMagnitude > 1f) dir = (other.Position - from).normalized;
            PrismBeam(from, from + dir * length, LzBeamDraw * (width / Meters(ChargeLaserWidthMeters)), 0.35f);
            var hit = new List<Unit>(EnemiesInPath(from, dir, length, width));
            for (int i = 0; i < hit.Count; i++)
            {
                var at = hit[i].Position;
                HitEnemyWith(hit[i], dmg, me.Profile);
                PlayFx("lzhit", at, 90f, loop: false);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(수류탄) 「융단 폭격」
        // ═══════════════════════════════════════════════════════════

        /// <summary>한 바퀴 더 — 바깥 원을 반 칸 엇갈려 돈다. 한 적 최대 2발 상한은 이 바퀴에도 따로 센다.</summary>
        private void IyCarpetLap(Unit me, int shots, int dmg)
        {
            UltAfter(IyCarpetLapDelay, () =>
            {
                if (!IsLiveCaster(me)) return;
                var dir = AimDirection(me);
                float a0 = Mathf.Atan2(dir.y, dir.x) + Mathf.PI / shots;
                var quota = new Dictionary<Unit, int>();
                for (int i = 0; i < shots; i++)
                {
                    float a = a0 + i * Mathf.PI * 2f / shots;
                    var at = ClampedInField(me, me.Position
                             + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(CarpetRingMeters * IyCarpetLapRingMul));
                    var near = NearestEnemy(at, Meters(CarpetSnapMeters));
                    if (near != null)
                    {
                        quota.TryGetValue(near, out int used);
                        if (used >= CarpetMaxPerTarget) near = null;
                        else quota[near] = used + 1;
                    }
                    if (near != null) at = near.Position;
                    PwCarpetThrown(ThrowSkillGrenade(me, at, dmg, Meters(1.8f)), i);
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(미사일) 「집속탄」
        // ═══════════════════════════════════════════════════════════

        // 집속탄으로 쏜 미사일 — 터질 때(`PwExplode`) 작은 폭발을 흩뿌린다. 값은 쏜 시각(풀에서 다시 쓰인 탄을 가른다)
        private readonly Dictionary<Projectile, float> _iyCluster = new();

        private void IyMarkCluster(Projectile shot)
        {
            if (shot != null) _iyCluster[shot] = Time.time;
        }

        /// <summary>탄이 터졌다 — 집속탄이면 둘레에 작은 폭발 셋.</summary>
        private void IyClusterBurst(Projectile shot, Vector2 at)
        {
            if (shot == null || !_iyCluster.TryGetValue(shot, out float firedAt)) return;
            _iyCluster.Remove(shot);
            var me = _host;
            if (me == null || Time.time - firedAt > IyClusterTrackSeconds) return;
            int dmg = SkillDamage(me, IyClusterMul);
            float r = Meters(IyClusterBlastMeters) * _buffs.AoeMul;
            float a0 = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < IyClusterCount; i++)
            {
                float a = a0 + i * Mathf.PI * 2f / IyClusterCount;
                var p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(IyClusterSpreadMeters);
                UltAfter(IyClusterDelay * (i + 1), () =>
                {
                    if (_host == null) return;
                    PlayFx("burst", p, r * 2f, loop: false);
                    var list = new List<Unit>(EnemiesInRange(p, r));
                    for (int k = 0; k < list.Count; k++) HitEnemyWith(list[k], dmg, _host.Profile);
                    Shake(1.5f);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  샐러맨더 「불독」
        // ═══════════════════════════════════════════════════════════

        /// <summary>적이 쓰러졌다(`PassiveOnKill`) — 샐러맨더 인연이면 중독된 적이 불꽃으로 터진다.</summary>
        private void IyOnKill(Unit victim)
        {
            var me = _host;
            if (me == null || victim == null || me.Key != "salamander" || !InyeonOn(me)) return;
            if (victim.Side != UnitSide.Enemy || !victim.IsPoisoned) return;
            var at = victim.Position;
            // 한 박자 늦춘다 — 죽는 도중(`KillEnemy`)에 옆 적을 또 죽이면 목록을 도는 쪽이 꼬인다
            UltAfter(0.05f, () =>
            {
                var host = _host;
                if (host == null || host.Key != "salamander") return;
                float r = Meters(IyVenomBurstMeters) * _buffs.AoeMul;
                int dmg = SkillDamage(host, IyVenomBurstMul);
                PlayFx("burst", at, r * 2f, loop: false);
                _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Fire, at, 10, 220f, 30f, 0.5f, 0f);
                var list = new List<Unit>(EnemiesInRange(at, r));
                for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, host.Profile);
                Shake(2.5f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  드라군 「업화」
        // ═══════════════════════════════════════════════════════════

        /// <summary>불바다를 밟은 적을 0.5초마다 다시 늦춘다(장판이 사는 동안).</summary>
        private void IyFireFieldSlow(Vector2 at, float radius)
        {
            int ticks = Mathf.RoundToInt(IyFireFieldSeconds / IyFireSlowTick);
            for (int t = 0; t < ticks; t++)
            {
                UltAfter(t * IyFireSlowTick, () =>
                {
                    var list = EnemiesInRange(at, radius);
                    for (int i = 0; i < list.Count; i++) list[i].ApplySlow(IyFireSlowPercent, IyFireSlowTick + 0.15f);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  구루 「결계 강령」
        // ═══════════════════════════════════════════════════════════

        private void IyBindSkullsNear(Unit me, List<Unit> bound)
        {
            for (int i = 0; i < IyBindSkulls; i++)
            {
                Vector2 at;
                if (bound != null && i < bound.Count && bound[i] != null && bound[i].IsAlive)
                {
                    var side = bound[i].Position - me.Position;
                    side = side.sqrMagnitude > 1f ? side.normalized : Vector2.right;
                    at = bound[i].Position - side * Meters(IyBindSkullSideMeters);   // 묶인 적의 내 쪽 곁
                }
                else
                {
                    float a = (i * 180f + 90f) * Mathf.Deg2Rad;
                    at = me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(1f);
                }
                at = ClampedInField(me, at);
                SummonSkull(at);
                PlayFx("soulripple", at, Meters(1f), loop: false);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  화이트 위저드 「빛과 얼음」
        // ═══════════════════════════════════════════════════════════

        private void IyFreezeGathered(Unit me, Vector2 c, float r)
        {
            UltAfter(GravityPullSeconds - IyLightIceFreezeLead, () =>
            {
                if (!IsLiveCaster(me)) return;
                PlayFx("breath_ice", c, r * 2f, loop: false);
                var list = new List<Unit>(EnemiesInRange(c, r * 1.15f));
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null || !e.IsAlive) continue;
                    if (e.IsBoss) { e.ApplySlow(IyIceDriveBossSlow, IyLightIceFreezeSeconds); continue; }
                    e.ApplyFreeze(IyLightIceFreezeSeconds);
                    PlayFx("freeze", e.Position, 72f, loop: false);
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  닌자 「그림자 분신」
        // ═══════════════════════════════════════════════════════════

        /// <summary>베기가 끝난 자리 곁에 보라빛 분신 하나 — 4초 표창을 던진다(필살기 삼분신과 같은 모양).</summary>
        private void IyShadowClone(Unit me, float after)
        {
            UltAfter(after, () =>
            {
                if (me != _host || !me.IsAlive) return;
                var side = new Vector2(-me.Facing.y, me.Facing.x);
                if (side.sqrMagnitude < 0.01f) side = Vector2.right;
                var at = ClampedInField(me, me.Position + side.normalized * Meters(1.2f));
                _cloneSeconds = Mathf.Max(_cloneSeconds, IyCloneSeconds);   // 안 켜면 `TickClones` 가 바로 거둔다
                SpawnClone(at, IyCloneSeconds, SkillDamage(me, IyCloneMul), IyCloneInterval);
                if (_clones.Count > 0)
                {
                    var c = _clones[_clones.Count - 1];
                    var body = me.BodySprite;
                    if (body != null) c.SetSprite(body);
                    c.SetSize(me.GetComponent<RectTransform>().sizeDelta);
                    c.SetTint(IyCloneTint);
                    c.ShotKey = "shuriken";
                }
                PlayFx("njsmoke", at, SsrBody * 2f, loop: false)?.SetFrameSeconds(0.07f);
                _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Curse, at, 3, 60f, 60f, 0.5f, 0f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  닌자(사슬) 「뇌전 사슬」
        // ═══════════════════════════════════════════════════════════

        private void IyChainShock(Unit me, List<Unit> pulled)
        {
            int dmg = SkillDamage(me, IyChainShockMul);
            for (int k = 0; k < 2; k++)
            {
                UltAfter(k == 0 ? IyChainShockFirst : IyChainShockSecond, () =>
                {
                    if (!IsLiveCaster(me)) return;
                    for (int i = 0; i < pulled.Count; i++)
                    {
                        var e = pulled[i];
                        if (e == null || !e.IsAlive) continue;
                        PlayBolt(me.Position, e.Position);
                        var at = e.Position;
                        HitEnemyWith(e, dmg, me.Profile);
                        SpawnImpact(at, "thunder", ChainImpactSize * 0.7f);
                    }
                    Shake(2f);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  로봇 「레이저 포탑」
        // ═══════════════════════════════════════════════════════════

        private sealed class IyLaserTurret
        {
            public Deployable D;
            public float Cd;
            public int Dmg;
        }

        private readonly List<IyLaserTurret> _iyLaserTurrets = new();

        /// <summary>방금 세운 포탑을 레이저 포탑으로 — 포탑의 맥동탄은 사거리를 0 으로 막고 여기서 레이저를 쏜다.</summary>
        private void IyAdoptLaserTurret(int damage)
        {
            for (int i = _deployables.Count - 1; i >= 0; i--)
            {
                var d = _deployables[i];
                if (!d.IsActive || _clones.Contains(d)) continue;
                bool known = false;
                for (int k = 0; k < _iyLaserTurrets.Count; k++) if (_iyLaserTurrets[k].D == d) { known = true; break; }
                if (known) continue;
                _iyLaserTurrets.Add(new IyLaserTurret { D = d, Cd = 0f, Dmg = Mathf.Max(1, Mathf.RoundToInt(damage * IyTurretLaserMul)) });
                return;
            }
        }

        private void TickIyLaserTurrets(float dt)
        {
            for (int i = _iyLaserTurrets.Count - 1; i >= 0; i--)
            {
                var t = _iyLaserTurrets[i];
                if (t.D == null || !t.D.IsActive) { _iyLaserTurrets.RemoveAt(i); continue; }
                t.Cd -= dt;
                if (t.Cd > 0f) continue;
                float length = Meters(IyTurretLaserMeters);
                var target = NearestEnemy(t.D.Position, length);
                if (target == null) continue;
                t.Cd = IyTurretLaserInterval;
                var from = t.D.Position;
                var dir = target.Position - from;
                dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
                float width = Meters(IyTurretLaserWidthMeters);
                PrismBeam(from, from + dir * length, LzBeamDraw * (width / Meters(ChargeLaserWidthMeters)), 0.25f);
                var hit = new List<Unit>(EnemiesInPath(from, dir, length, width));
                var p = _host != null ? _host.Profile : null;
                for (int k = 0; k < hit.Count; k++)
                {
                    var at = hit[k].Position;
                    HitEnemyWith(hit[k], t.Dmg, p);
                    PlayFx("lzhit", at, 72f, loop: false);
                }
                Shake(1.5f);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  슬러거 「얼음 장타」
        // ═══════════════════════════════════════════════════════════

        private void IyIceDrive(Unit e)
        {
            if (e == null || !e.IsAlive) return;
            if (e.IsBoss) { e.ApplySlow(IyIceDriveBossSlow, IyIceDriveFreezeSeconds); return; }
            e.ApplyFreeze(IyIceDriveFreezeSeconds);
            PlayFx("freeze", e.Position, 72f, loop: false);
        }

        // ═══════════════════════════════════════════════════════════
        //  흡혈귀 「피의 연회」
        // ═══════════════════════════════════════════════════════════

        /// <summary>빨아 올 머릿수 — 적 하나에 박쥐 둘(×1.5), 중독된 적은 그 두 배.</summary>
        private static float IyFeastCount(List<Unit> list)
        {
            float n = 0f;
            for (int i = 0; i < list.Count; i++) n += IyFeastBatMul * (list[i].IsPoisoned ? IyFeastPoisonMul : 1f);
            return n;
        }

        /// <summary>적마다 두 번째 박쥐를 조금 비켜 날린다.</summary>
        private void IyFeastSecondBats(Unit me, List<Unit> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var im = FreeImpact(BatFxSizeQ);
                if (im == null) return;
                float a = (i * 137f) * Mathf.Deg2Rad;
                var to = list[i].Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(IyFeastBatOffsetMeters);
                im.Play(me.Position, BatFrames, BatFxSizeQ, loop: true);
                _batTargets.Add((im, me.Position, to));
            }
        }
    }
}
