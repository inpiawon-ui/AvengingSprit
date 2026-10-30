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
    /// 연출 — 혼 깃들이기 시안 `mock_infuse_free1` · 강신 의식 시안 `mock_ritual_guide`(10-30 추천안).
    /// 부품 표 `Projects/AVSR/_exchange/fx_medium1030/elements.md`. 혼 깃들이기는 보라 일색(셋 모두 같은 고리),
    /// 강신 의식은 영매만 붉은-보라 불꽃 · 붉은 금빛 진이라 광폭화가 보조 버프와 구분된다.
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

        // 연출 크기 — 부품의 게임 판 px 그대로
        private const float InfuseBurstSize = 300f;
        private const float InfuseRingSize = 130f;
        private const float InfuseUpSize = 52f;
        private const float InfuseUpLift = 82f;
        private const float RitualPillarSize = 510f;
        private const float RitualRuneSize = 460f;
        private const float RitualAuraSize = 184f;
        private const float RitualAuraLift = 34f;
        private const float RitualLinkThick = 23f;
        private const float RitualEmberEvery = 0.3f;
        private const float RitualRuneDimAfter = 0.9f;
        private const float RitualRuneRestAlpha = 0.4f;
        private const float InfuseCastPillarSize = 220f;

        private static readonly Color MediumBuffColor = new(0.72f, 0.45f, 1f, 1f);
        private static readonly Color RitualEmberColor = new(1f, 0.45f, 0.3f, 1f);

        private float _infuseLeft;
        private bool _infusePartnerDouble;
        private float _infuseMoteClock;
        private float _ritualLeft;
        private float _ritualEmberClock;
        private Unit _ritualCaster;
        private Impact _ritualRune;
        private Impact _ritualAura;
        private bool _ritualRuneDimmed;

        // 걸려 있는 동안 몸마다 발밑 고리 · 머리 위 상승 표시가 따라다닌다
        private readonly List<Unit> _infuseOwners = new();
        private readonly List<Impact> _infuseRings = new();
        private readonly List<Impact> _infuseUps = new();
        private readonly List<Unit> _ritualSubOwners = new();
        private readonly List<Impact> _ritualSubRings = new();

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
            // 시전 순간 몸마다 짧은 보라 빛줄기가 솟는다(시안 — 셋에게 동시에 「깃들었다」)
            PlayFx("mdritpillar", u.Position + new Vector2(0f, InfuseCastPillarSize * 0.5f - 16f), InfuseCastPillarSize, loop: false)
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
            _ritualEmberClock = 0f;
            StopRitualMarks();
            _ritualRuneDimmed = false;

            // 하늘 빛기둥 — 아래 끝이 발밑에 닿게 올려 단다
            PlayFx("mdritpillar", me.Position + new Vector2(0f, RitualPillarSize * 0.5f - 24f), RitualPillarSize, loop: false)
                ?.SetFrameSeconds(0.08f);

            // 의식 진 — 0.4초에 펼쳐지고 마지막 장으로 35초 동안 바닥에 머문다
            var runeFrames = FxFrames("mdritrune");
            if (runeFrames != null)
            {
                _ritualRune = TakeFloorFx(RitualRuneSize);
                _ritualRune?.PlayOver(me.Position, runeFrames, RitualRuneSize, 0.4f);
            }

            // 광폭화는 영매 몸에만
            _ritualAura = PlayFx("mdritaura", me.Position + new Vector2(0f, RitualAuraLift), RitualAuraSize, loop: true);
            _ritualAura?.SetFrameSeconds(0.1f);
            Debug.Log($"[영매] 오라 {(_ritualAura != null ? "켜짐" : "없음")} · 프레임 {(FxFrames("mdritaura")?.Length ?? 0)}");

            var linkFrames = FxFrames("mdritlink");
            for (int i = 0; i < _subs.Count; i++)
            {
                var s = _subs[i].U;
                if (s == null || !s.IsAlive) continue;
                if (linkFrames != null)
                {
                    var link = FreeImpact(RitualLinkThick);
                    link?.PlayBeam(me.Position + new Vector2(0f, 30f), s.Position + new Vector2(0f, 30f), linkFrames, RitualLinkThick);
                    link?.SetLoop(true);
                    link?.SetFrameSeconds(0.05f);
                    link?.SetLife(0.5f, 0.15f);
                }
                var ring = PlayFloorFx("mdinfring", s.Position, InfuseRingSize, loop: true);
                ring?.SetFrameSeconds(0.12f);
                _ritualSubOwners.Add(s);
                _ritualSubRings.Add(ring);
                SprayBuffMotes(s);
            }
            _pfx?.Spray(ParticleFxKind.Ember, me.Position, Vector2.up, 40f, 24, 120f, 320f,
                        new Vector2(12f, 12f), 0.9f, RitualEmberColor, spread: 40f);

            PlayFx("soulripple", me.Position, Meters(RitualSlowMeters) * 2f, loop: false);
            var near = UltTargets(me.Position, Meters(RitualSlowMeters));
            for (int i = 0; i < near.Count; i++)
            {
                near[i].ApplySlow(RitualSlowPercent, RitualSlowSeconds);
                _pfx?.Burst(ParticleFxKind.Star4, ParticleElement.Ice, near[i].Position, 9, 70f, 18f, RitualSlowSeconds, 90f);
            }
            Shake(8f);
            HitStop(0.08f);
        }

        private void StopRitualMarks()
        {
            if (_ritualRune != null) _ritualRune.Stop();
            if (_ritualAura != null) _ritualAura.Stop();
            _ritualRune = null;
            _ritualAura = null;
            for (int i = 0; i < _ritualSubRings.Count; i++) _ritualSubRings[i]?.Stop();
            _ritualSubOwners.Clear();
            _ritualSubRings.Clear();
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
                    _ritualRune?.MoveTo(at);
                    // 펼쳐진 뒤엔 반투명으로 머문다 — 35초 내내 진하면 바닥 · 몸을 덮는다(10-30 녹화)
                    if (!_ritualRuneDimmed && RitualSeconds - _ritualLeft > RitualRuneDimAfter)
                    {
                        _ritualRuneDimmed = true;
                        _ritualRune?.SetTint(new Color(1f, 1f, 1f, RitualRuneRestAlpha));
                    }
                    _ritualAura?.MoveTo(at + new Vector2(0f, RitualAuraLift));
                    for (int i = _ritualSubOwners.Count - 1; i >= 0; i--)
                    {
                        var o = _ritualSubOwners[i];
                        if (o == null || !o.IsAlive)
                        {
                            _ritualSubRings[i]?.Stop();
                            _ritualSubOwners.RemoveAt(i);
                            _ritualSubRings.RemoveAt(i);
                            continue;
                        }
                        _ritualSubRings[i]?.MoveTo(o.Position);
                    }
                    _ritualEmberClock -= dt;
                    if (_ritualEmberClock <= 0f)
                    {
                        _ritualEmberClock = RitualEmberEvery;
                        _pfx?.Spray(ParticleFxKind.Ember, at + new Vector2(0f, 20f), Vector2.up, 25f, 3, 40f, 90f,
                                    new Vector2(9f, 9f), 0.8f, RitualEmberColor, spread: 26f);
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
