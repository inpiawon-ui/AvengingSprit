using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 내가 불러내는 것들 — 해골(사신 · 영매) · 골렘(영매) · 분신(닌자).
    ///
    /// 상점 동료(`BattleDirector.Ally.cs`)와 **따로 둔다.** 저쪽은 한 판에 하나뿐이고
    /// 죽을 때까지 남지만, 이쪽은 **여럿이 서고 시간이 지나면 사라진다.**
    /// 한 틀에 억지로 합치면 「하나뿐인 동료」 규칙이 깨진다.
    ///
    /// 셋의 차이는 세 가지 성질뿐이다.
    ///   때리는가(`Attacks`) · 움직이는가(`Mobile`) · 적이 쫓아오는가(`Taunt`)
    ///
    /// 도발은 **적의 표적을 바꾼다**(`TauntUnit` — `TickEnemies` 가 읽는다).
    /// 피해는 `SoakWithSummon` 이 대신 받는다 — 적 공격은 전부 플레이어를 겨누게
    /// 짜여 있어서, 맞는 쪽을 바꾸는 자리는 여기 하나다(동료와 같은 방식).
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>한 번에 설 수 있는 수. 넘으면 가장 오래된 것부터 지운다.</summary>
        private const int SummonMaxCount = 6;
        /// <summary>칠 적이 없을 때 이만큼보다 멀면 나를 따라온다.</summary>
        private const float SummonFollowMeters = 1.6f;

        // ── 해골 — 사신 · 영매가 처치할 때 ──────────────────────
        // 기획 2026-09-15 — 3초 뒤 사라진다. 남은 시간이 **체력바가 줄어드는 것**으로 보인다.
        private const float SkullLifeSeconds = 3f;
        private const float SkullHpPercent = 0.25f;
        private const float SkullAtkPercent = 0.40f;
        /// <summary>해골 평타 간격. 잡몹 해골(1.4초)을 그대로 쓰면 너무 느렸다 — 3초 사는 동안 두 번뿐이었다.</summary>
        private const float SkullAttackInterval = 0.6f;

        // ── 골렘 — 영매 액티브 ─────────────────────────────────
        /// <summary>골렘 그림 아틀라스 키(`atlas/unit_golem`). 잡몹 표에는 없는 몸이다.</summary>
        private const string GolemKey = "golem";
        // 골렘은 수명이 없다 — **죽을 때까지** 남고 다음 방에도 따라온다(기획 2026-09-15).

        /// <summary>
        /// 골렘의 몸값. **근접으로 때리는 몸**이다 — 주먹이 무기라 탄이 없다.
        ///
        /// 체력·공격력은 `SpawnSummon` 이 영매의 비율로 다시 계산하므로 여기 숫자는
        /// 자리만 채운다. 실제로 쓰이는 것은 **공격 방식 · 사거리 · 간격**이다.
        /// 해골(1.4 m)보다 조금 길다. 공격 간격은 1.6 → 0.8초로 **두 배 빠르게** 쳤다(기획 2026-09-15).
        /// </summary>
        private static HostEntry s_golem;
        private static HostEntry GolemProfile => s_golem ??= HostEntry.CreateTrash(
            GolemKey, "골렘", AttackKind.Melee,
            hp: 60, atk: 12, moveMps: 1.0f, engageMps: 1.8f,
            rangeMeters: 1.6f, interval: 0.8f, telegraph: 0.5f);
        private const float GolemHpPercent = 1.20f;   // 기획 2026-09-15 — 체력 두 배
        private const float GolemAtkPercent = 0.60f;

        private sealed class Summon
        {
            public Unit U;
            public float Life;
            public float LifeMax;
            public bool Drains;    // 남은 수명만큼 체력바를 줄인다(해골)
            public bool Attacks;
            public bool Mobile;
            public bool Taunt;
        }

        private readonly List<Summon> _summons = new();
        /// <summary>불러낸 몸이 때리는 중 — 사신 즉사(내 손으로만)를 가른다.</summary>
        private bool _summonStriking;

        /// <summary>적이 대신 쫓아갈 몸. 없으면 null — 그때는 평소대로 나를 쫓는다.</summary>
        private Unit TauntUnit
        {
            get
            {
                for (int i = 0; i < _summons.Count; i++)
                {
                    var s = _summons[i];
                    if (s.Taunt && s.U != null && s.U.IsAlive && !s.U.IsDying) return s.U;
                }
                return null;
            }
        }

        // ── 세우기 ───────────────────────────────────────────

        /// <summary>사신 · 영매 — 죽은 자리에서 해골이 일어선다.</summary>
        private void SummonSkull(Vector2 at) => SummonSkull(at, SkullLifeSeconds);

        /// <summary>
        /// 해골은 적이 못 노리고 시간이 다하면 사라진다 — 체력이 의미가 없어 체력바를 끈다.
        /// 적 해골과 같은 그림이라 **혼빛으로 물들여** 가른다(PD 2026-10-21 「내 해골이랑 몬스터 해골이 구분」).
        /// </summary>
        private void SummonSkull(Vector2 at, float life)
        {
            SpawnSummon(TrashSkeletonKey, "해골", SkullHpPercent, SkullAtkPercent,
                        life, attacks: true, mobile: true, taunt: false, at: at,
                        profile: Skeleton, intervalOverride: SkullAttackInterval);
            if (_summons.Count == 0) return;
            var u = _summons[_summons.Count - 1].U;
            if (u == null || u.Key != TrashSkeletonKey) return;
            u.MarkSpirit();
            u.SetHpBarVisible(false);
        }

        /// <param name="profile">
        /// 이 몸의 **싸우는 방식**. 넣지 않으면 공격 방식이 「기본 = 쏘기」로 떨어지고
        /// 사거리·간격까지 불러낸 몸의 것을 물려받는다 — 그래서 골렘과 해골이
        /// 영매의 탄을 대신 쏘고 있었다(2026-09-14). 때리는 몸에는 반드시 넣는다.
        /// </param>
        private void SpawnSummon(string key, string name, float hpPercent, float atkPercent,
                                 float life, bool attacks, bool mobile, bool taunt,
                                 Vector2 at, float scale = 1f, HostEntry profile = null,
                                 float intervalOverride = 0f, bool drains = false)
        {
            if (_host == null) return;

            var art = UnitGet(key);
            if (art == null) return;   // 그림이 없으면 세우지 않는다 — 깨진 사각형이 서느니 없는 게 낫다

            while (_summons.Count >= SummonMaxCount) RemoveSummon(0);

            var u = NewUnit($"Summon_{key}_{_summons.Count}");
            int hp = Mathf.Max(1, Mathf.RoundToInt(_host.HpMax * hpPercent));
            int atk = Mathf.Max(0, Mathf.RoundToInt(_host.Atk * atkPercent));
            float range = profile != null ? profile.CanonRange * _pxPerMeter : _host.AttackRange;
            float interval = intervalOverride > 0f ? intervalOverride
                           : profile != null ? profile.CanonInterval : _host.AttackInterval;
            u.Setup(UnitSide.Player, key, name, art, hp, atk,
                    _host.MoveSpeed * 0.9f, range, interval,
                    UnitBox(96f * scale, 92f * scale), isBoss: false, profile: profile);
            u.Position = ClampedInField(u, at);
            u.SetState(EnemyState.Idle);
            u.ResetPattern();
            ApplyFacingSprites(u, key);

            _summons.Add(new Summon { U = u, Life = life, LifeMax = life, Drains = drains,
                                      Attacks = attacks, Mobile = mobile, Taunt = taunt });
            // 골렘은 룬 원에서 솟는다(영매 퀄업 2026-10-07) — 돌 파편 · 보라 빛
            if (!(key == GolemKey && QxGolemRise(u.Position)))
                PlayFx(taunt ? "smoke" : "burst", u.Position, 96f, loop: false);
        }

        // ── 굴리기 ───────────────────────────────────────────

        private void TickSummons(float dt)
        {
            for (int i = _summons.Count - 1; i >= 0; i--)
            {
                var s = _summons[i];
                if (s.U == null || !s.U.IsAlive || s.U.IsDying) { RemoveSummon(i); continue; }

                s.Life -= dt;
                if (s.Life <= 0f) { PlayFx("smoke", s.U.Position, 96f, loop: false); RemoveSummon(i); continue; }
                if (s.Drains && s.LifeMax > 0f)
                    s.U.DrainHpTo(Mathf.CeilToInt(s.U.HpMax * s.Life / s.LifeMax));

                s.U.TickFlash(dt);
                s.U.TickAnim(dt);

                if (!s.Attacks && !s.Mobile) { s.U.SetMoving(false); continue; }

                var target = NearestEnemyTo(s.U.Position);
                if (target == null)
                {
                    // 칠 것이 없으면 내 곁으로 — 방을 비운 뒤 제자리에 서 있으면 데리고 다니는 느낌이 안 났다
                    var me = Avatar;
                    if (s.Mobile && me != null && Vector2.Distance(me.Position, s.U.Position) > Meters(SummonFollowMeters))
                    {
                        s.U.SetFacing(me.Position - s.U.Position);
                        s.U.SetState(EnemyState.Approach);
                        s.U.Position = SlideMove(s.U, s.U.Position, s.U.StepToward(me.Position, dt));
                        s.U.SetMoving(true);
                    }
                    else s.U.SetMoving(false);
                    continue;
                }

                s.U.SetFacing(target.Position - s.U.Position);
                float d = Vector2.Distance(target.Position, s.U.Position);

                if (d > s.U.AttackRange && s.Mobile)
                {
                    s.U.SetState(EnemyState.Approach);
                    s.U.Position = SlideMove(s.U, s.U.Position, s.U.StepToward(target.Position, dt));
                    s.U.SetMoving(true);
                    continue;
                }

                s.U.SetMoving(false);
                // 골렘은 주먹이 아니라 충격파 — 모아서 둘레를 친다 (BattleDirector.Fixtures)
                if (s.U.Key == GolemKey && GolemSlamReady() && TickGolemSlam(s.U, target, d, dt)) continue;
                // ⚠ `fromPlayer: true` — 이래야 피해가 적에게 간다(동료와 같은 규칙).
                if (s.Attacks && s.U.TickAttack(dt))
                {
                    _summonStriking = true;
                    try { PerformAttack(s.U, target, true); }
                    finally { _summonStriking = false; }
                }
                else s.U.SetState(EnemyState.Cooldown);
            }
        }

        private Unit NearestEnemyTo(Vector2 from)
        {
            Unit best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                float d = Vector2.Distance(e.Position, from);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private void RemoveSummon(int index)
        {
            var s = _summons[index];
            if (s.U != null) Destroy(s.U.gameObject);
            _summons.RemoveAt(index);
        }

        /// <summary>방이 바뀌면 다 거둔다 — 산 방에서만 같이 싸운다. 골렘은 남길 수 있다.</summary>
        private void ClearSummons(bool keepGolem = false)
        {
            for (int i = _summons.Count - 1; i >= 0; i--)
            {
                var s = _summons[i];
                if (keepGolem && s.U != null && s.U.IsAlive && s.U.Key == GolemKey) continue;
                RemoveSummon(i);
            }
        }

        /// <summary>남겨 둔 소환물(골렘)을 새 방의 내 곁으로 옮긴다.</summary>
        private void RegroupSummons(Vector2 around)
        {
            for (int i = 0; i < _summons.Count; i++)
            {
                var u = _summons[i].U;
                if (u == null || !u.IsAlive) continue;
                u.Position = ClampedInField(u, around + new Vector2(Meters(1.2f) * (i % 2 == 0 ? 1f : -1f), 0f));
                u.SetMoving(false);
            }
        }

        /// <summary>
        /// 도발 중인 분신이 내 피해를 대신 받는다. 받아 냈으면 true.
        /// 동료(`SoakWithAlly`)보다 **먼저** 본다 — 도발은 내가 방금 켠 것이고, 동료는 늘 서 있다.
        /// </summary>
        private bool SoakWithSummon(int damage)
        {
            var u = TauntUnit;
            if (u == null) return false;

            ShowDamage(u.Position, damage, toEnemy: false);
            if (u.TakeDamage(damage))
            {
                PlayFx("burst", u.Position, 128f, loop: false);
                for (int i = _summons.Count - 1; i >= 0; i--)
                    if (_summons[i].U == u) { RemoveSummon(i); break; }
            }
            return true;
        }
    }
}
