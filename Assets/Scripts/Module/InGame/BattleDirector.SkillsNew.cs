using System.Collections.Generic;
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

        /// <summary>호퍼 — 이 시간 동안 치명타 확률이 고정된다.</summary>
        private float _critLockSeconds;
        private float _critLockPercent = 90f;   // 정조준 액티브는 PD 2026-10-29 에 「조준 사격」으로 바뀌었다 — 켜는 곳이 없다

        /// <summary>설녀 — 얼음 안에 있는 동안. 맞지도 때리지도 않고 적이 표적으로 잡지도 않는다.</summary>
        private float _iceShellSeconds;


        /// <summary>호퍼(기관단총)·설녀 — **스킬이 준** 무적 시간. 이 동안만 몸 윤곽이 깜빡인다.</summary>
        private float _skillInvuln;

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
        private const float VenomRadiusMeters = 5f;
        /// <summary>독 지속에 얹는 시간. 표의 성장 축이 피해 계수와 같은 값(Lv1 0.5)이라 독이 0.5초뿐이었다.</summary>
        private const float VenomExtraSeconds = 2f;
        private const float SpitSeconds = 0.28f;               // 독침이 날아가는 시간
        private const float SpitFxSize = 48f;
        private const float SkillAuraFxSize = 184f;
        private const float MissileFanHoming = 1.2f;           // 벌어진 자리에서 표적으로 모이는 휨
        private const float SprayBurstSeconds = 1f;            // 명세 「1초간 3방향」
        private const float LeapFarInvulnSeconds = 2f;
        private const float VampireFeastPercentPerEnemy = 0.02f;
        private const float VampireBiteMul = 0.5f;
        private bool _batBitten;
        /// <summary>발사 지점을 반원으로 벌리는 거리. 이만큼 떨어져 날아올라 한 점에서 모인다.</summary>
        private const float MissileFanOutMeters = 0.45f;
        /// <summary>미사일 한 발의 폭발 반경. 1.2 m 는 기본 폭발(130px)보다 작아 안 터진 것처럼 보였다.</summary>
        private const float MissileBlastMeters = 1.9f;
        // 반원(180°)은 옆 · 아래로 가는 탄이 많아 시안의 전방 부채꼴(약 115°)과 달랐다(검수 2026-10-07)
        private const float MissileFanSpreadDeg = 115f;
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
            // 퀄업 연출(2026-10-07) — 조준이 걸리고 0.1초 뒤 같은 동작으로 뛰어든다
            if (WpAmazonWindup(me, target)) return;
            AmazonLeapLand(me, target);
        }

        private void AmazonLeapLand(Unit me, Unit target)
        {
            var from = me.Position;
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
            // 목록을 베껴 둔다 — 맞아 죽는 쪽(KillEnemy)이 같은 버퍼를 다시 쓸 수 있다
            var list = new List<Unit>(EnemiesInRange(me.Position, r));
            int killed = 0;
            for (int i = 0; i < list.Count; i++)
            {
                bool alive = list[i].IsAlive;
                HitEnemyWith(list[i], dmg, me.Profile);
                if (alive && !list[i].IsAlive) killed++;
            }
            if (killed > 0) AmazonSprint(me);   // 패시브 「질주」 — 처치하면 한 번 더 도약(PD 2026-10-29)
            // 큰 회색 폭발 구름은 캐릭터 · 적을 통째로 가렸다(코덱스 진단) — 퀄업 연출이 있으면 절삭 · 바닥 펄스로
            if (WpReady) WpAmazonImpact(from, me.Position, r);
            else PlayFx("slam", me.Position, r * 2f, loop: false);
            if (InyeonOn(me)) IyLeapRing(me);   // 인연 「번개 도약」 — 착지마다 번개 고리
        }

        /// <summary>
        /// 사신 — 3초 동안 때리는 것이 죽는다. 일반은 무조건, 중간 보스는 절반, 보스는 열에 하나.
        /// **즉사 자체가 스킬**이라 피해 계수가 없다.
        /// </summary>
        private void ReaperWindow(Unit me)
        {
            _reaperSeconds = ReaperWindowSeconds;
            if (!QxReaperOpen(me, _reaperSeconds))   // 퀄업(2026-10-07) — 낫 한 번 + 발밑 보라 기운
                PlayFx("scythe", me.Position, Meters(3f), loop: false);
        }

        /// <summary>때릴 때마다 굴린다. 죽였으면 true — 부르는 쪽은 더 할 일이 없다.</summary>
        private bool TryReaperKill(Unit victim)
        {
            if (SandboxBlocksExecute) return false;   // Sandbox — 지울 때 이 줄도 함께
            if (_reaperSeconds <= 0f || victim == null || !victim.IsAlive) return false;
            // 사신이 **직접** 친 것만 — 해골이 친 것까지 즉사시키면, 죽은 자리에 선 해골이 또 옆을 즉사시켜
            // 폭발이 한 줄로 번졌다(PD 2026-10-21 「근접인데 투사체가 나가면서 일자로 터진다」)
            if (_summonStriking) return false;

            int percent = victim.IsBoss ? ReaperBossPercent
                        : victim == _midBoss ? ReaperMidBossPercent : 100;
            if (!Roll(percent)) return false;

            if (!QxReaperKill(victim)) PlayFx("scythe", victim.Position, 96f, loop: false);
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
        /// 패시브 「사슬 감기」(닌자(사슬) · PD 2026-10-29) — 사슬로 끌려온 적은 3초간 받는 피해 +25%.
        /// 예전엔 「묶여 있는 동안 ×1.3」(묶임 1.5초)이었다. 얼린 적 보너스(설녀)와 같은 층이다.
        /// </summary>
        private float BindBonusMul(Unit victim)
            => (PassiveHostKey == "ninja_chain" && IsWrapped(victim) ? 1f + PassiveFrac(ChainWrapBonus) : 1f)
               * IyContractMul(victim);   // 인연 「청부 표식」 — 보조가 표식 적을 때리면 +50%(BattleDirector.SkillsInyeon)

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
            float fieldRadius = Meters(LavaRadiusMeters) * _buffs.AoeMul;
            bool iy = InyeonOn(me);   // 인연 「업화」 — 불바다 1.5배 · 밟은 적 둔화
            if (iy) fieldRadius *= IyFireFieldMul;
            SpawnField(at, fieldRadius, 6f,
                       FieldEffect.Burn, tick, fromPlayer: true, artKey: FireFieldArt);
            QxFireBloom(at, fieldRadius);   // 퀄업(2026-10-07) — 불바다가 확 피어난다
            if (iy) IyFireFieldSlow(at, fieldRadius);
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
            QxVenomCast(me);   // 퀄업(2026-10-07) — 입 앞에 모이는 빛 · 둘레로 퍼지는 독 안개
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


        /// <summary>
        /// 몸을 감싼 얼음(예전 설녀 액티브 「얼음 감옥」의 잔재 — 지금은 켜는 스킬이 없다).
        /// PD 2026-10-29 에 설녀 액티브가 「눈보라」(적을 얼린다)로 바뀌었다. 시계 · 그림을 거두는 쪽만 남겨 둔다.
        /// </summary>
        private Impact _iceFx;

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
            // 퀄업 연출(2026-10-07) — 가장 먼 적에게 조준이 걸리고 0.1초 뒤 뛰어든다
            if (WpSmgWindup(me, far)) return;
            LeapFarGo(me, far);
        }

        private void LeapFarGo(Unit me, Unit far)
        {
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
            if (WpReady) WpSmgLanded(me, _dashFrom, _dashTo);
            else PlayFx("dash", me.Position, GaleDashFxSize, loop: false);
        }

        /// <summary>흡혈귀 — 화면에 보이는 적 **머릿수만큼** 돌려받는다.</summary>
        private const int VampireBatTargets = 4;

        private void VampireFeast(Unit me)
        {
            // ⚠ **화면에 보이는 적 전부**에게 보낸다(기획 2026-09-15). 둘레 5 m 는 화면에 안 그려져
            //   누구는 박쥐가 가고 누구는 안 가는 게 이상해 보였다. 회복도 그 머릿수로 센다.
            var list = _onScreenBuffer;
            list.Clear();
            for (int i = 0; i < _enemies.Count; i++)
                if (Targetable(_enemies[i]) && IsOnScreen(_enemies[i])) list.Add(_enemies[i]);
            if (list.Count == 0) { PlayFx("drain", me.Position, 64f, loop: false); return; }
            // 기획서 「박쥐가 적 넷을 물고」(PD 2026-10-29 「기획서 기준으로」) — 화면 안에서 가까운 넷만 남긴다(회복도 그 머릿수)
            while (list.Count > VampireBatTargets)
            {
                int far = 0;
                float farD = -1f;
                for (int i = 0; i < list.Count; i++)
                {
                    float d = (list[i].Position - me.Position).sqrMagnitude;
                    if (d > farD) { farD = d; far = i; }
                }
                list.RemoveAt(far);
            }

            float per = VampireFeastPercentPerEnemy * BaseAxis(1f) * ActiveRebaseMul;   // Lv1 2% → Lv4 3% · 쿨 비율(BattleDirector.Rework1021)
            // 빨아 온 피는 **메인**에게 간다(PD 2026-10-29) — 보조가 써도 메인의 최대 체력으로 센다
            var main = _host != null ? _host : me;
            int heal = Mathf.Max(1, Mathf.RoundToInt(main.HpMax * per * list.Count));
            bool iy = InyeonOn(me);   // 인연 「피의 연회」 — 박쥐 두 배 · 중독된 적에게서 두 배
            if (iy) heal = Mathf.Max(1, Mathf.RoundToInt(main.HpMax * per * IyFeastCount(list)));

            // ⚠ **즉발이 아니다.** 머릿수만큼 박쥐를 날려 보내고, 돌아온 뒤에 피가 찬다
            //   (기획 2026-09-15). 즉시 차면 무엇 때문에 찼는지가 안 보인다.
            _batHeal = heal;
            _batSeconds = BatFlySeconds;
            _batBitten = false;
            _batTargets.Clear();
            QxFeastOpen(me, BatFlySeconds);   // 퀄업(2026-10-07) — 발밑 붉은 원, 박쥐 대신 피 구슬
            for (int i = 0; i < list.Count; i++)
            {
                var im = FreeImpact(BatFxSizeQ);
                if (im == null) break;
                im.Play(me.Position, BatFrames, BatFxSizeQ, loop: true);
                _batTargets.Add((im, me.Position, list[i].Position));
            }
            if (iy) IyFeastSecondBats(me, list);
            if (_batTargets.Count == 0) { Leech(heal); _batSeconds = 0f; }
        }

        /// <summary>코만도(기관총) 예전 방벽의 시계 — 액티브가 제압 사격으로 바뀌어(PD 2026-10-29) 지금은 켜는 곳이 없다.</summary>
        private float _barrierSeconds;

        // ── 갱스터 · 일제 마킹 ──────────────────────────────────
        //
        // ⚠ **유저가 «찍히는 것»을 봐야 한다.** 고친 내력이 둘이다.
        //
        //   ① 「일제」라고 한 프레임에 전부 찍었더니 아무 일도 안 일어난 것처럼 보였다 —
        //      눈이 한 군데씩 따라갈 시간이 없어 그냥 «화면이 바뀌었다»로 읽힌다.
        //   ② 하나씩 찍게 했더니 이번엔 **터지는 섬광에 묻혔다.** 시전 섬광이 방을 덮는
        //      0.26초 사이에 조용히 생겨 있어서, 유저는 한참 뒤에야 «저게 뭐지?» 했다.
        //
        // 그래서 지금은 이렇게 한다.
        //   섬광이 빠지기를 기다린다 → 시전자에서 적으로 **조준선이 날아간다** →
        //   닿는 순간 표적이 조여들며 박힌다 → 다음 적으로. 마지막 한 발에 화면이 한 번 멈칫한다.
        //
        // 시선이 «총구 → 날아가는 선 → 꽂히는 표적» 으로 끌려간다. 그게 전부다.

        /// <summary>섬광이 빠질 때까지 기다리는 시간. 이 사이에 찍으면 하얗게 묻힌다.</summary>
        private const float MarkLeadSeconds = 0.16f;

        /// <summary>표적 하나가 찍히는 간격(초). 너무 빠르면 다시 «한꺼번에»가 된다.</summary>
        private const float MarkStagger = 0.11f;

        /// <summary>조준선이 적에게 닿는 데 걸리는 시간. 짧아야 «쏜 것»이지 «떠다니는 것»이 아니다.</summary>
        private const float MarkTravelSeconds = 0.13f;

        /// <summary>마지막 한 발에 화면을 멈칫하게 하는 시간 — 「다 찍었다」를 맺는다.</summary>
        private const float MarkFinishHitStop = 0.06f;

        /// <summary>
        /// 찍을 차례를 기다리는 적들.
        /// <c>DartLeft</c> 는 조준선을 쏠 때까지, <c>MarkLeft</c> 는 표적이 박힐 때까지.
        /// 이미 쏜 것은 <c>DartLeft</c> 를 음수로 두어 두 번 쏘지 않는다.
        /// </summary>
        private readonly List<(Unit Target, float DartLeft, float MarkLeft, float Seconds, int Percent)> _markQueue = new();

        /// <summary>
        /// 갱스터 — **체력 높은 적 셋**에게 4초 표식(받는 피해 +30%). PD 2026-10-29 — 예전엔 방 전체였고
        /// 패시브가 표식 적을 즉사시켜 R 등급에 너무 셌다. 패시브 「청부 계약」(표식 적 처치 → 쿨 −1초)과 한 쌍이다.
        /// </summary>
        private void GangsterMarkAll(Unit me)
        {
            float seconds = GangMarkSeconds * GrowRatio(GangMarkTableBase);   // 4초 × 성급 성장(표 3 → 5 의 비율)
            int percent = GangMarkPercent;
            var targets = ToughestEnemies(GangMarkTargets);

            _markQueue.Clear();
            if (targets.Count == 0) { PlayFx("crit", me.Position, 96f, loop: false); return; }
            if (InyeonOn(me)) IyContractStrike(targets);   // 인연 「청부 표식」 — 보조가 표식 적을 노린다
            // 퀄업 연출(2026-10-07) — 빠르게 훑고 한 번에 박힌다(큰 불덩이 · 섬광 없이)
            if (WpGangOpen(me, targets, seconds, percent)) return;
            for (int i = 0; i < targets.Count; i++) _markQueue.Add((targets[i], 0f, 0f, seconds, percent));
            // 가까운 적부터 — 훑는 방향이 있어야 «시선이 지나갔다»로 읽힌다
            var from = me.Position;
            _markQueue.Sort((a, b) => (a.Target.Position - from).sqrMagnitude
                                      .CompareTo((b.Target.Position - from).sqrMagnitude));
            for (int i = 0; i < _markQueue.Count; i++)
            {
                var q = _markQueue[i];
                float dart = MarkLeadSeconds + i * MarkStagger;
                _markQueue[i] = (q.Target, dart, dart + MarkTravelSeconds, q.Seconds, q.Percent);
            }
        }

        /// <summary>차례가 된 적에게 조준선을 쏘고, 닿으면 표적을 박는다.</summary>
        private void TickMarkQueue(float dt)
        {
            for (int i = _markQueue.Count - 1; i >= 0; i--)
            {
                var q = _markQueue[i];
                float dart = q.DartLeft - dt;
                float mark = q.MarkLeft - dt;
                var e = q.Target;

                // 적이 먼저 죽었으면 조용히 뺀다 — 시체에 조준선을 쏘지 않는다
                if (e == null || !e.IsAlive || e.IsDying) { _markQueue.RemoveAt(i); continue; }

                // ① 조준선이 나간다
                if (q.DartLeft > 0f && dart <= 0f && _host != null)
                {
                    _pfx?.Dart(_host.MuzzlePosition, e.Position, ParticleElement.Fire, MarkTravelSeconds);
                    global::Game.Module.Common.GameSound.HostAttack(_host.Key);   // 표에 있는 소리만 쓴다
                }

                if (mark > 0f) { _markQueue[i] = (e, dart, mark, q.Seconds, q.Percent); continue; }

                // ② 닿았다 — 표적이 박힌다
                _markQueue.RemoveAt(i);
                e.ApplyAmp(q.Percent, q.Seconds);
                e.SetMark(q.Seconds);
                // ⚠ 표적은 **표식이 걸려 있는 내내 붙어 돈다.** 한 번 깜빡이고 사라지면
                //   누가 찍혔는지 알 수 없다(기획 2026-09-15).
                ShowReticleOn(e, ReticleKind.Mark, q.Seconds);
                Shake(2.5f);

                // 마지막 한 발 — 화면이 한 번 멈칫하며 「다 찍었다」를 맺는다
                if (_markQueue.Count == 0) HitStop(MarkFinishHitStop);
            }
        }

        /// <summary>
        /// 코만도(미사일) 한 발 — **곧게 날아간다.** 수류탄처럼 포물선으로 던졌더니
        /// 로켓이 아니라 폭탄을 던지는 것으로 보였다(기획 2026-09-15). 머리가 나는 쪽을 보고,
        /// 벌어진 자리에서 표적으로 모이도록 조금 휘며, 닿거나 수명이 다하면 터진다.
        /// </summary>
        private Projectile FireSkillMissile(Unit me, Vector2 from, Vector2 at, Unit target, int damage, float radius)
        {
            var shot = RentShot();
            if (shot == null) return null;
            shot.SetSprite(ShotFrames("missile"), "missile", LoopsFrames("missile"));
            shot.Fire(from, at, _config.ShotSpeedPlayer, damage, true, target,
                      _config.ShotSize, ShotPlayerColor, _config.ShotLifeSeconds);
            shot.SetBlastRadius(radius);
            if (target != null) shot.SetHoming(MissileFanHoming);
            return shot;
        }

        // ═══════════════════════════════════════════════════════════
        //  관통
        // ═══════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════
        //  시간 흐르기
        // ═══════════════════════════════════════════════════════════

        /// <summary>새 액티브의 지속 시간을 흘린다. `TickHostPassives` 옆에서 돈다.</summary>
        private void TickNewSkills(float dt)
        {
            TickLz(dt);   // 연쇄 방전 — 조준이 걸린 적에게 낙뢰(퀄업 연출 2026-10-07)
            TickWp(dt);   // 무기 7종 퀄업 연출
            TickLightningDance(dt);   // 아마존 번개 무희
            TickSpinFire(dt);         // 폭력배 회전 난사
            TickStickyBomb(dt);       // 코만도(수류탄) C4
            TickFullSwing(dt);        // 슬러거 풀스윙 회오리
            TickUltimates(dt);        // 필살기 18종(BattleDirector.Ultimates)
            TickSkills1029(dt);       // 다시 잡은 스킬(PD 2026-10-29 · BattleDirector.Skills1029)
            TickWraiths(dt);          // 사신 원혼
            TickQx();     // 파워 · 마법 퀄업 연출 — 몸을 따라다니는 부품
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
            TickMarkQueue(dt);
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
                // 박쥐가 적에게 닿는 순간 한 번 문다 — 피가 튀고 조금 아프다(무엇을 빨아 왔는지가 보이게)
                if (!_batBitten && k >= 0.5f)
                {
                    _batBitten = true;
                    for (int i = 0; i < _batTargets.Count; i++)
                    {
                        var to = _batTargets[i].To;
                        var victim = NearestEnemy(to, Meters(1f));
                        if (victim != null && _host != null) HitEnemyWith(victim, SkillDamage(_host, VampireBiteMul), _host.Profile);
                        PlayFx("drain", to, 72f, loop: false);
                    }
                }
                if (_batSeconds <= 0f)
                {
                    for (int i = 0; i < _batTargets.Count; i++) _batTargets[i].Fx?.Stop();
                    _batTargets.Clear();
                    if (_batHeal > 0) { Leech(_batHeal); _batHeal = 0; }
                    if (!QxFeastGlow(home)) PlayFx("drain", home, 96f, loop: false);
                }
            }
            if (_reaperSeconds > 0f) _reaperSeconds -= dt;
            if (_critLockSeconds > 0f) _critLockSeconds -= dt;
            if (_iceShellSeconds > 0f)
            {
                _iceShellSeconds -= dt;
                // 얼음은 몸을 따라다닌다. 끝나면 거둔다 — 안 거두면 방이 바뀌어도 남는다.
                if (_iceFx != null && _host != null) _iceFx.MoveTo(_host.Position);
                if (_iceShellSeconds <= 0f)
                {
                    _iceFx?.Stop();
                    _iceFx = null;
                    if (_host != null) QxIceBreak(_host.Position);
                }
            }

            // 쉴드는 시간이 지나면 걷힌다 — 안 걷으면 다음 방까지 들고 간다.
            if (_barrierSeconds > 0f)
            {
                _barrierSeconds -= dt;
                if (_barrierSeconds <= 0f && _host != null) _host.ClearShield();
            }
        }

        /// <summary>
        /// <paramref name="from"/> 에서 <paramref name="to"/> 로 번개 줄기를 뻗는다.
        /// 어디서 어디로 갔는지가 보여야 「튕겼다」가 읽힌다.
        /// </summary>
        private void PlayBolt(Vector2 from, Vector2 to)
        {
            if (QxSurgeBolt(from, to)) return;   // 청룡 퀄업 — 청백 지그재그 번개
            var frames = FxFrames("boltbeam");
            if (frames == null) { PlayFx("bolt", to, 64f, loop: false); return; }
            var im = FreeImpact(BoltBeamThickness);
            im?.PlayBeam(from, to, frames, BoltBeamThickness);
        }

        /// <summary>줄기 굵기(px). 그림이 192×64 라 세로 64 를 그대로 쓴다.</summary>
        private const float BoltBeamThickness = 64f;

        /// <summary>줄기가 닿은 자리의 번개 터짐 크기. 줄기 굵기보다 커야 「꽂혔다」로 읽힌다.</summary>
        private const float ChainImpactSize = 96f;

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
