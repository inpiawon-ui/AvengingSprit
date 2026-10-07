using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 잡몹 패턴 2차 (2026-09-28 — 「몬스터 패턴도 투사체도 다양하게」).
    ///
    /// 새 몸은 만들지 않는다(그림 40장). **있는 몸이 챕터마다 다르게 싸운다** —
    /// `BattleDirector.Patterns` 가 세운 그 원칙을 나머지 몸에도 편다.
    ///
    ///   박쥐      CH1 추격 → CH2 급강하 돌진(예고 띠) → CH4 지그재그로 다가와 돌진
    ///   집행자    도약 → CH4 내려찍기(착지에 충격파 원) → CH6 죽으면 박쥐 둘로 갈라진다
    ///   해골      추격 → CH3 죽을 때 4방향 탄 → CH5 매복 → CH6 죽을 때 8방향
    ///   코일      도약 사격 → CH5 벽에 튕기는 탄 → CH6 날아가다 여섯으로 갈라지는 큰 탄
    ///   십자 포탑 4방향 회전 → CH5 4방향·8방향 번갈아 → CH6 돌며 연사
    ///
    /// 투사체 움직임이 넷(직선·부채꼴·3연발·유도)에서 여덟으로 는다 —
    /// 튕김 · 갈라짐 · 죽음 탄 · 회전 연사.
    ///
    /// ⚠ 빼앗을 수 있는 몸(호스트)은 건드리지 않는다. 그쪽은 「타면 저렇게 된다」가
    ///   미리 보여야 해서 플레이어가 탔을 때와 같은 방식으로 움직인다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>
        /// 패턴을 고를 때 보는 챕터 — **지금 선 방의 챕터**다.
        ///
        /// ⚠ `_runChapter`(판의 진행 챕터)를 보면 안 된다. 그 값은 시작 챕터에서 출발해
        ///   올라가기만 하므로, 뒤 챕터에서 시작한 판이 앞 챕터 방에 서면 1챕터 박쥐가
        ///   급강하를 한다(2026-09-28 검수 스샷에서 실제로 그랬다). 방이 제 챕터를 안다.
        /// </summary>
        /// ⚠ 챕터 번호가 아니라 **패턴 단계**다(챕터 표의 `pattern`). 아래 `…FromChapter` 문턱은
        ///   1~6 챕터에서 배우는 순서이고, 7챕터부터는 전부 6단계로 싸운다.
        private int PatternChapter => PatternStageOf(_canonRoom != null ? _canonRoom.Chapter : _runChapter);

        // 같은 몸이 달라지는 챕터. 숫자는 **그 몹이 실제로 나오는 챕터**여야 한다(`TrashKeysFor`).
        // 1 챕터부터 — 「띠를 보면 옆으로」를 처음 방에서 배운다(기획 2026-10-07 「근접은 그냥 가서 때리는 수준」)
        private const int DiveFromChapter       = 1;   // 박쥐 → 급강하
        private const int ChargeFromChapter     = 7;   // 돌 고릴라 → 돌진이 먼저(도약과 번갈아)
        private const int GorillaMixFromStage   = 5;   // 돌 고릴라 → 도약 · 돌진을 번갈아

        /// <summary>돌 고릴라가 지금 「둘째 수」를 할 차례인가. 한 수가 끝날 때마다 뒤집는다.</summary>
        private readonly System.Collections.Generic.HashSet<Unit> _gorillaCharge = new();

        private void ToggleGorillaMove(Unit e)
        {
            if (PatternChapter < GorillaMixFromStage) return;
            if (!_gorillaCharge.Remove(e)) _gorillaCharge.Add(e);
        }
        private const int WeaveFromChapter      = 4;   // 박쥐 → 지그재그 (CH4)
        private const int SlamFromChapter       = 4;   // 집행자 → 내려찍기
        private const int SplitFromChapter      = 6;   // 집행자 → 죽으면 갈라진다
        private const int DeathBurstFromChapter = 3;   // 해골 → 죽을 때 4방향
        private const int DeathBurst8FromChapter = 6;  // 해골 → 죽을 때 8방향
        private const int BounceShotFromChapter = 5;   // 코일 → 튕기는 탄
        private const int SplitShotFromChapter  = 6;   // 코일 → 갈라지는 탄
        private const int CrossAltFromChapter   = 5;   // 십자 포탑 → 4·8 번갈아
        private const int CrossStreamFromChapter = 6;  // 십자 포탑 → 회전 연사

        // ═══════════════════════════════════════════════════════════
        //  DIVE — 급강하 돌진 (박쥐)
        // ═══════════════════════════════════════════════════════════
        //
        // 쫓아와 무는 놈이 **멈춰서 겨누고 한 줄로 꽂힌다.** 예고 띠가 0.55초 깔리고
        // 그 띠 위를 지나간다 — 옆으로 한 걸음이면 피하고, 피하면 박쥐가 내 뒤에 가 있다.
        //
        // ⚠ 방향은 예고를 시작할 때 **한 번** 정한다. 띠가 나를 따라오면 피할 수가 없다.

        private const float DiveTriggerMeters = 4.5f;
        private const float DiveMinMeters = 1.6f;
        private const float DiveTellSeconds = 0.55f;
        private const float DiveDashSeconds = 0.22f;
        private const float DiveMeters = 5.0f;
        private const float DiveWidthMeters = 0.9f;
        private const float DiveCooldown = 1.6f;
        /// <summary>
        /// 돌진이 끝나고 **멈춰 서는** 시간 — 피한 사람이 때릴 차례(예고 → 실행 → 빈틈).
        /// 예전에는 끝나자마자 다시 쫓아와서 피해도 때릴 틈이 없었다.
        /// </summary>
        private const float DiveRecoverSeconds = 0.7f;
        private const float WeaveHz = 1.4f;
        private const float WeaveRatio = 0.9f;   // 옆으로 흔드는 세기 — 앞으로 가는 속도 대비

        /// <summary>
        ///   0  다가온다 (CH4+ 는 지그재그로). 거리가 맞고 사선이 트이면 1 로
        ///   1  예고 0.55초 — 멈춰서 겨눈다. 바닥에 띠
        ///   2  돌진 0.22초 — 띠 위를 지나간다
        ///   3  숨 고르기 0.7초 — 그 자리에 선다(빈틈)
        /// </summary>
        private bool TickDive(Unit e, Unit me, float distance, float dt)
        {
            switch (e.PatternPhase)
            {
                case 0:
                {
                    var spec = DiveSpecOf(e);   // 박쥐 · 고릴라 · 멧돼지가 같은 세 박자를 다른 크기로 (BattleDirector.Boar)
                    e.PatternTimer -= dt;
                    bool chain = BoarChaining(e);   // 연속 돌진의 두 번째 — 붙어 있어도 건다 (BattleDirector.Boar)
                    if (chain && e.PatternTimer < -BoarChainWindowSeconds) { _boarChain.Remove(e); chain = false; }
                    bool ready = e.PatternTimer <= 0f
                              && distance <= Meters(spec.TriggerMeters)
                              && distance >= Meters(chain ? BoarChainMinMeters : spec.MinMeters)
                              && EnemyLineClear(e.Position, me.Position);
                    if (ready)
                    {
                        var dir = me.Position - e.Position;
                        dir = dir.sqrMagnitude < 0.0001f ? e.Facing : dir.normalized;
                        e.SetFacing(dir);
                        e.PatternFrom = e.Position;
                        e.PatternTo = ClampedInField(e, e.Position + dir * Meters(spec.Meters));
                        if (e.Key == TrashBoarKey) e.PatternTo = ClampToView(e.PatternTo, BoarViewMarginPx);   // 화면 밖으로 달려 나가지 않는다
                        float tell = chain ? spec.TellSeconds * BoarChainTellRatio : spec.TellSeconds;
                        e.PatternPhase = 1;
                        e.PatternTimer = tell;
                        e.SetTelegraph(true);
                        StartWarn(BandShape(e.Position, dir, Meters(spec.WidthMeters),
                                            Vector2.Distance(e.PatternFrom, e.PatternTo)),
                                  tell, ChargeContact(e) ? 0 : Mathf.Max(1, e.Atk), owner: e);   // 멧돼지는 띠가 안 친다 — 닿아야 친다
                        StartRushFx(e, e.PatternFrom, e.PatternTo, Meters(spec.WidthMeters), tell, dust: true);
                        return true;
                    }
                    // 멧돼지는 물지 않는다 — 돌진 사이엔 다가오거나 숨을 고른다
                    if (HoldBetweenCharges(e, me, distance, dt)) return true;
                    // 아직 멀거나 쉬는 중 — 평소처럼 쫓는다. CH4 부터는 좌우로 흔들며 온다.
                    if (PatternChapter < WeaveFromChapter || distance <= EffectiveRange(e)) return false;
                    e.PatternAngle += dt * WeaveHz * Mathf.PI * 2f;
                    var to = me.Position - e.Position;
                    var fwd = to.sqrMagnitude < 0.0001f ? e.Facing : to.normalized;
                    var side = new Vector2(-fwd.y, fwd.x) * Mathf.Sin(e.PatternAngle) * WeaveRatio;
                    float step = e.StepToward(me.Position, dt).magnitude;
                    e.SetState(EnemyState.Approach);
                    e.Position = SlideMove(e, e.Position, (fwd + side).normalized * step);
                    e.SetMoving(true);
                    return true;
                }

                case 1:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    e.PatternTimer -= dt;
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    e.PlayAttack();
                    e.PatternPhase = 2;
                    e.PatternTimer = DiveSpecOf(e).DashSeconds;
                    if (ChargeContact(e)) e.PatternAngle = 0f;   // 이번 돌진은 아직 안 쳤다
                    return true;

                case 2:
                {
                    e.SetMoving(true);
                    e.SetState(EnemyState.Approach);
                    var spec = DiveSpecOf(e);
                    e.PatternTimer -= dt;
                    float k = 1f - Mathf.Clamp01(e.PatternTimer / spec.DashSeconds);
                    var want = Vector2.Lerp(e.PatternFrom, e.PatternTo, k);
                    e.Position = SlideMove(e, e.Position, want - e.Position);   // 지형에는 막힌다
                    if (ChargeContact(e)) TickChargeContact(e, me, spec);       // 달리는 몸에 닿아야 아프다
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 3;
                    // 끝까지 못 갔다 = 벽 · 물건에 박았다 → 휘청(더 긴 빈틈). 멧돼지에게만 차이가 난다
                    bool crashed = Vector2.Distance(e.Position, e.PatternTo) > Meters(0.5f);
                    e.PatternTimer = AfterBoarDash(e, crashed, spec);   // 7단계부터 멧돼지는 한 번 더 이어 달린다
                    if (crashed && e.Key == TrashBoarKey) e.PlayHit();
                    return true;
                }

                default:
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                    e.PatternTimer -= dt;
                    if (e.PatternTimer > 0f) return true;
                    e.PatternPhase = 0;
                    // 멧돼지는 곧바로 다음 돌진을 노린다 — 이어 달리기면 기다림 없이
                    e.PatternTimer = BoarChaining(e) ? 0f : DiveSpecOf(e).CooldownSeconds;
                    if (e.Key == TrashEnforcerKey) ToggleGorillaMove(e);   // 다음은 도약
                    return true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SLAM — 내려찍기 (집행자 · CH4+)
        // ═══════════════════════════════════════════════════════════
        //
        // 도약이 끝나는 자리에 충격파가 난다. 도약만으로는 「붙었다」가 전부였는데,
        // 이제 **착지 자리에서 한 걸음 더 물러나야** 한다.

        // 1.7 m 였다가 1.4 m 로 줄였다 — 집행자 둘이 나란히 찍으면 원 둘이 방 폭의 2/3 를 덮었다(검수 스샷)
        private const float HopSlamRadiusMeters = 1.4f;
        private const float HopSlamTellSeconds = 0.5f;

        /// <summary>집행자가 도약을 마쳤다. `TickHop` 이 착지할 때 부른다.</summary>
        private void OnHopLanded(Unit e)
        {
            if (PatternChapter < SlamFromChapter) return;
            float r = Meters(HopSlamRadiusMeters);
            StartWarn(DiscShape(e.Position, r), HopSlamTellSeconds, Mathf.Max(1, e.Atk), owner: e,
                      fxName: "slam", fxSize: r * 2f);
        }

        // ═══════════════════════════════════════════════════════════
        //  죽을 때 — 죽음 탄 (해골) · 갈라짐 (집행자)
        // ═══════════════════════════════════════════════════════════
        //
        // 잡는 순간이 끝이 아니게 된다. 붙어서 잡으면 탄을 코앞에서 맞으므로
        // **어디서 잡느냐**가 선택이 된다 — 궁수의 전설의 「터지는 놈」이다.

        private const float DeathShotSpeedMul = 0.7f;   // 느려야 보고 피한다
        private const string DeathShotKind = "magic";
        private const int SplitChildren = 2;
        private const float SplitSpreadMeters = 0.7f;
        private const float SplitChildHpRatio = 0.6f;

        /// <summary>잡몹이 죽었다. `KillEnemy` 가 목록에서 빼기 전에 부른다.</summary>
        private void OnTrashDeath(Unit u)
        {
            if (u == null || u.IsBoss || u.IsHostBody) return;
            CancelWarnsOf(u);

            if (u.Key == TrashSkeletonKey && PatternChapter >= DeathBurstFromChapter)
            {
                int n = PatternChapter >= DeathBurst8FromChapter ? 8 : 4;
                // 4발은 ×자로 — 상하좌우(십자 포탑)와 겹치지 않게
                FireRadial(u.Position, n, n == 4 ? 45f : 0f, DeathShotKind,
                           Mathf.Max(1, u.Atk / 2), DeathShotSpeedMul);
            }
            else if (u.Key == TrashEnforcerKey && PatternChapter >= SplitFromChapter)
            {
                for (int i = 0; i < SplitChildren; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    SpawnTrashAt(Bat, u.Position + new Vector2(side * Meters(SplitSpreadMeters), 0f),
                                 SplitChildHpRatio);
                }
            }
        }

        /// <summary>한 점에서 사방으로 고르게 쏜다. 겨누지 않는다 — 자리로 피한다.</summary>
        private void FireRadial(Vector2 from, int count, float startDeg, string kind, int damage,
                                float speedMul)
        {
            float reach = _roomSize.magnitude;
            float speed = _config.ShotSpeedEnemy * Mathf.Max(0.1f, speedMul);
            float life = reach / Mathf.Max(1f, speed) + 0.25f;
            var frames = ShotFrames(kind);
            bool loop = LoopsFrames(kind);
            for (int i = 0; i < count; i++)
            {
                var dir = Rotate(Vector2.right, startDeg + 360f / count * i);
                var shot = RentShot();
                if (shot == null) return;
                shot.SetSprite(frames, kind, loop);
                shot.Fire(from, from + dir * reach, speed, damage,
                          false, null, _config.ShotSize, ShotEnemyColor, life);
            }
        }

        /// <summary>
        /// 잡몹 하나를 그 자리에 세운다(갈라진 박쥐). 방 상한을 넘으면 안 세운다.
        /// 불러낸 것은 처음부터 깨어 있다 — 기다렸다 덤비면 갈라진 줄도 모른다.
        /// </summary>
        private void SpawnTrashAt(HostEntry e, Vector2 at, float hpRatio)
        {
            if (e == null || _enemies.Count > MaxRoomUnits) return;
            var u = NewUnit($"Split_{e.HostKey}_{_enemies.Count}");
            u.Setup(UnitSide.Enemy, e.HostKey, e.DisplayName, TrashSprite(e),
                    Mathf.Max(1, Mathf.RoundToInt(EnemyHpOf(e) * hpRatio)),
                    Mathf.RoundToInt(EnemyAtkOf(e) * NormalEnemyAtkMul),
                    EnemySpeedOf(e), EnemyRangeOf(e),
                    EnemyIntervalOf(e) / EnemyHandSpeedMul,
                    UnitBox(84f, 78f), isBoss: false, profile: e);
            u.Position = at;
            ClampToField(u);
            ResolveObstacles(u);
            u.PossessPriority = e.PossessPriority;
            u.SetState(EnemyState.Detect);
            u.ResetPattern();
            u.IsAggro = true;
            ApplyFacingSprites(u, e.SpriteKey);
            _enemies.Add(u);
            _pfx?.Puff(at, ParticleElement.Dust, 0.5f);
        }

        // ═══════════════════════════════════════════════════════════
        //  코일 보행기의 탄 — 튕김 · 갈라짐
        // ═══════════════════════════════════════════════════════════

        private const int BounceShotBounces = 2;
        private const float SplitOrbSpeedMul = 0.6f;
        private const float SplitOrbSeconds = 0.75f;
        private const int SplitOrbPieces = 6;
        private const string SplitOrbKind = "darkorb";
        private const string SplitPieceKind = "magic";

        /// <summary>
        /// 도약 사격의 한 번. 챕터에 따라 탄이 달라진다.
        ///   CH3~4  부채꼴 3발
        ///   CH5    부채꼴 3발 — **벽과 기둥에 두 번 튕긴다.** 기둥 뒤가 안전하지 않게 된다
        ///   CH6    느린 큰 탄 한 발 — 0.75초 뒤 여섯으로 갈라진다. 멀리서 터지게 거리를 둔다
        /// </summary>
        private void FireVaultVolley(Unit e, Unit me)
        {
            int dmg = Mathf.Max(1, e.Atk / VaultShots);
            if (PatternChapter >= SplitShotFromChapter)
            {
                e.PlayAttack();
                var dir = me.Position - e.Position;
                dir = dir.sqrMagnitude < 0.0001f ? e.Facing : dir.normalized;
                float reach = _roomSize.magnitude;
                float speed = _config.ShotSpeedEnemy * SplitOrbSpeedMul;
                var shot = RentShot();
                if (shot == null) return;
                shot.SetSprite(ShotFrames(SplitOrbKind), SplitOrbKind, LoopsFrames(SplitOrbKind));
                shot.Fire(e.Position, e.Position + dir * reach, speed, Mathf.Max(1, e.Atk),
                          false, null, _config.ShotSize * 1.3f, ShotEnemyColor,
                          reach / Mathf.Max(1f, speed) + 0.25f);
                shot.SetSplit(SplitOrbSeconds, SplitOrbPieces);
                return;
            }

            if (PatternChapter >= BounceShotFromChapter)
            {
                e.PlayAttack();
                float reach = _roomSize.magnitude;
                float speed = _config.ShotSpeedEnemy;
                // 튕기는 탄은 오래 산다 — 벽 두 번을 돌 시간이 있어야 한다
                float life = reach / Mathf.Max(1f, speed) * 1.6f;
                for (int i = 0; i < VaultShots; i++)
                {
                    float off = -VaultSpreadDeg * 0.5f + VaultSpreadDeg * i / (VaultShots - 1);
                    var shot = RentShot();
                    if (shot == null) return;
                    shot.SetSprite(ShotSpriteOf(e), ShotKindOf(e), LoopsFrames(ShotKindOf(e)));
                    shot.Fire(e.Position, me.Position, speed, dmg, false, null,
                              _config.ShotSize * 1.15f, ShotEnemyColor, life,
                              angleOffsetDeg: off, bounces: BounceShotBounces);
                }
                return;
            }

            FireFan(e, me.Position, VaultShots, VaultSpreadDeg, dmg);
        }

        /// <summary>갈라질 때가 된 탄을 여섯으로 흩는다. `TickShots` 가 부른다.</summary>
        private void SplitShot(Projectile p)
        {
            int n = Mathf.Max(2, p.SplitCount);
            FireRadial(p.Position, n, 0f, SplitPieceKind, Mathf.Max(1, p.Damage / 2), 0.85f);
            SpawnImpact(p.Position, SplitPieceKind);
        }

        // ═══════════════════════════════════════════════════════════
        //  십자 포탑 — 8방향 번갈아 · 회전 연사
        // ═══════════════════════════════════════════════════════════

        private const float StreamTellSeconds = 0.7f;
        private const float StreamGapSeconds = 0.16f;
        private const float StreamStepDeg = 22.5f;
        private const int StreamShots = 16;          // 한 바퀴
        private const float StreamRestSeconds = 1.8f;

        /// <summary>이번 발이 몇 방향인가. CH5 부터 4방향과 8방향을 번갈아 쏜다.</summary>
        private int CrossDirsNow(Unit e)
        {
            if (PatternChapter < CrossAltFromChapter) return CrossDirs;
            // 축이 쏠 때마다 45° 돈다 — 짝수 번째가 4방향, 홀수 번째가 8방향
            bool odd = Mathf.RoundToInt(e.PatternAngle / CrossRotateDeg) % 2 != 0;
            return odd ? CrossDirs * 2 : CrossDirs;
        }

        /// <summary>
        /// 회전 연사(CH6). 한 발씩 22.5° 돌며 한 바퀴를 쏘고 쉰다.
        /// 십자(한꺼번에 네 발)와 달리 **탄이 나선으로 퍼진다** — 틈이 돌아가므로 같이 돌아야 한다.
        ///
        ///   0  쉼            1  예고 0.7초            2  연사 16발
        /// </summary>
        private void TickCrossStream(Unit e, float dt)
        {
            e.SetMoving(false);
            e.PatternTimer -= dt;
            if (e.PatternTimer > 0f) return;

            switch (e.PatternPhase)
            {
                case 0:
                    e.PatternPhase = 1;
                    e.PatternTimer = StreamTellSeconds;
                    e.SetTelegraph(true);
                    e.SetState(EnemyState.Attack);
                    break;

                case 1:
                    e.SetTelegraph(false);
                    e.PatternPhase = 2;
                    e.PatternFrom = Vector2.zero;   // x 에 쏜 발 수를 센다
                    e.PatternTimer = 0f;
                    break;

                default:
                {
                    e.PlayAttack();
                    float reach = _roomSize.magnitude;
                    float speed = _config.ShotSpeedEnemy * 0.8f;
                    var dir = Rotate(Vector2.right, e.PatternAngle);
                    var shot = RentShot();
                    if (shot != null)
                    {
                        shot.SetSprite(ShotSpriteOf(e), ShotKindOf(e), LoopsFrames(ShotKindOf(e)));
                        shot.Fire(e.Position, e.Position + dir * reach, speed, Mathf.Max(1, e.Atk),
                                  false, null, _config.ShotSize, ShotEnemyColor,
                                  reach / Mathf.Max(1f, speed) + 0.25f);
                    }
                    e.PatternAngle = (e.PatternAngle + StreamStepDeg) % 360f;
                    var count = e.PatternFrom;
                    count.x += 1f;
                    e.PatternFrom = count;
                    if (count.x >= StreamShots)
                    {
                        e.PatternPhase = 0;
                        e.PatternTimer = StreamRestSeconds;
                        e.SetState(EnemyState.Cooldown);
                    }
                    else e.PatternTimer = StreamGapSeconds;
                    break;
                }
            }
        }
    }
}
