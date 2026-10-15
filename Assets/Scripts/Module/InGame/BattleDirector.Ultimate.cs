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

        /// <summary>필살기 버튼을 눌러 쓸 수 있는가(몸 · 봉인 · 1회).</summary>
        public bool IsUltReady => _running && _ultReady && _host != null && !IsSkillSealed(_host.Key);

        /// <summary>지금 몸이 있는가 — 필살기 버튼 칸이 「빙의」 대신 「필살기」를 보여야 하는가.</summary>
        public bool HasHostBody => _host != null;

        public void TryUltimate()
        {
            if (!_running || IsCastFrozen || IsChanneling) return;
            var me = _host;
            if (me == null || !_ultReady || IsSkillSealed(me.Key)) return;
            _ultReady = false;
            BeginCast(me);   // 컷인 → 멈춤 → `ReleaseCast` → `CastUltimate`
        }

        /// <summary>필살기 — 컨셉 기획의 「크게 터뜨리는」 스킬.</summary>
        private void CastUltimate(Unit me)
        {
            if (me == null) return;
            global::Game.Module.Common.GameSound.Skill(me.Key);
            switch (me.Key)
            {
                case "amazon":           AmazonLightningDance(me); break;   // 번개 무희
                case "thug":             ThugSpinFire(me);      break;   // 회전 난사
                case "commando_grenade": CommandoStickyBomb(me); break;  // C4
                case "baseball":         SluggerFullSwing(me);  break;   // 풀스윙 회오리
                case "medium":           SsrGolem(me);          break;   // 골렘 강령(SSR 다듬기 — BattleDirector.UltSsr)
                case "gangster":         UltGangster(me);       break;   // 청부 처형
                case "hopper":           UltHopper(me);         break;   // 킬 포인트
                case "ninja_chain":      UltNinjaChain(me);     break;   // 사슬 돌격 연격
                case "death":            UltDeath(me);          break;   // 망자 군단
                case "robot":            UltRobot(me);          break;   // 포탑 군단
                case "ninja":            UltNinja(me);          break;   // 그림자 삼분신
                case "hopper_smg":       UltHopperSmg(me);      break;   // 돌진 난사
                case "vampire":          SsrVampire(me);        break;   // 피의 폭주(SSR)
                case "dragoon":          SsrDragoon(me);        break;   // 용 돌격(SSR)
                case "guru":             UltGuru(me);           break;   // 결계 가두기
                case "salamander":       UltSalamander(me);     break;   // 독 늪
                case "snowwoman":        UltSnowwoman(me);      break;   // 얼음 진
                case "commando_missile": UltMissile(me);        break;   // 자폭 미사일 떼
                case "commando_laser":   UltLaser(me);          break;   // 충전 레이저
                case "white_wizard":     UltWizard(me);         break;   // 빛 구슬 일제 발사
                case "dragon_blue":      UltDragonBlue(me);     break;   // 번개 구슬
                case "commando_mg":      UltCommandoMg(me);     break;   // 디플렉트 월
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
        public Sprite UltimateIcon(string hostKey)
        {
            if (_panelAtlas == null || hostKey == null) return null;
            return _panelAtlas.GetSprite($"ulticon_{hostKey}") ?? ActiveSkillIcon(hostKey);
        }

        // ── 바뀐 액티브 · 필살기 몸통 ──────────────────────────────

        /// <summary>영매 액티브 — 해골 2기(패시브 「강령」과 같은 해골).</summary>
        private const int MediumSkeletonCount = 2;

        private void MediumSkeletons(Unit me)
        {
            for (int i = 0; i < MediumSkeletonCount; i++)
            {
                var side = new Vector2(i == 0 ? -1f : 1f, 0.4f);
                var at = ClampedInField(me, me.Position + side * Meters(1.1f));
                SummonSkull(at);
                PlayFx("soulripple", at, Meters(1.2f), loop: false);
            }
        }

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
