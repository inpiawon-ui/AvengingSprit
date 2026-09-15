using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 액티브 스킬 — **명세 2026-09-14(대화)** 로 갈아 끼운 것들.
    ///
    /// 그대로 두는 넷(아마존 정예 불굴 · 슬러거 전탄 반사 · 코만도(수류탄) 융단 폭격 ·
    /// 로봇 포탑)은 `BattleDirector.Skills.cs` 에 남아 있다. 여기 있는 것은 **바뀐 것뿐**이다.
    ///
    /// 성장 두 축(`BaseAxis` · `SpecAxis`)은 예전 구조를 그대로 쓴다 —
    /// 숫자만 새 명세로 옮겼다. Lv5 특수 효과는 사용자 지시(2026-09-14)로 **나중에** 붙인다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 지속되는 것들 ────────────────────────────────────

        /// <summary>사신 — 이 시간 동안 때리는 것마다 즉사 판정을 굴린다.</summary>
        private float _reaperSeconds;

        /// <summary>청룡 — 이 시간 동안 패시브(번개 튕김)가 100% 로 터진다.</summary>
        private float _boltSurgeSeconds;

        /// <summary>호퍼 — 이 시간 동안 치명타 확률이 고정된다.</summary>
        private float _critLockSeconds;
        private float _critLockPercent;

        /// <summary>설녀 — 얼음 안에 있는 동안. 맞지도 때리지도 않고 적이 표적으로 잡지도 않는다.</summary>
        private float _iceShellSeconds;

        /// <summary>닌자(사슬) — 이 시간 동안 묶인 적이 더 아프다.</summary>
        private float _bindBonusSeconds;

        /// <summary>코만도(레이저) — 이 시간 동안 주변 적에게 번개가 계속 튄다.</summary>
        private float _bounceSeconds, _bounceTick;

        // ── 수치 ─────────────────────────────────────────────
        private const float AmazonStrikeRadiusMeters = 2.5f;
        private const float AmazonStrikeMul = 3.0f;            // 명세 「300% 정도」
        private const float ReaperWindowSeconds = 3f;          // 명세 「3초간 유지」
        private const int ReaperBossPercent = 10;
        private const int ReaperMidBossPercent = 50;
        private const float BindSeconds = 5f;
        private const float BindDamageMul = 1.3f;
        private const float VenomRadiusMeters = 5f;
        private const float BoltSurgeSeconds = 2f;
        private const float IceShellSeconds = 2f;
        private const float IceShellHealPercent = 0.30f;
        private const float SprayBurstSeconds = 1f;            // 명세 「1초간 3방향」
        private const float LeapFarInvulnSeconds = 2f;
        private const float VampireFeastPercentPerEnemy = 0.02f;
        private const float VampireFeastRadiusMeters = 5f;
        private const float BarrierSeconds = 5f;
        private const float GangsterMarkSeconds = 3f;
        private const float CritLockSeconds = 5f;              // 명세에 시간이 없어 5초로 잡았다
        private const float CritLockPercent = 90f;             // 「90퍼 고정」
        private const int MissileFanShots = 8;
        /// <summary>발사 지점을 반원으로 벌리는 거리. 이만큼 떨어져 날아올라 한 점에서 모인다.</summary>
        private const float MissileFanOutMeters = 1.6f;
        /// <summary>미사일 한 발의 폭발 반경. 1.2 m 는 기본 폭발(130px)보다 작아 안 터진 것처럼 보였다.</summary>
        private const float MissileBlastMeters = 1.9f;
        private const float MissileFanSpreadDeg = 180f;        // 반원
        private const int WizardFanShots = 8;
        // ── 스케일 연출 기본값 (기획 2026-09-15) ────────────────
        //
        // **돌아가는 표시는 0.8 ~ 1.0 사이를 천천히 오간다.** 사장님 지정값이다.
        // 크게 흔들면 그림이 커졌다 작아지는 것이 아니라 튀는 것으로 보인다.
        private const float SkillPulseMin = 0.8f;
        private const float SkillPulseSeconds = 1.6f;   // 한 번 왕복하는 데 걸리는 시간

        /// <summary>적 몸에 얹히는 표적 크기. 몸통(약 144)보다 작아야 얼굴을 가리지 않는다.</summary>
        private const float MarkFxSize = 96f;

        /// <summary>표식이 걸린 몸과 그 위의 표적. 시간이 다하면 거둔다.</summary>
        private readonly System.Collections.Generic.List<(Unit U, Impact Fx, float Life)> _markFx = new();

        private const float WizardFanSpreadDeg = 120f;
        private const float BounceSeconds = 3f;
        private const float BounceInterval = 0.4f;
        private const float BounceRangeMeters = 6f;
        private const float BounceDamageMul = 0.5f;

        // ═══════════════════════════════════════════════════════════
        //  격투
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 아마존 — 대상에게 **붙어서** 터뜨린다. 예전 돌풍 돌진이 방향으로 밀고 나가던 것을
        /// 표적 기준으로 바꿨다(명세). 붙는 것이 목적이라 도착 지점이 대상 앞이다.
        /// </summary>
        private void AmazonLeapStrike(Unit me)
        {
            var target = NearestEnemy(me.Position);
            if (target != null)
            {
                var dir = (target.Position - me.Position);
                var to = target.Position - (dir.sqrMagnitude > 0.01f ? dir.normalized : me.Facing)
                         * Meters(0.8f);
                _dashFrom = me.Position;
                _dashTo = ClampedInField(me, to);
                _dashTime = GaleDashSeconds;
                SpawnAfterimages(me, me.Position, _dashTo);
                me.Position = _dashTo;
            }

            float r = Meters(AmazonStrikeRadiusMeters) * _buffs.AoeMul;
            int dmg = SkillDamage(me, AmazonStrikeMul * BaseAxis(1f));   // Lv1 ×3.0 → Lv4 ×4.5
            var list = EnemiesInRange(me.Position, r);
            for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, me.Profile);
            PlayFx("slam", me.Position, r * 2f, loop: false);
        }

        /// <summary>
        /// 사신 — 3초 동안 때리는 것이 죽는다. 일반은 무조건, 중간 보스는 절반, 보스는 열에 하나.
        /// **즉사 자체가 스킬**이라 피해 계수가 없다.
        /// </summary>
        private void ReaperWindow(Unit me)
        {
            _reaperSeconds = ReaperWindowSeconds;
            PlayFx("scythe", me.Position, Meters(3f), loop: false);
        }

        /// <summary>때릴 때마다 굴린다. 죽였으면 true — 부르는 쪽은 더 할 일이 없다.</summary>
        private bool TryReaperKill(Unit victim)
        {
            if (SandboxBlocksExecute) return false;   // Sandbox — 지울 때 이 줄도 함께
            if (_reaperSeconds <= 0f || victim == null || !victim.IsAlive) return false;

            int percent = victim.IsBoss ? ReaperBossPercent
                        : victim == _midBoss ? ReaperMidBossPercent : 100;
            if (!Roll(percent)) return false;

            PlayFx("scythe", victim.Position, 96f, loop: false);
            if (victim.IsBoss)
            {
                // 보스는 즉사시키지 않는다 — 방을 통째로 건너뛰게 된다. 크게 깎는다.
                int dmg = Mathf.Max(1, Mathf.RoundToInt(victim.HpMax * 0.25f));
                ShowDamage(victim.Position, dmg, toEnemy: true);
                if (victim.TakeDamage(dmg)) KillEnemy(victim);
                return true;
            }
            KillEnemy(victim);
            return true;
        }

        /// <summary>
        /// 닌자(사슬) — 방 전체를 묶는다. 발만 묶고 손은 살아 있어서(`Unit.ApplyRoot`)
        /// 원거리 적은 계속 쏜다 — 묶었다고 판이 멈추면 안 된다.
        /// </summary>
        private void ChainBind(Unit me)
        {
            float seconds = BaseAxis(BindSeconds);   // Lv1 5 → Lv4 7초
            _bindBonusSeconds = seconds;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                e.ApplyRoot(seconds);
                PlayFx("chain", e.Position, 64f, loop: false);
            }
        }

        /// <summary>묶여 있는 적에게 더 아프다. 얼린 적 보너스(설녀)와 같은 층이다.</summary>
        private float BindBonusMul(Unit victim)
            => _bindBonusSeconds > 0f && PassiveHostKey == "ninja_chain"
               && victim != null && victim.IsRooted ? BindDamageMul : 1f;

        // ═══════════════════════════════════════════════════════════
        //  중거리
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 드라군 — 가장 가까운 적 발밑에 불바다. 예전 샐러맨더의 용암 지대가
        /// 드라군으로 옮겨 왔다(명세 — 샐러맨더는 독으로 갈렸다).
        /// </summary>
        private void DragoonFireField(Unit me)
        {
            float perSecond = BaseAxis(0.5f);
            int tick = SkillDamage(me, perSecond * 0.5f);     // 장판은 0.5초 간격
            var target = NearestEnemy(me.Position);
            var at = target != null ? target.Position : SkillTargetPoint(me, Meters(LavaThrowMeters));
            SpawnField(at, Meters(LavaRadiusMeters) * _buffs.AoeMul, 6f,
                       FieldEffect.Burn, tick, fromPlayer: true);
            PlayFx("lava", at, Meters(LavaRadiusMeters) * 2f, loop: false);
        }

        /// <summary>샐러맨더 — 둘레 5 m 에 독을 뱉는다. 걸린 적은 **무조건** 중독된다(명세).</summary>
        private void SalamanderVenom(Unit me)
        {
            float r = Meters(VenomRadiusMeters) * _buffs.AoeMul;
            float seconds = BaseAxis(4f);            // Lv1 4 → Lv4 6초
            int dmg = SkillDamage(me, BaseAxis(1f));
            var list = EnemiesInRange(me.Position, r);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                HitEnemyWith(e, dmg, me.Profile);
                if (!e.IsAlive) continue;
                e.ApplyPoison(seconds);
                // ⚠ 한 장을 반경만큼 늘리면 방을 덮는 흐릿한 덩어리가 된다(실제로 그랬다).
                //   맞은 **몸마다 작게** 터뜨린다 — 누가 중독됐는지도 그래야 읽힌다.
                PlayFx("venom", e.Position, 72f, loop: false);
            }
            // 내 자리의 독 구름은 **뿜는 내내 돈다.** 한 번 터지고 사라지면
            // 「뿜었다」가 아니라 「터졌다」로 읽힌다. 크기도 천천히 오르내린다.
            _venomFx?.Stop();
            _venomFx = TakeLoopFx("venom", me.Position, VenomFxSize);
            _venomFx?.SetPulse(SkillPulseMin, 1f, SkillPulseSeconds);
            _venomSeconds = seconds;
        }

        // ── 흡혈귀 박쥐 ────────────────────────────────────────
        //
        // 머릿수만큼 날아갔다 **돌아온 뒤에** 피가 찬다.
        private const float BatFlySeconds = 0.9f;   // 가고 오는 데 걸리는 전체 시간
        private const float BatFxSize = 48f;
        private const int BatMaxCount = 8;
        private readonly System.Collections.Generic.List<(Impact Fx, Vector2 From, Vector2 To)> _batTargets = new();
        private float _batSeconds;
        private int _batHeal;

        /// <summary>뿜는 동안 몸에 붙어 도는 독 구름.</summary>
        private Impact _venomFx;
        private float _venomSeconds;
        private const float VenomFxSize = 200f;

        /// <summary>청룡 — 2초 동안 번개 튕김이 **반드시** 터진다.</summary>
        private void DragonSurge(Unit me)
        {
            _boltSurgeSeconds = BaseAxis(BoltSurgeSeconds);   // Lv1 2 → Lv4 3초
            // ⚠ 예전에는 번개 한 덩이를 **제 발밑에** 띄웠다. 일자로 뜨고 적을 향하지도
            //   않아 무엇이 일어났는지 안 읽혔다(기획 2026-09-15).
            //   가까운 적 셋에게 **각각 줄기를 뻗는다.**
            var near = EnemiesInRange(me.Position, Meters(BounceRangeMeters));
            for (int i = 0; i < near.Count && i < 3; i++) PlayBolt(me.Position, near[i].Position);
            if (near.Count == 0) PlayFx("crit", me.Position, 96f, loop: false);
        }

        /// <summary>
        /// 설녀 — 스스로 얼음에 들어간다. 2초 동안 맞지도 때리지도 않고
        /// 적이 표적으로 잡지도 않는다. 나오면서 체력을 채운다.
        /// </summary>
        private void SnowIceShell(Unit me)
        {
            float seconds = BaseAxis(IceShellSeconds);   // Lv1 2 → Lv4 3초
            _iceShellSeconds = seconds;
            _invuln = Mathf.Max(_invuln, seconds);
            int heal = Mathf.Max(1, Mathf.RoundToInt(me.HpMax * IceShellHealPercent));
            Leech(heal);
            // 얼음은 **버티는 내내** 서 있어야 한다 — 한 번 터지고 사라지면 무적인지 알 수 없다.
            _iceFx?.Stop();
            // ⚠ 그림을 `iceblock`(바닥에서 솟는 결정)에서 `ward`(몸을 감싸는 결정 껍질)로
            //   바꾼다. 「갇혔다」가 읽히려면 몸을 **둘러싸야** 한다(기획 2026-09-15).
            //   구루가 쓰던 자리인데, 구루는 얼음이 아니어야 하므로 이쪽으로 넘긴다.
            _iceFx = TakeLoopFx("ward", me.Position, IceShellFxSize);
            _iceFx?.SetPulse(SkillPulseMin, 1f, SkillPulseSeconds);
        }

        /// <summary>몸을 감싼 얼음. 스킬이 끝나면 거둔다.</summary>
        private Impact _iceFx;

        // 캐릭터 몸통이 96×92 에 `UnitScale` 1.5 라 화면에서 약 144 px 다.
        // 「조금만 더 크게」 — 갇힌 것으로 보이되 몸을 덮어 가리지는 않는 크기.
        private const float IceShellFxSize = 168f;

        // ═══════════════════════════════════════════════════════════
        //  원거리
        // ═══════════════════════════════════════════════════════════

        /// <summary>호퍼(기관단총) — **가장 먼** 적에게 뛰어들고 2초 무적. 파고드는 기술이다.</summary>
        private void LeapFar(Unit me)
        {
            Unit far = null;
            float best = -1f;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                float d = Vector2.Distance(e.Position, me.Position);
                if (d > best) { best = d; far = e; }
            }

            var to = far != null
                   ? far.Position - (far.Position - me.Position).normalized * Meters(1.2f)
                   : me.Position + me.Facing * Meters(4f);
            _dashFrom = me.Position;
            _dashTo = ClampedInField(me, to);
            _dashTime = GaleDashSeconds;
            me.Position = _dashTo;
            SpawnAfterimages(me, _dashFrom, _dashTo);
            _invuln = Mathf.Max(_invuln, BaseAxis(LeapFarInvulnSeconds));   // Lv1 2 → Lv4 3초
            PlayFx("dash", me.Position, GaleDashFxSize, loop: false);
        }

        /// <summary>닌자 — 방 한가운데에 분신을 세운다. 서 있는 동안 적이 전부 그쪽을 본다.</summary>
        private void NinjaCloneSkill(Unit me)
        {
            var center = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.5f);
            SummonClone(center);
        }

        /// <summary>흡혈귀 — 둘레 5 m 의 **머릿수만큼** 돌려받는다. 몰린 곳에서 쓰는 기술이다.</summary>
        private void VampireFeast(Unit me)
        {
            var list = EnemiesInRange(me.Position, Meters(VampireFeastRadiusMeters));
            if (list.Count == 0) { PlayFx("drain", me.Position, 64f, loop: false); return; }

            float per = VampireFeastPercentPerEnemy * BaseAxis(1f);   // Lv1 2% → Lv4 3%
            int heal = Mathf.Max(1, Mathf.RoundToInt(me.HpMax * per * list.Count));

            // ⚠ **즉발이 아니다.** 머릿수만큼 박쥐를 날려 보내고, 돌아온 뒤에 피가 찬다
            //   (기획 2026-09-15). 즉시 차면 무엇 때문에 찼는지가 안 보인다.
            _batHeal = heal;
            _batSeconds = BatFlySeconds;
            _batTargets.Clear();
            for (int i = 0; i < list.Count && i < BatMaxCount; i++)
            {
                var im = FreeImpact(BatFxSize);
                if (im == null) break;
                im.Play(me.Position, FxFrames("bat"), BatFxSize, loop: true);
                _batTargets.Add((im, me.Position, list[i].Position));
            }
            if (_batTargets.Count == 0) { Leech(heal); _batSeconds = 0f; }
        }

        /// <summary>코만도(기관총) — 최대 체력만큼 쉴드를 두른다. 5초가 지나거나 깎이면 끝난다.</summary>
        private void CommandoBarrier(Unit me)
        {
            int amount = Mathf.Max(1, Mathf.RoundToInt(me.HpMax * BaseAxis(1f)));   // Lv1 100% → Lv4 150%
            me.AddShield(amount, me.HpMax * 2);
            _barrierSeconds = BarrierSeconds;
            PlayFx("shield", me.Position, 128f, loop: false);
        }

        private float _barrierSeconds;

        /// <summary>갱스터 — 방 전체에 표식. 패시브(20% 즉사)와 한 쌍이다.</summary>
        private void GangsterMarkAll(Unit me)
        {
            float seconds = BaseAxis(GangsterMarkSeconds);   // Lv1 3 → Lv4 5초
            int percent = _markPercent;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                e.ApplyAmp(percent, seconds);
                e.SetMark(seconds);
                // ⚠ 표적은 **표식이 걸려 있는 내내 붙어 돈다.** 한 번 깜빡이고 사라지면
                //   누가 찍혔는지 알 수 없다(기획 2026-09-15). 크기도 천천히 오르내린다.
                var im = TakeLoopFx("mark", e.Position, MarkFxSize);
                im?.SetPulse(SkillPulseMin, 1f, SkillPulseSeconds);
                if (im != null) _markFx.Add((e, im, seconds));
            }
        }

        /// <summary>호퍼 — 치명타 확률을 90% 로 **고정**한다(명세). 패시브(치명타 피해)와 한 쌍이다.</summary>
        private void HopperCritSurge(Unit me)
        {
            _critLockSeconds = BaseAxis(CritLockSeconds);   // Lv1 5 → Lv4 7.5초
            _critLockPercent = CritLockPercent;
            PlayFx("crit", me.Position, 96f, loop: false);
        }

        /// <summary>코만도(미사일) — 반원으로 퍼진 8발이 **한 대상**으로 모인다.</summary>
        private void MissileFan(Unit me)
        {
            var target = NearestEnemy(me.Position);
            int dmg = SkillDamage(me, BaseAxis(1f));
            float radius = Meters(MissileBlastMeters);
            for (int i = 0; i < MissileFanShots; i++)
            {
                float off = -MissileFanSpreadDeg * 0.5f
                          + MissileFanSpreadDeg * i / (MissileFanShots - 1);
                // ⚠ **출발점을 벌린다.** 예전에는 각도를 계산해 놓고 타겟이 있으면
                //   쓰지 않아, 8발이 총구 한 점에서 같은 점으로 날아 완전히 겹쳤다 —
                //   화면에서 두 발로 보였다(기획 2026-09-15).
                //   반원으로 벌어졌다가 한 대상에서 모이는 것이 명세다.
                var from = ClampedInField(me, me.Position
                                            + Rotate(me.Facing, off) * Meters(MissileFanOutMeters));
                var at = target != null
                       ? target.Position
                       : ClampedInField(me, me.Position + Rotate(me.Facing, off) * Meters(6f));
                ThrowSkillGrenade(me, at, dmg, radius, from);
                PlayFx("missile_trail", from, 32f, loop: false);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  관통
        // ═══════════════════════════════════════════════════════════

        /// <summary>영매 — 골렘을 세운다. 해골(패시브)보다 크고 오래 간다.</summary>
        private void MediumGolem(Unit me)
            => SummonGolem(ClampedInField(me, me.Position + me.Facing * Meters(1.5f)));

        /// <summary>화이트 위저드 — 부채꼴 8방향. 관통은 패시브가 준다.</summary>
        private void WizardFan(Unit me)
        {
            var target = NearestEnemy(me.Position);
            // ⚠ `FireShot` 은 표적의 자리를 보고 쏜다 — 표적이 없으면 그 안에서 터진다.
            //   방이 비었으면 쏠 이유도 없다.
            if (target == null) return;
            for (int i = 0; i < WizardFanShots; i++)
            {
                float off = -WizardFanSpreadDeg * 0.5f
                          + WizardFanSpreadDeg * i / (WizardFanShots - 1);
                FireShot(me, target, fromPlayer: true, angleOffsetDeg: off);
            }
            PlayFx("holy_beam", me.MuzzlePosition, 96f, loop: false);
        }

        /// <summary>코만도(레이저) — 3초 동안 주변 적에게 계속 튄다.</summary>
        private void LaserBounce(Unit me)
        {
            _bounceSeconds = BaseAxis(BounceSeconds);   // Lv1 3 → Lv4 4.5초
            _bounceTick = 0f;
            // 시전 순간 한 번은 나에게서 가장 가까운 적으로 뻗는다 — 시작이 보여야 한다.
            var first = NearestEnemy(me.Position);
            if (first != null) PlayBolt(me.Position, first.Position);
            else PlayFx("crit", me.Position, 96f, loop: false);
        }

        // ═══════════════════════════════════════════════════════════
        //  시간 흐르기
        // ═══════════════════════════════════════════════════════════

        /// <summary>새 액티브의 지속 시간을 흘린다. `TickHostPassives` 옆에서 돈다.</summary>
        private void TickNewSkills(float dt)
        {
            if (_venomSeconds > 0f)
            {
                _venomSeconds -= dt;
                if (_venomFx != null && _host != null) _venomFx.MoveTo(_host.Position);
                if (_venomSeconds <= 0f) { _venomFx?.Stop(); _venomFx = null; }
            }
            for (int i = _markFx.Count - 1; i >= 0; i--)
            {
                var (u, fx, life) = _markFx[i];
                life -= dt;
                if (u == null || !u.IsAlive || life <= 0f) { fx?.Stop(); _markFx.RemoveAt(i); continue; }
                fx.MoveTo(u.Position);
                _markFx[i] = (u, fx, life);
            }
            if (_batSeconds > 0f)
            {
                _batSeconds -= dt;
                // 앞 절반은 가고 뒤 절반은 돌아온다. 돌아온 순간에 피가 찬다.
                float k = 1f - Mathf.Clamp01(_batSeconds / BatFlySeconds);
                float t = k < 0.5f ? k * 2f : (1f - k) * 2f;
                var home = _host != null ? _host.Position : Vector2.zero;
                for (int i = 0; i < _batTargets.Count; i++)
                {
                    var (fx, _, to) = _batTargets[i];
                    fx?.MoveTo(Vector2.Lerp(home, to, t));
                }
                if (_batSeconds <= 0f)
                {
                    for (int i = 0; i < _batTargets.Count; i++) _batTargets[i].Fx?.Stop();
                    _batTargets.Clear();
                    if (_batHeal > 0) { Leech(_batHeal); _batHeal = 0; }
                    PlayFx("drain", home, 96f, loop: false);
                }
            }
            if (_reaperSeconds > 0f) _reaperSeconds -= dt;
            if (_boltSurgeSeconds > 0f) _boltSurgeSeconds -= dt;
            if (_bindBonusSeconds > 0f) _bindBonusSeconds -= dt;
            if (_critLockSeconds > 0f) _critLockSeconds -= dt;
            if (_iceShellSeconds > 0f)
            {
                _iceShellSeconds -= dt;
                // 얼음은 몸을 따라다닌다. 끝나면 거둔다 — 안 거두면 방이 바뀌어도 남는다.
                if (_iceFx != null && _host != null) _iceFx.MoveTo(_host.Position);
                if (_iceShellSeconds <= 0f) { _iceFx?.Stop(); _iceFx = null; }
            }

            // 쉴드는 시간이 지나면 걷힌다 — 안 걷으면 다음 방까지 들고 간다.
            if (_barrierSeconds > 0f)
            {
                _barrierSeconds -= dt;
                if (_barrierSeconds <= 0f && _host != null) _host.ClearShield();
            }

            if (_bounceSeconds <= 0f) return;
            _bounceSeconds -= dt;
            _bounceTick -= dt;
            if (_bounceTick > 0f || _host == null) return;

            _bounceTick = BounceInterval;
            var list = EnemiesInRange(_host.Position, Meters(BounceRangeMeters));
            int dmg = SkillDamage(_host, BounceDamageMul);
            // ⚠ **줄기를 이어 그린다.** 예전에는 맞는 적 자리에 번개 한 덩이만 띄웠다 —
            //   시전자 발밑의 레이저 기둥은 가만히 있는데 멀리 있는 적이 맞아서,
            //   무엇이 무엇을 때리는지 안 보였다(기획 2026-09-15).
            //   나 → 첫 적 → 둘째 → 셋째 로 **타고 흐르는** 것이 이 스킬이다.
            var link = _host.Position;
            for (int i = 0; i < list.Count && i < 3; i++)
            {
                PlayBolt(link, list[i].Position);
                link = list[i].Position;
                HitEnemyWith(list[i], dmg, _host.Profile);
            }
        }

        /// <summary>
        /// <paramref name="from"/> 에서 <paramref name="to"/> 로 번개 줄기를 뻗는다.
        /// 어디서 어디로 갔는지가 보여야 「튕겼다」가 읽힌다.
        /// </summary>
        private void PlayBolt(Vector2 from, Vector2 to)
        {
            var frames = FxFrames("boltbeam");
            if (frames == null) { PlayFx("bolt", to, 64f, loop: false); return; }
            var im = FreeImpact(BoltBeamThickness);
            im?.PlayBeam(from, to, frames, BoltBeamThickness);
        }

        /// <summary>줄기 굵기(px). 그림이 192×64 라 세로 64 를 그대로 쓴다.</summary>
        private const float BoltBeamThickness = 64f;

        /// <summary>설녀가 얼음 안에 있는 동안은 손도 멈춘다(명세 — 공격도 못 한다).</summary>
        private bool IsSelfFrozen => _iceShellSeconds > 0f;
    }
}
