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

        private static readonly Color MediumBuffColor = new(0.72f, 0.45f, 1f, 1f);

        private float _infuseLeft;
        private bool _infusePartnerDouble;
        private float _infuseMoteClock;
        private float _ritualLeft;
        private Unit _ritualCaster;
        private Impact _ritualRune;

        // ── 액티브 ──────────────────────────────────────────

        private void MediumSoulInfuse(Unit me)
        {
            float seconds = InfuseSeconds + (SpecOpen ? InfuseSpecSeconds : 0f);
            _infusePartnerDouble = InyeonOn(me);
            if (_infusePartnerDouble) seconds *= InfuseInyeonDurationMul;
            _infuseLeft = Mathf.Max(_infuseLeft, seconds);
            _infuseMoteClock = 0f;

            if (_host != null) MarkBuffed(_host, 1.6f);
            for (int i = 0; i < _subs.Count; i++)
                if (_subs[i].U != null && _subs[i].U.IsAlive) MarkBuffed(_subs[i].U, 1.2f);
            PlayFx("soulripple", me.Position, Meters(1.6f), loop: false);

            // 걸려 있는 동안 버프 받은 몸마다 발밑 보랏빛 문양이 따라다닌다 — 혼불 몇 알로는 걸렸는지 안 보였다(10-30 녹화)
            StopInfuseRunes();
            if (_host != null) AddInfuseRune(_host, 1.2f);
            for (int i = 0; i < _subs.Count; i++)
                if (_subs[i].U != null && _subs[i].U.IsAlive) AddInfuseRune(_subs[i].U, 0.9f);
        }

        private readonly List<Unit> _infuseRuneOwners = new();
        private readonly List<Impact> _infuseRunes = new();

        private void AddInfuseRune(Unit u, float meters)
        {
            var fx = PlayFloorFx("mdrune", u.Position, Meters(meters), loop: true);
            if (fx == null) return;
            fx.SetTint(MediumBuffColor);
            _infuseRuneOwners.Add(u);
            _infuseRunes.Add(fx);
        }

        private void StopInfuseRunes()
        {
            for (int i = 0; i < _infuseRunes.Count; i++) _infuseRunes[i]?.Stop();
            _infuseRunes.Clear();
            _infuseRuneOwners.Clear();
        }

        /// <summary>버프가 걸린 몸 발밑에 영매 문양 · 위로 혼불.</summary>
        private void MarkBuffed(Unit u, float meters)
        {
            PlayFloorFx("mdrune", u.Position, Meters(meters), loop: false);
            _pfx?.Spray(ParticleFxKind.GhostMote, u.Position, Vector2.up, 35f, 8, 60f, 140f,
                        new Vector2(10f, 10f), 0.8f, MediumBuffColor, spread: 30f);
        }

        // ── 필살기 ──────────────────────────────────────────

        private void MediumRitual(Unit me)
        {
            _ritualLeft = RitualSeconds;
            _ritualCaster = me;
            StopRitualRune();
            _ritualRune = PlayFloorFx("mdrune", me.Position, Meters(1.8f), loop: true);

            PlayFloorFx("mdrune", me.Position, SsrBody * 3.4f, loop: false);
            PlayFx("soulripple", me.Position, Meters(RitualSlowMeters) * 2f, loop: false);
            for (int i = 0; i < _subs.Count; i++)
                if (_subs[i].U != null && _subs[i].U.IsAlive) MarkBuffed(_subs[i].U, 1.2f);
            _pfx?.Spray(ParticleFxKind.GhostMote, me.Position, Vector2.up, 180f, 24, 120f, 320f,
                        new Vector2(12f, 12f), 0.9f, MediumBuffColor, spread: 40f);

            var near = UltTargets(me.Position, Meters(RitualSlowMeters));
            for (int i = 0; i < near.Count; i++) near[i].ApplySlow(RitualSlowPercent, RitualSlowSeconds);
            Shake(8f);
            HitStop(0.08f);
        }

        private void StopRitualRune()
        {
            if (_ritualRune != null) _ritualRune.Stop();
            _ritualRune = null;
        }

        // ── 매 프레임 ────────────────────────────────────────

        private void TickMediumBuffs(float dt)
        {
            if (_infuseLeft > 0f)
            {
                _infuseLeft -= dt;
                _infuseMoteClock -= dt;
                for (int i = _infuseRunes.Count - 1; i >= 0; i--)
                {
                    var o = _infuseRuneOwners[i];
                    if (o == null || !o.IsAlive) { _infuseRunes[i]?.Stop(); _infuseRunes.RemoveAt(i); _infuseRuneOwners.RemoveAt(i); continue; }
                    _infuseRunes[i]?.MoveTo(o.Position);
                }
                if (_infuseLeft <= 0f) StopInfuseRunes();
                if (_infuseMoteClock <= 0f && _infuseLeft > 0f)
                {
                    _infuseMoteClock = InfuseMoteEvery;
                    if (_host != null) MotesOver(_host);
                    for (int i = 0; i < _subs.Count; i++)
                        if (_subs[i].U != null && _subs[i].U.IsAlive) MotesOver(_subs[i].U);
                }
            }

            if (_ritualLeft > 0f)
            {
                _ritualLeft -= dt;
                // 광폭화한 영매가 메인에서 내려오면(사망 · 교체) 의식도 끝난다
                bool casterGone = _ritualCaster == null || !_ritualCaster.IsAlive || _ritualCaster != _host;
                if (_ritualLeft <= 0f || casterGone) { _ritualLeft = 0f; _ritualCaster = null; StopRitualRune(); }
                else if (_ritualRune != null) _ritualRune.MoveTo(_ritualCaster.Position);
            }
        }

        private void MotesOver(Unit u)
            => _pfx?.Spray(ParticleFxKind.GhostMote, u.Position + new Vector2(0f, 70f), Vector2.up, 25f, 4, 30f, 70f,
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
