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
            HoldSkillCharge(MiniGolemLifeSeconds + (SpecOpen ? MiniGolemSpecSeconds : 0f));   // 골렘이 사는 동안
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
            HoldSkillCharge(_evadeSeconds);
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
                // 연기 그림(`slam`)은 줄여도 끌려온 무리 위에 회색 덩어리로 1초 가까이 남아 적을 가렸다 — 흙먼지 알갱이만
                _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, front, 6, 160f, 30f, 0.5f, 0f);
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
        //  코만도(레이저) — 프리즘 레이저: 굵은 빔이 첫 적에 꽂히고, 거기서 갈래로 갈라져 둘레 적에게 꺾여 든다
        //  PD 2026-10-21 「몬스터가 일자로 있지 않아서」 · 「ㄷ(프리즘 분열)」 — 예전 충전 레이저는 14 m 일직선이라 한두 놈만 맞았다
        // ═══════════════════════════════════════════════════════════

        private const float PrismRangeMeters = 10f;
        private const float PrismSplitMeters = 5f;
        private const int PrismBranches = 4;
        private const float PrismMainMul = 4f;
        private const float PrismBranchMul = 2.5f;
        private const float PrismBranchDraw = 150f;

        private void PrismLaser(Unit me)
        {
            var t = NearestEnemy(me.Position, Meters(PrismRangeMeters));
            var dir = t != null ? (t.Position - me.Position).normalized : me.Facing;
            me.SetFacing(dir);
            var charge = PlayFx("lzcharge", me.MuzzlePosition, SsrBody * 1.6f, loop: false) ?? PlayFx("wfgather", me.MuzzlePosition, 140f, loop: false);
            charge?.SetFrameSeconds(0.085f);
            _pfx?.Spray(ParticleFxKind.Streak, me.MuzzlePosition + dir * 60f, -dir, 70f, 14, 120f, 260f, new Vector2(30f, 4f), 0.4f,
                        new Color(0.5f, 0.9f, 1f, 1f));
            UltInvuln(1.2f);
            UltAfter(0.5f, () =>
            {
                var from = me.MuzzlePosition;
                var first = t != null && t.IsAlive ? t : NearestEnemy(me.Position, Meters(PrismRangeMeters));
                var hitAt = first != null ? first.Position : from + dir * Meters(PrismRangeMeters);
                PrismBeam(from, hitAt, LzBeamDraw, 0.5f);
                if (first == null) { Shake(6f); return; }
                UltHit(me, first, PrismMainMul);
                (PlayFx("lzblast", hitAt, SsrBody * 2.4f, loop: false) ?? PlayFx("lzhit", hitAt, 110f, loop: false))?.SetFrameSeconds(0.05f);
                PlayFx("lzring", hitAt, SsrBody * 2f, loop: false);   // 갈라지는 자리 — 프리즘
                Shake(8f);
                HitStop(0.08f);

                // 첫 적 둘레에서 가까운 넷에게 갈래가 꺾여 든다
                var near = UltTargets(hitAt, Meters(PrismSplitMeters));
                int n = 0;
                for (int i = 0; i < near.Count && n < PrismBranches; i++)
                {
                    var e = near[i];
                    if (e == first) continue;
                    float delay = 0.08f + n * 0.05f;
                    n++;
                    UltAfter(delay, () =>
                    {
                        if (e == null || !e.IsAlive) return;
                        PrismBeam(hitAt, e.Position, PrismBranchDraw, 0.3f);
                        UltHit(me, e, PrismBranchMul);
                        (PlayFx("lzblast", e.Position, SsrBody * 1.7f, loop: false) ?? PlayFx("lzhit", e.Position, 90f, loop: false))?.SetFrameSeconds(0.05f);
                        Shake(3f);
                    });
                }
            });
        }

        private void PrismBeam(Vector2 from, Vector2 to, float draw, float life)
        {
            var frames = FxFrames("lzbeam");
            if (frames == null) { UltBeam("laser_wide", from, to, draw * 0.42f, 0.05f); return; }
            var beam = FreeImpact(draw);
            beam?.PlayBeam(from, to, frames, draw);
            beam?.SetLoop(true);
            beam?.SetFrameSeconds(0.04f);
            beam?.SetLife(life, 0.15f);
        }

        // ═══════════════════════════════════════════════════════════
        //  화이트 위저드 액티브 — 중력진: 적이 가장 몰린 자리에 황금 마법진 → 1.2초 동안 가운데로 빨아들이며 깎다가
        //  → 빛 폭발로 마무리 (PD 2026-10-21 「냇 할로우 중력진」 · 시안 in/mock_wizard_gravity.png)
        //  그림은 마법진 `gvcircle` · 소용돌이 `gvswirl` · 마무리 `gvburst`. 아직 없으면 조준 원 · `wwburst` 로 돈다.
        // ═══════════════════════════════════════════════════════════

        private const float GravityRangeMeters = 8f;
        // 2.6 m 는 마법진이 화면 폭의 83% 를 덮어 적이 다 가려졌다(시안은 약 67%) — 녹화 검수 2026-10-21
        // 2.2 m 도 PD 「조금 더 줄여」(2026-10-21) → 1.8 m
        private const float GravityMeters = 1.8f;
        private const float GravitySpecMeters = 2.2f;   // Lv5 옵션 — 범위가 넓어진다
        private const float GravitySwirlDelay = 0.15f;  // 시안 순서 — 마법진이 먼저 깔리고 소용돌이가 돈다
        private const float GravityPullSeconds = 1.2f;
        private const int GravitySteps = 24;            // 0.05초마다 끌어당긴다
        private const int GravityTickEvery = 6;         // 0.3초마다 한 번 깎는다
        private const float GravityPullRate = 0.18f;    // 한 걸음에 남은 거리의 18% — 처음엔 확 끌리고 가운데서 느려진다
        private const float GravityHoldMeters = 0.35f;  // 한 점에 겹치지 않게 가운데서 이만큼 떨어져 선다
        private const float GravityTickMul = 0.3f;
        private const float GravityBurstMul = 2.4f;
        private static readonly Color GravityMarkTint = new(1f, 0.85f, 0.35f, 1f);

        private void GravityWell(Unit me)
        {
            var near = UltTargets(me.Position, Meters(GravityRangeMeters));
            if (near.Count == 0) { PlayFx("holy_beam", me.MuzzlePosition, 96f, loop: false); return; }
            float r = Meters(SpecOpen ? GravitySpecMeters : GravityMeters) * _buffs.AoeMul;
            var c = DensestSpot(near, r);
            me.SetFacing(c - me.Position);
            me.PlayAttack();

            float life = GravityPullSeconds + 0.25f;
            HoldSkillCharge(GravityPullSeconds);
            var circle = PlayFloorFx("gvcircle", c, r * 2.2f, loop: true);
            if (circle != null) { circle.SetFrameSeconds(0.08f); circle.SetLife(life, 0.2f); }
            else
            {
                var mark = PlayFloorFx("castring", c, r * 2f, loop: true);
                if (mark != null) { mark.SetTint(GravityMarkTint); mark.SetLife(life, 0.2f); }
            }
            UltAfter(GravitySwirlDelay, () =>
            {
                // 소용돌이는 마법진 안쪽 테두리까지만 — 바깥 룬 띠는 보이게 둔다
                var swirl = PlayFx("gvswirl", c, r * 1.9f, loop: true);
                if (swirl != null) { swirl.SetFrameSeconds(0.07f); swirl.SetLife(GravityPullSeconds - GravitySwirlDelay, 0.2f); }
            });

            int tickDmg = SkillDamage(me, GravityTickMul);
            float hold = Meters(GravityHoldMeters);
            for (int s = 1; s <= GravitySteps; s++)
            {
                bool tick = s % GravityTickEvery == 0;   // 루프 변수를 람다에 잡지 않는다
                UltAfter(GravityPullSeconds * s / GravitySteps, () =>
                {
                    if (_host != me || me == null || !me.IsAlive) return;
                    var list = new List<Unit>(EnemiesInRange(c, r * 1.15f));
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        var off = e.Position - c;
                        float dist = off.magnitude;
                        // 보스는 끌지 않는다(사슬 끌어오기 · 필살기와 같은 규칙) — 깎기만 한다
                        if (!e.IsBoss && dist > hold)
                            e.Position = ClampedInField(e, c + off / dist * (dist - (dist - hold) * GravityPullRate));
                        if (!tick) continue;
                        HitEnemyWith(e, tickDmg, me.Profile);
                        _pfx?.Dart(e.Position, c, ParticleElement.Fire, 0.3f, 1);
                    }
                });
            }
            UltAfter(GravityPullSeconds + 0.05f, () =>
            {
                if (_host != me || me == null || !me.IsAlive) return;
                int dmg = SkillDamage(me, GravityBurstMul);
                var hit = new List<Unit>(EnemiesInRange(c, r));
                for (int i = 0; i < hit.Count; i++) HitEnemyWith(hit[i], dmg, me.Profile);
                (PlayFx("gvburst", c, r * 2.6f, loop: false) ?? PlayFx("wwburst", c, r * 2.2f, loop: false))?.SetFrameSeconds(0.05f);
                _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Fire, c, 16, 260f, 40f, 0.7f, 0f);
                Shake(6f);
                HitStop(0.06f);
            });
        }

        /// <summary>반경 안에 이웃이 가장 많은 적의 자리 — 무리의 한가운데.</summary>
        private static Vector2 DensestSpot(List<Unit> list, float radius)
        {
            float r2 = radius * radius;
            int best = -1;
            var at = list[0].Position;
            for (int i = 0; i < list.Count; i++)
            {
                int n = 0;
                for (int j = 0; j < list.Count; j++)
                    if ((list[j].Position - list[i].Position).sqrMagnitude <= r2) n++;
                if (n > best) { best = n; at = list[i].Position; }
            }
            return at;
        }

        // ═══════════════════════════════════════════════════════════
        //  드라군 — 메테오: 가장 센 적에게 조준 → 하늘에서 불덩이가 떨어져 터지고, 그 자리가 4초 동안 불바다가 된다
        //  PD 2026-10-21 「용 돌격 말고 가장 센 몬스터에 메테오 · 터질 때 데미지랑 거기를 용암 지대로」
        //  그림은 시안(in/mock_dragoon_meteor.png)대로 받은 운석 `mtrock` · 폭발 빛 `mtblast` · 바위 파편 `mtdebris` · 용암 `mtlava`.
        //  아직 없으면 예전 그림(드라군 불덩이 탄 · `dgblast` · 불바다)으로 돈다.
        // ═══════════════════════════════════════════════════════════

        private const float MeteorFallSeconds = 0.85f;
        private const float MeteorSize = 190f;
        private const float MeteorArtSize = 320f;   // 전용 운석 — 바위(칸 256 중 약 100 px)가 왼쪽 아래, 꼬리가 오른쪽 위
        private const float MeteorBlastMeters = 2.6f;
        private const float MeteorBlastMul = 6f;
        private const float MeteorFieldMeters = 2.4f;
        private const float MeteorFieldSeconds = 4f;
        private const float MeteorFieldMul = 0.6f;
        private static readonly Color MeteorMarkTint = new(1f, 0.45f, 0.2f, 1f);

        private void DragoonMeteor(Unit me)
        {
            var t = UltStrongest(me.Position) ?? NearestEnemy(me.Position);
            if (t == null) { UltFallback(me); return; }
            var at = t.Position;
            me.PlayAttack();
            // 예고 — 발밑 조준 원
            var mark = PlayFloorFx("castring", at, Meters(MeteorBlastMeters) * 2f, loop: true);
            if (mark != null) { mark.SetTint(MeteorMarkTint); mark.SetLife(MeteorFallSeconds + 0.1f, 0.1f); }
            PlayFx("dgrise", me.Position, SsrBody * 1.6f, loop: false);

            // 떨어지는 불덩이 — 전용 운석은 그림의 꼬리 방향대로 오른쪽 위(약 31°)에서 온다
            var rockArt = FxFrames("mtrock");
            var frames = rockArt ?? ShotFrames("dragoon");
            float size = rockArt != null ? MeteorArtSize : MeteorSize;
            // 6 m 에서 출발하면 화면 밖에서 시간을 다 쓰고 마지막 0.2초만 보였다(녹화 검수) — 화면 안에서 출발
            var sky = rockArt != null ? at + new Vector2(Meters(4.5f), Meters(2.7f)) : at + new Vector2(-Meters(3f), Meters(9f));
            // 운석 그림은 바위가 칸 가운데가 아니라 왼쪽 아래(칸 256 중 x 64 · y 162)에 있다 — 그만큼 옮겨 바위가 조준 원에 닿게
            var lift = rockArt != null ? new Vector2(size * 0.25f, size * 0.13f) : Vector2.zero;
            var rock = frames != null ? FreeImpact(size) : null;
            rock?.Play(sky + lift, frames, size, loop: true);
            if (rockArt != null) rock?.SetFrameSeconds(0.07f);
            var d = at - sky;
            if (rock != null)
                rock.transform.localEulerAngles = rockArt != null ? Vector3.zero
                                                : new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            const int Steps = 14;
            for (int s = 1; s <= Steps; s++)
            {
                float k = s / (float)Steps;
                UltAfter(MeteorFallSeconds * k, () =>
                {
                    var p = Vector2.Lerp(sky, at, Mathf.Pow(k, 1.4f));   // 점점 빨라진다(제곱은 처음이 너무 느려 안 보였다)
                    rock?.MoveTo(p + lift);
                    _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Fire, p, 3, 60f, 30f, 0.4f, 0f);
                });
            }
            UltAfter(MeteorFallSeconds + 0.02f, () =>
            {
                rock?.Stop();
                // 착탄 — 가장 셀 때. 빛(더하기) 위에 검은 바위 파편을 겹친다
                bool art = Qx("mtblast");
                if (art)
                {
                    PlayFx("mtblast", at, SsrBody * 4.6f, loop: false)?.SetFrameSeconds(0.06f);
                    PlayFx("mtdebris", at, SsrBody * 4.6f, loop: false)?.SetFrameSeconds(0.06f);
                }
                else
                {
                    PlayFx("dgblast", at, SsrBody * 4.2f, loop: false)?.SetFrameSeconds(0.05f);
                    PlayFx("elcrack", at, Meters(MeteorBlastMeters) * 2f, loop: false);
                }
                UltAoe(me, at, MeteorBlastMeters, MeteorBlastMul, art ? null : "slam", 2f, knockMeters: 1f);
                _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Fire, at, 18, 320f, 60f, 0.8f, 0f);
                _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, at, 10, 200f, 50f, 0.9f, 0f);
                Shake(14f);
                HitStop(0.12f);
                // 남는 것 — 4초 용암 지대(밟은 적이 탄다). 용암 그림은 가로로 긴 탑뷰 타원이라 비율을 지킨다
                bool lava = Qx("mtlava");
                SpawnField(at, Meters(MeteorFieldMeters) * _buffs.AoeMul, MeteorFieldSeconds, FieldEffect.Burn,
                           UltDamage(me, MeteorFieldMul), fromPlayer: true, artKey: lava ? "mtlava" : FireFieldArt,
                           keepAspect: lava);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  사신 — 원혼: 몸에서 떠올라 적에게 날아가 붙어 터진다(망자 군단의 「유령」)
        // ═══════════════════════════════════════════════════════════

        // PD 2026-10-21 「사신은 또 그냥 해골만 나오네 유령은 왜 반영이 안 되는 거지」 —
        //   원혼은 적이 있을 때만 0.45초 날아갔는데, 1.6초 뒤엔 해골이 방을 이미 비워 한 번도 안 떴다.
        //   이제 해골과 함께 **바로 떠올라 사신 둘레에 머물다가** 내리꽂힌다. 적이 없으면 흩어진다.
        private const float WraithFlySeconds = 0.45f;
        private const float WraithHoverSeconds = 0.6f;
        private const float WraithSize = 150f;
        private const float WraithRingMeters = 2.2f;   // 떠 있는 둘레(가로 반경, 세로는 절반)

        private sealed class Wraith
        {
            public Impact Fx;
            public Unit Target;
            public Unit Caster;
            public Vector2 From;
            public Vector2 Hold;    // 떠올라 머무는 자리
            public Vector2 Last;
            public float Hover;     // 남은 머묾 시간 — 0 이 되면 내리꽂힌다
            public float Phase;     // 둥실거림이 다 같이 움직이지 않게
            public float T;
        }

        private readonly List<Wraith> _wraiths = new();

        /// <param name="target">없거나 머무는 사이 죽으면 그때 가장 가까운 적으로 바꾼다.</param>
        private void LaunchWraith(Unit me, Unit target, int slot, int slots)
        {
            // 둘레 1.2 m 는 여섯이 겹쳐 파란 덩어리로 보였다(PD 「더 넓게 벌려서 하나씩 보이게」) — 2.2 m 타원 + 높이를 엇갈리게
            float a = slot * Mathf.PI * 2f / Mathf.Max(1, slots);
            float lift = SsrBody * (slot % 2 == 0 ? 0.7f : 1.1f);
            var hold = me.Position + new Vector2(Mathf.Cos(a) * Meters(WraithRingMeters), lift + Mathf.Sin(a) * Meters(WraithRingMeters * 0.5f));
            var fx = FxFrames("mdwraith") != null ? TakeLoopFx("mdwraith", me.Position, WraithSize) : null;
            fx?.SetFrameSeconds(0.07f);
            _wraiths.Add(new Wraith
            {
                Fx = fx, Target = target, Caster = me, From = me.Position, Hold = hold,
                Last = target != null ? target.Position : hold, Hover = WraithHoverSeconds, Phase = a,
            });
            if (fx == null) _pfx?.Dart(me.Position, hold, ParticleElement.Ice, WraithHoverSeconds, 4);
        }

        private void TickWraiths(float dt)
        {
            for (int i = _wraiths.Count - 1; i >= 0; i--)
            {
                var w = _wraiths[i];
                if (w.Hover > 0f)
                {
                    // 몸에서 빠져나와 둘레 자리로 떠오르고, 둥실거리며 머문다
                    w.Hover -= dt;
                    w.Phase += dt * 6f;
                    float rise = Ease.OutCubic(Mathf.Clamp01((WraithHoverSeconds - w.Hover) / (WraithHoverSeconds * 0.5f)));
                    var hover = Vector2.Lerp(w.From, w.Hold, rise) + new Vector2(0f, Mathf.Sin(w.Phase) * 6f);
                    w.Fx?.MoveTo(hover);
                    if (w.Hover > 0f) continue;
                    w.From = hover;
                    if (w.Target == null || !w.Target.IsAlive) w.Target = NearestEnemy(hover);
                    if (w.Target == null)
                    {
                        w.Fx?.SetLife(0.3f, 0.3f);   // 남은 적이 없다 — 그 자리에서 흩어진다
                        _wraiths.RemoveAt(i);
                        continue;
                    }
                    w.Last = w.Target.Position;
                }
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
