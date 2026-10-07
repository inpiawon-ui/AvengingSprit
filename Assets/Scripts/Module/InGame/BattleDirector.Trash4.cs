using System.Collections.Generic;
using Game.Character;
using UnityEngine;
using GameSound = Game.Module.Common.GameSound;

namespace Game.Module.InGame
{
    /// <summary>
    /// 새 잡몹 넷 — 라인업 2차(PD 통과) 3 · 6 · 7 · 8번. 기획 `AVSR_Content_EnemyGimmick.md` 3절 · 시안 `batch_mocks/mock_monsters4.png`.
    ///
    ///   갑옷 아르마딜로(4챕터 · 파워)  방패병 — 앞에서 친 것은 90% 막고 불똥이 튄다. 옆 · 뒤로 돌아 친다. 평타는 머리 박치기
    ///   폭탄 버섯(5챕터 · 마법)       자폭 — 다가와 1초 깜빡이다 원(1.5 m) 안을 터뜨린다. 먼저 잡으면 0.5초 뒤 그 자리에서 터져
    ///                                 옆 적을 휩쓴다. 한 방에 죽이지 않는다 — 원을 보고 피하는 것이 재미(PD)
    ///   미라(9챕터 · 마법)            링 방사 — 멈춰 0.7초 손에 보랏빛을 모았다가 16발 고리. 빈틈 두 칸은 **내 쪽이 아닌 곳**에 난다 — 움직여 빠져야 한다
    ///   알 거미(8챕터 · 마법)          소환사 — 4.5초마다 새끼 둘을 낳고(최대 넷) 본체는 거리를 벌린다. 먼저 잡는다
    ///
    /// 새끼 거미(키 `spiderling`)는 등에 깨진 알껍질을 진 전용 그림을 절반 크기로 세운다 —
    /// 어미 그림을 줄여 쓰니 「알에서 나온 새끼」로 안 읽혔다(코덱스 검수 2026-10-07).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string TrashArmadilloKey = "armadillo";
        private const string TrashMushroomKey = "mushroom";
        private const string TrashMummyKey = "mummy";
        private const string TrashSpiderKey = "spider";
        private const string TrashSpiderlingKey = "spiderling";   // 새끼 — 등에 알껍질을 진 전용 그림(코덱스 검수)

        private static HostEntry s_armadillo, s_mushroom, s_mummy, s_spider, s_spiderling;

        private static HostEntry Armadillo => s_armadillo ??= HostEntry.CreateTrash(
            TrashArmadilloKey, "갑옷 아르마딜로", AttackKind.Melee,
            hp: 40, atk: 9, moveMps: 0.9f, engageMps: 1.2f,
            rangeMeters: 1.1f, interval: 1.6f, telegraph: 0.5f);

        private static HostEntry Mushroom => s_mushroom ??= HostEntry.CreateTrash(
            TrashMushroomKey, "폭탄 버섯", AttackKind.Melee,
            hp: 18, atk: 16, moveMps: 1.5f, engageMps: 2.0f,
            rangeMeters: 1.3f, interval: 9f, telegraph: 1.0f);

        private static HostEntry Mummy => s_mummy ??= HostEntry.CreateTrash(
            TrashMummyKey, "미라", AttackKind.Single,
            hp: 30, atk: 8, moveMps: 0.8f, engageMps: 1.0f,
            rangeMeters: 6f, interval: 3.2f, telegraph: 0.7f);

        private static HostEntry Spider => s_spider ??= HostEntry.CreateTrash(
            TrashSpiderKey, "알 거미", AttackKind.Single,
            hp: 34, atk: 6, moveMps: 1.2f, engageMps: 1.6f,
            rangeMeters: 5f, interval: 4.5f, telegraph: 0.6f);

        // 새끼 — 등에 깨진 알껍질을 진 전용 그림. 빠르고 약한 근접
        private static HostEntry Spiderling => s_spiderling ??= HostEntry.CreateTrash(
            TrashSpiderlingKey, "새끼 거미", AttackKind.Melee,
            hp: 6, atk: 4, moveMps: 2.4f, engageMps: 3.0f,
            rangeMeters: 0.8f, interval: 1.0f, telegraph: 0.25f);

        // ── 아르마딜로 · 방패 발전기 공용 — 막기 ──────────────────────
        private const float GuardFrontDot = 0.35f;     // 정면 ±70° 를 막는다
        private const float GuardDamageMul = 0.1f;
        private const float GuardSparkSeconds = 0.12f; // 불똥 · 소리는 이만큼에 한 번(연사에 소리가 쌓이지 않게)
        private float _guardSparkAt;

