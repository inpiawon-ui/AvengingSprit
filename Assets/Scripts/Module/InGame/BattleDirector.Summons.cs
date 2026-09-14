using System.Collections.Generic;
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

        // ── 해골 — 사신 · 영매가 처치할 때 ──────────────────────
        private const float SkullLifeSeconds = 12f;
        private const float SkullHpPercent = 0.25f;
        private const float SkullAtkPercent = 0.40f;

        // ── 골렘 — 영매 액티브 ─────────────────────────────────
        /// <summary>골렘 그림 아틀라스 키(`atlas/unit_golem`). 잡몹 표에는 없는 몸이다.</summary>
        private const string GolemKey = "golem";
        private const float GolemLifeSeconds = 15f;
        private const float GolemHpPercent = 0.60f;
        private const float GolemAtkPercent = 0.60f;

        // ── 분신 — 닌자 액티브 ─────────────────────────────────
        //
        // 명세 2026-09-14 — 5초 · 최대 체력 100% · 맵 가운데 · 모든 적이 분신을 본다.
        // 때리지도 움직이지도 않는다. **맞아 주는 것이 전부**다.
        private const float CloneLifeSeconds = 5f;
        private const float CloneHpPercent = 1.00f;

        private sealed class Summon
        {
            public Unit U;
            public float Life;
            public bool Attacks;
            public bool Mobile;
            public bool Taunt;
        }

        private readonly List<Summon> _summons = new();

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
        private void SummonSkull(Vector2 at)
            => SpawnSummon(TrashSkeletonKey, "해골", SkullHpPercent, SkullAtkPercent,
                           SkullLifeSeconds, attacks: true, mobile: true, taunt: false, at: at);

        /// <summary>
        /// 영매 액티브 — 골렘. 해골보다 크고 오래 간다.
        ///
        /// 제 그림이 들어오기 전에는 해골을 1.35 배로 키워 썼다. 이제 제 몸이 있으므로
        /// **배율을 1 로 돌린다** — 골렘은 원래 어깨가 넓게 그려져 있어, 키우면
        /// 해골 자리에 맞춰 두었던 크기만 두 번 곱해진다.
        /// </summary>
        private void SummonGolem(Vector2 at)
            => SpawnSummon(GolemKey, "골렘", GolemHpPercent, GolemAtkPercent,
                           GolemLifeSeconds, attacks: true, mobile: true, taunt: false, at: at);

        /// <summary>닌자 액티브 — 분신. 방 한가운데 서서 맞아 준다.</summary>
        private void SummonClone(Vector2 at)
        {
            string key = _host != null ? _host.Key : TrashSkeletonKey;
            SpawnSummon(key, "분신", CloneHpPercent, 0f, CloneLifeSeconds,
                        attacks: false, mobile: false, taunt: true, at: at);
        }

        private void SpawnSummon(string key, string name, float hpPercent, float atkPercent,
                                 float life, bool attacks, bool mobile, bool taunt,
                                 Vector2 at, float scale = 1f)
        {
            if (_host == null) return;

            var art = UnitGet(key);
            if (art == null) return;   // 그림이 없으면 세우지 않는다 — 깨진 사각형이 서느니 없는 게 낫다

            while (_summons.Count >= SummonMaxCount) RemoveSummon(0);

            var u = NewUnit($"Summon_{key}_{_summons.Count}");
            int hp = Mathf.Max(1, Mathf.RoundToInt(_host.HpMax * hpPercent));
            int atk = Mathf.Max(0, Mathf.RoundToInt(_host.Atk * atkPercent));
            u.Setup(UnitSide.Player, key, name, art, hp, atk,
                    _host.MoveSpeed * 0.9f, _host.AttackRange, _host.AttackInterval,
                    UnitBox(96f * scale, 92f * scale), isBoss: false, profile: null);
            u.Position = ClampedInField(u, at);
            u.SetState(EnemyState.Idle);
            u.ResetPattern();
            ApplyFacingSprites(u, key);

            _summons.Add(new Summon { U = u, Life = life, Attacks = attacks, Mobile = mobile, Taunt = taunt });
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

                s.U.TickFlash(dt);
                s.U.TickAnim(dt);

                if (!s.Attacks && !s.Mobile) { s.U.SetMoving(false); continue; }

                var target = NearestEnemyTo(s.U.Position);
                if (target == null) { s.U.SetMoving(false); continue; }

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
                // ⚠ `fromPlayer: true` — 이래야 피해가 적에게 간다(동료와 같은 규칙).
                if (s.Attacks && s.U.TickAttack(dt)) PerformAttack(s.U, target, true);
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

        /// <summary>방이 바뀌면 다 거둔다 — 산 방에서만 같이 싸운다.</summary>
        private void ClearSummons()
        {
            for (int i = _summons.Count - 1; i >= 0; i--) RemoveSummon(i);
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
