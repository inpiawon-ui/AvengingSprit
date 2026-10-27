using System.Collections.Generic;
using Game.Module.Events;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 필살기 — 빙의할 때 한 번 쓸 수 있고, 쓰면 다음 빙의까지 잠긴다 (PD 2026-10-15
    /// 「영혼 탈출을 빼고 액티브 · 필살기 버튼 두 개」 · 「필살기는 한 번, 대신 빙의하면 다시」).
    ///
    /// 버튼은 예전 빙의 · 탈출 버튼 자리다 — 유령이면 「빙의」, 몸이 있으면 「필살기」.
    /// 액티브(`CastActiveSkill`)는 평타로 채워 컷인 없이 나가고, 필살기만 컷인 · 멈춤(`BeginCast`)을 쓴다.
    /// 기획 · 시안: `Projects/AVSR/_exchange/doc_skill_concept` (컨셉 단일 출처 `Projects/AVSR/Tools/skill_concepts.py`)
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>이 몸에서 아직 필살기를 안 썼다.</summary>
        private bool _ultReady;

        // ── 못 가진 몸은 1회 체험 (PD 2026-10-15 「그 표대로 1회 체험으로」) ──────────
        //
        // 가진 몸(숙련도 ≥ 1)은 빙의할 때마다 1회. 못 가진 몸은 **이 판에서 그 몸으로 처음 빙의할 때만** 1회.
        // 판이 바뀌면(이 컴포넌트가 새로 생기면) 다시 1회가 생긴다 — 판마다 한 번씩은 「이 몸 필살기 좋았지」를 본다.
        // 나중에 「필살기 초기화」 카드가 생기면 두 경우 모두 `_ultReady` 만 다시 켜면 된다.
        private readonly HashSet<string> _ultTrialUsed = new();

        /// <summary>그 몸을 가졌는가 — 못 가졌으면 필살기는 판당 1회 체험.</summary>
        private bool OwnsHostForUlt(string hostKey)
        {
#if UNITY_EDITOR
            // 녹화 도구 전용 — 시험 세이브는 전부 숙련도 10 이라 「못 가진 몸」을 흉내 낸다(세이브는 안 건드린다)
            if (EditorUnownedHost != null && EditorUnownedHost == hostKey) return false;
#endif
            return _player == null || !_player.IsReady || _player.GetMastery(hostKey) >= 1;
        }

#if UNITY_EDITOR
        /// <summary>에디터 녹화 전용 — 이 몸을 못 가진 몸으로 친다(필살기 1회 체험 확인).</summary>
        public static string EditorUnownedHost;
#endif

        /// <summary>지금 몸이 못 가진 몸이고 이번이 체험 1회다(아직 안 씀).</summary>
        public bool IsUltTrial => _host != null && _ultReady && !OwnsHostForUlt(_host.Key);

        /// <summary>지금 몸이 못 가진 몸이고 이 판의 체험을 이미 썼다 — 다시 빙의해도 안 채워진다.</summary>
        public bool IsUltTrialSpent => _host != null && !_ultReady && !OwnsHostForUlt(_host.Key)
                                       && _ultTrialUsed.Contains(_host.Key);

        /// <summary>빙의할 때 필살기를 채울지 — 가진 몸은 늘, 못 가진 몸은 이 판에서 처음일 때만.</summary>
        private bool UltReadyOnEnter(string hostKey) => OwnsHostForUlt(hostKey) || !_ultTrialUsed.Contains(hostKey);

        // ── 「필살기 가능」 표시 (PD 2026-10-15 C안 승인) ─────────────────
        //
        // 지금 필살기를 못 쓸 때(다 썼거나 유령)만 띄운다 — 가진 몸은 빙의마다 차므로 늘 띄우면 모든 적 호스트에 별이 붙어
        // 아무 말도 안 하게 된다. 「필살기를 다 썼다 → 저 몸을 뺏으면 다시 쓸 수 있다」가 이 표시의 할 말이다.
        // 못 가진 몸이 이 판의 체험을 이미 썼으면 별이 없다.
        private Sprite[] _ultMarkFrames;
        private bool _ultMarkLoaded;
        private bool _ultMarksShown;

        private void TickUltReadyMarks()
        {
            if (!_ultMarkLoaded) { _ultMarkLoaded = true; _ultMarkFrames = FxFrames("ultready"); }
            bool want = _running && _ultMarkFrames != null && (_host == null || !_ultReady);
            if (!want && !_ultMarksShown) return;
            _ultMarksShown = want;
            Sprite frame = want ? _ultMarkFrames[(int)(Time.time * 6f) % _ultMarkFrames.Length] : null;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                bool show = want && e.IsHostBody && e.IsAlive && !e.IsBoss && e.Side == UnitSide.Enemy
                            && UltReadyOnEnter(e.Key);
                e.SetUltReadyMark(show ? frame : null);
            }
        }

        // ── 필살기 초기화 카드 (PD 2026-10-15 D안 승인 · c013) ─────────────
        //
        // 고르는 순간 지금 몸의 필살기를 다시 채운다. 이미 차 있거나 유령이면 **한 번 남겨 둔다** —
        // 다음에 필살기가 비었을 때 버튼을 누르면 그것을 쓴다. 고른 카드가 그냥 사라지면 안 된다.
        // 못 가진 몸의 체험을 다 쓴 뒤에도 먹힌다(카드 값어치).
        private int _ultSpare;

        // ── 필살기 쿨 (PD 2026-10-28 「한 번만 쓰니 재미가 떨어진다 · 쿨타임 5분으로 일단」) ─────────
        //
        // 가진 몸은 쓰고 나서 `GameConfig.UltCooldownSeconds` 가 지나면 같은 몸으로도 다시 찬다.
        // 빙의하면 예전처럼 바로 찬다. 못 가진 몸의 1회 체험은 그대로(쿨로 다시 차지 않는다)
        private float _ultCooldownLeft;

        private float UltCooldownFull => _config != null && _config.UltCooldownSeconds > 0f ? _config.UltCooldownSeconds : 300f;

        /// <summary>필살기가 다시 찰 때까지 남은 초(쿨이 안 돌면 0).</summary>
        public float UltCooldownRemain => _host != null && !_ultReady ? _ultCooldownLeft : 0f;

        /// <summary>필살기 쿨이 찬 비율(0 → 1). 쿨이 안 돌면 1.</summary>
        public float UltCooldownRatio => UltCooldownRemain <= 0f ? 1f : 1f - _ultCooldownLeft / UltCooldownFull;

        private void TickUltCooldown(float dt)
        {
            if (!_running || _ultCooldownLeft <= 0f) return;
            _ultCooldownLeft -= dt;
            if (_ultCooldownLeft > 0f) return;
            _ultCooldownLeft = 0f;
            if (_host != null && !_ultReady && OwnsHostForUlt(_host.Key)) { _ultReady = true; UltResetFx(); }
        }

        private void ApplyUltResetCard()
        {
            if (_host != null && !_ultReady) { _ultReady = true; UltResetFx(); }
            else _ultSpare++;
        }

        private void UltResetFx()
        {
            if (_host == null) return;
            _pfx?.Burst(ParticleFxKind.Star4, ParticleElement.Curse, _host.Position, 10, 180f, 16f, 0.5f, 0f);
            _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Curse, _host.Position, 4, 60f, 70f, 0.5f, 0f);
        }

        /// <summary>필살기 버튼을 눌러 쓸 수 있는가(몸 · 봉인 · 1회).</summary>
        public bool IsUltReady => _running && _ultReady && _host != null && !IsSkillSealed(_host.Key);

        /// <summary>지금 몸이 있는가 — 필살기 버튼 칸이 「빙의」 대신 「필살기」를 보여야 하는가.</summary>
        public bool HasHostBody => _host != null;

        public void TryUltimate()
        {
            if (!_running || IsCastFrozen || IsChanneling) return;
            var me = _host;
            if (me == null) return;
            if (!_ultReady && _ultSpare > 0)
            {
                // 필살기 초기화 카드로 남겨 둔 한 번
                _ultSpare--;
                _ultReady = true;
            }
            if (!_ultReady)
            {
                // 체험을 다 쓴 몸 — 왜 안 나가는지 버튼 위에 알린다
                if (IsUltTrialSpent) _bus.Publish(new PossessDeniedEvent { UltTrialSpent = true });
                return;
            }
            if (IsSkillSealed(me.Key)) return;
            _ultReady = false;
            if (!OwnsHostForUlt(me.Key)) _ultTrialUsed.Add(me.Key);
            else _ultCooldownLeft = UltCooldownFull;
            BeginCast(me);   // 컷인 → 멈춤 → `ReleaseCast` → `CastUltimate`
        }

        /// <summary>필살기 — 컨셉 기획의 「크게 터뜨리는」 스킬.</summary>
        private void CastUltimate(Unit me)
        {
            if (me == null) return;
            global::Game.Module.Common.GameSound.Skill(me.Key);
            OpenSkillChainWindow();
            switch (me.Key)
            {
                case "amazon":           AmazonLightningDance(me); break;   // 번개 무희
                case "thug":             ThugSpinFire(me);      break;   // 회전 난사
                case "commando_grenade": CommandoStickyBomb(me); break;  // C4
                case "baseball":         SluggerFullSwing(me);  break;   // 풀스윙 회오리
                case "medium":           GiantWraith(me);       break;   // 거대 원혼(PD 2026-10-21) — 골렘은 액티브(미니 골렘)로 갔다
                case "gangster":         UltGangster(me);       break;   // 청부 처형
                case "hopper":           UltHopper(me);         break;   // 킬 포인트
                case "ninja_chain":      SrNinjaChain(me);      break;   // 사슬 돌격 연격(SR — BattleDirector.UltSr)
                case "death":            UltDeath(me);          break;   // 망자 군단
                case "robot":            RobotSquad(me);        break;   // 로봇 특공대(PD 2026-10-21)
                case "ninja":            SrNinja(me);           break;   // 그림자 삼분신(SR)
                case "hopper_smg":       SrHopperSmg(me);       break;   // 돌진 난사(SR)
                case "vampire":          BloodPool(me);         break;   // 피의 웅덩이(PD 2026-10-21)
                case "dragoon":          DragoonMeteor(me);     break;   // 메테오 + 불바다(PD 2026-10-21)
                case "guru":             UltGuru(me);           break;   // 결계 가두기
                case "salamander":       UltSalamander(me);     break;   // 독 늪
                case "snowwoman":        UltSnowwoman(me);      break;   // 얼음 진
                case "commando_missile": MissileFocus(me);      break;   // 집중 포격 — 가장 센 적에게 8발(PD 2026-10-21)
                case "commando_laser":   PrismLaser(me);        break;   // 프리즘 레이저(PD 2026-10-21)
                case "white_wizard":     LightRain(me);         break;   // 빛기둥 비(PD 2026-10-21)
                case "dragon_blue":      UltDragonStorm(me);    break;   // 천뢰 — 둘레 모든 적에게 낙뢰(PD 2026-10-21)
                case "commando_mg":      MgSpinFire(me);        break;   // 회전 난사 + 사방 산탄(PD 2026-10-21)
                case "amazon_elite":     UltAmazonElite(me);    break;   // 역린
                default:                 UltFallback(me);       break;   // 표에 없는 몸 — 아래 참고
            }
        }

        // ── 아직 안 만든 필살기 ──────────────────────────────
        //
        // 18명의 필살기는 시안만 있고 연출 · 부품이 아직이다(차례로 넣는다).
        // 그동안 버튼이 죽어 있으면 「고장」으로 보이므로 **그 몸의 액티브를 세게 한 번** 낸다(임시).
        // 지속형 스킬은 시전 순간에 정하는 피해만 커진다.
        private const float UltFallbackPowerMul = 2.5f;
        private float _ultFallbackMul = 1f;

        private void UltFallback(Unit me)
        {
            _ultFallbackMul = UltFallbackPowerMul;
            try { CastActiveSkill(me); }
            finally { _ultFallbackMul = 1f; }
        }

        // ── 액티브 시전 표시 ──────────────────────────────
        //
        // 컷인이 없으니 「지금 스킬을 썼다」를 몸에서 짧게 보여 준다 — 발밑 링 한 번(시전 색) + 몸 흰 번쩍.
        private void ActiveCastPop(Unit me)
        {
            var color = _config != null ? _config.CastColorOf(me.Key) : Color.white;
            var feet = me.Position - Vector2.up * CastFootDrop;
            var ring = PlayFloorFx("castring", feet, CastReleaseRingSize * 0.6f, loop: false);
            if (ring != null)
            {
                ring.SetFrameSeconds(0.05f);
                ring.SetSquash(CastRingSquash);
                ring.SetTint(color);
            }
            me.FlashRim(color, 0.25f);
        }

        // ── 이름 · 아이콘 ──────────────────────────────

        /// <summary>필살기 이름(지금 언어). 표에 없으면 액티브 이름으로 떨어진다.</summary>
        public string UltimateName(string hostKey)
        {
            string key = $"uskill.{hostKey}.name";
            string v = global::Game.Module.Common.Localize.Get(key);
            if (!string.IsNullOrEmpty(v) && v != key) return v;
            var skill = SkillEntryOf(hostKey);
            return skill == null ? string.Empty : skill.DisplayName;
        }

        /// <summary>필살기 영문 이름(컷인 작은 글씨).</summary>
        public string UltimateNameEn(string hostKey)
        {
            string key = $"uskill.{hostKey}.en";
            string v = global::Game.Module.Common.Localize.Get(key);
            return !string.IsNullOrEmpty(v) && v != key ? v : string.Empty;
        }

        /// <summary>필살기 버튼 아이콘(`ulticon_{호스트}`). 아직 없으면 액티브 아이콘.</summary>
        /// <summary>체험을 다 쓴 필살기 버튼에 얹는 자물쇠 — 호스트 상세의 잠금 그림을 같이 쓴다.</summary>
        public Sprite UltimateLockIcon => _panelAtlas != null ? _panelAtlas.GetSprite("hostdetaillockicon") : null;

        public Sprite UltimateIcon(string hostKey)
        {
            if (_panelAtlas == null || hostKey == null) return null;
            return _panelAtlas.GetSprite($"ulticon_{hostKey}") ?? ActiveSkillIcon(hostKey);
        }

        // ── 바뀐 액티브 · 필살기 몸통 ──────────────────────────────

        /// <summary>영매 액티브 — 해골 2기(패시브 「강령」과 같은 해골).</summary>
        private const int MediumSkeletonCount = 2;

        /// <summary>아마존 정예 액티브 — 불굴. 2.5초 무적(스킬 무적 윤곽).</summary>
        private const float EliteUnyieldingSeconds = 2.5f;

        private void EliteUnyielding(Unit me)
        {
            float s = BaseAxis(EliteUnyieldingSeconds);
            _invuln = Mathf.Max(_invuln, s);
            _skillInvuln = Mathf.Max(_skillInvuln, s);
            PlayFx("guard", me.Position, Meters(1.6f), loop: false);
        }

        /// <summary>
        /// 호퍼(기관단총) 액티브 — 백스텝 사격. 가까운 적에게서 뒤로 크게 뛰며 앞으로 부채꼴 연사.
        /// 필살기 「돌진 난사」(먼 적까지 돌파)와 반대 방향이라 둘이 겹치지 않는다.
        /// </summary>
        private const float SmgBackstepMeters = 3f;
        private const int SmgBackstepShots = 7;
        private const float SmgBackstepSpreadDeg = 50f;

        private void SmgBackstep(Unit me)
        {
            var near = NearestEnemy(me.Position);
            var away = near != null ? (me.Position - near.Position) : -me.Facing;
            away = away.sqrMagnitude > 1f ? away.normalized : Vector2.down;
            var from = me.Position;
            var to = ClampedInField(me, from + away * Meters(SmgBackstepMeters));
            me.Position = to;
            SpawnAfterimages(me, from, to);
            if (near != null)
            {
                me.SetFacing(near.Position - to);
                FireFan(me, near.Position, SmgBackstepShots, SmgBackstepSpreadDeg,
                        SkillDamage(me, BaseAxis(0.6f)), fromPlayer: true);
            }
            PlayFx("dash", to, GaleDashFxSize, loop: false);
        }
    }
}
