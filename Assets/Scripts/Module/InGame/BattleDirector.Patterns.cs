using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 잡몹의 **행동 패턴**과 적 호스트의 액티브 스킬.
    ///
    /// ── 왜 필요한가 ─────────────────────────────────────────────
    /// 잡몹은 이미 7종이고 챕터마다 짝이 다르다. 그런데 움직이는 방식은 둘뿐이었다 —
    /// 근접 셋은 전부 매끄럽게 쫓고(빠르기만 다르고), 원거리 셋은 전부 단발 쏘고
    /// 옆으로 갔다(세기만 다르고). **수치가 달라도 하는 짓이 같으면 다른 몹으로 안 읽힌다.**
    ///
    /// 몸을 더 만드는 대신 패턴을 나눈다. 새 생물 1종은 그림 40장이지만
    /// 패턴 1종은 여기 스무 줄이다.
    ///
    /// ── 얹는 것이지 새로 만드는 것이 아니다 ────────────────────
    /// 부채꼴 다발(`_canonShotCount`·`_spreadDegrees`) · 예고 동작(`_canonTelegraph`) ·
    /// 동시 공격 상한(`_canonMaxConcurrent`) 은 **이미 있다.** 새 패턴은 전부 그 위에 선다.
    /// 적 탄이 지형을 통과하는 것(`_blocksEnemyShot = false`)도 이미 그렇다 —
    /// 다만 적 **몸**은 지형에 막힌다(`SlideMove`). 몸까지 통과하면 엄폐물이 아무 일도 안 한다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>
        /// 잡몹이 무엇을 하는가.
        ///
        /// ⚠ 이 자리가 예전에는 `IsMelee(e)` 하나였다. 근접이냐 원거리냐로만 갈리니
        ///   새 행동을 넣을 자리가 없었다 — 넣으려면 그 두 갈래 안에 `if` 를 파야 했다.
        /// </summary>
        private enum EnemyPattern
        {
            /// <summary>계속 쫓는다. 사거리에 들면 예고 후 때린다 — 해골 · 박쥐</summary>
            Chase,
            /// <summary>2발 쏘고 옆으로. 옮기는 동안은 안 쏜다 — 폐품 사수</summary>
            Strafe,
            /// <summary>자리를 지키고 각도로 덮는다 — 순찰기</summary>
            Fan,
            /// <summary>멈췄다 한 번에 3 m 를 건너뛴다 — 집행자</summary>
            Hop,
            /// <summary>점프 → 체공 → 내려오며 3발 — 코일 보행기</summary>
            Vault,
            /// <summary>안 움직이고 조준도 없이 4방향 — 십자 포탑</summary>
            Cross,

            // ── 챕터가 깊어지면 같은 몸이 다르게 싸운다 (2026-09-08) ──
            //
            // 잡몹이 여섯 종뿐이라 새 몸을 만들지 않고 **행동을 갈아 끼운다.**
            // 5챕터의 순찰기는 2챕터의 순찰기와 같은 그림이지만 다른 적이다.

            /// <summary>겨눈 선으로 0.14초 간격 3발 — 순찰기(CH4)</summary>
            Burst,
            /// <summary>땅에 숨어 있다가 다가오면 솟아오른다 — 해골(CH5+)</summary>
            Ambush,
            /// <summary>느린 회오리 한 발이 따라온다 — 순찰기(CH6)</summary>
            Spiral,
        }

        /// <summary>
        /// 이 몸이 쓰는 패턴.
        ///
        /// 잡몹만 자기 패턴을 갖는다. **빼앗을 수 있는 몸(적 호스트)은 예전대로** —
        /// 그쪽은 플레이어가 탔을 때와 같은 방식으로 움직여야 "저 몸을 타면 저렇게 된다"가
        /// 미리 보인다. 잡몹은 탈 수 없으니 그 제약이 없다.
        /// </summary>
        /// <summary>
        /// 같은 몸이 챕터에 따라 다르게 싸우기 시작하는 지점.
        ///
        /// 새 잡몹을 늘리는 대신 **행동을 바꾼다.** 그림이 같으니 플레이어는
        /// "아는 놈" 이라고 생각하고 들어왔다가 한 번 당하고 다시 배운다.
        /// </summary>
        // ⚠ 챕터 숫자는 **그 몹이 실제로 나오는 챕터**여야 한다.
        //   처음엔 3연발을 폐품 사수(CH1 전용)에 붙였는데, 그러면 CH3 부터라는 말이
        //   무색하게 **한 번도 안 나온다.** 순찰기는 CH2·CH4·CH6 에 있어서
        //   부채꼴 → 3연발 → 회오리로 세 번 달라진다 — 같은 그림, 다른 적.
        private const int BurstFromChapter  = 4;   // 순찰기 → 3연발
        private const int AmbushFromChapter = 5;   // 해골   → 매복
        private const int SpiralFromChapter = 6;   // 순찰기 → 회오리 유도탄

        private EnemyPattern PatternOf(Unit e)
        {
            if (e == null) return EnemyPattern.Chase;
            int ch = _runChapter;
            switch (e.Key)
            {
                case TrashEnforcerKey: return EnemyPattern.Hop;
                case TrashCoilKey:     return EnemyPattern.Vault;
                case TrashCrossKey:    return EnemyPattern.Cross;
                case TrashWardenKey:
                    return ch >= SpiralFromChapter ? EnemyPattern.Spiral
                         : ch >= BurstFromChapter  ? EnemyPattern.Burst
                         : EnemyPattern.Fan;
                case TrashSkeletonKey:
                    return ch >= AmbushFromChapter ? EnemyPattern.Ambush : EnemyPattern.Chase;
                default:               return IsMelee(e) ? EnemyPattern.Chase : EnemyPattern.Strafe;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  십자 포탑 — 신규 적 · 새 그림 없음
        // ═══════════════════════════════════════════════════════════
        //
        // 맞히는 게임이 아니라 **피하는** 게임이다. 조준하지 않으므로
        // 안 맞는 자리에 서는 것이 답인데, 축이 45° 씩 돌아 그 자리가 계속 바뀐다.
        // 방에 하나만 있어도 서 있을 곳이 계속 달라진다.
        //
        // 그림은 `obj_turret.png` · `obj_turret_fire.png` 가 이미 프로젝트에 있고
        // 아무 데서도 안 쓰고 있었다 — 집행자 40장과 같은 경우다.

        private const string TrashCrossKey = "obj_turret";
        private static HostEntry s_cross;

        private static HostEntry Cross => s_cross ??= HostEntry.CreateTrash(
            TrashCrossKey, "십자 포탑", AttackKind.Single,
            hp: 30, atk: 8, moveMps: 0f, engageMps: 0f,
            // 사거리 0 은 "무제한" 이다 — 탄이 방 끝까지 간다. `EffectiveRange` 가 아니라
            // 이 패턴이 직접 쏘므로 사거리 판정을 아예 안 거친다.
            rangeMeters: 99f, interval: 2.2f, telegraph: 0.8f);

        private const float CrossInterval = 2.2f;
        private const float CrossTelegraph = 0.8f;
        private const float CrossRotateDeg = 45f;
        private const int CrossDirs = 4;

        /// <summary>
        /// 십자 포탑. 안 움직이고 조준도 안 한다.
        ///
        ///   0  기다림 (간격 − 예고)
        ///   1  예고 0.8초 — 포신이 달아오른다
        ///   2  발사 → 축을 45° 돌리고 0 으로
        /// </summary>
        private void TickCross(Unit e, float dt)
        {
            e.SetMoving(false);
            e.PatternTimer -= dt;
            if (e.PatternTimer > 0f) return;

            switch (e.PatternPhase)
            {
                case 0:
                    e.PatternPhase = 1;
                    e.PatternTimer = CrossTelegraph;
                    e.SetTelegraph(true);
                    e.SetState(EnemyState.Attack);
                    break;

                default:
                    e.SetTelegraph(false);
                    FireCross(e);
                    // 쏠 때마다 축이 돈다 — 십자(＋)와 ×자가 번갈아 나온다.
                    // 360 에서 접는다. 그냥 더하기만 하면 오래 둔 방에서 값이 커져
                    // 삼각함수 정밀도가 떨어진다 — 축이 미묘하게 어긋나 보인다.
                    e.PatternAngle = (e.PatternAngle + CrossRotateDeg) % 360f;
                    e.PatternPhase = 0;
                    e.PatternTimer = Mathf.Max(0.1f, CrossInterval - CrossTelegraph);
                    e.SetState(EnemyState.Cooldown);
                    break;
            }
        }

        private void FireCross(Unit e)
        {
            e.PlayAttack();
            float reach = _roomSize.magnitude;
            float speed = _config.ShotSpeedEnemy;
            float life = reach / Mathf.Max(1f, speed) + 0.25f;
            int dmg = Mathf.Max(1, e.Atk);

            for (int i = 0; i < CrossDirs; i++)
            {
                float deg = e.PatternAngle + 360f / CrossDirs * i;
                var dir = Rotate(Vector2.right, deg);
                var shot = RentShot();
                if (shot == null) return;
                shot.SetSprite(ShotSpriteOf(e), ShotKindOf(e), LoopsFrames(ShotKindOf(e)));
                shot.Fire(e.Position, e.Position + dir * reach, speed, dmg,
                          false, null, _config.ShotSize, ShotEnemyColor, life);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HOP — 쑥쑥 접근 (집행자)
        // ═══════════════════════════════════════════════════════════
        //
        // 무겁고 느린 놈이 갑자기 3 m 를 좁히는 것이 전부다.
        //
        // ⚠ **무적을 주지 마라.** 주면 근접으로 접근을 끊을 방법이 없어져
        //   "붙기 전에 잘라낸다" 라는 선택지가 통째로 사라진다.
        //   (체공 무적은 VAULT 에만 준다 — 그쪽은 이미 멀리 있어서 반대다.)
        //
        // ⚠ 집행자에게만 붙인다. 해골(벽)·박쥐(추격)에 주면 그 몸의 정체가 지워진다.

        private const float HopIdleSeconds = 1.5f;
        private const float HopCrouchSeconds = 0.35f;
        private const float HopMeters = 3.0f;
        private const float HopSeconds = 0.2f;

        /// <summary>
        /// 도약 한 마디를 흘린다. 사거리 안이면 도약하지 않고 <c>false</c> —
        /// 부르는 쪽이 평소대로 때린다.
        ///
        ///   0  멈춤 1.5초    이 사이에 때리거나 피한다
        ///   1  웅크림 0.35초 곧 뛴다는 신호
        ///   2  도약 0.2초    시작할 때 정한 방향으로. 중간에 안 꺾인다
        /// </summary>
        private bool TickHop(Unit e, Unit me, float distance, float dt)
        {
            // 이미 때릴 수 있으면 뛸 이유가 없다. 자세를 풀고 평소 흐름으로 돌아간다.
            if (e.PatternPhase == 0 && distance <= EffectiveRange(e))
            {
                e.PatternTimer = HopIdleSeconds;
                return false;
            }

            e.PatternTimer -= dt;

            switch (e.PatternPhase)
            {
                case 0:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Detect);
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 1;
                    e.PatternTimer = HopCrouchSeconds;
                    e.SetTelegraph(true);
                    return true;

                case 1:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.PatternTimer > 0f) return true;

                    // ⚠ 방향은 **여기서 한 번** 정한다. 매 프레임 다시 잡으면
                    //   유도탄이 되어 "뛰었다" 가 아니라 "따라온다" 가 된다.
                    e.SetTelegraph(false);
                    var dir = me.Position - e.Position;
                    dir = dir.sqrMagnitude < 0.0001f ? e.Facing : dir.normalized;
                    e.SetFacing(dir);
                    e.PatternFrom = e.Position;
                    e.PatternTo = ClampedInField(e, e.Position + dir * Meters(HopMeters));
                    e.PatternPhase = 2;
                    e.PatternTimer = HopSeconds;
                    return true;

                default:
                    e.SetMoving(true);
                    e.SetState(EnemyState.Approach);
                    if (e.PatternTimer > 0f)
                    {
                        float k = 1f - Mathf.Clamp01(e.PatternTimer / HopSeconds);
                        // 지형에 막힌다. `SlideMove` 로 밀면 벽에 걸려 그 앞까지만 간다 —
                        // 통과하면 엄폐물이 아무 일도 안 한다.
                        var want = Vector2.Lerp(e.PatternFrom, e.PatternTo, k);
                        e.Position = SlideMove(e, e.Position, want - e.Position);
                        return true;
                    }
                    e.PatternPhase = 0;
                    e.PatternTimer = HopIdleSeconds;
                    return true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  VAULT — 도약 사격 (코일 보행기)
        // ═══════════════════════════════════════════════════════════
        //
        // ⚠ **체공 중 무적은 여기엔 준다.** HOP 과 반대다 —
        //   HOP 은 다가오는 것을 끊을 수 있어야 하고, VAULT 는 이미 멀리 있으므로
        //   무적이 있어야 "언제 때릴지" 라는 문제가 성립한다.
        //   잡몹이 처음으로 타이밍 문제가 되는 자리다.

        private const float VaultCrouchSeconds = 0.4f;
        private const float VaultRiseSeconds = 0.35f;
        private const float VaultHangSeconds = 0.35f;
        private const float VaultFallSeconds = 0.35f;
        private const float VaultCloserMeters = 2.0f;
        private const float VaultLiftPixels = 96f;
        private const int VaultShots = 3;
        private const float VaultSpreadDeg = 30f;

        /// <summary>
        ///   0  웅크림 0.4초   다리를 접는다
        ///   1  상승 0.35초    위로 뜬다 — 이때부터 무적 · 내 탄이 통과
        ///   2  체공 0.35초    그림자만 바닥에 남는다
        ///   3  낙하 0.35초    내려오며 부채꼴 3발
        /// </summary>
        private bool TickVault(Unit e, Unit me, float dt)
        {
            e.PatternTimer -= dt;
            e.SetMoving(false);

            switch (e.PatternPhase)
            {
                case 0:
                    e.SetState(EnemyState.Attack);
                    e.SetTelegraph(true);
                    if (e.PatternTimer > 0f) return true;

                    e.SetTelegraph(false);
                    // 착지 지점은 여기서 정한다 — 지금 자리에서 플레이어 쪽으로 2 m.
                    // 조금씩 좁혀 오지만 붙지는 않는다.
                    var toward = me.Position - e.Position;
                    toward = toward.sqrMagnitude < 0.0001f ? e.Facing : toward.normalized;
                    e.PatternFrom = e.Position;
                    e.PatternTo = ClampedInField(e, e.Position + toward * Meters(VaultCloserMeters));
                    e.PatternPhase = 1;
                    e.PatternTimer = VaultRiseSeconds;
                    e.SetInvulnerable(true);
                    return true;

                case 1:
                case 2:
                {
                    // 뜬다 · 떠 있다. 자리는 그대로고 그림만 위로 올라간다.
                    float span = e.PatternPhase == 1 ? VaultRiseSeconds : VaultHangSeconds;
                    float k = 1f - Mathf.Clamp01(e.PatternTimer / span);
                    e.SetSpriteLift(VaultLiftPixels * (e.PatternPhase == 1 ? k : 1f));
                    if (e.PatternTimer > 0f) return true;

                    if (e.PatternPhase == 1) { e.PatternPhase = 2; e.PatternTimer = VaultHangSeconds; }
                    else
                    {
                        e.PatternPhase = 3;
                        e.PatternTimer = VaultFallSeconds;
                        // 내려오면서 쏜다. 착지할 때가 아니라 **낙하 시작**에 쏘아야
                        // 탄과 몸이 같이 내려오는 그림이 된다.
                        FireFan(e, me.Position, VaultShots, VaultSpreadDeg,
                                Mathf.Max(1, e.Atk / VaultShots));
                    }
                    return true;
                }

                default:
                {
                    float k = 1f - Mathf.Clamp01(e.PatternTimer / VaultFallSeconds);
                    e.Position = Vector2.Lerp(e.PatternFrom, e.PatternTo, k);
                    e.SetSpriteLift(VaultLiftPixels * (1f - k));
                    if (e.PatternTimer > 0f) return true;

                    // 착지. **이 순간부터 다시 맞는다.**
                    e.SetSpriteLift(0f);
                    e.SetInvulnerable(false);
                    e.Position = e.PatternTo;
                    e.PatternPhase = 0;
                    e.PatternTimer = VaultCrouchSeconds
                                   + Mathf.Max(0f, (e.Profile?.CanonInterval ?? 2.4f)
                                                   - VaultCrouchSeconds - VaultRiseSeconds
                                                   - VaultHangSeconds - VaultFallSeconds);
                    return true;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  적 호스트도 액티브 스킬을 쓴다
        // ═══════════════════════════════════════════════════════════
        //
        // 23명이 각자 다른 스킬을 갖는데, 뺏기 전까지는 그 사실을 알 방법이 없었다.
        // 저 몸이 무엇을 하는지 한 번 보고 나면 "저걸 내가 쓰고 싶다" 가 된다 —
        // 지금은 뺏는 이유가 "더 세 보여서" 하나뿐이다.
        //
        // ⚠ **쿨 ×3 이 이 설계의 전부다.** 플레이어와 같은 쿨로 두면 방에 호스트 둘일 때
        //   스킬이 끊이지 않고 날아온다. 한 방에 한두 번 보는 것이 목표다 —
        //   위협이 아니라 소개다.

        private const float EnemySkillCoolMul = 3f;
        private const float EnemySkillFirstDelay = 8f;
        private const float EnemySkillTelegraph = 1.0f;

        /// <summary>방 안에서 한 번에 하나만 시전한다. 둘이 동시에 쓰면 소개가 아니라 폭격이다.</summary>
        private Unit _enemyCasting;
        private float _enemySkillClock;
        private bool _enemySkillSeen;

        private void ResetEnemySkills()
        {
            _enemyCasting = null;
            _enemySkillClock = 0f;
            _enemySkillSeen = false;
        }

        /// <summary>
        /// 지금 기다려야 하는 시간.
        ///
        /// ⚠ **첫 발동은 쿨이 아니라 8초다.** 처음부터 쿨(×3 = 24~84초)을 기다리게 하면
        ///   방 하나가 그만큼 가지 않아 **한 번도 못 본다** — 소개하려고 만든 것이
        ///   아무도 못 보는 것이 된다. 두 번째부터가 쿨이고, 그 쿨이 곧
        ///   "한 방에 한두 번" 을 만든다.
        /// </summary>
        private float EnemySkillGate(HostEntry e)
            => _enemySkillSeen ? SkillCooldownOf(e) * EnemySkillCoolMul : EnemySkillFirstDelay;

        /// <summary>
        /// 적 호스트의 스킬 차례를 본다. 시전 중이면 <c>true</c> — 부르는 쪽은 평소 흐름을 건너뛴다.
        /// </summary>
        private bool TickEnemySkill(Unit e, Unit me, float dt)
        {
            // 잡몹은 스킬이 없다. 빼앗을 수 있는 몸만 쓴다.
            if (e == null || !e.IsHostBody || e.Profile == null) return false;

            // 시전 중 — 예고를 흘린다.
            if (_enemyCasting == e)
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Attack);
                e.PatternTimer -= dt;
                if (e.PatternTimer > 0f) return true;

                e.SetTelegraph(false);
                _enemyCasting = null;
                _enemySkillClock = 0f;
                _enemySkillSeen = true;
                CastEnemySkill(e, me);
                return true;
            }

            if (_enemyCasting != null) return false;   // 다른 몸이 쓰는 중이다

            _enemySkillClock += dt;
            if (_enemySkillClock < EnemySkillGate(e.Profile)) return false;

            _enemyCasting = e;
            e.PatternTimer = EnemySkillTelegraph;
            e.SetTelegraph(true);
            // 몸 위에 표식을 띄운다 — 평타 예고(0.45~0.75초)보다 길어야 구별된다.
            PlayFx("mark", e.Position + new Vector2(0f, 52f), 48f, loop: false);
            return true;
        }

        /// <summary>
        /// 적이 쓰는 스킬. 플레이어 것과 **같은 몸짓, 낮은 위력**이다.
        ///
        /// ⚠ 플레이어 쪽 구현(`CastHostSkill`)을 그대로 부르지 않는다.
        ///   저것은 전부 `_host`(내 몸)를 기준으로 자란 값·버프·숙련도를 읽는다 —
        ///   적이 부르면 **내 숙련도로** 적이 때리게 된다.
        ///   여기서는 Lv1 고정 · 보스 전용 항 무시로 **줄여서** 낸다.
        /// </summary>
        private void CastEnemySkill(Unit e, Unit me)
        {
            if (e == null || me == null) return;
            e.PlayAttack();

            var dir = me.Position - e.Position;
            dir = dir.sqrMagnitude < 0.0001f ? e.Facing : dir.normalized;
            int dmg = Mathf.Max(1, e.Atk);

            switch (e.Key)
            {
                // 부채꼴로 덮는다 — 브레스 셋과 코만도 수류탄
                case "dragoon":
                case "dragon_blue":
                case "salamander":
                case "commando_grenade":
                    FireFan(e, e.Position + dir * 400f, 5, 50f, Mathf.Max(1, dmg / 2));
                    PlayFx(e.Key == "dragon_blue" ? "breath_ice" : "breath_fire",
                           e.Position + dir * 160f, 200f, loop: false);
                    break;

                // 사방으로 흩뿌린다 — 광역기 무리
                case "amazon_elite":
                case "death":
                case "snowwoman":
                case "white_wizard":
                    FireFan(e, e.Position + dir * 400f, 8, 360f, Mathf.Max(1, dmg / 3));
                    PlayFx("burst", e.Position, 96f, loop: false);
                    break;

                // 한 줄로 길게 — 관통 무리
                case "medium":
                case "commando_laser":
                case "commando_missile":
                    FireFan(e, e.Position + dir * 400f, 3, 8f, Mathf.Max(1, dmg / 2));
                    PlayFx("muzzle", e.MuzzlePosition, 48f, loop: false);
                    break;

                // 붙어 오는 것들 — 한 걸음에 거리를 좁히고 때린다
                case "amazon":
                case "ninja_chain":
                case "hopper":
                case "ninja":
                {
                    var to = ClampedInField(e, e.Position + dir * Meters(3f));
                    e.Position = to;
                    PlayFx("dash", to, 48f, loop: false);
                    if (Vector2.Distance(to, me.Position) <= Meters(2.5f)) DamagePlayer(dmg);
                    break;
                }

                // 나머지는 연사로 낸다. 몸짓이 없어도 "무언가 크게 했다" 는 읽힌다.
                default:
                    FireFan(e, me.Position, 3, 24f, Mathf.Max(1, dmg / 2));
                    PlayFx("muzzle", e.MuzzlePosition, 48f, loop: false);
                    break;
            }
        }

        // ===========================================================
        //  BURST - 겨눈 선으로 3연발 (순찰기 · CH4)
        // ===========================================================
        //
        // 동시에 세 발 뿌리는 확산과 다르다. **같은 선으로 시간차**라
        // 옆으로 한 걸음만 비켜도 뒤 두 발이 빈다 - 대신 안 비키면 세 발을 다 맞는다.
        //
        // 주의: 겨냥은 **첫 발을 쏘기 직전 한 번**만 한다. 매 발마다 다시 겨누면
        //   유도탄이 되어 비키는 것이 답이 아니게 된다.

        private const float BurstIdleSeconds = 1.9f;
        private const float BurstTellSeconds = 0.45f;
        private const float BurstGapSeconds  = 0.14f;
        private const int   BurstShots       = 3;

        private bool TickBurst(Unit e, Unit me, float distance, float dt)
        {
            // 사거리 밖이면 평소 흐름(접근)에 맡긴다.
            if (e.PatternPhase == 0 && distance > EffectiveRange(e))
            {
                e.PatternTimer = BurstIdleSeconds;
                return false;
            }

            e.PatternTimer -= dt;
            switch (e.PatternPhase)
            {
                case 0:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Detect);
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 1;
                    e.PatternTimer = BurstTellSeconds;
                    e.SetTelegraph(true);
                    return true;

                case 1:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    e.SetFacing(me.Position - e.Position);
                    e.PatternTo = me.Position;   // 세 발이 갈 선을 여기서 못 박는다
                    e.PatternAngle = 0f;         // 쏜 발 수를 센다
                    e.PatternPhase = 2;
                    e.PatternTimer = 0f;
                    return true;

                default:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.PatternTimer > 0f) return true;
                    FireAimed(e, e.PatternTo, 1f);
                    e.PatternAngle += 1f;
                    if (e.PatternAngle >= BurstShots)
                    {
                        e.PatternPhase = 0;
                        e.PatternTimer = BurstIdleSeconds;
                        return true;
                    }
                    e.PatternTimer = BurstGapSeconds;
                    return true;
            }
        }

        /// <summary>한 발을 겨눈 자리로 쏜다. 패턴들이 같은 자를 쓰게 여기 모아 둔다.</summary>
        private void FireAimed(Unit e, Vector2 at, float speedMul,
                               string kindOverride = null, float homing = 0f)
        {
            e.PlayAttack();
            var dir = at - e.Position;
            if (dir.sqrMagnitude < 0.0001f) dir = e.Facing;
            dir = dir.normalized;

            float reach = _roomSize.magnitude;
            float speed = _config.ShotSpeedEnemy * Mathf.Max(0.1f, speedMul);
            float life = reach / Mathf.Max(1f, speed) + 0.25f;

            var shot = RentShot();
            if (shot == null) return;
            string kind = kindOverride ?? ShotKindOf(e);
            shot.SetSprite(kindOverride != null ? ShotFrames(kindOverride) : ShotSpriteOf(e),
                           kind, LoopsFrames(kind));
            shot.Fire(e.Position, e.Position + dir * reach, speed, Mathf.Max(1, e.Atk),
                      false, null, _config.ShotSize, ShotEnemyColor, life);
            if (homing > 0f) shot.SetHoming(homing);
        }

        // ===========================================================
        //  AMBUSH - 땅에 숨었다가 솟아오른다 (해골 · CH5+)
        // ===========================================================
        //
        // 방에 들어설 때 이 놈은 화면에 없다. 다가가면 바닥이 갈라지고(예고)
        // 그 다음에 나온다. 방을 한눈에 읽고 들어가는 습관을 한 번 깨는 자리다.
        //
        // 주의: 숨어 있는 동안 `IsHidden` 이 켜져 있으면 `Targetable` 이 걸러 내므로
        //   **자동 조준이 안 보이는 것을 쏘지 않는다.** 그게 이 패턴의 전제다.
        //
        // 주의: 예고(0.55초)를 반드시 준다. 없으면 그냥 갑자기 맞는 것이고,
        //   그건 어렵기만 하고 배울 것이 없다.

        private const float AmbushTriggerMeters = 3.2f;
        private const float AmbushTellSeconds   = 0.55f;

        private int AliveEnemyCount()
        {
            int n = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var u = _enemies[i];
                if (u != null && u.IsAlive && !u.IsDying) n++;
            }
            return n;
        }

        private bool TickAmbush(Unit e, Unit me, float distance, float dt)
        {
            switch (e.PatternPhase)
            {
                case 0:
                    e.SetHidden(true);
                    e.SetMoving(false);
                    e.IsAggro = false;
                    // 거리로 깨운다. 다만 **마지막 하나 남았으면 거리와 무관하게** 나온다 —
                    // 안 그러면 구석에 숨은 놈 하나 때문에 방이 영영 안 열린다.
                    if (distance > Meters(AmbushTriggerMeters) && AliveEnemyCount() > 1) return true;
                    e.PatternPhase = 1;
                    e.PatternTimer = AmbushTellSeconds;
                    PlayFx("burrow", e.Position, 96f, loop: false);   // 바닥이 갈라진다
                    return true;

                case 1:
                    e.PatternTimer -= dt;
                    e.SetMoving(false);
                    if (e.PatternTimer > 0f) return true;
                    e.SetHidden(false);
                    e.IsAggro = true;
                    e.SetState(EnemyState.Attack);
                    PlayFx("bulge", e.Position, 128f, loop: false);
                    e.PatternPhase = 2;
                    return true;

                default:
                    return false;   // 한 번 나왔으면 평소대로 쫓는다
            }
        }

        // ===========================================================
        //  SPIRAL - 느린 회오리 한 발이 따라온다 (순찰기 · CH6)
        // ===========================================================
        //
        // 부채꼴(FAN)의 반대다. 부채꼴은 **빠른 세 발을 넓게** 뿌려 서 있을 자리를 묻고,
        // 회오리는 **느린 한 발이 따라와** 계속 움직이게 만든다.
        // 엄폐물 뒤로 숨는 것이 처음으로 답이 되는 자리이기도 하다 -
        // 탄이 느려서 지형을 낀 회전으로 떼어낼 수 있다.
        //
        // 주의: 유도를 세게 주면 절대 안 떨어지는 탄이 되어 회피가 사라진다.
        //   0.55 는 "달리면 떨어지고 서 있으면 맞는" 세기다.

        private const float SpiralIdleSeconds = 2.4f;
        private const float SpiralTellSeconds = 0.7f;
        private const float SpiralSpeedMul    = 0.55f;
        private const float SpiralHoming      = 0.55f;

        private bool TickSpiral(Unit e, Unit me, float distance, float dt)
        {
            if (e.PatternPhase == 0 && distance > EffectiveRange(e))
            {
                e.PatternTimer = SpiralIdleSeconds;
                return false;
            }

            e.PatternTimer -= dt;
            switch (e.PatternPhase)
            {
                case 0:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Detect);
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 1;
                    e.PatternTimer = SpiralTellSeconds;
                    e.SetTelegraph(true);
                    return true;

                default:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    e.SetFacing(me.Position - e.Position);
                    FireAimed(e, me.Position, SpiralSpeedMul, "spiral", SpiralHoming);
                    e.PatternPhase = 0;
                    e.PatternTimer = SpiralIdleSeconds;
                    return true;
            }
        }
    }
}
