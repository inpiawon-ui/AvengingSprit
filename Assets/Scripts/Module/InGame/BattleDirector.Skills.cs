using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 호스트 23명의 액티브 스킬.
    ///
    /// ── 왜 파일을 나눴나 ────────────────────────────────────────
    /// `BattleDirector` 본체는 이미 8천 줄이다. 스킬은 23개가 서로를 안 부르고
    /// 각자 독립이라, 본체에 이어 붙이면 읽을 수 없는 크기가 된다.
    ///
    /// ── 단일 출처 ───────────────────────────────────────────────
    /// 수치의 출처는 `_exchange/out/42_jobs/AVSR_HostSkills.js` 다.
    /// 그중 **레벨에 따라 자라는 두 축만** `ActiveSkillTable._scaling` 에 굽혀 있고,
    /// 거리·반경·각도 같은 **고정 공간값은 여기 상수로** 적는다.
    ///   자라는 값이 표에 있는 이유는 강화 화면이 읽어야 하기 때문이고,
    ///   고정값이 코드에 있는 이유는 방 설계와 묶여 있어 표에서 흔들리면 안 되기 때문이다.
    ///
    /// ── 왜 호스트 키로 가르나 ───────────────────────────────────
    /// 예전에는 정본 스킬키(`psg_h01`) → 동작이름(`gale_dash`) 표를 한 번 거쳤다.
    /// 23명이 각자 다른 스킬을 갖게 된 지금 그 표는 1:1 이라 아무 일도 안 하면서,
    /// **동작 하나를 여러 몸이 나눠 쓰던 시절의 이름**만 남겨 놓았다
    /// (드라군·청룡·샐러맨더가 전부 `dragon_breath` 였다).
    /// 몸 이름으로 바로 가르면 표도 없고 `.js` 와 이름이 같아진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ═══════════════════════════════════════════════════════════
        //  공유 부품
        // ═══════════════════════════════════════════════════════════

        /// <summary>`평타 ×N`. 정본은 스킬 피해를 전부 평타 배수로 적는다.</summary>
        private int SkillDamage(Unit me, float mul)
            => me == null ? 1
             : Mathf.Max(1, Mathf.RoundToInt(me.Atk * _buffs.AttackMul * ReversalMul * mul));

        /// <summary>Lv1 → Lv4 에 자라는 축. Lv5 부터는 끝값에서 멈춘다.</summary>
        private float BaseAxis(float fallback)
        {
            var sc = ActiveScalingOf(_host?.Profile);
            return sc == null || sc.Value.IsEmpty ? fallback : sc.Value.BaseAt(HostMastery);
        }

        /// <summary>Lv5 에 열려 Lv10 까지 자라는 축. **Lv5 미만이면 0.**</summary>
        private float SpecAxis(float fallback)
        {
            if (!SpecOpen) return 0f;
            var sc = ActiveScalingOf(_host?.Profile);
            return sc == null || sc.Value.IsEmpty
                 ? fallback : sc.Value.SpecAt(HostMastery, MasteryMaxOrDefault);
        }

        /// <summary>특수 효과가 열렸는가(숙련도 5 이상).</summary>
        private bool SpecOpen => HostMastery >= SkillScaling.SpecLevel;

        private float Meters(float m) => m * _pxPerMeter;

        // ── 부채꼴 판정 ──────────────────────────────────────────
        //
        // `FireFan` 은 **탄을** 부채꼴로 뿌리는 함수다. 브레스는 탄이 아니라
        // 한 번에 범위 안을 때리는 것이므로 판정이 따로 필요하다.
        //
        // ⚠ 덩치를 본다. 보스는 반경이 108px 라 중심이 부채꼴 밖이어도
        //   몸의 절반은 안에 들어와 있다 — 중심만 재면 보스에게 브레스가 안 닿는다.

        private readonly List<Unit> _coneHits = new();

        private List<Unit> EnemiesInCone(Vector2 origin, Vector2 dir, float degrees, float length)
        {
            _coneHits.Clear();
            if (dir.sqrMagnitude < 0.0001f) return _coneHits;
            dir = dir.normalized;
            float half = degrees * 0.5f;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                var to = e.Position - origin;
                float d = to.magnitude - BodyExcess(e);
                if (d > length) continue;
                // 코앞에 붙은 적은 각도를 잴 수 없다(방향 벡터가 0에 가깝다). 무조건 맞는다.
                if (d <= e.BodyRadius) { _coneHits.Add(e); continue; }
                if (Vector2.Angle(dir, to) > half) continue;
                _coneHits.Add(e);
            }
            return _coneHits;
        }

        // ── 관통 경로 판정 ───────────────────────────────────────
        //
        // 영매의 저주 광선·화이트위저드의 광휘가 쓴다. 아마존 대시도 같은 모양이지만
        // 저쪽은 출발점과 끝점이 정해져 있고 이쪽은 방향과 길이로 준다.

        private readonly List<Unit> _pathHits = new();

        private List<Unit> EnemiesInPath(Vector2 from, Vector2 dir, float length, float width)
        {
            _pathHits.Clear();
            if (dir.sqrMagnitude < 0.0001f) return _pathHits;
            var to = from + dir.normalized * length;
            float half = width * 0.5f;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (DistanceToSegment(e.Position, from, to) - BodyExcess(e) > half) continue;
                _pathHits.Add(e);
            }
            return _pathHits;
        }

        // ── 공격 간격 일시 단축 ──────────────────────────────────
        //
        // 코만도(기관총)·코만도(레이저)·호퍼(기관단총) 셋이 쓴다.
        // **평타 루프에 곱하는 값 하나**로 끝난다 — 따로 쏘는 루프를 만들면
        // 총구 위치·버프·카드 판정이 전부 두 벌이 된다.

        private float _hasteSeconds;
        private float _hasteMul = 1f;

        private float HasteMul => _hasteSeconds > 0f ? _hasteMul : 1f;

        /// <summary>`percent` 50 이면 간격이 절반이 된다.</summary>
        private void GrantHaste(float percent, float seconds)
        {
            _hasteMul = Mathf.Max(0.1f, 1f - percent * 0.01f);
            _hasteSeconds = seconds;
        }

        // ── 일시 관통 ────────────────────────────────────────────
        private float _pierceSeconds;
        private bool IsPierceGranted => _pierceSeconds > 0f;

        // ── 빔 폭 배율 (코만도 레이저) ───────────────────────────
        private float _beamSeconds;
        private float _beamWidth = 1f;
        private float BeamWidthMul => _beamSeconds > 0f ? _beamWidth : 1f;

        // ── 난사 (폭력배) ────────────────────────────────────────
        private float _spraySeconds;
        private int _sprayShots;
        private float _sprayDegrees = 20f;
        private int SprayExtraShots => _spraySeconds > 0f ? _sprayShots : 0;
        private float SprayDegrees => _sprayDegrees;

        // ── 연속 명중 카운터 (호퍼 기관단총 · 탄창 과열) ─────────
        //
        // ⚠ C032 영혼 복제(`_echoHits`)와 **주기가 같아도 카운터는 나눈다.**
        //   하나를 나눠 쓰면 카드를 든 판에서만 패시브가 빨리 차오른다.

        private int _overheatHits;
        private bool _overheatArmed;

        /// <summary>과열이 터질 차례인가. 빗나가면 <see cref="ResetOverheat"/> 가 되돌린다.</summary>
        private bool CountOverheat()
        {
            if (_host == null || _host.Key != "hopper_smg") return false;
            int need = SpecOpen ? 6 : 8;      // Lv5 부터 8타 → 6타
            if (++_overheatHits < need) return false;
            _overheatHits = 0;
            _overheatArmed = true;
            return true;
        }

        /// <summary>탄이 아무도 못 맞히고 사라졌다. 연속이 끊겼으므로 처음부터 센다.</summary>
        private void ResetOverheat() => _overheatHits = 0;

        /// <summary>과열이 터질 때의 배율. Lv5 미만이면 3배, Lv6~10 에 5배까지 자란다.</summary>
        private float OverheatMul
            => _host != null && _host.Key == "hopper_smg" && SpecOpen ? SpecAxis(3f) : 3f;

        // ── 지연 폭발 예약 (화이트 위저드) ───────────────────────
        //
        // 1초 뒤에 경로 전체가 다시 터진다. 코루틴을 쓰지 않는다 —
        // 방을 나가거나 몸을 갈아입으면 예약이 남아 빈 자리에서 터진다.

        private struct DelayedBlast
        {
            public Vector2 From, Dir;
            public float Length, Width, Timer;
            public int Damage;
        }

        private readonly List<DelayedBlast> _delayed = new();

        private void ScheduleBlast(Vector2 from, Vector2 dir, float length, float width,
                                   int damage, float delay)
            => _delayed.Add(new DelayedBlast
            {
                From = from, Dir = dir, Length = length, Width = width,
                Damage = damage, Timer = delay,
            });

        private void TickDelayedBlasts(float dt)
        {
            for (int i = _delayed.Count - 1; i >= 0; i--)
            {
                var d = _delayed[i];
                d.Timer -= dt;
                if (d.Timer > 0f) { _delayed[i] = d; continue; }

                _delayed.RemoveAt(i);
                var hit = EnemiesInPath(d.From, d.Dir, d.Length, d.Width);
                for (int k = 0; k < hit.Count; k++) HitEnemyWith(hit[k], d.Damage, _host?.Profile);
                PlayFx("burst", d.From + d.Dir.normalized * (d.Length * 0.5f), d.Width, loop: false);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  지속형 스킬의 남은 시간
        // ═══════════════════════════════════════════════════════════
        //
        // ⚠ 한 칸짜리 `_skillEffectKey` 로 묶지 않는다. 이것들은 켜져 있는 동안
        //   **다른 코드가 읽는 깃발**이지 스스로 무언가를 하는 것이 아니다.
        //   깃발마다 이름이 있어야 어디서 읽히는지 따라갈 수 있다.

        private float _reflectSeconds;      // 슬러거 — 적 탄 100% 반사
        private float _reflectMul = 2f;
        private bool _reflectPierce;

        private float _wardSeconds;         // 구루 — 받는 피해 감소 · 쉴드 2배
        private float _wardReduce = 0.5f;

        private float _drainSeconds;        // 흡혈귀 — 흡혈 확정 · 회복 배율
        private float _drainMul = 2f;

        private float _cloneSeconds;        // 닌자(표창) — 분신
        private readonly List<Deployable> _clones = new();

        private float _turretHasteSeconds;  // 로봇 — 포탑 생존 중 −15%

        /// <summary>지금 적 탄이 전부 되돌아가는가.</summary>
        public bool IsReflectingAll => _reflectSeconds > 0f;

        /// <summary>구루 결계가 도는 동안의 피해 감소(0~1).</summary>
        private float WardReduce => _wardSeconds > 0f ? _wardReduce : 0f;

        /// <summary>지속형 스킬의 시계를 한꺼번에 흘린다.</summary>
        private void TickSkillEffect(float dt)
        {
            Countdown(ref _hasteSeconds, dt);
            Countdown(ref _pierceSeconds, dt);
            Countdown(ref _beamSeconds, dt);
            Countdown(ref _spraySeconds, dt);
            Countdown(ref _reflectSeconds, dt);
            Countdown(ref _wardSeconds, dt);
            Countdown(ref _drainSeconds, dt);
            Countdown(ref _cloneSeconds, dt);

            TickPoise(dt);
            TickFrostBreath(dt);
            TickOverheatBlast(dt);
            TickWardAura(dt);
            TickClones(dt);
            TickTurretHaste(dt);
            TickDelayedBlasts(dt);
        }

        private static void Countdown(ref float t, float dt)
        {
            if (t > 0f) t = Mathf.Max(0f, t - dt);
        }

        /// <summary>몸을 갈아입거나 방을 나갈 때 걸려 있던 것을 전부 끈다.</summary>
        private void ClearSkillState()
        {
            _hasteSeconds = _pierceSeconds = _beamSeconds = _spraySeconds = 0f;
            _reflectSeconds = _wardSeconds = _drainSeconds = 0f;
            _cloneSeconds = _turretHasteSeconds = 0f;
            // 정조준(호퍼)은 몸에 붙은 표시가 있다 — 안 끄면 갈아탄 다른 몸에 금빛 고리가 따라붙는다.
            _critLockSeconds = 0f;
            // 새 스킬의 지속 상태도 몸을 바꾸면 끊는다 — 설녀 얼음이 남아 갈아탄 몸의 손이 묶였다(2026-09-15).
            _iceShellSeconds = 0f;
            _iceFx?.Stop();
            _iceFx = null;
            _chainHopsLeft = 0;
            for (int i = 0; i < _spits.Count; i++) _spits[i].Fx?.Stop();
            _spits.Clear();
            _hasteMul = 1f;
            _beamWidth = 1f;
            _overheatHits = 0;
            _overheatArmed = false;
            _delayed.Clear();
            DespawnClones();

            // ⚠ **아직 진행 중인 동작**도 같이 끊는다. 깃발만 내리고 이것들을 두면
            //   몸을 갈아입은 뒤에도 앞 몸의 기술이 새 몸에서 이어진다 —
            //     브레스 큐    남은 타수가 **새 몸 앞에서** 냉기를 뿜는다
            //     도약         새 몸이 앞 몸의 착지 지점으로 끌려간다
            //     대시         같은 이유
            //   전이·전염도 마찬가지다. 갱스터로 표식을 걸고 다른 몸으로 갈아타면
            //   그 뒤로 죽는 적마다 계속 옆으로 옮겨 붙는다.
            _frostQueue = 0;
            _slamTime = 0f;
            _dashTime = 0f;
            _markTransfersLeft = 0;
            _curseSpreadMeters = 0f;
            _curseSpreadChains = false;
            ResetPoise();
        }

        // ═══════════════════════════════════════════════════════════
        //  패시브 — 상시로 도는 것
        // ═══════════════════════════════════════════════════════════
        //
        // 23명 중 **여섯은 이미 있던 것**을 그대로 쓴다 —
        // 탄 반사(슬러거) · 흡혈(사신 · 흡혈귀) · 약화(청룡 · 설녀 · 화이트위저드).
        // 여기 있는 것은 **신규 다섯**뿐이고, 그중 둘(탄창 과열 · 예비 회로)은
        // 각자 제 스킬 옆에 있다. 나머지 셋이 이 절이다.
        //
        // 셋 다 **곱하는 값 하나**로 끝난다. 조건만 다르다 —
        //   역전의 자세  내 쉴드가 남아 있는가   (아마존 정예)
        //   탄력         방금 멈췄는가            (호퍼)
        //   작열         맞는 놈이 타는 중인가    (샐러맨더)

        private const float ReversalAtkBonus = 0.15f;   // 쉴드 있는 동안 +15%
        private const float PoiseFirstShotBonus = 0.25f;
        private const float PoiseChargeSeconds = 0.3f;  // 이만큼은 움직여야 충전된다
        private const float ScorchVsBurningBonus = 0.20f;

        /// <summary>
        /// 역전의 자세 (아마존 정예) — 쉴드가 남아 있는 동안 공격력 +15%.
        ///
        /// 쉴드는 **맞아야 쌓인다.** 그래서 이 몸은 "안 맞으면 약한" 유일한 몸이 된다 —
        /// 불굴(상한 해제)과 같은 방향이다.
        /// </summary>
        private float ReversalMul
            => _host != null && _host.Key == "amazon_elite" && _host.Shield > 0
             ? 1f + ReversalAtkBonus : 1f;

        // ── 탄력 (호퍼) ──────────────────────────────────────────
        //
        // 0.3초 이상 움직이다 **멈춘 직후 첫 발**이 +25%.
        // 치고 빠지는 몸이라 "멈춰서 쏜다" 가 이 몸의 리듬인데, 그 리듬에 값을 준다.

        private float _poiseMoved;      // 연속으로 움직인 시간
        private bool _poiseArmed;       // 충전됐고 아직 안 썼다

        private void TickPoise(float dt)
        {
            if (_host == null || _host.Key != "hopper") { ResetPoise(); return; }

            if (MoveInput.sqrMagnitude > 0.0001f)
            {
                _poiseMoved += dt;
                _poiseArmed = false;    // 움직이는 동안은 안 터진다
                return;
            }

            // 멈췄다. 충분히 움직였으면 다음 한 발에 얹는다.
            if (_poiseMoved >= PoiseChargeSeconds) _poiseArmed = true;
            _poiseMoved = 0f;
        }

        private void ResetPoise()
        {
            _poiseMoved = 0f;
            _poiseArmed = false;
        }

        /// <summary>충전된 첫 발을 **써 버린다.** 한 번 쓰면 다시 움직여야 찬다.</summary>
        private float TakePoiseMul()
        {
            if (!_poiseArmed) return 1f;
            _poiseArmed = false;
            return 1f + PoiseFirstShotBonus;
        }

        /// <summary>
        /// 작열 (샐러맨더) — **화상 중인 적**에게 주는 피해 +20%.
        ///
        /// 장판을 먼저 깔고 평타로 마무리하는 순서에 값을 준다.
        /// 순서를 지키면 이득이 되는 것이 이 몸의 리듬이다.
        /// </summary>
        private float ScorchMul(Unit victim)
            => victim != null && victim.BurnStack > 0
            && _host != null && _host.Key == "salamander"
             ? 1f + ScorchVsBurningBonus : 1f;

        /// <summary>
        /// 이번 평타 한 번에 실리는 배수. <see cref="PerformAttack"/> 이 시작할 때 한 번 잡는다.
        ///
        /// ⚠ 탄마다 다시 잡지 않는다. 확산 3발이면 탄력이 **한 발에만** 붙어야 하는데,
        ///   탄마다 부르면 첫 탄이 다 써 버려 나머지 둘이 손해를 본다.
        ///   한 번의 공격에는 한 번의 값이다.
        /// </summary>
        private float _swingMul = 1f;

        private void BeginSwing(bool fromPlayer)
            => _swingMul = fromPlayer ? ReversalMul * TakePoiseMul() : 1f;

        /// <summary>이번 공격의 패시브 배수. 적 공격에는 안 실린다.</summary>
        private float SwingMul(bool fromPlayer) => fromPlayer ? _swingMul : 1f;

        // ═══════════════════════════════════════════════════════════
        //  분기 — 몸 하나에 스킬 하나
        // ═══════════════════════════════════════════════════════════

        private void CastHostSkill(Unit me)
        {
            if (me == null) return;
            switch (me.Key)
            {
                // ── 근거리 6 ──
                //
                // ⚠ 명세 2026-09-14 로 갈아 끼웠다. 그대로 둔 넷(아마존 정예 · 슬러거 ·
                //   코만도(수류탄) · 로봇)만 예전 함수를 그대로 부른다.
                // ⚠ 직업은 **근거리 6 · 중거리 3 · 원거리 14** 셋이다(확정본 2026-09-14).
                //   예전의 「중거리 5 · 관통 4」 묶음은 폐기됐다 — 관통은 직업이 아니라
                //   공격 종류(`AttackKind.Pierce`)다.
                case "amazon":           AmazonLeapStrike(me);  break;
                // 아마존 정예는 **스킬 보류**다(확정본 2026-09-14). 아무 일도 안 한다.
                case "amazon_elite":                            break;
                case "baseball":         ReflectAll(me);        break;
                case "death":            ReaperWindow(me);      break;
                case "guru":             GuardianWard(me);      break;
                case "ninja_chain":      ChainBind(me);         break;

                // ── 중거리 3 (기관총 셋) ──
                case "thug":             SprayFire(me);         break;
                case "hopper_smg":       LeapFar(me);           break;
                case "commando_mg":      CommandoBarrier(me);   break;

                // ── 원거리 14 ──
                case "dragoon":          DragoonFireField(me);  break;
                case "salamander":       SalamanderVenom(me);   break;
                case "dragon_blue":      DragonSurge(me);       break;
                case "commando_grenade": CarpetBomb(me);        break;
                case "snowwoman":        SnowIceShell(me);      break;
                case "ninja":            NinjaCloneSkill(me);   break;
                case "vampire":          VampireFeast(me);      break;
                case "gangster":         GangsterMarkAll(me);   break;
                case "hopper":           HopperCritSurge(me);   break;
                case "commando_missile": MissileFan(me);        break;
                case "medium":           MediumGolem(me);       break;
                case "white_wizard":     WizardFan(me);         break;
                case "commando_laser":   LaserBounce(me);       break;
                case "robot":            DeployTurret(me);      break;

                // 표에 없는 몸(유령 등). 조용히 넘어간다 — 오류가 아니다.
                default: break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  격투 6종
        // ═══════════════════════════════════════════════════════════

        // ── 아마존 정예 · (스킬 미정) ────────────────────────────
        //
        // 확정본 2026-09-14 에서 **보류**다. 이 자리에 있던 「불굴」(무적 2.5초 +
        // 쉴드 상한 해제)은 확정 전에 임의로 잡아 둔 것이라 걷어냈다 —
        // 스킬이 정해지면 그때 새로 쓴다. 지금 이 몸은 액티브가 없다.

        // ── 슬러거 · 전탄 반사 ───────────────────────────────────
        //
        // 패시브가 확률 40% 로 하던 것을 지속 시간 동안 100% 로 만든다.
        // **원거리가 두꺼운 방에서만 강하다** — 근접만 있는 방에서는 아무 일도 안 난다.
        // 그것이 이 스킬의 답이다. 언제 쓸지가 전부인 스킬이 하나는 있어야 한다.

        private void ReflectAll(Unit me)
        {
            _reflectSeconds = BaseAxis(4f);         // Lv1 4 → Lv4 6초
            _reflectMul = SpecOpen ? SpecAxis(2f) : 2f;   // Lv5 2배 → Lv10 4배
            _reflectPierce = SpecOpen;              // Lv5 부터 반사탄이 관통
            PlayFx("reflect", me.Position, 64f, loop: false);
            // 반사 중인 것이 **보여야 한다**(기획 2026-09-15). 방벽 쉴드에 가려 반사 상태인지 몰랐다.
            StartSkillAura(ref _reflectAuraFx, "reflectaura");
        }

        /// <summary>날아온 적 탄을 되받아친다. 반사가 도는 동안 호출된다.</summary>
        private void ReflectShot(Projectile shot)
        {
            if (shot == null) return;
            shot.TurnFriendly(_reflectMul);
            if (_reflectPierce) shot.GrantPierce();
            PlayFx("reflect", shot.Position, 64f, loop: false);
        }

        // ── 사신 · 영혼 수확 ─────────────────────────────────────
        //
        // 보스에게 **현재 체력** 비례다. 드라군이 최대 체력 비례인 것과 짝을 이룬다 —
        // 이쪽은 보스전 앞머리를 잘라내고, 저쪽은 깎여도 같은 양이 들어간다.
        // 그래서 둘을 번갈아 쓰는 것이 한쪽만 쓰는 것보다 낫다.

        private const float HarvestRadiusMeters = 3.0f;
        private const float HarvestBossPercent = 0.08f;

        // ── 구루 · 수호 결계 ─────────────────────────────────────
        //
        // 격투 여섯 중 쉴드를 **가장 느리게** 쌓는 몸이다(공격 간격 0.85초).
        // 그 느린 것을 스킬이 두 배로 메운다 — 느린 것이 약점이 아니라
        // 스킬을 쓸 이유가 되게 만든다.

        private const float WardRadiusMeters = 3.0f;
        private const float WardTickInterval = 0.5f;

        private float _wardTick;

        private void GuardianWard(Unit me)
        {
            _wardReduce = BaseAxis(0.5f);           // Lv1 −50% → Lv4 −70%
            _wardSeconds = SpecOpen ? SpecAxis(3f) : 3f;   // 명세 2026-09-14 — 3초
            _wardTick = 0f;
            // ⚠ 그림을 `ward`(얼음 결정)에서 `guard`(빛으로 짜인 방어막)로 바꾼다 —
            //   얼음은 설녀의 것이라 둘이 구별되지 않았다(기획 2026-09-15).
            //   그리고 **버티는 내내 돈다.** 한 번 터지고 사라지면 결계가 있는지 알 수 없다.
            _wardFx?.Stop();
            _wardFx = TakeLoopFx("guard", me.Position, WardFxSize);
            _wardFx?.SetPulse(SkillPulseMin, 1f, SkillPulseSeconds);
        }

        /// <summary>버티는 내내 몸에 붙어 도는 방어막.</summary>
        private Impact _wardFx;

        /// <summary>
        /// 캐릭터보다 **약간만** 크게. 176 은 방어막이 몸의 세 배 폭이라 캐릭터가 안 보였다
        /// (기획 2026-09-15). 그림이 칸의 91% 를 채우고, 화면의 몸은 약 55×114 px 다.
        /// </summary>
        private const float WardFxSize = 136f;

        /// <summary>결계는 몸을 따라 움직인다. 서 있으라고 만든 스킬이 아니다.</summary>
        private void TickWardAura(float dt)
        {
            // 그림은 Lv 와 무관하게 결계가 있는 동안 계속 돈다.
            if (_wardFx != null)
            {
                if (_wardSeconds > 0f && _host != null) _wardFx.MoveTo(_host.Position);
                else { _wardFx.Stop(); _wardFx = null; }
            }

            if (_wardSeconds <= 0f || !SpecOpen) return;
            var me = _host;
            if (me == null) return;

            _wardTick -= dt;
            if (_wardTick > 0f) return;
            _wardTick = WardTickInterval;

            // Lv5 — 결계 안의 적은 약화가 확정으로 걸린다.
            var list = EnemiesInRange(me.Position, Meters(WardRadiusMeters));
            for (int i = 0; i < list.Count; i++) list[i].ApplySlow(35, 1.5f);
        }

        // ── 닌자(사슬) · 사슬 견인 ───────────────────────────────
        //
        // 격투 전체의 답이다. 나머지 다섯은 **붙는 방법**을 갖지만
        // 이 몸만 적을 데려온다 — 방향이 반대다.
        //
        // ⚠ 보스는 안 끌린다. 대신 피해가 두 배다.
        //   보스를 끌 수 있으면 방 설계(엄폐물·거리)가 통째로 무의미해진다.

        private const int ChainMaxTargets = 4;
        private const float ChainLandMeters = 1.5f;
        private const float ChainDamageMul = 2.0f;

        // ═══════════════════════════════════════════════════════════
        //  중거리 5종
        // ═══════════════════════════════════════════════════════════

        // ── 드라군 · 처형의 숨결 ─────────────────────────────────
        //
        // 사신이 현재 체력 비례라면 이쪽은 **최대 체력** 비례다.
        // 보스가 깎여도 같은 양이 들어간다 — 마무리 담당이다.

        private const float DragoonConeDeg = 60f;
        private const float DragoonConeMeters = 5.0f;
        private const float DragoonBossPercent = 0.08f;

        // ── 샐러맨더 · 용암 지대 ─────────────────────────────────
        //
        // 장판을 먼저 깔고 평타로 마무리하는 것이 이 몸의 리듬이다.
        // 장판은 이미 있는 `SpawnField` 를 그대로 쓴다 — 화상도 장판이 건다.

        private const float LavaThrowMeters = 5.5f;
        private const float LavaRadiusMeters = 2.0f;

        // ── 청룡 · 냉기 브레스 ───────────────────────────────────
        //
        // 청룡은 약화를 **뿌리고** 설녀는 약화를 **소비한다.**
        // 둘을 번갈아 빙의하면 서로의 밑작업이 된다 — 두 몸을 다 가질 이유다.

        private const float FrostConeDeg = 45f;
        private const float FrostConeMeters = 5.5f;
        private const float FrostTickGap = 0.15f;

        private int _frostQueue;
        private float _frostGap;
        private int _frostDamage;
        private float _frostSlowSeconds;

        /// <summary>브레스는 여러 타로 나뉜다. 한 프레임에 다 때리면 3타인 줄 모른다.</summary>
        private void TickFrostBreath(float dt)
        {
            if (_frostQueue <= 0) return;
            var me = _host;
            if (me == null) { _frostQueue = 0; return; }

            _frostGap -= dt;
            if (_frostGap > 0f) return;
            _frostGap = FrostTickGap;
            _frostQueue--;

            var dir = AimDirection(me);
            var hit = EnemiesInCone(me.Position, dir, FrostConeDeg, Meters(FrostConeMeters));
            for (int i = hit.Count - 1; i >= 0; i--)
            {
                var e = hit[i];
                if (e == null || !e.IsAlive) continue;
                HitEnemyWith(e, _frostDamage, me.Profile);
                // 약화 확정 — 청룡의 패시브는 확률이지만 브레스는 반드시 건다.
                if (e.IsAlive) e.ApplySlow(35, _frostSlowSeconds);
            }
            PlayFx("breath_ice", me.Position + dir * Meters(FrostConeMeters * 0.5f),
                   Meters(FrostConeMeters), loop: false);
        }

        // ── 코만도(수류탄) · 융단 폭격 ───────────────────────────
        //
        // ⚠ **한 적 최대 2발**이 이 스킬의 전부다. 없으면 보스에 5발이 다 꽂혀
        //   광역기가 단일기가 된다 — 코만도(미사일)와 구별이 사라진다.
        //
        // 상한은 던질 때 적용한다. 던진 뒤 세면 이미 날아간 탄을 못 되돌린다.

        private const int CarpetShots = 5;
        /// <summary>이 거리 안에 적이 있으면 그 적을 가운데 두고 던진다.</summary>
        private const float CarpetFocusMeters = 9.0f;
        /// <summary>옆으로 한 발씩 벌리는 간격 · 앞뒤로 엇갈리는 거리.</summary>
        private const float CarpetSideStepMeters = 1.3f;
        private const float CarpetStaggerMeters = 0.5f;
        private const float CarpetRangeMeters = 6.0f;
        private const int CarpetMaxPerTarget = 2;

        private void CarpetBomb(Unit me)
        {
            float perShot = BaseAxis(1.2f);         // Lv1 ×1.2 → Lv4 ×1.8
            int shots = SpecOpen ? Mathf.RoundToInt(SpecAxis(5f)) : CarpetShots;  // 5 → 8발
            int dmg = SkillDamage(me, perShot);

            // ⚠ **표적 기준으로 퍼뜨린다**(기획 2026-09-15). 예전에는 바라보는 쪽 6 m 앞에
            //   부채꼴로 떨궈서, 코앞에 적이 있어도 수류탄이 머리 위를 넘어 멀리 떨어졌다.
            //   가까운 적이 있으면 그 적을 가운데 두고, 없으면 앞으로 던진다.
            var focus = NearestEnemy(me.Position, Meters(CarpetFocusMeters));
            var toFocus = focus != null ? focus.Position - me.Position : Vector2.zero;
            var dir = toFocus.sqrMagnitude > 1f ? toFocus.normalized : AimDirection(me);
            float reach = focus != null ? toFocus.magnitude : Meters(CarpetRangeMeters);
            var side = new Vector2(-dir.y, dir.x);

            // 적마다 몇 발이 갈지 미리 센다. 같은 적을 겨눈 세 번째 발은 옆으로 밀어낸다.
            var quota = new Dictionary<Unit, int>();
            for (int i = 0; i < shots; i++)
            {
                // 표적(또는 앞 지점) 둘레에 **옆으로 한 줄**, 앞뒤로 살짝 엇갈려 떨어진다.
                // 각도로 벌리면 가까운 적일수록 다섯 발이 한 점에 겹친다.
                float k = shots == 1 ? 0f : i - (shots - 1) * 0.5f;
                float stagger = (i % 2 == 0 ? 1f : -1f) * Meters(CarpetStaggerMeters);
                var at = ClampedInField(me, me.Position + dir * (reach + stagger)
                                            + side * (k * Meters(CarpetSideStepMeters)));

                var near = NearestEnemy(at, Meters(1.8f));
                if (near != null)
                {
                    quota.TryGetValue(near, out int used);
                    if (used >= CarpetMaxPerTarget) near = null;  // 상한 — 빈 땅에 떨어진다
                    else quota[near] = used + 1;
                }
                if (near != null) at = near.Position;

                ThrowSkillGrenade(me, at, dmg, Meters(1.8f));

                // Lv5 — 착탄마다 작은 화상 장판이 남는다
                if (SpecOpen)
                    SpawnField(at, Meters(1.2f) * _buffs.AoeMul, 3f,
                               FieldEffect.Burn, SkillDamage(me, 0.15f), fromPlayer: true);
            }
        }

        // ── 설녀 · 빙결 파쇄 ─────────────────────────────────────
        //
        // 약화가 걸려 있어야 제값이 나온다. 자기 패시브로도 걸리지만
        // 청룡이 확정으로 깔아 준다 — 두 몸이 한 벌이다.

        private const float ShatterRadiusMeters = 2.5f;
        private const float ShatterRangeMeters = 5.5f;
        private const float ShatterChainMeters = 1.5f;
        private const float FreezeSeconds = 0.5f;

        private void Shatter(Unit me, Vector2 at, float radius, int dmg, int chainsLeft)
        {
            PlayFx("freeze", at, radius * 2f, loop: false);
            var list = new List<Unit>(EnemiesInRange(at, radius));
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;

                e.ApplyFreeze(FreezeSeconds);
                // 약화가 걸려 있으면 파쇄가 두 배다. 이것이 이 몸을 쓰는 방법이다.
                int deal = e.IsSlowed ? dmg * 2 : dmg;
                var where = e.Position;
                HitEnemyWith(e, deal, me.Profile);
                PlayFx("shatter", where, radius, loop: false);

                // 죽은 자리에서 다시 한 번 깨진다.
                if (chainsLeft > 0 && !e.IsAlive)
                    Shatter(me, where, Meters(ShatterChainMeters), dmg, chainsLeft - 1);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  원거리 8종
        // ═══════════════════════════════════════════════════════════

        // ── 폭력배 · 난사 ────────────────────────────────────────
        //
        // 단일 대상에게는 평타와 **같다.** 이득은 오직 여럿을 동시에 맞힐 때다 —
        // 23명 중 "언제 쓸지" 가 전부인 스킬이 하나는 있어야 한다.
        //
        // ⚠ 발당 피해가 평타 그대로여야 한다. 그래서 `_buffs.ExtraShots` 와 같은 자리에
        //   얹는다 — 저기는 `split`(나누기)에 안 들어가는 자리다.

        /// <summary>
        /// 난사 동안 발사 간격을 얼마나 줄이는가(%).
        ///
        /// 3방향만 얹으면 1초에 15발이라 **평타가 조금 굵어진 것**으로 보였다.
        /// 「난사」는 쏟아지는 것이 눈에 보여야 이름값을 한다 — 간격을 4분의 1로 줄여
        /// 초당 60발쯤 나가게 한다. 발당 피해는 그대로이므로 화력이 아니라 **밀도**만 는다.
        /// 따로 쏘는 루프를 만들지 않고 이미 있는 가속 한 곳(`GrantHaste`)을 쓴다.
        /// </summary>
        private const float SprayHastePercent = 75f;

        private void SprayFire(Unit me)
        {
            _spraySeconds = BaseAxis(1f);           // 명세 2026-09-14 — 1초간 3방향
            int total = SpecOpen ? Mathf.RoundToInt(SpecAxis(3f)) : 3;   // Lv5 3 → Lv10 7발
            _sprayShots = Mathf.Max(1, total - 1);  // 원래 1발에 얹는 몫
            _sprayDegrees = SpecOpen ? Mathf.Lerp(20f, 40f, Mathf.InverseLerp(3f, 7f, total)) : 20f;
            if (SpecOpen) _pierceSeconds = _spraySeconds;   // Lv5 — 탄이 관통
            GrantHaste(SprayHastePercent, _spraySeconds);
            PlayFx("muzzle", me.MuzzlePosition, 48f, loop: false);
        }

        // ── 호퍼(기관단총) · 도약 연사 ───────────────────────────
        //
        // 격투가 붙어 오는 **순간**에 쓴다. 거리를 벌면서 딜을 넣는 유일한 기술이다.
        // 아마존 대시와 같은 이동을 쓰되 방향이 반대다.

        private const float LeapFireBackMeters = 3.0f;
        private const float LeapFireAirSeconds = 0.5f;

        // ── 닌자(표창) · 그림자 분신 ─────────────────────────────
        //
        // 순간이동이 적 **뒤로** 간다. 도망 기술이 아니라 들어가서 세 배로 쏟는 기술이다.
        // 분신은 피격 판정이 없다 — `Deployable` 을 유령 모드로 쓴다.

        private const float BlinkBehindMeters = 1.5f;
        private const float CloneSideMeters = 1.5f;

        private void SpawnClone(Vector2 at, float seconds, int damage, float interval)
        {
            SpawnDeployable(at, ghostly: true, seconds: seconds,
                            range: Meters(7f), damage: damage, fireInterval: interval);
            // 방금 세운 것을 기억해 둔다 — 몸을 갈아입으면 같이 사라져야 한다.
            for (int i = _deployables.Count - 1; i >= 0; i--)
                if (_deployables[i].IsActive && !_clones.Contains(_deployables[i]))
                { _clones.Add(_deployables[i]); break; }
        }

        private void TickClones(float dt)
        {
            if (_cloneSeconds > 0f || _clones.Count == 0) return;
            DespawnClones();
        }

        private void DespawnClones()
        {
            for (int i = 0; i < _clones.Count; i++)
                if (_clones[i] != null && _clones[i].IsActive) _clones[i].Despawn();
            _clones.Clear();
        }

        /// <summary>혈갈이 도는 동안의 회복 배율. 꺼져 있으면 1배다.</summary>
        private float DrainMul => _drainSeconds > 0f ? _drainMul : 1f;

        /// <summary>혈갈이 도는 동안은 흡혈이 확률이 아니라 확정이다.</summary>
        private bool IsDrainForced => _drainSeconds > 0f;

        private float _overheatBlastAt;

        /// <summary>Lv5 — 오버히트가 끝날 때 주변이 터진다.</summary>
        private void TickOverheatBlast(float dt)
        {
            if (_overheatBlastAt <= 0f) return;
            _overheatBlastAt -= dt;
            if (_overheatBlastAt > 0f) return;
            _overheatBlastAt = 0f;

            var me = _host;
            if (me == null || me.Key != "commando_mg") return;
            float r = Meters(3f) * _buffs.AoeMul;
            int dmg = SkillDamage(me, 4f);
            var list = EnemiesInRange(me.Position, r);
            for (int i = 0; i < list.Count; i++) HitEnemyWith(list[i], dmg, me.Profile);
            PlayFx("burst", me.Position, r * 2f, loop: false);
        }

        // ── 갱스터 · 표식 사격 ───────────────────────────────────
        //
        // 자기 딜이 아니라 **판 전체의 딜**을 올린다. 빙의를 이어 갈수록 이득이 커진다 —
        // 표식을 걸어 두고 다른 몸으로 갈아타도 표식은 남는다.

        private const float MarkTransferMeters = 5.0f;

        private int _markTransfersLeft;
        private int _markPercent = 30;

        /// <summary>표식이 붙은 적이 죽었다. 남은 시간을 그대로 옆으로 넘긴다.</summary>
        private void TransferMark(Unit dead)
        {
            if (dead == null || _markTransfersLeft <= 0 || !dead.HasAmp) return;
            float left = dead.AmpSecondsLeft;
            if (left <= 0f) return;

            var next = NearestEnemy(dead.Position, Meters(MarkTransferMeters));
            if (next == null) return;

            _markTransfersLeft--;
            next.ApplyAmp(_markPercent, left);
            next.SetMark(left);
            PlayFx("mark", next.Position, 48f, loop: false);
        }

        // ── 호퍼 · 도약 강타 ─────────────────────────────────────
        //
        // 아마존과 정반대 방향의 같은 기술이다. 아마존은 **붙으려고** 대시하고
        // 호퍼는 **빠지려고** 뛴다 — 그래서 넉백이 여기엔 붙는다.

        private const float SlamMaxMeters = 5.0f;
        private const float SlamAirSeconds = 0.6f;
        private const float SlamRadiusMeters = 2.5f;
        private const float SlamKnockMeters = 1.6f;

        private Vector2 _slamFrom, _slamTo;
        private float _slamTime, _slamStun;
        private int _slamDamage;

        /// <summary>도약 중인가. 이 동안은 조작을 받지 않는다.</summary>
        private bool IsSlamming => _slamTime > 0f;

        private void TickSlam(float dt)
        {
            if (_slamTime <= 0f) return;
            var me = _host;
            if (me == null) { _slamTime = 0f; return; }

            _slamTime -= dt;
            if (_slamTime > 0f)
            {
                // 포물선 — 지형을 넘어간다. 그림자만 바닥에 남기고 몸은 위로 뜬다.
                float k = 1f - Mathf.Clamp01(_slamTime / SlamAirSeconds);
                var at = Vector2.Lerp(_slamFrom, _slamTo, k);
                at.y += Meters(1.6f) * 4f * k * (1f - k);
                me.Position = at;
                return;
            }

            _slamTime = 0f;
            me.Position = _slamTo;

            float r = Meters(SlamRadiusMeters) * _buffs.AoeMul;
            var list = new List<Unit>(EnemiesInRange(_slamTo, r));
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;
                HitEnemyWith(e, _slamDamage, me.Profile);
                if (!e.IsAlive || _slamStun <= 0f) continue;

                // Lv5 — 넉백 + 스턴. 밀어내는 것이 이 몸의 뜻이다(아마존과 반대).
                var away = e.Position - _slamTo;
                if (away.sqrMagnitude > 0.0001f)
                    e.Position = ClampedInField(e, e.Position
                                 + away.normalized * Meters(SlamKnockMeters));
                e.ApplyStun(_slamStun);
            }
            PlayFx("slam", _slamTo, r * 2f, loop: false);
        }

        // ── 코만도(미사일) · 다중 유도 ───────────────────────────
        //
        // 적이 하나면 5발이 전부 간다. 다른 광역기가 보스전에서 약해지는 것과 **정반대** —
        // 보스 앞에서 가장 세다. 광역기가 전부 같은 곡선을 그리면 안 된다.

        private const int MissileShots = 5;

        // ═══════════════════════════════════════════════════════════
        //  관통 4종
        // ═══════════════════════════════════════════════════════════

        // ── 영매 · 저주 전파 ─────────────────────────────════════
        //
        // 선을 면으로 바꾸는 가장 직접적인 방식이다. 처음엔 한 줄이지만
        // 죽을 때마다 옆으로 번진다.
        //
        // ⚠ 갱스터 표식과 **같은 층**이다. 곱연산으로 겹치고 상한은 +150%.

        private const float CurseWidthMeters = 1.0f;
        private const float CurseLengthMeters = 7.2f;
        private const float CurseSeconds = 8f;

        private float _curseSpreadMeters;
        private bool _curseSpreadChains;
        private int _cursePercent = 20;

        /// <summary>저주받은 적이 죽었다. 옆으로 옮겨 붙는다.</summary>
        private void SpreadCurse(Unit dead)
        {
            if (dead == null || _curseSpreadMeters <= 0f || !dead.HasAmp) return;
            float left = dead.AmpSecondsLeft;
            if (left <= 0f) return;

            var list = EnemiesInRange(dead.Position, Meters(_curseSpreadMeters));
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null || !list[i].IsAlive) continue;
                list[i].ApplyAmp(_cursePercent, left);
                list[i].SetMark(left);
            }
            // Lv5 미만이면 한 번 번지고 끝이다. 연쇄가 아니면 여기서 꺼 둔다.
            if (!_curseSpreadChains) _curseSpreadMeters = 0f;
        }

        // ── 화이트 위저드 · 광휘 파열 ────────────────────────────
        //
        // 폭이 굵어 줄을 안 서도 여럿이 들어오고, 1초 뒤 폭발이 **도망친 자리까지** 덮는다.
        // 관통기의 "정렬해야 한다" 는 요구를 두 겹으로 없앤다.

        private const float RadiantWidthMeters = 2.0f;
        private const float RadiantLengthMeters = 8.0f;

        // ── 로봇 · 포탑 전개 ─────────────────────────────────────
        //
        // 23명 중 유일하게 방에 **물건을 남긴다.** 세워 두고 자리를 옮겨
        // 두 각도에서 쏘는 것이 이 몸의 전술이다.
        //
        // ⚠ 포탑은 자리를 차지하지 않는다(적도 나도 통과). 안 그러면
        //   좁은 문·회전 관문 방에서 길막이 된다.

        private const float RobotTurretRangeMeters = 8.2f;
        private const float RobotTurretInterval = 1.0f;

        /// <summary>포탑을 몸에서 옆으로 떼어 놓는 거리(m). 겹치면 누가 쏘는지 안 보인다.</summary>
        private const float TurretSideMeters = 2f;

        private void DeployTurret(Unit me)
        {
            float mul = BaseAxis(0.6f);             // Lv1 ×0.6 → Lv4 ×1.0
            float seconds = SpecOpen ? SpecAxis(10f) : 10f;   // 명세 2026-09-14 — 10초
            int dmg = SkillDamage(me, mul);

            // ⚠ **내 자리에 세우지 않는다.** `me.Position` 그대로 두었더니 포탑이 몸과
            //   겹쳐서 누가 쏘는지 안 보였다(기획 2026-09-15). 바라보는 쪽의 **옆**에 세운다.
            var side = new Vector2(-me.Facing.y, me.Facing.x);      // 바라보는 쪽의 직각
            if (side.sqrMagnitude < 0.01f) side = Vector2.right;
            side = side.normalized * Meters(TurretSideMeters);

            SpawnDeployable(ClampedInField(me, me.Position + side), ghostly: false, seconds: seconds,
                            range: Meters(RobotTurretRangeMeters), damage: dmg,
                            fireInterval: RobotTurretInterval);
            // Lv5 — 두 번째는 **반대쪽 옆**. 양옆에 한 대씩 서야 좌우가 다 덮인다.
            if (SpecOpen)
                SpawnDeployable(ClampedInField(me, me.Position - side),
                                ghostly: false, seconds: seconds,
                                range: Meters(RobotTurretRangeMeters), damage: dmg,
                                fireInterval: RobotTurretInterval);

            _turretHasteSeconds = seconds;
            PlayFx("turret", me.Position, 96f, loop: false);
        }

        /// <summary>패시브 · 예비 회로 — 포탑이 살아 있는 동안 자기 간격 −15%.</summary>
        private void TickTurretHaste(float dt)
        {
            if (_turretHasteSeconds <= 0f) return;
            _turretHasteSeconds -= dt;
            // 겹쳐 걸리는 것을 막는다 — 오버히트 같은 큰 단축이 도는 중이면 그것을 남긴다.
            if (_hasteSeconds <= 0f && _host != null && _host.Key == "robot")
                GrantHaste(15f, Mathf.Max(0f, _turretHasteSeconds));
        }

        // ═══════════════════════════════════════════════════════════
        //  작은 도구
        // ═══════════════════════════════════════════════════════════

        /// <summary>스킬이 나갈 방향. 걷는 중이면 걷던 쪽, 서 있으면 가장 가까운 적 쪽.</summary>
        private Vector2 AimDirection(Unit me)
        {
            if (me == null) return Vector2.right;
            var near = NearestEnemy(me.Position);
            if (near != null)
            {
                var d = near.Position - me.Position;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }
            if (MoveInput.sqrMagnitude > 0.0001f) return MoveInput.normalized;
            return me.Facing.sqrMagnitude > 0.0001f ? me.Facing.normalized : Vector2.right;
        }

        /// <summary>장판·파쇄가 떨어질 지점. 사거리 안의 적, 없으면 조준 방향 끝.</summary>
        private Vector2 SkillTargetPoint(Unit me, float range)
        {
            var near = NearestEnemy(me.Position, range);
            if (near != null) return near.Position;
            return ClampedInField(me, me.Position + AimDirection(me) * range);
        }

        /// <summary>스킬이 던지는 폭탄. 평타 수류탄과 달리 피해·반경을 직접 준다.</summary>
        /// <param name="from">
        /// 던지는 자리. 비우면 총구다. 미사일 반원처럼 **여러 발을 벌려 쏠 때**만 넣는다 —
        /// 한 점에서 같은 곳으로 던지면 전부 겹쳐 한 발로 보인다.
        /// </param>
        private void ThrowSkillGrenade(Unit me, Vector2 at, int damage, float radius,
                                       Vector2? from = null)
        {
            var shot = RentShot();
            if (shot == null) return;
            var origin = from ?? me.MuzzlePosition;
            shot.SetSprite(ShotSpriteOf(me), "grenade", LoopsFrames("grenade"));
            shot.Fire(origin, at, _config.ShotSpeedPlayer, damage,
                      true, null, _config.ShotSize, ShotPlayerColor, _config.ShotLifeSeconds);
            shot.SetBlastRadius(radius);
            ThrowAsGrenade(shot, origin, at, 0f, _config.ShotSpeedPlayer);
        }
    }
}
