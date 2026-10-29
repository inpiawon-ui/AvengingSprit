using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 호스트 패시브 — **PD 2026-10-29 다시 잡기**(`Projects/AVSR/_exchange/plan_party_skills.md`)가 정본이다.
    ///
    /// 빙의 파티에서 패시브는 **메인일 때만** 켜진다(보조는 없다). 그래서 패시브는 「놀이 방식을 정하는 규칙」 —
    /// 태그 시너지가 맡은 맨 스탯(공속 · 회피 · 치명 · 받는 피해 · 체력 · 쿨)과 겹치지 않게 조건 · 반응형으로 잡았다.
    ///
    /// 표는 화면에 글을 띄우는 쪽이고, **실제로 무슨 일이 벌어지는가는 이 파일**이다 — 두 곳에 적으면 한쪽이 낡는다.
    /// (맞는 적을 보고 곱하는 몇 개 — 독니 · 표적 고정 · 용의 피 · 치고 빠지기 — 는 `BattleDirector.Skills.cs` 의
    ///  `ScorchMul` · `BeginSwing` 이 부르고, 몸값은 아래 있다.)
    ///
    /// 걸리는 자리.
    ///   입을 때   `ApplyHostPassives`  — 몸의 성질(회피 · 방어력)
    ///   때릴 때   `PassiveOnHit`       — 맞힐 때마다
    ///   잡을 때   `PassiveOnKill`      — 처치 보상
    ///   매 프레임 `TickHostPassives`   — 상태에 따라 변하는 것(난전 · 불굴 · 반사 신경 등)
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 수치 — PD 2026-10-29 시험값 ─────────────────────────
        //
        // 패시브의 **대표 수치 하나**는 성급과 함께 오른다(패시브 표 `_growthMin/Max`, % 단위 —
        // 원천 `Editor/SkillGrowthImporter.cs`). 아래 상수는 표가 비었을 때의 Lv1 값이다.

        private const int PassiveProcPercent = 10;            // 청룡 낙뢰 확률
        /// <summary>확률로 거는 상태이상이 도는 시간(필살기가 같이 쓴다).</summary>
        private const float PassiveAilmentSeconds = 3f;
        private const int NinjaDodgePercent = 20;             // 닌자 몸놀림 — 회피
        /// <summary>코만도(기관총) 중장갑 — 받는 피해 −15%(기획서 plan_party_skills · PD 2026-10-29 「기획서 기준으로」). 성장 표가 Lv5 까지 키운다.</summary>
        private const float HeavyArmorDamageCut = 0.15f;
        private const int GrenadeArmorIgnorePercent = 50;     // 코만도(수류탄) 파편탄
        private const float FrostBiteBonus = 0.30f;           // 설녀 서릿발 — 빙결된 적에게
        private const float VenomFangBonus = 0.20f;           // 샐러맨더 독니 — 중독된 적에게
        private const float ChainWrapBonus = 0.25f;           // 닌자(사슬) 사슬 감기 — 끌려온 적에게
        private const float ChainWrapSeconds = 3f;
        private const float ContractCooldownSeconds = 1f;     // 갱스터 청부 계약 — 표식 적 처치마다 쿨 −1초
        private const float BrawlHasteBonus = 0.20f;          // 폭력배 난전 — 평타 공속 +20%
        private const int BrawlEnemyCount = 3;
        private const float BrawlRangeMeters = 3f;
        private const float BrawlRefreshSeconds = 0.25f;
        private const float HopperCritDamageBonus = 0.30f;    // 호퍼 급소
        private const float RunGunPower = 0.60f;              // 호퍼(기관단총) 치고 빠지기 — 걸으며 쏘는 위력
        private const float LockOnStepBonus = 0.10f;          // 코만도(미사일) 표적 고정 — 연속 명중마다
        private const int LockOnMaxStacks = 5;                // 최대 +50%
        private const float SpareCircuitDamageCut = 0.20f;    // 로봇 예비 회로 — 포탑이 서 있는 동안 받는 피해
        private const float UnbowedHpCut = 0.30f;             // 아마존 정예 불굴 — 체력 30% 아래
        private const float UnbowedInvulnSeconds = 3f;
        private const float UnbowedHealBonus = 0.30f;         // 최대 체력의 30% 회복
        private const int SluggerReflexPercent = 30;          // 슬러거 반사 신경 — 날아온 적 탄 자동 반사
        private const float SluggerReflexMeters = 1.2f;       // 몸에 이만큼 붙은 탄을 본다
        private const float DragonBloodAtkBonus = 0.30f;      // 드라군 용의 피 — 불바다 위 공격력
        private const float BloodThirstBonus = 0.05f;         // 흡혈귀 피의 갈증 — 주는 피해의 5% 흡혈
        private const float BloodThirstFlushSeconds = 0.5f;   // 모아서 한 번에 — 맞힐 때마다 「+1」이 수십 개 뜨지 않게
        private const int MediumSkullPercent = 30;            // 영매 강령술
        private const int MediumSummonCapBonus = 2;           // 소환물 최대 수 +2
        private const float LightWardShieldBonus = 0.15f;     // 화이트 위저드 빛의 가호 — 최대 체력 15% 쉴드
        private const int LightWardMinHits = 3;
        private const float DragonBoltRangeMeters = 3.5f;     // 청룡 — 튕겨 갈 거리
        private const float DragonBoltDamageMul = 0.6f;

        /// <summary>지금 입고 있는 몸의 키. 몸이 없으면 null — 유령에는 패시브가 없다.</summary>
        private string PassiveHostKey => _host != null ? _host.Key : null;

        // ── 성급과 함께 오르는 대표 수치 (기획 2026-10-06) ─────────────
        //
        // 패시브마다 **대표 수치 하나**가 성급(숙련도)을 따라 오른다. 값은 패시브 표(`_growthMin/Max`, % 단위)에 있고
        // 몸을 입을 때 한 번 계산해 둔다 — 한 번에 입는 몸은 하나라 값도 하나다.
        // 위 상수들은 **표가 비었을 때의 값**이다(표를 안 구운 빌드 · 오르지 않는 패시브).
        private float _passiveGrowth = -1f;

        /// <summary>대표 수치(%)를 정수 확률로. 표가 비었으면 상수.</summary>
        private int PassivePct(int fallback) => _passiveGrowth >= 0f ? Mathf.RoundToInt(_passiveGrowth) : fallback;

        /// <summary>대표 수치(%)를 배수 몫(0.2 = 20%)으로. 표가 비었으면 상수.</summary>
        private float PassiveFrac(float fallback) => _passiveGrowth >= 0f ? _passiveGrowth / 100f : fallback;

        private void CachePassiveGrowth(Game.Character.HostEntry entry)
        {
            _passiveGrowth = -1f;
            if (entry == null || _player == null) return;
            var passive = _player.GetPassiveSkill(entry.PassiveSkillKey);
            if (passive == null || !passive.HasGrowth) return;
            // 육성 개편(2026-10-29) — 성급이 아니라 패시브 스킬 레벨(1~5)로 Lv1 → 끝값을 잇는다
            _passiveGrowth = passive.GrowthAt(_player.SkillLevel(entry.HostKey, Game.User.HostSkillSlot.Passive),
                                              _player.SkillLevelMax(Game.User.HostSkillSlot.Passive));
        }

        // ── 몸에 붙는 성질 ────────────────────────────────────

        /// <summary>몸을 입는 순간 그 몸의 성질을 넣는다(`EnterHost`).</summary>
        private void ApplyHostPassives(Unit body, string key, Game.Character.HostEntry entry)
        {
            if (body == null) return;
            CachePassiveGrowth(entry);
            // 회피는 닌자 패시브로 옮겼다(PD 2026-10-29) — 사슬 닌자의 「흘리기」는 「사슬 감기」로 바뀌었다
            body.SetDodge(key == "ninja" ? PassivePct(NinjaDodgePercent) : 0);
            body.SetDefense(HostDefensePercent(key, entry));
            body.SetSpeedMul(1f);
            _brawlOn = false;
            _lockOnTarget = null;
            _lockOnHits = 0;
            _thirstCarry = 0f;
            _thirstFlush = 0f;
        }

        /// <summary>
        /// 이 몸이 타고난 방어력(%). 출처는 **표의 등급 한 칸**이다(`HostEntry.DefensePercent`) —
        /// 여기에 23줄을 또 적으면 표와 어긋난다.
        /// 표에 없는 몸(잡몹 · 유령)은 0 이다.
        /// </summary>
        private int HostDefensePercent(string key, Game.Character.HostEntry entry)
        {
            if (entry == null) return 0;
            float percent = entry.DefensePercent;
            // 유령 강화 「받는 피해 감소」(%p)를 더한다 — 모든 몸(2026-10-29, 호스트 탭 강화는 없앴다)
            if (_player != null) percent += _player.StatBonusFlat(key, Game.Character.HostStat.Defense);
            return Mathf.Min(Mathf.RoundToInt(percent), Game.Character.GameConfig.DefenseCapPercent);
        }

        /// <summary>코만도(수류탄) — 상대 방어력을 이만큼 무시한다.</summary>
        private int ArmorIgnorePercent
            => PassiveHostKey == "commando_grenade" ? PassivePct(GrenadeArmorIgnorePercent) : 0;

        /// <summary>
        /// 주운 골드 배율. 폭력배 「삥」(+10%)은 싸움과 상관없어 PD 2026-10-29 에 「난전」으로 바뀌었다 — 지금은 1배다.
        /// 골드 줍는 코드가 이 값을 부르므로 자리는 남겨 둔다.
        /// </summary>
        private float GoldGainMul => 1f;

        /// <summary>
        /// 치명타 배율에 **더하는** 몫 — 호퍼 패시브 + 로비 치명타 피해 강화(%p → 배율, 2026-10-06).
        /// </summary>
        private float CritDamageBonus
            => (PassiveHostKey == "hopper" ? PassiveFrac(HopperCritDamageBonus) : 0f)
             + (_player != null ? _player.StatBonusFlat(PassiveHostKey, Game.Character.HostStat.CritDamage) / 100f : 0f);

        /// <summary>구루 — 걸어 다닐 때 장애물을 통과한다.</summary>
        private bool HostIgnoresObstacles => PassiveHostKey == "guru";

        /// <summary>
        /// 탄이 지형지물을 통과하는 몸 — 코만도(레이저) 「집속 광선」만(PD 2026-10-29).
        /// 미사일 · 화이트 위저드도 같은 패시브였는데 겹쳐서 각자 다른 패시브로 바꿨다.
        /// 관통(적을 뚫는 것)은 무기 종류(`AttackKind.Pierce`)가 따로 갖는다. 이것은 **엄폐물** 쪽이다.
        /// </summary>
        private bool ShotIgnoresObstacles => PassiveHostKey == "commando_laser";

        /// <summary>설녀 서릿발 — 얼어 있는 적에게는 더 아프다. 액티브 「눈보라」가 얼린다.</summary>
        private float FrozenBonusMul(Unit victim)
            => PassiveHostKey == "snowwoman" && victim != null && victim.IsFrozen
               ? 1f + PassiveFrac(FrostBiteBonus) : 1f;

        /// <summary>호퍼(기관단총) 치고 빠지기 — 걸으면서도 평타를 쏜다(이동 사격). `TickPlayer` 가 읽는다.</summary>
        private bool PassiveMoveAttack => PassiveHostKey == "hopper_smg";

        /// <summary>걸으면서 쏜 평타의 위력 — 치고 빠지기는 60% 로 쏜다. 서서 쏘면 그대로다.</summary>
        private float RunGunMul
            => PassiveHostKey == "hopper_smg" && MoveInput.sqrMagnitude > 0.0001f
               ? Mathf.Min(1f, PassiveFrac(RunGunPower)) : 1f;

        /// <summary>영매 강령술 — 소환물을 이만큼 더 세운다(`SpawnSummon` 의 상한).</summary>
        private int SummonCapBonus => PassiveHostKey == "medium" ? MediumSummonCapBonus : 0;

        /// <summary>코만도(기관총) 중장갑 — 늘 받는 피해를 줄인다(`WardReduce` 가 곱한다).</summary>
        private float HeavyArmorCut => PassiveHostKey == "commando_mg" ? PassiveFrac(HeavyArmorDamageCut) : 0f;

        /// <summary>로봇 예비 회로 — 포탑이 서 있는 동안 받는 피해를 줄인다(`WardReduce` 가 곱한다).</summary>
        private float SpareCircuitCut
            => PassiveHostKey == "robot" && _turretHasteSeconds > 0f ? PassiveFrac(SpareCircuitDamageCut) : 0f;

        /// <summary>
        /// 드라군 용의 피 — 내가 깐 불바다 위에 서 있으면 공격력 +30%.
        /// 불바다를 깔고 그 위에서 싸우는 것이 이 몸의 놀이다(PD 2026-10-29).
        /// </summary>
        private float DragonBloodMul(Unit me)
            => me != null && me == _host && me.Key == "dragoon" && StandsOnOwnFire(me.Position)
               ? 1f + PassiveFrac(DragonBloodAtkBonus) : 1f;

        private bool StandsOnOwnFire(Vector2 at)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                var f = _fields[i];
                if (f.IsActive && f.FromPlayer && f.Effect == FieldEffect.Burn && f.Contains(at)) return true;
            }
            return false;
        }

        // ── 표적 고정 (코만도(미사일)) ─────────────────────────
        //
        // 같은 적을 연달아 맞히면 10% 씩 쌓인다(최대 +50%). 다른 적을 맞히면 처음부터.
        // 미사일 7발이 여럿에게 갈라지는 액티브와 반대로, 평타는 한 놈을 물고 늘어지게 한다.

        private Unit _lockOnTarget;
        private int _lockOnHits;

        private float LockOnMul(Unit victim)
        {
            if (_summonStriking) return 1f;   // 보조 · 소환물이 친 것은 세지 않는다
            if (victim != _lockOnTarget) { _lockOnTarget = victim; _lockOnHits = 0; }
            int stacks = Mathf.Min(LockOnMaxStacks, _lockOnHits);
            _lockOnHits++;
            return 1f + stacks * PassiveFrac(LockOnStepBonus);
        }

        // ── 사슬 감기 (닌자(사슬)) ─────────────────────────────

        private readonly System.Collections.Generic.Dictionary<Unit, float> _wrapUntil = new();

        /// <summary>메인이 사슬로 끌어온 적 — 지금부터 3초간 더 아프다.</summary>
        private void WrapChained(System.Collections.Generic.List<Unit> pulled)
        {
            float until = Time.time + ChainWrapSeconds;
            for (int i = 0; i < pulled.Count; i++)
                if (pulled[i] != null && pulled[i].IsAlive) _wrapUntil[pulled[i]] = until;
        }

        private bool IsWrapped(Unit victim)
            => victim != null && _wrapUntil.TryGetValue(victim, out float until) && until > Time.time;

        // ── 빛의 가호 (화이트 위저드) ──────────────────────────

        /// <summary>액티브(빛의 인력)가 셋 이상 맞혔으면 메인에게 쉴드. 보조가 쓰면 없다(패시브).</summary>
        private void LightWardOnBurst(Unit me, int hits)
        {
            if (me == null || me != _host || me.Key != "white_wizard" || hits < LightWardMinHits) return;
            int amount = Mathf.Max(1, Mathf.RoundToInt(me.HpMax * PassiveFrac(LightWardShieldBonus)));
            me.AddShield(amount, me.HpMax);
            PlayFx("guard", me.Position, Meters(1.4f), loop: false);
        }

        // ── 때릴 때 ──────────────────────────────────────────

        /// <summary>
        /// 플레이어가 적을 때린 **뒤에** 부른다(근접 · 탄 공통).
        /// 피해 계산이 끝난 뒤라 여기서 죽여도 숫자가 먼저 보인다.
        /// </summary>
        private void PassiveOnHit(Unit victim, int damage)
        {
            if (_host == null || victim == null || !victim.IsAlive || victim.IsDying) return;

            // 사신 액티브가 도는 동안은 때리는 것마다 즉사 판정을 굴린다(명세 — 3초).
            if (TryReaperKill(victim)) return;

            switch (_host.Key)
            {
                // 청룡 — 옆 적에게 튄다. **튈 곳이 없으면 안 터진다**(명세).
                case "dragon_blue":
                    if (Roll(PassivePct(PassiveProcPercent))) ChainBolt(victim, damage);
                    break;

                // 흡혈귀 피의 갈증 — 주는 피해의 5% 를 늘 모은다. 0.5초마다 한 번에 돌려받는다(TickBloodThirst).
                case "vampire":
                    _thirstCarry += damage * PassiveFrac(BloodThirstBonus);
                    break;
            }
        }

        /// <summary>청룡 — 가장 가까운 **다른** 적에게 번개가 튄다.</summary>
        private void ChainBolt(Unit from, int damage)
        {
            Unit best = null;
            float bestDist = Meters(DragonBoltRangeMeters);
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || e == from || !e.IsAlive || e.IsDying) continue;
                float d = Vector2.Distance(from.Position, e.Position);
                if (d < bestDist) { bestDist = d; best = e; }
            }
            if (best == null) return;   // 튈 곳이 없으면 발동하지 않는다(명세)

            PlayFx("bolt", Vector2.Lerp(from.Position, best.Position, 0.5f), 64f, loop: false);
            int dmg = Mathf.Max(1, Mathf.RoundToInt(damage * DragonBoltDamageMul));
            ShowDamage(best.Position, dmg, toEnemy: true);
            if (best.TakeDamage(dmg)) KillEnemy(best);
        }

        /// <summary>맞은 적을 메인에게서 뒤로 민다. 방 밖으로는 못 나간다(필살기 넉백).</summary>
        private void Knockback(Unit victim, float meters)
        {
            var me = Avatar;
            if (me == null) return;
            var away = victim.Position - me.Position;
            if (away.sqrMagnitude < 0.01f) return;
            victim.Position = ClampedInField(victim, victim.Position + away.normalized * Meters(meters));
        }

        // ── 원거리 몸이 근접 몹을 맞히면 밀어낸다 (기획 2026-09-16) ────────
        //
        // 멈춰야만 쏘는 규칙이라 근접 몹을 잡으려면 **제자리에서 맞으면서** 쏘는 수밖에 없었다.
        // 맞힐 때마다 뒤로 밀어내 붙기 전에 한 대 더 칠 틈을 준다.
        //
        // ⚠ **거꾸로는 안 민다** — 몬스터가 나를 때릴 때는 아무 일도 없다.
        // ⚠ 보스 · 원거리 몹은 안 민다. 근거리 몸이 때릴 때도 안 민다(그건 따로 정한다).
        // ⚠ 연사 몸(초당 수십 발)이 맞힐 때마다 밀면 근접 몹이 영영 못 온다 — **적마다 0.2초에 한 번**.
        private const float RangedKnockMeters = 1f;
        /// <summary>
        /// 밀린 뒤 맞은 자세로 서 있는 시간. 바로 걸어오면 순간이동처럼 보였다(기획 2026-09-16).
        /// 0.1초 → 1초 → 0.5초 → 0.3초로 시험 중(기획 2026-09-16 · 사용자가 직접 테스트).
        /// </summary>
        private const float RangedKnockHoldSeconds = 0.3f;
        private const float RangedKnockCooldown = 0.2f;

        /// <summary>
        /// 넉백 대신 **제자리 경직**으로 시험 중(기획 2026-09-18 · 사용자가 직접 테스트).
        /// true 면 밀지 않고 맞은 자리에서 <see cref="RangedStaggerSeconds"/> 동안 선다.
        /// false 로 되돌리면 예전 넉백(1 m 밀고 0.3초)이 그대로 돈다 — 넉백 코드는 지우지 않았다.
        /// </summary>
        private static readonly bool RangedHitStaggerInsteadOfKnockback = true;   // const 면 아래 넉백 줄이 «닿지 않는 코드» 경고가 된다
        private const float RangedStaggerSeconds = 0.3f;   // 1초 → 0.5초 → 0.3초(기획 2026-09-18 · 사용자 테스트)
        private readonly System.Collections.Generic.Dictionary<Unit, float> _rangedKnockAt = new();

        private void RangedKnockback(Unit victim)
        {
            var host = _host;
            if (host == null || host.Profile == null || victim == null || !victim.IsAlive || victim.IsBoss) return;
            if (IsMeleeKind(host.Profile.Kind)) return;              // 근거리 몸이 쏜 것은 안 민다
            if (victim.Profile == null || !IsMeleeKind(victim.Profile.Kind)) return;   // 근접 몹만

            float now = Time.time;
            if (_rangedKnockAt.TryGetValue(victim, out float last) && now - last < RangedKnockCooldown) return;
            _rangedKnockAt[victim] = now;

            if (RangedHitStaggerInsteadOfKnockback)
            {
                victim.CancelWindup();                  // 휘두르던 자세는 풀린다
                victim.HoldHit(RangedStaggerSeconds);   // 밀지 않고 맞은 자리에서 선다
                return;
            }

            var away = victim.Position - host.Position;
            if (away.sqrMagnitude < 0.01f) return;
            // 엄폐물을 뚫고 박히지 않게 미끄러지며 밀린다.
            // ⚠ `SlideMove` 의 셋째 인자는 **옮길 양**이다(도착 자리가 아니다). 자리를 넘겼더니
            //   자리 좌표가 통째로 더해져 한 번에 방 끝(4.8 m)까지 날아갔다(실측 2026-09-16).
            var push = away.normalized * Meters(RangedKnockMeters);
            victim.Position = ClampedInField(victim, SlideMove(victim, victim.Position, push));
            victim.CancelWindup();   // 휘두르던 자세는 풀린다 — 다시 붙어서 자세를 잡아야 한다
            victim.HoldHit(RangedKnockHoldSeconds);   // 밀린 자리에서 잠깐 맞은 자세로 섰다가 움직인다
        }

        private static bool IsMeleeKind(Game.Character.AttackKind kind)
            => kind == Game.Character.AttackKind.Melee || kind == Game.Character.AttackKind.Pulse;

        /// <summary>스킬 게이지를 이만큼 더 채운다(쿨이 그만큼 줄어든 것과 같다) — 갱스터 청부 계약.</summary>
        private void GainSkillCharge(float seconds)
        {
            float full = SkillCooldownOf(_lastHostEntry) * _buffs.ActiveCooldownMul;
            _skillCooldown = Mathf.Min(full, _skillCooldown + seconds);
        }

        // ── 잡을 때 ──────────────────────────────────────────

        /// <summary>적을 잡은 **직후**에 부른다(`KillEnemy`).</summary>
        private void PassiveOnKill(Unit victim)
        {
            if (_host == null || victim == null) return;
            IyOnKill(victim);   // 인연 「불독」 — 중독된 적이 쓰러지면 불꽃(BattleDirector.SkillsInyeon)

            switch (_host.Key)
            {
                // 갱스터 청부 계약 — 표식 단 적을 잡으면 액티브 쿨 −1초(PD 2026-10-29 — 예전 「표식 적 즉사」는 R 에 너무 셌다)
                case "gangster":
                    if (victim.IsMarked) GainSkillCharge(ContractCooldownSeconds);
                    break;

                // 사신 — 잡을 때마다 그 자리에서 해골이 일어선다(명세 — 확률 없음).
                case "death":
                    SummonSkull(victim.Position);
                    break;

                // 영매 강령술 — 30% 로 해골(소환물 최대 수 +2 는 `SummonCapBonus`).
                case "medium":
                    if (Roll(PassivePct(MediumSkullPercent))) SummonSkull(victim.Position);
                    break;
            }
        }

        // ── 매 프레임 ────────────────────────────────────────

        private bool _brawlOn;
        private bool _unbowedUsed;     // 아마존 정예 불굴 — 한 판에 한 번
        private float _thirstCarry;    // 흡혈귀 — 모아 둔 흡혈량
        private float _thirstFlush;
        private readonly System.Collections.Generic.HashSet<Projectile> _reflexSeen = new();

        /// <summary>상태에 따라 변하는 패시브.</summary>
        private void TickHostPassives(float dt)
        {
            if (_host == null) { _reflexSeen.Clear(); return; }

            switch (_host.Key)
            {
                case "thug":         TickBrawl(); break;
                case "amazon_elite": TickUnbowed(); break;
                case "vampire":      TickBloodThirst(dt); break;
                case "baseball":     TickReflex(); break;
                case "dragoon":      if (_host.BurnStack > 0) _host.ClearBurn(); break;   // 용의 피 — 화상을 입지 않는다
            }
            if (_host.Key != "baseball" && _reflexSeen.Count > 0) _reflexSeen.Clear();
        }

        /// <summary>폭력배 난전 — 둘레 3 m 에 적이 셋 이상이면 평타 공속 +20%(간격 ÷ 1.2).</summary>
        private void TickBrawl()
        {
            float r = Meters(BrawlRangeMeters);
            float r2 = r * r;
            int n = 0;
            var at = _host.Position;
            for (int i = 0; i < _enemies.Count && n < BrawlEnemyCount; i++)
            {
                var e = _enemies[i];
                if (e != null && e.IsAlive && !e.IsDying && (e.Position - at).sqrMagnitude <= r2) n++;
            }
            _brawlOn = n >= BrawlEnemyCount;
            if (!_brawlOn) return;
            // 다른 단축이 더 크게 돌고 있으면 그것을 남긴다. 짧게 잡아 계속 갱신한다 — 무리가 흩어지면 바로 꺼진다.
            float cut = 100f * (1f - 1f / (1f + PassiveFrac(BrawlHasteBonus)));
            if (_hasteSeconds > BrawlRefreshSeconds && HasteMul < 1f - cut * 0.01f) return;
            GrantHaste(cut, BrawlRefreshSeconds);
        }

        /// <summary>아마존 정예 불굴 — 체력이 30% 아래로 떨어지면 한 판에 한 번 3초 무적 + 체력 30% 회복.</summary>
        private void TickUnbowed()
        {
            if (_unbowedUsed || _host.Hp <= 0 || _host.Hp >= _host.HpMax * UnbowedHpCut) return;
            _unbowedUsed = true;
            UltInvuln(UnbowedInvulnSeconds);
            Leech(Mathf.Max(1, Mathf.RoundToInt(_host.HpMax * PassiveFrac(UnbowedHealBonus))));
            PlayFx("guard", _host.Position, Meters(1.8f), loop: false);
            _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Fire, _host.Position, 14, 220f, 36f, 0.6f, 0f);
            Shake(5f);
        }

        /// <summary>흡혈귀 피의 갈증 — 모은 흡혈을 돌려받는다. 체력이 가득이면 넘친 만큼 쉴드.</summary>
        private void TickBloodThirst(float dt)
        {
            _thirstFlush -= dt;
            if (_thirstFlush > 0f) return;
            _thirstFlush = BloodThirstFlushSeconds;
            int amount = Mathf.FloorToInt(_thirstCarry);
            if (amount <= 0) return;
            _thirstCarry -= amount;

            int room = _host.HpMax - _host.Hp;
            if (room > 0) Leech(Mathf.Min(amount, room));
            int over = amount - Mathf.Max(0, room);
            if (over <= 0) return;
            int cap = Mathf.Max(1, _host.HpMax * (_config != null ? _config.ShieldCapPercent : 30) / 100);
            _host.AddShield(over, cap);
        }

        /// <summary>
        /// 슬러거 반사 신경 — 몸에 닿으려는 적 탄을 30% 로 자동으로 되받아친다.
        /// 탄 하나에 한 번만 굴린다(`_reflexSeen`) — 매 프레임 굴리면 붙어 있는 동안 거의 다 튕긴다.
        /// </summary>
        private void TickReflex()
        {
            float r = Meters(SluggerReflexMeters);
            float r2 = r * r;
            var at = _host.Position;
            int chance = PassivePct(SluggerReflexPercent);
            for (int i = 0; i < _shots.Count; i++)
            {
                var s = _shots[i];
                if (!s.IsActive || s.FromPlayer) continue;
                if ((s.Position - at).sqrMagnitude > r2) continue;
                if (!_reflexSeen.Add(s)) continue;
                if (!Roll(chance)) continue;
                global::Game.Module.Common.GameSound.Cue("hit.reflect");
                ReflectShot(s);
            }
            if (_reflexSeen.Count > 0) _reflexSeen.RemoveWhere(s => !s.IsActive || s.FromPlayer);
        }
    }
}
