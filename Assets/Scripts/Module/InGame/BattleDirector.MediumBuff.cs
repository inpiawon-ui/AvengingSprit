using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 영매 — 소환사에서 「버프 마법사」로(PD 2026-10-30 「캐릭이 많은데 소환까지 하니 안 맞다」).
    /// 안: `Projects/AVSR/_exchange/proposal_medium_buffer_1030.md` · PD 답(10-30):
    /// 액티브는 안 그대로, 필살기는 「나를 광폭화 + 파티원에게 그 효과를 30~40초」 — 쿨이 길어 한 번 쓰면 오래 가야 한다.
    ///
    /// 소환물은 하나도 없다. 효과는 피해 계산 두 길(탄 `ApplyShotHit` · 근접 `HitEnemyWith`)과
    /// 공격 간격 · 치명 자리에 배수로만 얹는다 — 누가 때렸는지는 탄의 `SourceKey` · 근접의 `HostEntry` 로 가른다.
    ///
    ///   액티브 「혼 깃들이기」  8초(Lv5 +4초) — 파티 전원 피해 +25% · 치명 +15%p. 인연 「공명 강령」이면 지속 1.5배 · 짝(구루 · 백마법사)은 피해 두 배로
    ///   필살기 「강신 의식」   35초 — 파티 전원 공속 +40% · 치명 피해 +50%, 영매 자신은 광폭화(피해 +50% · 공속 +60% 더). 시전 순간 둘레 적 2초 둔화
    ///   패시브 「영매의 인도」 메인일 때 보조 스킬 간격 −25% · 보조일 때 메인 피해 +10% (패시브 중 유일하게 보조일 때도 켜진다)
    ///
    /// 연출 — 혼 깃들이기 시안 `mock_infuse_free1` · 강신 의식 시안 `mock_ritual_free1`(PD 10-30 「첫 번째 보라색」 · 「시안이랑 똑같이」).
    /// 부품 표(크기 · 자리 · 겹침) `Projects/AVSR/_exchange/fx_medium1030/elements.md` · `fx_medium1030_r1/elements.md`.
    /// 효과 글자 판은 넣지 않는다(PD 「투 머치」) — 머리 위 작은 상승 표시로만 알린다.
    /// 광폭화는 영매에게만 빛기둥 · 오라 · 초승달 · 불꽃 테 표시, 보조는 발밑 고리 · 흰-보라 표시.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float InfuseSeconds = 8f;
        private const float InfuseSpecSeconds = 4f;          // Lv5 옵션 「지속 +4초」
        private const float InfuseDamageBonus = 0.25f;
        private const float InfuseCritAdd = 15f;              // %p
        private const float InfuseInyeonDurationMul = 1.5f;   // 인연 「공명 강령」
        private const float InfuseMoteEvery = 0.6f;

        private const float RitualSeconds = 35f;
        private const float RitualHasteBonus = 0.40f;
        private const float RitualCritDamageBonus = 0.50f;
        private const float RitualBerserkDamageBonus = 0.50f;
        private const float RitualBerserkHasteBonus = 0.60f;
        private const float RitualSlowMeters = 5f;
        private const int RitualSlowPercent = 40;
        private const float RitualSlowSeconds = 2f;

        private const float GuideSubCdCut = 0.25f;            // 메인일 때 보조 스킬 간격
        private const float GuideMainDamageBonus = 0.10f;     // 보조일 때 메인 피해

        // 연출 크기 — 부품의 게임 판 px 그대로(긴 변)
        private const float InfuseBurstSize = 300f;
        private const float InfuseRingSize = 130f;
        private const float InfuseUpSize = 52f;
        private const float InfuseUpLift = 82f;
        private const float InfuseCastPillarSize = 180f;

        // 강신 의식 — 자리는 영매 발밑 기준(위가 +). 부품 표의 「화면 아래 +」 값을 뒤집은 것
        private const float RitualRuneSize = 420f;
        private static readonly Vector2 RitualRuneAt = new(13f, -25f);
        private const float RitualPillarSize = 243f;
        private static readonly Vector2 RitualPillarAt = new(5f, 110f);
        private const float RitualAuraSize = 183f;
        private static readonly Vector2 RitualAuraAt = new(2f, 88f);
        private const float RitualMoonSize = 110f;
        private static readonly Vector2 RitualMoonAt = new(0f, 150f);
        private const float RitualUpSelfSize = 52f;
        private static readonly Vector2 RitualUpSelfAt = new(0f, 222f);
        private const float RitualSubRingSize = 105f;
        private const float RitualUpSize = 52f;
        private const float RitualUpLift = 100f;
        private const float RitualGhostSize = 100f;
        private static readonly Vector2[] RitualGhostAt = { new(-55f, 210f), new(120f, 115f), new(-130f, 120f), new(60f, 240f) };
        private const float RitualChainSize = 85f;
        private const float RitualRuneRestAlpha = 0.85f;
        private const float RitualSmokeEvery = 0.4f;
        private const float RitualMoteEvery = 0.3f;

        private static readonly Color MediumBuffColor = new(0.72f, 0.45f, 1f, 1f);

        private float _infuseLeft;
        private bool _infusePartnerDouble;
        private float _infuseMoteClock;
        private float _ritualLeft;
        private float _ritualSmokeClock;
        private float _ritualMoteClock;
        private bool _ritualRuneDimmed;
        private Unit _ritualCaster;

        // 걸려 있는 동안 몸마다 발밑 고리 · 머리 위 상승 표시가 따라다닌다
        private readonly List<Unit> _infuseOwners = new();
        private readonly List<Impact> _infuseRings = new();
        private readonly List<Impact> _infuseUps = new();

        // 강신 의식 — 영매에 붙는 것(자리 오프셋과 함께) · 보조마다 붙는 것 · 둔화된 적 사슬
        private readonly List<Impact> _ritualSelfFx = new();
        private readonly List<Vector2> _ritualSelfAt = new();
        private readonly List<Unit> _ritualSubOwners = new();
        private readonly List<Impact> _ritualSubRings = new();
        private readonly List<Impact> _ritualSubUps = new();
        private readonly List<Unit> _ritualChained = new();
        private readonly List<Impact> _ritualChains = new();

        // ── 액티브 ──────────────────────────────────────────

        private void MediumSoulInfuse(Unit me)
        {
            float seconds = InfuseSeconds + (SpecOpen ? InfuseSpecSeconds : 0f);
            _infusePartnerDouble = InyeonOn(me);
            if (_infusePartnerDouble) seconds *= InfuseInyeonDurationMul;
            _infuseLeft = Mathf.Max(_infuseLeft, seconds);
            _infuseMoteClock = 0f;

            PlayFloorFx("mdinfburst", me.Position, InfuseBurstSize, loop: false)?.SetFrameSeconds(0.08f);

            StopInfuseMarks();
            if (_host != null) AddInfuseMark(_host);
            for (int i = 0; i < _subs.Count; i++)
                if (_subs[i].U != null && _subs[i].U.IsAlive) AddInfuseMark(_subs[i].U);
        }

        private void AddInfuseMark(Unit u)
        {
            SprayBuffMotes(u);
            // 시전 순간 몸마다 가는 보라 빛줄기가 솟는다(시안 — 셋에게 동시에 「깃들었다」)
            PlayFloorFx("mdr1pillar", u.Position + new Vector2(0f, InfuseCastPillarSize * 0.5f - 16f), InfuseCastPillarSize, loop: false)
                ?.SetFrameSeconds(0.06f);
            var ring = PlayFloorFx("mdinfring", u.Position, InfuseRingSize, loop: true);
            ring?.SetFrameSeconds(0.12f);
            var up = PlayFx("mdinfup", u.Position + new Vector2(0f, InfuseUpLift), InfuseUpSize, loop: true);
            up?.SetFrameSeconds(0.15f);
            _infuseOwners.Add(u);
            _infuseRings.Add(ring);
            _infuseUps.Add(up);
        }

        private void StopInfuseMarks()
        {
            for (int i = 0; i < _infuseOwners.Count; i++)
            {
                _infuseRings[i]?.Stop();
                _infuseUps[i]?.Stop();
            }
            _infuseOwners.Clear();
            _infuseRings.Clear();
            _infuseUps.Clear();
        }

        private void SprayBuffMotes(Unit u)
            => _pfx?.Spray(ParticleFxKind.GhostMote, u.Position, Vector2.up, 35f, 8, 60f, 140f,
                           new Vector2(10f, 10f), 0.8f, MediumBuffColor, spread: 30f);

        // ── 필살기 ──────────────────────────────────────────

        private void MediumRitual(Unit me)
        {
            _ritualLeft = RitualSeconds;
            _ritualCaster = me;
            _ritualSmokeClock = 0f;
            _ritualMoteClock = 0f;
            _ritualRuneDimmed = false;
            StopRitualMarks();
            var at = me.Position;

            // 바닥 · 몸 뒤 — 의식 진은 0.5초에 펼쳐지고 마지막 장으로 35초 깔린다
            var runeFrames = FxFrames("mdr1rune");
            if (runeFrames != null)
            {
                var rune = TakeFloorFx(RitualRuneSize);
                rune?.PlayOver(at + RitualRuneAt, runeFrames, RitualRuneSize, 0.5f);
                AddRitualSelf(rune, RitualRuneAt);
            }
            AddRitualSelf(LoopFloorFx("mdr1pillar", at + RitualPillarAt, RitualPillarSize, 0.12f), RitualPillarAt);
            AddRitualSelf(LoopFloorFx("mdr1aura", at + RitualAuraAt, RitualAuraSize, 0.1f), RitualAuraAt);
            AddRitualSelf(LoopFloorFx("mdr1moon", at + RitualMoonAt, RitualMoonSize, 0.15f), RitualMoonAt);
            // 몸 앞 — 광폭화 표시(불꽃 테)는 영매만
            var upSelf = PlayFx("mdr1upself", at + RitualUpSelfAt, RitualUpSelfSize, loop: true);
            upSelf?.SetFrameSeconds(0.15f);
            AddRitualSelf(upSelf, RitualUpSelfAt);

            // 보조 — 발밑 고리 + 흰-보라 머리 위 표시
            for (int i = 0; i < _subs.Count; i++)
            {
                var s = _subs[i].U;
                if (s == null || !s.IsAlive) continue;
                var ring = LoopFloorFx("mdr1subring", s.Position, RitualSubRingSize, 0.12f);
                var up = PlayFx("mdr1up", s.Position + new Vector2(0f, RitualUpLift), RitualUpSize, loop: true);
                up?.SetFrameSeconds(0.15f);
                _ritualSubOwners.Add(s);
                _ritualSubRings.Add(ring);
                _ritualSubUps.Add(up);
                SprayBuffMotes(s);
            }

            // 피어오르는 유령 혼 — 진 가장자리에서 0.1초 간격
            for (int i = 0; i < RitualGhostAt.Length; i++)
            {
                var off = RitualGhostAt[i];
                UltAfter(0.1f * i, () =>
                {
                    if (_ritualCaster == null) return;
                    PlayFx("mdr1ghost", _ritualCaster.Position + off, RitualGhostSize, loop: false)?.SetFrameSeconds(0.1f);
                });
            }
            RitualSmoke(at, 10);
            _pfx?.Spray(ParticleFxKind.GhostMote, at, Vector2.up, 50f, 24, 120f, 320f,
                        new Vector2(12f, 12f), 0.9f, MediumBuffColor, spread: 60f);

            PlayFx("soulripple", at, Meters(RitualSlowMeters) * 2f, loop: false);
            var near = UltTargets(at, Meters(RitualSlowMeters));
            for (int i = 0; i < near.Count; i++)
            {
                near[i].ApplySlow(RitualSlowPercent, RitualSlowSeconds);
                var chain = LoopFloorFx("mdr1chain", near[i].Position, RitualChainSize, 0.1f);
                if (chain == null) continue;
                chain.SetLife(RitualSlowSeconds, 0.3f);
                _ritualChained.Add(near[i]);
                _ritualChains.Add(chain);
            }
            Shake(8f);
            HitStop(0.08f);
        }

        private Impact LoopFloorFx(string name, Vector2 at, float size, float frameSeconds)
        {
            var fx = PlayFloorFx(name, at, size, loop: true);
            fx?.SetFrameSeconds(frameSeconds);
            return fx;
        }

        private void AddRitualSelf(Impact fx, Vector2 offset)
        {
            if (fx == null) return;
            _ritualSelfFx.Add(fx);
            _ritualSelfAt.Add(offset);
        }

        private void RitualSmoke(Vector2 at, int count)
            => _pfx?.Spray(ParticleFxKind.Smoke, at + new Vector2(0f, 10f), Vector2.up, 70f, count, 50f, 130f,
                           new Vector2(34f, 48f), 1.4f, MediumBuffColor, spread: 140f, element: ParticleElement.Dust);

        private void StopRitualMarks()
        {
            for (int i = 0; i < _ritualSelfFx.Count; i++) _ritualSelfFx[i]?.Stop();
            _ritualSelfFx.Clear();
            _ritualSelfAt.Clear();
            for (int i = 0; i < _ritualSubOwners.Count; i++)
            {
                _ritualSubRings[i]?.Stop();
                _ritualSubUps[i]?.Stop();
            }
            _ritualSubOwners.Clear();
            _ritualSubRings.Clear();
            _ritualSubUps.Clear();
        }

        // ── 매 프레임 ────────────────────────────────────────

        private void TickMediumBuffs(float dt)
        {
            if (_infuseLeft > 0f)
            {
                _infuseLeft -= dt;
                _infuseMoteClock -= dt;
                for (int i = _infuseOwners.Count - 1; i >= 0; i--)
                {
                    var o = _infuseOwners[i];
                    if (o == null || !o.IsAlive)
                    {
                        _infuseRings[i]?.Stop();
                        _infuseUps[i]?.Stop();
                        _infuseOwners.RemoveAt(i);
                        _infuseRings.RemoveAt(i);
                        _infuseUps.RemoveAt(i);
                        continue;
                    }
                    _infuseRings[i]?.MoveTo(o.Position);
                    _infuseUps[i]?.MoveTo(o.Position + new Vector2(0f, InfuseUpLift));
                }
                if (_infuseLeft <= 0f) StopInfuseMarks();
                if (_infuseMoteClock <= 0f && _infuseLeft > 0f)
                {
                    _infuseMoteClock = InfuseMoteEvery;
                    for (int i = 0; i < _infuseOwners.Count; i++) MotesOver(_infuseOwners[i]);
                }
            }

            // 사슬은 둔화된 적을 따라간다(수명은 Impact 가 센다)
            for (int i = _ritualChains.Count - 1; i >= 0; i--)
            {
                var e = _ritualChained[i];
                var c = _ritualChains[i];
                if (c == null || !c.IsActive || e == null || !e.IsAlive)
                {
                    c?.Stop();
                    _ritualChains.RemoveAt(i);
                    _ritualChained.RemoveAt(i);
                    continue;
                }
                c.MoveTo(e.Position);
            }

            if (_ritualLeft > 0f)
            {
                _ritualLeft -= dt;
                // 광폭화한 영매가 메인에서 내려오면(사망 · 교체) 의식도 끝난다
                bool casterGone = _ritualCaster == null || !_ritualCaster.IsAlive || _ritualCaster != _host;
                if (_ritualLeft <= 0f || casterGone)
                {
                    _ritualLeft = 0f;
                    _ritualCaster = null;
                    StopRitualMarks();
                }
                else
                {
                    var at = _ritualCaster.Position;
                    for (int i = 0; i < _ritualSelfFx.Count; i++) _ritualSelfFx[i]?.MoveTo(at + _ritualSelfAt[i]);
                    // 펼쳐진 진은 조금 가라앉힌다 — 35초 내내 최대 밝기면 바닥의 적 · 탄이 묻힌다
                    if (!_ritualRuneDimmed && RitualSeconds - _ritualLeft > 0.9f && _ritualSelfFx.Count > 0)
                    {
                        _ritualRuneDimmed = true;
                        _ritualSelfFx[0].SetTint(new Color(1f, 1f, 1f, RitualRuneRestAlpha));
                    }
                    for (int i = _ritualSubOwners.Count - 1; i >= 0; i--)
                    {
                        var o = _ritualSubOwners[i];
                        if (o == null || !o.IsAlive)
                        {
                            _ritualSubRings[i]?.Stop();
                            _ritualSubUps[i]?.Stop();
                            _ritualSubOwners.RemoveAt(i);
                            _ritualSubRings.RemoveAt(i);
                            _ritualSubUps.RemoveAt(i);
                            continue;
                        }
                        _ritualSubRings[i]?.MoveTo(o.Position);
                        _ritualSubUps[i]?.MoveTo(o.Position + new Vector2(0f, RitualUpLift));
                    }
                    _ritualSmokeClock -= dt;
                    if (_ritualSmokeClock <= 0f)
                    {
                        _ritualSmokeClock = RitualSmokeEvery;
                        RitualSmoke(at, 2);
                    }
                    _ritualMoteClock -= dt;
                    if (_ritualMoteClock <= 0f)
                    {
                        _ritualMoteClock = RitualMoteEvery;
                        _pfx?.Spray(ParticleFxKind.GhostMote, at, Vector2.up, 60f, 3, 40f, 110f,
                                    new Vector2(9f, 9f), 0.9f, MediumBuffColor, spread: 120f);
                    }
                }
            }
        }

        private void MotesOver(Unit u)
            => _pfx?.Spray(ParticleFxKind.GhostMote, u.Position + new Vector2(0f, 70f), Vector2.up, 25f, 2, 30f, 70f,
                           new Vector2(8f, 8f), 0.7f, MediumBuffColor, spread: 20f);

        // ── 계산 자리에서 부르는 배수 ────────────────────────

        /// <summary>때린 몸(키)에 붙는 피해 배수 — 혼 깃들이기 · 광폭화 · 영매의 인도(보조일 때).</summary>
        private float MediumDamageMul(string attackerKey)
        {
            float m = 1f;
            if (_infuseLeft > 0f)
            {
                bool partner = _infusePartnerDouble && attackerKey != null && IsInfusePartner(attackerKey);
                m *= 1f + InfuseDamageBonus * (partner ? 2f : 1f);
            }
            if (_ritualLeft > 0f && _ritualCaster != null && attackerKey == _ritualCaster.Key)
                m *= 1f + RitualBerserkDamageBonus;
            if (attackerKey != null && _host != null && attackerKey == _host.Key && _host.Key != "medium" && MediumIsSub)
                m *= 1f + GuideMainDamageBonus;
            return m;
        }

        /// <summary>공격 간격 배수 — 강신 의식(파티) · 광폭화(영매 자신).</summary>
        private float MediumIntervalMul(Unit u)
        {
            if (_ritualLeft <= 0f) return 1f;
            float haste = RitualHasteBonus + (u != null && u == _ritualCaster ? RitualBerserkHasteBonus : 0f);
            return 1f / (1f + haste);
        }

        private float MediumCritAdd => _infuseLeft > 0f ? InfuseCritAdd : 0f;
        private float MediumCritDamageAdd => _ritualLeft > 0f ? RitualCritDamageBonus : 0f;

        /// <summary>영매의 인도(메인일 때) — 보조 스킬 간격 배수. 대표 수치라 패시브 레벨과 함께 오른다(25 → 35%).</summary>
        private float MediumSubCdMul => PassiveHostKey == "medium" ? 1f - Mathf.Min(0.5f, PassiveFrac(GuideSubCdCut)) : 1f;

        private bool MediumIsSub
        {
            get
            {
                if (!PartyOn) return false;
                for (int i = 0; i < _subs.Count; i++)
                    if (_subs[i].U != null && _subs[i].U.IsAlive && _subs[i].U.Key == "medium") return true;
                return false;
            }
        }

        private static bool IsInfusePartner(string key)
            => s_inyeon.TryGetValue("medium", out var iy) && iy.Partners != null && System.Array.IndexOf(iy.Partners, key) >= 0;
    }
}
