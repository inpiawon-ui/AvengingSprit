using Game.Character;
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

        /// <summary>호퍼(기관단총)·설녀 — **스킬이 준** 무적 시간. 이 동안만 몸 윤곽이 깜빡인다.</summary>
        private float _skillInvuln;

        /// <summary>청룡 — 적에서 적으로 튕겨 가는 번개. 남은 튕김 · 다음 튕김까지 · 지난 자리.</summary>
        private int _chainHopsLeft;
        private float _chainHopTimer;
        private Vector2 _chainFrom;
        private Unit _chainLast;
        // 이번 연쇄에서 이미 맞은 적. 안 맞은 적이 있으면 그쪽으로 먼저 튄다(기획 2026-09-15).
        private readonly System.Collections.Generic.HashSet<Unit> _chainStruck = new();
        private int _chainDamage;
        private HostEntry _chainProfile;

        /// <summary>샐러맨더 — 날아가는 독침. 닿는 순간 피해와 중독이 들어간다.</summary>
        private readonly System.Collections.Generic.List<(Impact Fx, Unit Target, Vector2 From, float T)> _spits = new();
        private int _spitDamage;
        private float _spitPoisonSeconds;

        /// <summary>지속 중인 스킬 표시(슬러거 반사 · 호퍼 정조준). 몸을 따라 돈다.</summary>
        private Impact _reflectAuraFx, _critAuraFx;

        // ── 수치 ─────────────────────────────────────────────
        private const float AmazonStrikeRadiusMeters = 2.5f;
        private const float AmazonStrikeMul = 6.0f;            // 명세 「300% 정도」 → 기획 2026-09-15 피해 2배
        private const float ReaperWindowSeconds = 3f;          // 명세 「3초간 유지」
        private const int ReaperBossPercent = 10;
        private const int ReaperMidBossPercent = 50;
        private const float BindSeconds = 5f;
        private const float BindDamageMul = 1.3f;
        private const float VenomRadiusMeters = 5f;
        /// <summary>독 지속에 얹는 시간. 표의 성장 축이 피해 계수와 같은 값(Lv1 0.5)이라 독이 0.5초뿐이었다.</summary>
        private const float VenomExtraSeconds = 2f;
        private const float BoltSurgeSeconds = 2f;
        private const int ChainHops = 20;                      // 기획 2026-09-15 — 10번 → 20번
        private const float ChainHopSeconds = 0.08f;           // 한 번에 다 그으면 여러 갈래로 보인다
        private const float ChainHopRangeMeters = 6f;
        private const float ChainHopDamageMul = 0.6f;          // 튕길 때마다 **같은** 피해
        private const float SpitSeconds = 0.28f;               // 독침이 날아가는 시간
        private const float SpitFxSize = 48f;
        private const float SkillAuraFxSize = 184f;
        private const float MissileFanHoming = 1.2f;           // 벌어진 자리에서 표적으로 모이는 휨
        // ⚠ 아래 값은 **표가 비었을 때만** 쓰는 대비값이다. 실제 지속은 `BaseAxis` 가 액티브 스킬 표의
        //   성장 축(psg_h21 `_scaling`)에서 읽는다 — 상수만 반으로 줄였더니 여전히 2.5초였다(2026-09-15).
        //   반으로 줄인 값은 표(1.25~1.9)와 원천(`Editor/HostSkillTable.cs`)에 들어 있다.
        private const float IceShellSeconds = 1f;
        private const float IceShellHealPercent = 0.30f;
        private const float SprayBurstSeconds = 1f;            // 명세 「1초간 3방향」
        private const float LeapFarInvulnSeconds = 2f;
        private const float VampireFeastPercentPerEnemy = 0.02f;
        private const float BarrierSeconds = 5f;
        private const float GangsterMarkSeconds = 3f;
        private const float CritLockSeconds = 5f;              // 명세에 시간이 없어 5초로 잡았다
        private const float CritLockPercent = 90f;             // 「90퍼 고정」
        private const int MissileFanShots = 8;
        /// <summary>발사 지점을 반원으로 벌리는 거리. 이만큼 떨어져 날아올라 한 점에서 모인다.</summary>
        private const float MissileFanOutMeters = 0.6f;
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
            int dmg = SkillDamage(me, AmazonStrikeMul * BaseAxis(1f));   // 6.0 × 표 성장축(Lv1 1.5 → Lv4 2.2)
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
            // ⚠ 불바다는 **사는 내내 일렁인다.** 한 장짜리 `field_burn` 이 깔려 6초를 가만히 있었다
            //   (기획 2026-09-15). 컷 네 장(`fx_lava_1~4`)을 장판 그림으로 넘겨 돌린다 —
            //   시작할 때 한 번 터지던 같은 그림은 겹쳐 보이므로 뺀다.
            SpawnField(at, Meters(LavaRadiusMeters) * _buffs.AoeMul, 6f,
                       FieldEffect.Burn, tick, fromPlayer: true, artKey: "firefield");
            // ⚠ `fx_lava` 는 불 둘레의 검붉은 얼룩이 **피웅덩이**로 읽혀 반려됐다(기획 2026-09-15).
            //   영역 곳곳에서 불길이 솟고 바깥은 숯빛인 `fx_firefield_1~4` 로 바꾼다.
        }

        /// <summary>샐러맨더 — 둘레 5 m 에 독을 뱉는다. 걸린 적은 **무조건** 중독된다(명세).</summary>
        private void SalamanderVenom(Unit me)
        {
            float r = Meters(VenomRadiusMeters) * _buffs.AoeMul;
            // ⚠ `BaseAxis` 는 표의 성장 축을 돌려준다 — 피해 계수와 **같은 값**이라 표를 바꾸면 피해도 바뀐다.
            //   독 시간만 2초를 더 얹는다(기획 2026-09-15).
            float seconds = BaseAxis(4f) + VenomExtraSeconds;
            int dmg = SkillDamage(me, BaseAxis(1f));
            // ⚠ **적에게 독을 뱉어 묻힌다**(기획 2026-09-15). 예전에는 독 구름을 **내 몸에** 돌려서
            //   나에게 독을 뿜는 것처럼 보였다. 이제 둘레의 적마다 독침이 날아가고,
            //   닿는 순간 피해와 중독이 들어간다 — 누가 중독됐는지도 그 자리에서 읽힌다.
            _spitDamage = dmg;
            _spitPoisonSeconds = seconds;
            var from = me.MuzzlePosition;
            var frames = ShotFrames("venom");
            var list = EnemiesInRange(me.Position, r);
            for (int i = 0; i < list.Count; i++)
            {
                var im = frames != null ? FreeImpact(SpitFxSize) : null;
                if (im == null) { LandSpit(list[i]); continue; }   // 그림·자리가 없으면 바로 묻힌다
                im.Play(from, frames, SpitFxSize, loop: true);
                _spits.Add((im, list[i], from, 0f));
            }
        }

        /// <summary>독침이 닿았다. 피해 · 중독 · 튀는 독.</summary>
        private void LandSpit(Unit e)
        {
            if (e == null || !e.IsAlive || e.IsDying) return;
            HitEnemyWith(e, _spitDamage, _host != null ? _host.Profile : null);
            if (!e.IsAlive) return;
            e.ApplyPoison(_spitPoisonSeconds);
            // 맞은 **몸마다 작게** 터뜨린다 — 한 장을 반경만큼 늘리면 방을 덮는 흐릿한 덩어리가 된다.
            PlayFx("venom", e.Position, 72f, loop: false);
        }

        // ── 흡혈귀 박쥐 ────────────────────────────────────────
        //
        // 머릿수만큼 날아갔다 **돌아온 뒤에** 피가 찬다.
        private const float BatFlySeconds = 0.9f;   // 가고 오는 데 걸리는 전체 시간
        private const float BatFxSize = 48f;
        private readonly System.Collections.Generic.List<Unit> _onScreenBuffer = new();
        private readonly System.Collections.Generic.List<(Impact Fx, Vector2 From, Vector2 To)> _batTargets = new();
        private float _batSeconds;
        private int _batHeal;


        /// <summary>청룡 — 2초 동안 번개 튕김이 **반드시** 터진다.</summary>
        private void DragonSurge(Unit me)
        {
            _boltSurgeSeconds = BaseAxis(BoltSurgeSeconds);   // Lv1 2 → Lv4 3초
            // ⚠ 예전에는 번개 한 덩이를 **제 발밑에** 띄웠다. 일자로 뜨고 적을 향하지도
            //   않아 무엇이 일어났는지 안 읽혔다(기획 2026-09-15).
            //   가까운 적 셋에게 **각각 줄기를 뻗었더니** 튕기는 게 아니라 여러 갈래로 쏘는 것으로
            //   보였다(기획 2026-09-15). 이제 **적에서 적으로 20번** 튕겨 간다 — 한 번에 긋지 않고
            //   0.08초마다 한 칸씩. **아직 안 맞은 적이 사거리에 있으면 그쪽으로** 튄다(기획 2026-09-15).
            //   다 맞았으면 맞은 적을 다시 맞아도 되지만 방금 맞은 적은 건너뛴다 —
            //   그래서 **몹이 하나뿐이면 한 번 쏘고 끝난다.**
            _chainHopsLeft = ChainHops;
            _chainHopTimer = 0f;
            _chainFrom = me.Position;
            _chainLast = null;
            _chainStruck.Clear();
            _chainDamage = SkillDamage(me, ChainHopDamageMul * BaseAxis(1f));
            _chainProfile = me.Profile;
            if (NearestEnemy(me.Position, Meters(ChainHopRangeMeters)) == null)
            { _chainHopsLeft = 0; PlayFx("crit", me.Position, 96f, loop: false); }
        }

        /// <summary>청룡 번개 한 칸. 지난 자리에서 가장 가까운 적으로 뻗는다.</summary>
        private void TickChain(float dt)
        {
            if (_chainHopsLeft <= 0) return;
            _chainHopTimer -= dt;
            if (_chainHopTimer > 0f) return;
            _chainHopTimer = ChainHopSeconds;

            // ① 아직 안 맞은 적 중 가장 가까운 적 ② 없으면 방금 맞은 적만 빼고 가장 가까운 적
            float range = Meters(ChainHopRangeMeters);
            Unit next = null, fallback = null;
            float best = range, bestFallback = range;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || e == _chainLast) continue;
                float d = Vector2.Distance(e.Position, _chainFrom);
                if (d <= bestFallback) { bestFallback = d; fallback = e; }
                if (!_chainStruck.Contains(e) && d <= best) { best = d; next = e; }
            }
            if (next == null) next = fallback;
            // 튈 곳이 방금 맞은 적뿐이면 **거기서 끝낸다**(기획 2026-09-15) — 몹이 하나면 한 번 쏘고 끝이다.
            if (next == null) { _chainHopsLeft = 0; return; }

            PlayBolt(_chainFrom, next.Position);
            HitEnemyWith(next, _chainDamage, _chainProfile);
            _chainFrom = next.Position;
            _chainLast = next;
            _chainStruck.Add(next);
            _chainHopsLeft--;
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
            _skillInvuln = Mathf.Max(_skillInvuln, seconds);
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
            float invuln = BaseAxis(LeapFarInvulnSeconds);   // Lv1 2 → Lv4 3초
            _invuln = Mathf.Max(_invuln, invuln);
            // 스킬이 준 무적이다 — 이 동안만 몸 윤곽이 깜빡인다(기획 2026-09-15).
            _skillInvuln = Mathf.Max(_skillInvuln, invuln);
            PlayFx("dash", me.Position, GaleDashFxSize, loop: false);
        }

        /// <summary>닌자 — 방 한가운데에 분신을 세운다. 서 있는 동안 적이 전부 그쪽을 본다.</summary>
        private void NinjaCloneSkill(Unit me)
        {
            var center = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.5f);
            SummonClone(center);
        }

        /// <summary>흡혈귀 — 화면에 보이는 적 **머릿수만큼** 돌려받는다.</summary>
        private void VampireFeast(Unit me)
        {
            // ⚠ **화면에 보이는 적 전부**에게 보낸다(기획 2026-09-15). 둘레 5 m 는 화면에 안 그려져
            //   누구는 박쥐가 가고 누구는 안 가는 게 이상해 보였다. 회복도 그 머릿수로 센다.
            var list = _onScreenBuffer;
            list.Clear();
            for (int i = 0; i < _enemies.Count; i++)
                if (Targetable(_enemies[i]) && IsOnScreen(_enemies[i])) list.Add(_enemies[i]);
            if (list.Count == 0) { PlayFx("drain", me.Position, 64f, loop: false); return; }

            float per = VampireFeastPercentPerEnemy * BaseAxis(1f);   // Lv1 2% → Lv4 3%
            int heal = Mathf.Max(1, Mathf.RoundToInt(me.HpMax * per * list.Count));

            // ⚠ **즉발이 아니다.** 머릿수만큼 박쥐를 날려 보내고, 돌아온 뒤에 피가 찬다
            //   (기획 2026-09-15). 즉시 차면 무엇 때문에 찼는지가 안 보인다.
            _batHeal = heal;
            _batSeconds = BatFlySeconds;
            _batTargets.Clear();
            for (int i = 0; i < list.Count; i++)
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
            // 지속 내내 **몸에 붙어 도는 표시**(기획 2026-09-15) — 시작 별 한 번으로는 켜져 있는지 몰랐다.
            StartSkillAura(ref _critAuraFx, "critlock");
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
                // ⚠ **몸 가까이에서 부채꼴로 뻗어 나갔다가 휘어 모인다**(기획 2026-09-15).
                //   1.6 m 밖에서 출발시켰더니 옆에 붙은 적 몸 안에서 태어나 그 자리에서 다 터졌다.
                //   출발은 몸 곁(0.6 m), 첫 방향은 제 각도, 표적 쪽으로는 유도가 휘어 준다.
                var aim = target != null && (target.Position - me.Position).sqrMagnitude > 1f
                        ? (target.Position - me.Position).normalized
                        : me.Facing;
                var fan = Rotate(aim, off);
                var from = ClampedInField(me, me.Position + fan * Meters(MissileFanOutMeters));
                var at = from + fan * Meters(6f);
                FireSkillMissile(me, from, at, target, dmg, radius);
                PlayFx("missile_trail", from, 32f, loop: false);
            }
        }

        /// <summary>
        /// 코만도(미사일) 한 발 — **곧게 날아간다.** 수류탄처럼 포물선으로 던졌더니
        /// 로켓이 아니라 폭탄을 던지는 것으로 보였다(기획 2026-09-15). 머리가 나는 쪽을 보고,
        /// 벌어진 자리에서 표적으로 모이도록 조금 휘며, 닿거나 수명이 다하면 터진다.
        /// </summary>
        private void FireSkillMissile(Unit me, Vector2 from, Vector2 at, Unit target, int damage, float radius)
        {
            var shot = RentShot();
            if (shot == null) return;
            shot.SetSprite(ShotFrames("missile"), "missile", LoopsFrames("missile"));
            shot.Fire(from, at, _config.ShotSpeedPlayer, damage, true, target,
                      _config.ShotSize, ShotPlayerColor, _config.ShotLifeSeconds);
            shot.SetBlastRadius(radius);
            if (target != null) shot.SetHoming(MissileFanHoming);
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
            for (int i = _spits.Count - 1; i >= 0; i--)
            {
                var (fx, target, from, t) = _spits[i];
                t += dt / SpitSeconds;
                bool gone = target == null || !target.IsAlive || target.IsDying;
                if (gone || t >= 1f)
                {
                    fx?.Stop();
                    _spits.RemoveAt(i);
                    if (!gone) LandSpit(target);
                    continue;
                }
                var to = target.Position;
                if (fx != null)
                {
                    fx.MoveTo(Vector2.Lerp(from, to, t));
                    var d = to - from;
                    // 독침 그림은 가로로 길다 — 나는 쪽을 보게 돌린다.
                    fx.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                }
                _spits[i] = (fx, target, from, t);
            }
            TickChain(dt);
            TickSkillAura(ref _reflectAuraFx, IsReflectingAll);
            TickSkillAura(ref _critAuraFx, _critLockSeconds > 0f);
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

        /// <summary>지속 표시를 몸 위에 띄운다. 이미 떠 있으면 새로 건다.</summary>
        private void StartSkillAura(ref Impact slot, string fx)
        {
            slot?.Stop();
            var at = _host != null ? _host.Position : Vector2.zero;
            slot = TakeLoopFx(fx, at, SkillAuraFxSize);
        }

        /// <summary>지속 중이면 몸을 따라가고, 끝나면 거둔다 — 안 거두면 방이 바뀌어도 남는다.</summary>
        private void TickSkillAura(ref Impact slot, bool alive)
        {
            if (slot == null) return;
            if (alive && _host != null) slot.MoveTo(_host.Position);
            else { slot.Stop(); slot = null; }
        }

        /// <summary>설녀가 얼음 안에 있는 동안은 손도 멈춘다(명세 — 공격도 못 한다).</summary>
        private bool IsSelfFrozen => _iceShellSeconds > 0f;
    }
}
