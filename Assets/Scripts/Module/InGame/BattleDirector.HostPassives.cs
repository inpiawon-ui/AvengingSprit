using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 호스트 패시브 — **명세 2026-09-14(대화)** 가 정본이다.
    ///
    /// 예전에는 11명만 패시브를 가졌고 그 목록은 `PassiveSkillTable` 이 들고 있었다.
    /// 새 명세는 **23명 전원**에게 하나씩 준다. 표는 화면에 글을 띄우는 쪽이고,
    /// **실제로 무슨 일이 벌어지는가는 이 파일 하나**다 — 두 곳에 적으면 한쪽이 낡는다.
    ///
    /// 걸리는 자리는 넷뿐이다.
    ///   입을 때   `ApplyHostPassives`  — 몸의 성질(회피 · 방어력)
    ///   때릴 때   `PassiveOnHit`       — 확률로 터지는 것
    ///   잡을 때   `PassiveOnKill`      — 처치 보상
    ///   매 프레임 `TickHostPassives`   — 상태에 따라 변하는 것(쉴드 이속 등)
    ///
    /// 소환 계열(사신 · 영매의 해골)은 여기서 부르고, 실제로 세우고 굴리는 것은
    /// `BattleDirector.Summons.cs` 가 한다 — 여럿이 서고 시간이 지나면 사라지는 것들이다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 수치 ─────────────────────────────────────────────
        //
        // 「10% 확률」이 여럿이라 한 값으로 묶는다. 명세가 드라군 · 샐러맨더 · 청룡 ·
        // 호퍼(기관단총)에 모두 10% 를 줬다 — 따로 두면 하나만 고치고 나머지를 잊는다.
        private const int PassiveProcPercent = 10;

        /// <summary>확률로 거는 상태이상이 도는 시간.</summary>
        private const float PassiveAilmentSeconds = 3f;

        private const int NinjaChainDodgePercent = 10;      // 닌자(사슬)

        /// <summary>
        /// 코만도(기관총) — 「기본 방어력 20% 증가」. 제 등급이 준 값에 **곱한다**.
        /// 20%p 를 더하면 등급 9(27%)가 47% 가 되어 이 몸만 다른 게임을 한다.
        /// </summary>
        private const float CommandoMgDefenseBonus = 0.20f;
        private const int GrenadeArmorIgnorePercent = 50;   // 코만도(수류탄)
        private const int SnowFreezeChancePercent = 30;     // 설녀
        private const float FrozenExtraDamageMul = 1.3f;    // 얼어 있는 적에게
        private const int SluggerFlingChancePercent = 20;   // 슬러거
        private const float SluggerFlingMeters = 2.2f;
        private const int GangsterExecutePercent = 20;      // 갱스터 — 표식이 붙은 적에게
        private const float GangsterEliteHpCut = 0.5f;
        private const float GangsterBossHpCut = 0.3f;
        private const int MediumSkullPercent = 30;          // 영매
        private const int RobotCoolProcPercent = 2;         // 로봇
        private const float RobotCoolGainSeconds = 1f;
        private const float ThugGoldBonus = 0.10f;          // 폭력배
        private const float HopperCritDamageBonus = 0.30f;  // 호퍼
        private const float AmazonShieldSpeedBonus = 0.40f; // 아마존 — 쉴드가 가득일 때
        private const float NinjaKillSpeedBonus = 0.30f;    // 닌자
        private const float NinjaKillSpeedSeconds = 1.5f;
        private const float DragonBoltRangeMeters = 3.5f;   // 청룡 — 튕겨 갈 거리
        private const float DragonBoltDamageMul = 0.6f;
        private const float KnockbackMeters = 1.2f;         // 호퍼(기관단총)

        /// <summary>
        /// 흡혈귀 — 명세는 「3% 확률로 공격력의 1%」인데 그대로면 100 대를 때려 3 번,
        /// 한 번에 0.1 이라 **화면에서 아무 일도 안 일어난다.** 사용자 확인(2026-09-14
        /// "약간만 올려봐 · 최대한 낮게")에 따라 아주 조금만 올린다.
        /// </summary>
        private const int VampireProcPercent = 8;
        private const float VampireProcAtkPercent = 0.05f;

        /// <summary>지금 입고 있는 몸의 키. 몸이 없으면 null — 유령에는 패시브가 없다.</summary>
        private string PassiveHostKey => _host != null ? _host.Key : null;

        // ── 몸에 붙는 성질 ────────────────────────────────────

        /// <summary>몸을 입는 순간 그 몸의 성질을 넣는다(`EnterHost`).</summary>
        private void ApplyHostPassives(Unit body, string key, Game.Character.HostEntry entry)
        {
            if (body == null) return;
            body.SetDodge(key == "ninja_chain" ? NinjaChainDodgePercent : 0);
            body.SetDefense(HostDefensePercent(key, entry));
            body.SetSpeedMul(1f);
            _ninjaKillSpeedTimer = 0f;
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
            if (key == "commando_mg") percent *= 1f + CommandoMgDefenseBonus;
            return Mathf.RoundToInt(percent);
        }

        /// <summary>코만도(수류탄) — 상대 방어력을 이만큼 무시한다.</summary>
        private int ArmorIgnorePercent
            => PassiveHostKey == "commando_grenade" ? GrenadeArmorIgnorePercent : 0;

        /// <summary>폭력배 — 주운 골드가 더 들어온다.</summary>
        private float GoldGainMul
            => PassiveHostKey == "thug" ? 1f + ThugGoldBonus : 1f;

        /// <summary>호퍼 — 치명타가 더 아프다. 치명타 배율에 **더한다**.</summary>
        private float CritDamageBonus
            => PassiveHostKey == "hopper" ? HopperCritDamageBonus : 0f;

        /// <summary>구루 — 걸어 다닐 때 장애물을 통과한다.</summary>
        private bool HostIgnoresObstacles => PassiveHostKey == "guru";

        /// <summary>
        /// 탄이 지형지물을 통과하는 몸 — 코만도(미사일) · 화이트 위저드 · 코만도(레이저).
        /// 관통(적을 뚫는 것)은 무기 종류(`AttackKind.Pierce`)가 따로 갖는다. 이것은 **엄폐물** 쪽이다.
        /// </summary>
        private bool ShotIgnoresObstacles
            => PassiveHostKey == "commando_missile"
            || PassiveHostKey == "white_wizard"
            || PassiveHostKey == "commando_laser";

        /// <summary>설녀 — 얼어 있는 적에게는 더 아프다. 얼리는 것과 한 쌍이다.</summary>
        private float FrozenBonusMul(Unit victim)
            => PassiveHostKey == "snowwoman" && victim != null && victim.IsFrozen
               ? FrozenExtraDamageMul : 1f;

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
                // 드라군 — 불이 붙는다. 이미 타고 있으면 시간만 늘어난다(중복 없음).
                case "dragoon":
                    if (Roll(PassiveProcPercent))
                    {
                        victim.ApplyBurn(PassiveAilmentSeconds);
                        PlayFx("burn", victim.Position, 64f, loop: false);
                    }
                    break;

                // 샐러맨더 — 독. 겹치지 않는다(명세).
                case "salamander":
                    if (!victim.IsPoisoned && Roll(PassiveProcPercent))
                    {
                        victim.ApplyPoison(PassiveAilmentSeconds);
                        PlayFx("venom", victim.Position, 64f, loop: false);
                    }
                    break;

                // 청룡 — 옆 적에게 튄다. **튈 곳이 없으면 안 터진다**(명세).
                case "dragon_blue":
                    // 액티브가 도는 2초 동안은 확률을 보지 않는다 — 무조건 튄다(명세).
                    if (_boltSurgeSeconds > 0f || Roll(PassiveProcPercent)) ChainBolt(victim, damage);
                    break;

                // 설녀 — 얼린다. 보스는 안 걸린다(명세).
                case "snowwoman":
                    if (!victim.IsBoss && Roll(SnowFreezeChancePercent))
                    {
                        victim.ApplyFreeze(PassiveAilmentSeconds);
                        PlayFx("freeze", victim.Position, 72f, loop: false);
                    }
                    break;

                // 호퍼(기관단총) — 살짝 밀어낸다.
                case "hopper_smg":
                    if (Roll(PassiveProcPercent)) Knockback(victim, KnockbackMeters);
                    break;

                // 흡혈귀 — 아주 조금 돌려받는다.
                case "vampire":
                    if (Roll(VampireProcPercent) && _host != null)
                        Leech(Mathf.Max(1, Mathf.RoundToInt(_host.Atk * VampireProcAtkPercent)));
                    break;

                // 갱스터 — **표식이 붙은 적에게만**. 일반은 즉사, 엘리트·보스는 최대 체력을 깎는다.
                case "gangster":
                    if (victim.IsMarked && Roll(GangsterExecutePercent)) MarkExecute(victim);
                    break;

                // 로봇 — 평타가 스킬 쿨을 당긴다.
                case "robot":
                    if (Roll(RobotCoolProcPercent)) GainSkillCharge(RobotCoolGainSeconds);
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

        /// <summary>갱스터 — 표식이 붙은 적을 처형한다. 큰 몸은 죽지 않고 크게 깎인다.</summary>
        private void MarkExecute(Unit victim)
        {
            // 표적은 이미 몸에 붙어 돌고 있다 — 처형 순간에 하나 더 띄우면 겹친다.
            // 처형은 **터짐**으로 보여 준다.
            PlayFx("shatter", victim.Position, 120f, loop: false);
            // Sandbox — 테스트 판에서는 즉사가 안 터진다 (지울 때 이 줄도 함께)
            if (SandboxBlocksExecute) return;
            if (!victim.IsBoss && !victim.IsElite) { KillEnemy(victim); return; }

            float cut = victim.IsBoss ? GangsterBossHpCut : GangsterEliteHpCut;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(victim.HpMax * cut));
            ShowDamage(victim.Position, dmg, toEnemy: true);
            if (victim.TakeDamage(dmg)) KillEnemy(victim);
        }

        /// <summary>호퍼(기관단총) — 맞은 적을 뒤로 민다. 방 밖으로는 못 나간다.</summary>
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
        private const float RangedKnockCooldown = 0.2f;
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

            var away = victim.Position - host.Position;
            if (away.sqrMagnitude < 0.01f) return;
            // 엄폐물을 뚫고 박히지 않게 미끄러지며 밀린다.
            // ⚠ `SlideMove` 의 셋째 인자는 **옮길 양**이다(도착 자리가 아니다). 자리를 넘겼더니
            //   자리 좌표가 통째로 더해져 한 번에 방 끝(4.8 m)까지 날아갔다(실측 2026-09-16).
            var push = away.normalized * Meters(RangedKnockMeters);
            victim.Position = ClampedInField(victim, SlideMove(victim, victim.Position, push));
            victim.CancelWindup();   // 휘두르던 자세는 풀린다 — 다시 붙어서 자세를 잡아야 한다
        }

        private static bool IsMeleeKind(Game.Character.AttackKind kind)
            => kind == Game.Character.AttackKind.Melee || kind == Game.Character.AttackKind.Pulse;

        /// <summary>로봇 — 스킬 게이지를 이만큼 더 채운다(쿨이 그만큼 줄어든 것과 같다).</summary>
        private void GainSkillCharge(float seconds)
        {
            float full = SkillCooldownOf(_lastHostEntry);
            _skillCooldown = Mathf.Min(full, _skillCooldown + seconds);
        }

        // ── 잡을 때 ──────────────────────────────────────────

        private float _ninjaKillSpeedTimer;

        /// <summary>적을 잡은 **직후**에 부른다(`KillEnemy`).</summary>
        private void PassiveOnKill(Unit victim)
        {
            if (_host == null || victim == null) return;

            switch (_host.Key)
            {
                // 슬러거 — 막타에 주변이 날아간다. 야구 방망이다.
                case "baseball":
                    if (Roll(SluggerFlingChancePercent)) FlingAround(victim.Position);
                    break;

                // 닌자 — 잡으면 잠깐 빨라진다.
                case "ninja":
                    _ninjaKillSpeedTimer = NinjaKillSpeedSeconds;
                    _host.SetSpeedMul(1f + NinjaKillSpeedBonus);
                    PlayFx("dash", _host.Position, 96f, loop: false);
                    break;

                // 사신 — 잡을 때마다 그 자리에서 해골이 일어선다(명세 — 확률 없음).
                case "death":
                    SummonSkull(victim.Position);
                    break;

                // 영매 — 30% 로 해골.
                case "medium":
                    if (Roll(MediumSkullPercent)) SummonSkull(victim.Position);
                    break;
            }
        }

        /// <summary>슬러거 — 죽은 자리 둘레의 적을 밀어낸다.</summary>
        private void FlingAround(Vector2 at)
        {
            float r = Meters(SluggerFlingMeters);
            PlayFx("slam", at, r * 2f, loop: false);
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                var away = e.Position - at;
                if (away.sqrMagnitude > r * r || away.sqrMagnitude < 0.01f) continue;
                e.Position = ClampedInField(e, e.Position + away.normalized * Meters(SluggerFlingMeters));
            }
        }

        // ── 매 프레임 ────────────────────────────────────────

        /// <summary>
        /// 상태에 따라 변하는 패시브. 아마존은 **쉴드가 찰수록 빨라진다** —
        /// 쉴드를 쌓는 격투 규칙과 한 몸이라, 쉴드가 곧 속도가 된다.
        /// </summary>
        private void TickHostPassives(float dt)
        {
            if (_host == null) return;

            if (_ninjaKillSpeedTimer > 0f)
            {
                _ninjaKillSpeedTimer -= dt;
                if (_ninjaKillSpeedTimer <= 0f) _host.SetSpeedMul(1f);
                return;   // 닌자 이속이 도는 동안은 다른 배수를 얹지 않는다
            }

            if (_host.Key != "amazon") return;
            int cap = Mathf.Max(1, _host.HpMax * (_config != null ? _config.ShieldCapPercent : 30) / 100);
            float ratio = Mathf.Clamp01(_host.Shield / (float)cap);
            _host.SetSpeedMul(1f + AmazonShieldSpeedBonus * ratio);
        }
    }
}