        // ── 폭탄 버섯 ────────────────────────────────────────────
        private const float MushroomFuseMeters = 1.3f;   // 이만큼 붙으면 심지에 불을 붙인다
        private const float MushroomFuseSeconds = 1.0f;
        private const float MushroomRadiusMeters = 1.5f;
        private const float MushroomDeathDelay = 0.5f;   // 잡혔을 때 터지기까지 — 붙어 친 근접이 빠질 틈
        private const float MushroomEnemyHpRatio = 0.4f;

        /// <summary>미뤄 둔 폭발. 적 목록을 도는 도중에 적을 지우지 않으려고 목록 밖(`TickBlasts`)에서 터뜨린다.</summary>
        private struct Blast { public Vector2 At; public float Timer; public Unit Self; }
        private readonly List<Blast> _blasts = new();
        private readonly HashSet<Unit> _mushroomBlown = new();

        // ── 미라 ────────────────────────────────────────────────
        private const float MummyKeepMeters = 4.0f;
        private const float MummyRingSeconds = 3.2f;
        private const float MummyTellSeconds = 0.7f;
        private const int MummyRingShots = 16;
        private const int MummyGapShots = 2;
        private const float MummyGapAwayDegrees = 70f;   // 빈틈은 내 쪽에서 이만큼 이상 비켜 난다
        private const float MummyShotSpeedMul = 0.6f;
        private const string MummyShotKind = "darkorb";   // 영매의 어둠 구슬을 같이 쓴다 — 화면에선 흰 · 청록빛이라 보라 바닥(9챕터)에서 오히려 잘 읽힌다

        // ── 알 거미 ──────────────────────────────────────────────
        private const float SpiderKeepMeters = 4.0f;
        private const float SpiderBroodSeconds = 4.5f;
        private const float SpiderTellSeconds = 0.6f;
        private const int SpiderBroodMax = 4;
        private const float SpiderlingBox = 0.5f;        // 새끼는 어미 칸의 절반
        private readonly HashSet<Unit> _spiderlings = new();

        // ═══════════════════════════════════════════════════════════
        //  막기 — 아르마딜로 정면 · 방패 발전기 보호막
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 내가 때린 피해를 막는 것이 있으면 줄인다. `ApplyShotHit` · `HitEnemyWith` 가 부른다.
        /// 앞에서 오는가는 **때린 몸의 자리**로 잰다 — 탄이 어디서 날아왔든 쏜 쪽이 앞이다.
        /// </summary>
        private int GuardedDamage(Unit victim, int damage)
        {
            if (victim == null || damage <= 0) return damage;
            bool blocked = false;
            if (victim.Key == TrashArmadilloKey && !victim.IsStunned && Avatar != null)
            {
                var to = Avatar.Position - victim.Position;
                if (to.sqrMagnitude > 1f && Vector2.Dot(victim.Facing, to.normalized) > GuardFrontDot) blocked = true;
            }
            if (!blocked && IsShielded(victim)) blocked = true;
            if (!blocked) return damage;

            if (Time.time - _guardSparkAt >= GuardSparkSeconds)
            {
                _guardSparkAt = Time.time;
                var toward = Avatar != null ? (Avatar.Position - victim.Position).normalized : Vector2.up;
                _pfx?.Hit(victim.Position + toward * victim.BodyRadius, ParticleElement.Fire, 0.35f);
                GameSound.Cue("hit.reflect");
            }
            return Mathf.Max(1, Mathf.RoundToInt(damage * GuardDamageMul));
        }

        // ═══════════════════════════════════════════════════════════
        //  걷기 — 걸리면 옆으로 돌아 나간다(평소 잡몹과 같은 우회)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// `goal` 쪽으로 한 걸음. 담 · 엄폐에 걸려 제자리걸음이면 평소 잡몹처럼 옆 자리로 돌아 나간다 —
        /// 곧장 다가가기만 하면 낮은 담 뒤에서 한 발도 못 나왔다(실측 2026-10-08, 5-10 폭탄 버섯).
        /// </summary>
        private void StepWithDetour(Unit e, Unit me, Vector2 step, float dt)
        {
            var before = e.Position;
            if (e.IsRepositioning && e.IsDetouring)
            {
                e.Position = SlideMove(e, e.Position, e.StepToward(e.RepositionTarget, dt));
                if (Vector2.Distance(e.Position, e.RepositionTarget) < 24f || NoteStuck(e, before, dt)) e.EndReposition();
                return;
            }
            e.Position = SlideMove(e, e.Position, step);
            if (NoteStuck(e, before, dt)) e.BeginDetour(PickDetourSpot(e, me));
        }

