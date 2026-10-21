using System.Collections.Generic;
using Game.Module.Common.UI;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 스킬 손질 (PD 2026-10-21 수정 사항 15건 중 그림이 새로 필요 없는 것).
    ///
    ///   폭력배 액티브   — 1초 난사(가속 상태) → 원래 탄으로 「파파팍」 세 번 쏘고 끝
    ///   코만도(미사일)  — 액티브 = 여러 적에게 한 발씩 7발, 필살기 = 가장 센 적에게 8발 집중(예전엔 둘이 거꾸로였다)
    ///   청룡 필살기     — 번개 구슬 → 둘레 모든 적에게 낙뢰 세 번 + 가장 센 적에게 큰 낙뢰
    ///   영매 액티브     — 해골 둘 → 따라다니며 싸우는 미니 골렘
    ///   닌자 액티브     — 분신 → 몸놀림(회피 · 이동 속도 · 잔상)
    ///   로봇 필살기     — 포탑 군단 → 로봇 특공대(빛기둥으로 내려와 일제 사격 → 사라짐)
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ═══════════════════════════════════════════════════════════
        //  액티브 쿨타임 5~10초 (PD 2026-10-21 「쿨타임 5~6초 · 너가 감안해서 쿨타임과 데미지를 체크」)
        //
        //  쿨은 HostTable `_activeSkillCooldown` 에 넣었다. 지속형은 지속보다 길게 둔다(정조준 5초 → 8초 등).
        //  한 번에 들어가는 위력은 **쿨이 준 비율 × 1.2** 로 줄인다 — 초당 위력은 예전보다 20% 세다
        //  (액티브가 이제 주 버튼이라 조금 더 준다). 회복 · 쉴드도 같은 비율.
        //  예전 쿨(기준)을 여기 적어 둔다 — 표의 쿨을 다시 바꾸면 이 비율이 따라 바뀐다.
        // ═══════════════════════════════════════════════════════════

        private const float ActiveRebaseBonus = 1.2f;

        private static readonly Dictionary<string, float> ActiveBaseCooldown = new()
        {
            { "gangster", 14f }, { "thug", 8f }, { "amazon", 8f }, { "amazon_elite", 28f }, { "hopper", 14f },
            { "hopper_smg", 14f }, { "commando_mg", 14f }, { "commando_laser", 20f }, { "commando_grenade", 20f },
            { "salamander", 14f }, { "dragoon", 20f }, { "dragon_blue", 14f }, { "guru", 20f }, { "white_wizard", 20f },
            { "medium", 14f }, { "ninja", 20f }, { "ninja_chain", 14f }, { "robot", 20f }, { "baseball", 14f },
            { "snowwoman", 14f }, { "vampire", 20f }, { "commando_missile", 20f }, { "death", 28f },
        };

        /// <summary>지금 몸의 액티브 한 번 위력 배율(쿨이 줄어든 만큼). 1 을 넘지 않는다.</summary>
        private float ActiveRebaseMul
        {
            get
            {
                var me = _host;
                if (me == null || !ActiveBaseCooldown.TryGetValue(me.Key, out var old) || old <= 0f) return 1f;
                return Mathf.Min(1f, SkillCooldownOf(me.Profile) / old * ActiveRebaseBonus);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  폭력배 — 3연발 난사: 한 번 「파파팍」 쏘고 끝 (PD 「너무 오래가 · 원래 투사체로」)
        // ═══════════════════════════════════════════════════════════

        private const int ThugBurstVolleys = 3;
        private const float ThugBurstGap = 0.08f;
        private const int ThugBurstShots = 5;          // 한 번에 부채꼴 다섯 발
        private const float ThugBurstSpanDeg = 40f;
        private const float ThugBurstShotMul = 0.8f;   // 발당 — 15발이 한꺼번에 들어간다

        private void ThugBurst(Unit me)
        {
            int shots = SpecOpen ? Mathf.Max(ThugBurstShots, Mathf.RoundToInt(SpecAxis(3f)) + 2) : ThugBurstShots;   // Lv5 5 → 9발
            int dmg = SkillDamage(me, ThugBurstShotMul);
            for (int v = 0; v < ThugBurstVolleys; v++)
            {
                UltAfter(v * ThugBurstGap, () =>
                {
                    if (_host != me || me == null || !me.IsAlive) return;
                    var target = NearestEnemy(me.Position);
                    var at = target != null ? target.Position : me.Position + AimDirection(me) * Meters(6f);
                    me.SetFacing(at - me.Position);
                    me.PlayAttack();
                    // 원래 평타 탄(`ShotSpriteOf`)으로 나간다 — 예광탄으로 바꾸지 않는다
                    FireFan(me, at, shots, ThugBurstSpanDeg, dmg, fromPlayer: true);
                    PlayFx("muzzle", me.MuzzlePosition, 56f, loop: false);
                    Shake(2f);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(미사일) — 액티브: 여러 적에게 한 발씩 / 필살기: 가장 센 적에게 8발 집중
        // ═══════════════════════════════════════════════════════════

        private const int MissileVolleyShots = 7;   // PD 「6~8발」
        private const float MissileVolleyRangeMeters = 10f;

        private void MissileVolley(Unit me)
        {
            var list = UltTargets(me.Position, Meters(MissileVolleyRangeMeters));
            int dmg = SkillDamage(me, BaseAxis(1f));
            float radius = Meters(MissileBlastMeters);
            var aim = list.Count > 0 && (list[0].Position - me.Position).sqrMagnitude > 1f
                    ? (list[0].Position - me.Position).normalized : me.Facing;
            for (int i = 0; i < MissileVolleyShots; i++)
            {
                // 적이 일곱보다 적으면 가까운 쪽부터 한 바퀴 더 돈다
                var target = list.Count > 0 ? list[i % list.Count] : null;
                float off = -MissileFanSpreadDeg * 0.5f + MissileFanSpreadDeg * i / (MissileVolleyShots - 1);
                var fan = Rotate(aim, off);
                var from = ClampedInField(me, me.Position + fan * Meters(MissileFanOutMeters));
                FireSkillMissile(me, from, from + fan * Meters(6f), target, dmg, radius);
                PlayFx("missile_trail", from, 32f, loop: false);
            }
        }

        private const int MissileFocusShots = 8;

        private void MissileFocus(Unit me)
        {
            var t = UltStrongest(me.Position) ?? NearestEnemy(me.Position);
            if (t == null) { UltFallback(me); return; }
            // 한 놈에게 조준 표식 하나 — 여덟 발이 반원으로 퍼졌다가 그 놈에게 모인다
            for (int i = 0; i < MissileFocusShots; i++)
            {
                float a = (-90f + i * 180f / (MissileFocusShots - 1)) * Mathf.Deg2Rad;
                var from = me.Position + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Meters(0.7f);
                var target = t;
                bool first = i == 0;   // 루프 변수를 람다에 잡지 않는다 — 다 돈 뒤의 값이 잡힌다
                UltAfter(i * 0.05f, () =>
                {
                    if (target == null || !target.IsAlive) return;
                    SrMissile(me, from, from + (from - me.Position).normalized * Meters(3f), target,
                              UltDamage(me, 2.6f), Meters(1.6f), lockMark: first);
                    PlayFx("missile_trail", from, 32f, loop: false);
                });
            }
            Shake(4f);
        }

        // ═══════════════════════════════════════════════════════════
        //  청룡 — 천뢰: 둘레 모든 적에게 낙뢰 세 번 → 가장 센 적에게 큰 낙뢰 (PD 「내 주변의 모든 적에게 낙뢰」)
        // ═══════════════════════════════════════════════════════════

        private const float StormRangeMeters = 7f;
        private const int StormWaves = 3;
        private const float StormWaveGap = 0.55f;
        private const float StormHitMul = 1.3f;

        private void UltDragonStorm(Unit me)
        {
            if (UltTargets(me.Position, Meters(StormRangeMeters)).Count == 0) { UltFallback(me); return; }
            PlayFx("dscoil", me.Position, SsrBody * 1.6f, loop: false);
            for (int w = 0; w < StormWaves; w++)
            {
                UltAfter(0.25f + w * StormWaveGap, () =>
                {
                    // 그 순간 둘레에 있는 적 전부 — 가까운 쪽부터 0.04초씩 엇갈려 떨어진다(물결)
                    var list = new List<Unit>(UltTargets(me.Position, Meters(StormRangeMeters)));
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        UltAfter(i * 0.04f, () =>
                        {
                            if (e == null || !e.IsAlive) return;
                            var s = PlayFx("dbstrike", e.Position + new Vector2(0f, SsrBody * 1.1f), SsrBody * 2.6f, loop: false);
                            if (s != null) s.SetFrameSeconds(0.04f);
                            else UltBeam("dsbolt", e.Position + new Vector2(0f, Meters(5f)), e.Position, 90f);
                            UltHit(me, e, StormHitMul);
                            PlayFx("lzhit", e.Position, 96f, loop: false);
                        });
                    }
                    Shake(5f);
                });
            }
            UltAfter(0.25f + StormWaves * StormWaveGap + 0.2f, () =>
            {
                var t = UltStrongest(me.Position) ?? NearestEnemy(me.Position);
                if (t == null) return;
                var at = t.Position;
                var strike = PlayFx("dbstrike", at + new Vector2(0f, SsrBody * 1.9f), SsrBody * 4.6f, loop: false);
                if (strike != null) strike.SetFrameSeconds(0.05f);
                else UltBeam("dsbolt", at + new Vector2(0f, Meters(6f)), at, 120f);
                UltAoe(me, at, 2.4f, 3.5f, "lzhit", 2f);
                _pfx?.Spray(ParticleFxKind.Streak, at, Vector2.up, 180f, 18, 300f, 600f, new Vector2(40f, 5f), 0.3f,
                            new Color(0.6f, 0.9f, 1f, 1f));
                Shake(12f);
                HitStop(0.1f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  영매 — 미니 골렘: 따라다니며 같이 싸운다 (PD 「해골보다 미니 골렘을 데리고 다니면서」)
        // ═══════════════════════════════════════════════════════════

        private const float MiniGolemScale = 1.0f;     // 필살기 골렘은 2.2
        private const float MiniGolemHpPercent = 0.6f;
        private const float MiniGolemAtkPercent = 0.45f;
        private const float MiniGolemLifeSeconds = 10f;
        private const float MiniGolemSpecSeconds = 4f;   // Lv5 옵션 「지속 +4초」

        private void MediumMiniGolem(Unit me)
        {
            // 한 마리만 — 다시 쓰면 앞 골렘이 흙먼지로 돌아가고 새로 선다
            for (int i = _summons.Count - 1; i >= 0; i--)
                if (_summons[i].U != null && _summons[i].U.name.StartsWith("Summon_" + GolemKey) && _summons[i].LifeMax >= MiniGolemLifeSeconds)
                {
                    PlayFx("smoke", _summons[i].U.Position, 96f, loop: false);
                    RemoveSummon(i);
                }
            var at = ClampedInField(me, me.Position + AimDirection(me) * Meters(1.2f));
            SpawnSummon(GolemKey, "미니 골렘", MiniGolemHpPercent, MiniGolemAtkPercent, MiniGolemLifeSeconds + (SpecOpen ? MiniGolemSpecSeconds : 0f),
                        attacks: true, mobile: true, taunt: false, at: at, scale: MiniGolemScale, profile: GolemProfile);
            PlayFx("soulripple", at, Meters(1.4f), loop: false);
        }

        // ═══════════════════════════════════════════════════════════
        //  닌자 — 몸놀림: 4초 동안 회피 · 이동 속도가 오르고 잔상이 남는다 (PD 「회피율을 올린다거나 닌자 같은 느낌」)
        // ═══════════════════════════════════════════════════════════

        private const float EvadeSeconds = 4f;
        private const int EvadeDodgePercent = 50;
        private const int EvadeSpecDodgePercent = 70;   // Lv5 옵션 「회피 50 → 70%」
        private const float EvadeSpeedBonus = 0.35f;
        private const float EvadeTrailEvery = 0.07f;
        private static readonly Color EvadeTrailTint = new(0.55f, 0.75f, 1f, 1f);
        private float _evadeSeconds;
        private float _evadeTrail;

        private void NinjaEvade(Unit me)
        {
            _evadeSeconds = BaseAxis(EvadeSeconds);
            _evadeTrail = 0f;
            me.SetDodge(SpecOpen ? EvadeSpecDodgePercent : EvadeDodgePercent);
            // 처치 이속(패시브)과 같은 시계를 쓴다 — 둘이 따로 돌면 먼저 끝나는 쪽이 속도를 1로 되돌린다
            _ninjaKillSpeedTimer = Mathf.Max(_ninjaKillSpeedTimer, _evadeSeconds);
            me.SetSpeedMul(1f + EvadeSpeedBonus);
            PlayFx("njsmoke", me.Position, SsrBody * 1.8f, loop: false);
            PlayFx("dash", me.Position, 96f, loop: false);
        }

        private void TickEvade(float dt)
        {
            if (_evadeSeconds <= 0f) return;
            _evadeSeconds -= dt;
            var me = _host;
            if (me == null || me.Key != "ninja") { _evadeSeconds = 0f; return; }
            if (_evadeSeconds <= 0f)
            {
                me.SetDodge(0);   // 닌자는 기본 회피가 없다(사슬 닌자만 패시브로 가진다)
                return;
            }
            _evadeTrail -= dt;
            if (_evadeTrail > 0f) return;
            _evadeTrail = EvadeTrailEvery;
            var sprite = me.BodySprite;
            var img = sprite != null ? RentAfterimage() : null;
            img?.Play(sprite, me.Position, me.GetComponent<RectTransform>().sizeDelta, me.BodyFlipX, 0.4f, 0.25f, EvadeTrailTint);
        }

        // ═══════════════════════════════════════════════════════════
        //  닌자(사슬) — 사슬 끌어오기: 둘레 적을 사슬로 내 앞에 끌어와 한데 묶고 한 번 후려친다
        //  PD 2026-10-21 「ㄱ 이 맞긴 한데 범위로 때리는 판정이 애매」 — 끌려온 무리를 **스킬이 직접** 한 번 범위로 친다.
        //  평타 판정을 따로 고치지 않아도 「모아서 때리기」가 성립한다(평타는 묶인 적 ×1.3 은 그대로).
        // ═══════════════════════════════════════════════════════════

        private const int ChainPullMax = 5;
        private const int ChainPullSpecMax = 7;
        private const float ChainPullRangeMeters = 6f;
        private const float ChainPullFrontMeters = 1.1f;
        private const float ChainPullSeconds = 0.25f;
        private const float ChainPullRootSeconds = 1.5f;
        private const float ChainPullSlamMul = 1.6f;
        private const float ChainPullSlamMeters = 1.8f;

        private void ChainPull(Unit me)
        {
            var near = UltTargets(me.Position, Meters(ChainPullRangeMeters));
            int max = SpecOpen ? ChainPullSpecMax : ChainPullMax;   // Lv5 옵션 「끌어오는 적 5 → 7」
            var list = new List<Unit>(max);
            for (int i = 0; i < near.Count && list.Count < max; i++)
                if (!near[i].IsBoss) list.Add(near[i]);   // 보스는 끌지 않는다(필살기와 같은 규칙)
            if (list.Count == 0) { PlayFx("chain", me.Position, 96f, loop: false); return; }

            var front = ClampedInField(me, me.Position + AimDirection(me) * Meters(ChainPullFrontMeters));
            var from = new Vector2[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                from[i] = list[i].Position;
                if (!QxChainCast(me, list[i])) PlayFx("chain", list[i].Position, 64f, loop: false);
            }
            // 0.25초 동안 앞으로 끌려온다(열 걸음) — 한 점에 겹치지 않게 앞 지점 둘레로 조금씩 흩는다
            const int Steps = 10;
            for (int s = 1; s <= Steps; s++)
            {
                float k = Ease.OutCubic(s / (float)Steps);
                UltAfter(ChainPullSeconds * s / Steps, () =>
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        if (e == null || !e.IsAlive) continue;
                        float a = i * Mathf.PI * 2f / list.Count;
                        var slot = front + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * Meters(0.5f);
                        e.Position = ClampedInField(e, Vector2.Lerp(from[i], slot, k));
                    }
                });
            }
            UltAfter(ChainPullSeconds + 0.02f, () =>
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && list[i].IsAlive) list[i].ApplyRoot(ChainPullRootSeconds);
                _bindBonusSeconds = Mathf.Max(_bindBonusSeconds, ChainPullRootSeconds);
                // 끌려온 무리를 한 번 범위로 후려친다
                int dmg = SkillDamage(me, BaseAxis(ChainPullSlamMul));
                var hit = new List<Unit>(EnemiesInRange(front, Meters(ChainPullSlamMeters) * _buffs.AoeMul));
                for (int i = 0; i < hit.Count; i++) HitEnemyWith(hit[i], dmg, me.Profile);
                PlayFx("cbsnap", front, Meters(2.4f), loop: false);
                PlayFx("slam", front, Meters(ChainPullSlamMeters) * 2f, loop: false);
                Shake(5f);
                HitStop(0.05f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  흡혈귀 — 피의 웅덩이: 둘레에 피 웅덩이 → 안의 적 전부에게 피가 묻고 2초 동안 빨아들인 뒤 펑 (PD 「롤 블라디 궁」)
        // ═══════════════════════════════════════════════════════════

        private const float BloodPoolMeters = 3.4f;
        private const float BloodPoolSeconds = 2f;
        private const float BloodTickSeconds = 0.25f;
        private const float BloodTickMul = 0.35f;
        private const float BloodLeechPercent = 0.4f;   // 빨아들인 피해의 40% 를 회복
        private const float BloodPopMul = 3f;

        private void BloodPool(Unit me)
        {
            var marked = UltTargets(me.Position, Meters(BloodPoolMeters));
            if (marked.Count == 0) { UltFallback(me); return; }
            UltInvuln(BloodPoolSeconds + 0.3f);   // 웅덩이에 녹아든다 — 그동안 안 맞는다
            var pool = PlayFloorFx("vmcircle", me.Position, Meters(BloodPoolMeters) * 2.2f, loop: true);
            if (pool != null) { pool.SetFrameSeconds(0.08f); pool.SetLife(BloodPoolSeconds + 0.3f, 0.3f); }
            for (int i = 0; i < marked.Count; i++) PlayFx("vpclaw", marked[i].Position, SsrBody * 1.2f, loop: false);

            int ticks = Mathf.RoundToInt(BloodPoolSeconds / BloodTickSeconds);
            for (int t = 1; t <= ticks; t++)
            {
                UltAfter(t * BloodTickSeconds, () =>
                {
                    int drained = 0;
                    for (int i = 0; i < marked.Count; i++)
                    {
                        var e = marked[i];
                        if (e == null || !e.IsAlive) continue;
                        int dmg = UltDamage(me, BloodTickMul);
                        HitEnemyWith(e, dmg, me.Profile);
                        drained += dmg;
                        _pfx?.Dart(e.Position, me.Position, ParticleElement.Fire, 0.3f, 1);
                    }
                    // 한 번에 모아 회복 — 적마다 띄우면 「+3」 숫자가 수십 개 겹친다
                    if (drained > 0) Leech(Mathf.Max(1, Mathf.RoundToInt(drained * BloodLeechPercent)));
                });
            }
            UltAfter(BloodPoolSeconds + 0.15f, () =>
            {
                for (int i = 0; i < marked.Count; i++)
                {
                    var e = marked[i];
                    if (e == null || !e.IsAlive) continue;
                    PlayFx("vpburst", e.Position, SsrBody * 1.6f, loop: false)?.SetFrameSeconds(0.06f);
                    UltHit(me, e, BloodPopMul);
                }
                Shake(9f);
                HitStop(0.08f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  화이트 위저드 — 빛기둥 비: 3초 동안 둘레 적 머리 위로 빛기둥이 연달아 꽂힌다 (PD 「ㄱ」)
        // ═══════════════════════════════════════════════════════════

        private const int LightRainCount = 14;
        private const float LightRainSeconds = 3f;
        private const float LightRainRangeMeters = 8f;
        private const float LightRainMul = 1.6f;

        private void LightRain(Unit me)
        {
            if (UltTargets(me.Position, Meters(LightRainRangeMeters)).Count == 0) { UltFallback(me); return; }
            PlayFx("holy_beam", me.MuzzlePosition, 140f, loop: false);
            for (int n = 0; n < LightRainCount; n++)
            {
                int order = n;
                UltAfter(0.2f + order * LightRainSeconds / LightRainCount, () =>
                {
                    var list = UltTargets(me.Position, Meters(LightRainRangeMeters));
                    if (list.Count == 0) return;
                    // 차례로 돌아가며 — 한 놈에게만 몰리지 않게
                    var e = list[order % list.Count];
                    var at = e.Position + Random.insideUnitCircle * 12f;
                    var beam = PlayFx("holy_beam", at + new Vector2(0f, SsrBody * 1.6f), SsrBody * 3.6f, loop: false);
                    beam?.SetFrameSeconds(0.04f);
                    UltAoe(me, at, 1.2f, LightRainMul, "wwburst", 2.2f);
                    Shake(2.5f);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  영매 — 거대 원혼: 큰 원혼이 떠올라 둘레 적을 할퀴다가, 끝에 크게 터진다 (PD 「거대 원혼 소환 → 공격 → 일정 시간 뒤 폭발」)
        // ═══════════════════════════════════════════════════════════

        private const float GiantWraithSeconds = 5f;
        private const float GiantWraithLashEvery = 0.5f;
        private const float GiantWraithLashMul = 1.1f;
        private const float GiantWraithRangeMeters = 4.5f;
        private const float GiantWraithBlastMeters = 3.2f;
        private const float GiantWraithBlastMul = 5f;

        private void GiantWraith(Unit me)
        {
            var at = ClampedInField(me, me.Position + AimDirection(me) * Meters(1.6f));
            PlayFloorFx("mdrune", at, SsrBody * 3.4f, loop: false);
            var body = FxFrames("mdwraith") != null ? TakeLoopFx("mdwraith", at + new Vector2(0f, SsrBody * 0.9f), SsrBody * 3.2f) : null;
            if (body != null) { body.SetFrameSeconds(0.08f); _ultFx.Add(body); }
            int lashes = Mathf.RoundToInt(GiantWraithSeconds / GiantWraithLashEvery);
            for (int k = 1; k <= lashes; k++)
            {
                UltAfter(k * GiantWraithLashEvery, () =>
                {
                    // 원혼은 적이 많은 쪽으로 천천히 흘러간다
                    var list = UltTargets(at, Meters(GiantWraithRangeMeters));
                    if (list.Count == 0) return;
                    var t = list[0];
                    at = ClampedInField(me, Vector2.Lerp(at, t.Position, 0.25f));
                    body?.MoveTo(at + new Vector2(0f, SsrBody * 0.9f));
                    for (int i = 0; i < list.Count && i < 3; i++)
                    {
                        UltHit(me, list[i], GiantWraithLashMul);
                        PlayFx("mdslash", list[i].Position, SsrBody * 1.3f, loop: false)?.SetFrameSeconds(0.05f);
                    }
                });
            }
            UltAfter(GiantWraithSeconds + 0.1f, () =>
            {
                body?.Stop();
                _ultFx.Remove(body);
                UltAoe(me, at, GiantWraithBlastMeters, GiantWraithBlastMul, "mdpop", 2.6f, knockMeters: 1.2f);
                PlayFx("soulripple", at, Meters(GiantWraithBlastMeters) * 2f, loop: false);
                Shake(12f);
                HitStop(0.1f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  사신 — 원혼: 몸에서 떠올라 적에게 날아가 붙어 터진다(망자 군단의 「유령」)
        // ═══════════════════════════════════════════════════════════

        private const float WraithFlySeconds = 0.45f;
        private const float WraithSize = 110f;

        private sealed class Wraith
        {
            public Impact Fx;
            public Unit Target;
            public Unit Caster;
            public Vector2 From;
            public Vector2 Last;
            public float T;
        }

        private readonly List<Wraith> _wraiths = new();

        private void LaunchWraith(Unit me, Unit target)
        {
            if (target == null || !target.IsAlive) return;
            var from = me.Position + new Vector2(Random.Range(-30f, 30f), SsrBody * 0.6f);
            var fx = FxFrames("mdwraith") != null ? TakeLoopFx("mdwraith", from, WraithSize) : null;
            fx?.SetFrameSeconds(0.07f);
            _wraiths.Add(new Wraith { Fx = fx, Target = target, Caster = me, From = from, Last = target.Position });
            if (fx == null) _pfx?.Dart(from, target.Position, ParticleElement.Ice, WraithFlySeconds, 4);
        }

        private void TickWraiths(float dt)
        {
            for (int i = _wraiths.Count - 1; i >= 0; i--)
            {
                var w = _wraiths[i];
                if (w.Target != null && w.Target.IsAlive) w.Last = w.Target.Position;
                w.T += dt / WraithFlySeconds;
                // 위로 휘었다가 내리꽂힌다 — 곧게 가면 탄처럼 보인다
                float k = Mathf.Clamp01(w.T);
                var p = Vector2.Lerp(w.From, w.Last, k * k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * SsrBody * 0.8f);
                w.Fx?.MoveTo(p);
                if (w.T < 1f) continue;
                w.Fx?.Stop();
                _wraiths.RemoveAt(i);
                if (_host != w.Caster || w.Caster == null) continue;
                UltAoe(w.Caster, w.Last, 1.3f, 2.4f, "mdpop", 2.6f);
            }
        }

        private void ClearWraiths()
        {
            for (int i = 0; i < _wraiths.Count; i++) _wraiths[i].Fx?.Stop();
            _wraiths.Clear();
        }

        // ═══════════════════════════════════════════════════════════
        //  로봇 — 로봇 특공대: 빛기둥으로 다섯이 내려와 미사일을 퍼붓고 빛기둥으로 돌아간다 (PD 「로봇 특공대 · 여러 개 쏘고 사라지는」)
        // ═══════════════════════════════════════════════════════════

        private const int SquadCount = 5;
        private const float SquadSeconds = 3.2f;
        private static readonly Vector2 SquadSize = new(80f, 80f);

        private void RobotSquad(Unit me)
        {
            var body = UnitGet("robot", "s") ?? UnitGet("robot");
            for (int i = 0; i < SquadCount; i++)
            {
                float a = (i * 360f / SquadCount + 90f) * Mathf.Deg2Rad;
                var at = ClampedInField(me, me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.65f) * Meters(2f));
                int idx = i;   // 루프 변수를 람다에 잡지 않는다
                UltAfter(i * 0.1f, () =>
                {
                    PlayFx("rbdrop", at + new Vector2(0f, SsrBody * 0.9f), SsrBody * 2.6f, loop: false)?.SetFrameSeconds(0.05f);
                    SpawnDeployable(at, ghostly: false, seconds: SquadSeconds, range: Meters(9f),
                                    damage: UltDamage(me, 0.6f), fireInterval: 0.3f);
                    for (int k = _deployables.Count - 1; k >= 0; k--)
                        if (_deployables[k].IsActive && Vector2.Distance(_deployables[k].Position, at) < 1f)
                        {
                            if (body != null) _deployables[k].SetSprite(body);
                            _deployables[k].SetSize(SquadSize);
                            _deployables[k].ShotKey = "missile";
                            break;
                        }
                    _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, at, 6, 90f, 36f, 0.5f, 0f);
                    Shake(2.5f);
                });
                // 끝 — 저마다 미사일 세 발을 쏘고 빛기둥으로 사라진다
                UltAfter(SquadSeconds - 0.4f + i * 0.06f, () =>
                {
                    var list = UltTargets(at, Meters(12f));
                    for (int m = 0; m < 3 && list.Count > 0; m++)
                    {
                        var t = list[(idx + m) % list.Count];
                        float d = (-30f + m * 30f) * Mathf.Deg2Rad;
                        var from = at + new Vector2(Mathf.Sin(d), Mathf.Cos(d)) * Meters(0.5f);
                        SrMissile(me, from, from + Vector2.up * Meters(2f), t, UltDamage(me, 1.4f), Meters(1.4f), false);
                    }
                });
                UltAfter(SquadSeconds + 0.05f + i * 0.06f, () =>
                    PlayFx("rbdrop", at + new Vector2(0f, SsrBody * 0.9f), SsrBody * 2.2f, loop: false)?.SetFrameSeconds(0.04f));
            }
            Shake(4f);
        }
    }
}
