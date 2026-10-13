using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 몸의 전투 수치(체력 · 한 대 피해 · 초당 타수)와 **전투력**을 재는 단 한 곳 (2026-10-13).
    ///
    /// 전투(`BattleDirector.EnterHost`)와 로비 전투력이 같은 자로 잰다 — 두 곳이 따로 셈하면
    /// 「전투력이 높은데 더 약하다」가 생긴다. 등급 → 실수 곡선은 `GameConfig`, 강화 배율은 `PlayerDataService` 가 갖고,
    /// 여기는 그 둘을 「판에서 실제로 쓰는 순서」대로 곱한다.
    /// </summary>
    public static class HostCombat
    {
        // ⚠ **유령(유저) 레벨은 능력치를 직접 올리지 않는다**(PD 2026-10-13 「레벨이 오르면 스텟을 찍을 수 있는 능력이 오르는 거지,
        //   추가 스텟이 오르는 건 없어」). 레벨이 여는 것은 유령 탭 강화 상한이다(`GameConfig.GhostStatCap`).
        //   예전에는 레벨마다 체력 3% · 공격 1.6% · 치명타(등급 값 → 40%)가 모든 몸에 공짜로 붙었다.

        // ── 성급(숙련도)이 주는 체력 · 공격 — 그 몸에만 쌓인다 (PD 2026-10-13) ──
        //   숙련도 한 단계마다 7% 씩 곱한다. 1 = ×1.00 · 5 = ×1.31 · 10 = ×1.84. 0(봉인)은 1.
        public const float MasteryStatPerLevel = 0.07f;

        public static float MasteryStatMul(int mastery)
            => Mathf.Pow(1f + MasteryStatPerLevel, Mathf.Max(0, mastery - 1));

        // ── 손 속도 — 기획 2026-09-15 아마존 2배 · 근접 1.5배(겹치지 않는다) ──
        public const float AmazonHandSpeedMul = 2f;
        public const float MeleeHandSpeedMul = 1.5f;

        public static float HandSpeedMul(HostEntry e)
            => e == null ? 1f
             : e.HostKey == "amazon" ? AmazonHandSpeedMul
             : e.Kind == AttackKind.Melee || e.Kind == AttackKind.Pulse ? MeleeHandSpeedMul
             : 1f;

        /// <summary>등급에서 나온 기본 체력.</summary>
        public static int BaseHp(GameConfig config, HostEntry e)
            => e == null || config == null ? 100 : config.HpOfGrade(e.HpGrade);

        /// <summary>
        /// 등급에서 나온 한 대 피해 = 공격력 등급(DPS) ÷ 공속 등급(횟수) ÷ 한 번에 나가는 탄 수.
        /// 공격력 등급만 보면 공속이 빠른 몸이 총량까지 같이 가져간다.
        /// </summary>
        public static int BaseAtk(GameConfig config, HostEntry e)
            => e == null || config == null ? 10
             : Mathf.Max(1, Mathf.RoundToInt(config.AtkOfGrade(e.AtkGrade, e.RateGrade) / Mathf.Max(1, e.ShotCount)));

        // ── 전투력 ───────────────────────────────────────────
        //
        // 전투력 = √(공격 지수 × 생존 지수) × 10 × (1 + 숙련도 × 3%).
        //   공격 지수 = 실제 초당 피해 — 한 대 피해 × 초당 타수 × 치명 기대값
        //   생존 지수 = 실제로 버티는 체력 — 체력 ÷ (1 − 방어)
        // 곱의 제곱근이라 공격만 · 체력만 몰아 올리면 덜 오르고, 고르게 올리면 더 오른다.
        // ⚠ 사거리 · 이동속도는 넣지 않는다. 근접 · 원거리라는 몸의 성격이라, 넣으면 원거리가 늘 높게 나온다.
        // ⚠ 숙련도는 스킬 · 패시브를 키운다(수치로 바로 안 바뀐다) — 그 몫을 3%씩 얹는다.

        public const float PowerScale = 10f;
        public const float PowerPerMastery = 0.03f;

        /// <summary>로비 강화 · 레벨을 얹어 전투력을 잴 재료. 배율(Mul)은 1 = 강화 없음, Flat 은 %p.</summary>
        public struct Growth
        {
            public int Mastery;
            public float HpMul, AtkMul, CritMul, AtkSpeedMul;
            public float CritDamageFlat, DefenseFlat;

            public static Growth None => new Growth
            {
                Mastery = 0, HpMul = 1f, AtkMul = 1f, CritMul = 1f, AtkSpeedMul = 1f,
            };
        }

        /// <summary>실제 초당 피해(치명 기대값 포함).</summary>
        public static float Dps(GameConfig config, HostEntry e, in Growth g)
        {
            if (config == null || e == null) return 0f;
            float hit = BaseAtk(config, e) * g.AtkMul * MasteryStatMul(g.Mastery);
            float rate = config.RateOfGrade(e.RateGrade) * HandSpeedMul(e) * g.AtkSpeedMul;
            // 판(`BattleDirector.CritPercent`)과 같이 확률에 곱한다 — 100% 를 넘으면 늘 치명이다
            float critP = Mathf.Clamp01(HostStats.CritPercent(config, e) * g.CritMul / 100f);
            float critX = config.CritMultiplier + g.CritDamageFlat / 100f;
            return hit * rate * (1f + critP * (critX - 1f));
        }

        /// <summary>실제로 버티는 체력 = 체력 ÷ (1 − 방어).</summary>
        public static float EffectiveHp(GameConfig config, HostEntry e, in Growth g)
        {
            if (config == null || e == null) return 0f;
            float hp = BaseHp(config, e) * g.HpMul * MasteryStatMul(g.Mastery);
            float def = Mathf.Min(e.DefensePercent + g.DefenseFlat, GameConfig.DefenseCapPercent) / 100f;
            return hp / Mathf.Max(0.05f, 1f - def);
        }

        /// <summary>전투력. 유령(몸 아님)은 0.</summary>
        public static int Power(GameConfig config, HostEntry e, in Growth g)
        {
            if (config == null || e == null || e.IsGhost) return 0;
            float raw = Mathf.Sqrt(Mathf.Max(0f, Dps(config, e, g)) * Mathf.Max(0f, EffectiveHp(config, e, g)));
            return Mathf.RoundToInt(raw * PowerScale * (1f + PowerPerMastery * Mathf.Max(0, g.Mastery)));
        }
    }
}