        // ═══════════════════════════════════════════════════════════
        //  폭탄 버섯
        // ═══════════════════════════════════════════════════════════

        private bool TickMushroom(Unit e, Unit me, float distance, float dt)
        {
            if (e.PatternPhase == 2) { e.SetMoving(false); return true; }   // 터지는 중
            if (e.PatternPhase == 1)
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Attack);
                e.PatternTimer -= dt;
                if (e.PatternTimer > 0f) return true;
                // 터진다 — 피해(나)는 예고 원이 이미 쳤다. 둘레 적 · 제 몸은 목록 밖에서
                e.SetTelegraph(false);
                e.PatternPhase = 2;
                _mushroomBlown.Add(e);
                _blasts.Add(new Blast { At = e.Position, Timer = 0f, Self = e });
                return true;
            }

            if (distance > Meters(MushroomFuseMeters))
            {
                e.SetState(EnemyState.Approach);
                StepWithDetour(e, me, e.StepToward(me.Position, dt), dt);
                e.SetMoving(true);
                return true;
            }

            // 심지 — 1초 깜빡이며 원을 깐다. 원 밖으로 나가면 안 맞는다
            e.PatternPhase = 1;
            e.PatternTimer = MushroomFuseSeconds;
            e.SetTelegraph(true);
            e.PlayAttack();
            StartWarn(DiscShape(e.Position, Meters(MushroomRadiusMeters)), MushroomFuseSeconds,
                      Mathf.Max(1, e.Atk), owner: e);
            return true;
        }

        /// <summary>잡혀 죽은 버섯 — 0.5초 뒤 그 자리에서 터진다. `OnTrashDeath` 가 부른다.</summary>
        private void OnMushroomDeath(Unit u)
        {
            if (!_mushroomBlown.Remove(u) && u.Key == TrashMushroomKey)
            {
                float r = Meters(MushroomRadiusMeters);
                StartWarn(DiscShape(u.Position, r), MushroomDeathDelay, Mathf.Max(1, u.Atk));
                _blasts.Add(new Blast { At = u.Position, Timer = MushroomDeathDelay });
            }
        }

        /// <summary>잡혀서 미뤄 둔 폭발 — `TickHazards2` 가 매 프레임 부른다.</summary>
        private void TickBlasts(float dt)
        {
            for (int i = _blasts.Count - 1; i >= 0; i--)
            {
                var b = _blasts[i];
                b.Timer -= dt;
                if (b.Timer > 0f) { _blasts[i] = b; continue; }
                _blasts.RemoveAt(i);
                BlastEnemies(b.At, b.Self);
                SpawnImpact(b.At, "grenade", Meters(MushroomRadiusMeters) * 2f);
                if (b.Self != null && b.Self.IsAlive && !b.Self.IsDying) KillEnemy(b.Self);
            }
        }

        private void BlastEnemies(Vector2 at, Unit self)
        {
            float r = Meters(MushroomRadiusMeters);
            // ⚠ 뒤에서부터 — `KillEnemy` 가 목록에서 뺀다
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || e == self || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                if (Vector2.Distance(at, e.Position) > r + e.BodyRadius * 0.5f) continue;
                int dmg = Mathf.Max(1, Mathf.RoundToInt(e.HpMax * MushroomEnemyHpRatio));
                e.IsAggro = true;
                ShowDamage(e.Position, dmg, toEnemy: true);
                if (e.TakeDamage(dmg)) KillEnemy(e);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  미라 — 빈틈 있는 고리
        // ═══════════════════════════════════════════════════════════

        private bool TickMummy(Unit e, Unit me, float distance, float dt)
        {
            if (e.PatternPhase == 1)
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Attack);
                e.PatternTimer -= dt;
                if (e.PatternTimer > 0f) return true;
                e.SetTelegraph(false);
                FireMummyRing(e, me);
                e.PatternPhase = 0;
                e.PatternTimer = MummyRingSeconds;
                return true;
            }

            e.PatternTimer -= dt;
            if (e.PatternTimer <= 0f && distance <= EffectiveRange(e))
            {
                e.PatternPhase = 1;
                e.PatternTimer = MummyTellSeconds;
                e.SetTelegraph(true);
                e.PlayAttack();
                return true;
            }

            // 알맞은 거리를 지키며 천천히 — 너무 가까우면 물러난다
            var to = me.Position - e.Position;
            var dir = to.sqrMagnitude < 0.0001f ? -e.Facing : to.normalized;
            e.SetFacing(dir);
            e.SetState(EnemyState.Approach);
            float want = distance - Meters(MummyKeepMeters);
            if (Mathf.Abs(want) < Meters(0.5f)) { e.SetMoving(false); return true; }
            StepWithDetour(e, me, dir * (Mathf.Sign(want) * e.MoveSpeed * dt), dt);
            e.SetMoving(true);
            return true;
        }

        private void FireMummyRing(Unit e, Unit me)
        {
            float toMe = me != null ? Mathf.Atan2(me.Position.y - e.Position.y, me.Position.x - e.Position.x) * Mathf.Rad2Deg : 0f;
            // 빈틈 — 내 쪽에서 70° 이상 비켜 난다. 가만히 서 있으면 맞는다
            float gap = toMe + Mathf.Sign(Random.value - 0.5f) * Random.Range(MummyGapAwayDegrees, 180f);
            float step = 360f / MummyRingShots;
            for (int i = 0; i < MummyRingShots - MummyGapShots; i++)   // 마지막 두 칸이 빈틈(gap 바로 앞)
            {
                float a = gap + (i + 0.5f) * step;
                FireAimed(e, e.Position + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 100f,
                          MummyShotSpeedMul, MummyShotKind);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  알 거미 — 소환사
        // ═══════════════════════════════════════════════════════════

        private bool TickSpider(Unit e, Unit me, float distance, float dt)
        {
            if (e.PatternPhase == 1)
            {
                e.SetMoving(false);
                e.SetState(EnemyState.Attack);
                e.PatternTimer -= dt;
                if (e.PatternTimer > 0f) return true;
                e.SetTelegraph(false);
                for (int k = 0; k < 2 && CountBrood() < SpiderBroodMax; k++)
                {
                    var side = new Vector2(-e.Facing.y, e.Facing.x) * (k == 0 ? 1f : -1f);
                    SpawnSpiderling(e.Position + side * Meters(0.6f) + e.Facing * Meters(0.3f));
                }
                e.PatternPhase = 0;
                e.PatternTimer = SpiderBroodSeconds;
                return true;
            }

            e.PatternTimer -= dt;
            if (e.PatternTimer <= 0f && CountBrood() < SpiderBroodMax)
            {
                e.PatternPhase = 1;
                e.PatternTimer = SpiderTellSeconds;
                e.SetTelegraph(true);
                e.PlayAttack();
                return true;
            }

            // 본체는 거리를 벌린다 — 붙으면 달아나고, 멀면 슬금슬금
            var away = e.Position - me.Position;
            away = away.sqrMagnitude < 0.0001f ? -e.Facing : away.normalized;
            e.SetFacing(-away);
            e.SetState(EnemyState.Approach);
            if (distance < Meters(SpiderKeepMeters))
            {
                StepWithDetour(e, me, away * (e.MoveSpeed * dt), dt);
                e.SetMoving(true);
            }
            else e.SetMoving(false);
            return true;
        }

        private int CountBrood()
        {
            int n = 0;
            foreach (var s in _spiderlings) if (s != null && s.IsAlive && !s.IsDying) n++;
            return n;
        }

        /// <summary>새끼 거미 — 어미 그림을 절반 크기로. 처음부터 깨어 덤빈다.</summary>
        private void SpawnSpiderling(Vector2 at)
        {
            var p = Spiderling;
            if (_enemies.Count > MaxRoomUnits) return;
            var u = NewUnit($"Brood_{_enemies.Count}");
            u.Setup(UnitSide.Enemy, p.HostKey, p.DisplayName, TrashSprite(p),
                    Mathf.Max(1, EnemyHpOf(p)), Mathf.RoundToInt(EnemyAtkOf(p) * NormalEnemyAtkMul),
                    EnemySpeedOf(p), EnemyRangeOf(p), EnemyIntervalOf(p) / EnemyHandSpeedMul,
                    UnitBox(96f * SpiderlingBox, 92f * SpiderlingBox), isBoss: false, profile: p);
            u.Position = at;
            ClampToField(u);
            ResolveObstacles(u);
            u.PossessPriority = p.PossessPriority;
            u.SetState(EnemyState.Detect);
            u.ResetPattern();
            u.IsAggro = true;
            ApplyFacingSprites(u, p.SpriteKey);
            _enemies.Add(u);
            _spiderlings.Add(u);
            _pfx?.Puff(at, ParticleElement.Dust, 0.4f);
        }

        /// <summary>방을 나갈 때 — `ClearWarns` 가 부른다.</summary>
        private void ClearTrash4()
        {
            _blasts.Clear();
            _mushroomBlown.Clear();
            _spiderlings.Clear();
        }
    }
}
