using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 레벨이 붙은 능력치를 뽑는 **단 한 곳**.
    ///
    /// 로비 카드도, 전투도 여기서 값을 받아 간다. 두 곳에서 따로 계산하면
    /// 화면에 적힌 숫자와 실제로 때리는 숫자가 어긋난다 — 그건 밸런스를 잡을 때
    /// 가장 찾기 어려운 종류의 버그다.
    ///
    /// ⚠ **계산 결과를 테이블에 되쓰지 않는다.** 성장한 사거리를 `_canonHostRange`
    ///   에 써넣으면 다음 판정에서 직업이 바뀐다(중거리 → 원거리). 직업은 언제나
    ///   테이블 기본값으로만 판정하고, 성장은 런타임에만 얹는다.
    ///   `_rangeMul` 같은 칸이 죽어 있던 것과 같은 함정이다.
    /// </summary>
    public static class HostStats
    {
        // ── 직업 ──────────────────────────────────────────────
        //
        // **직업을 가르는 자는 여기 하나다.** 전투(`BattleDirector.JobOf`)와 로비(`HostSelectPanel.JobOf`)가
        // 규칙을 따로 들고 있었는데, 사거리 강화가 직업을 보게 되면서(2026-10-06) 셋째 자리가 생겼다 —
        // 세 곳이 어긋나기 전에 한 곳으로 모은다.
        //   0 근거리  근접 · 광역        1 중거리  사거리 6 m 이하       2 원거리  나머지

        public const int JobMelee = 0, JobMid = 1, JobRanged = 2;

        /// <summary>중거리와 원거리를 가르는 선(m).</summary>
        public const float MidRangeMeters = 6.0f;

        public static int JobIndex(HostEntry e)
        {
            if (e == null) return JobRanged;
            if (e.Kind == AttackKind.Melee || e.Kind == AttackKind.Pulse) return JobMelee;
            return e.CanonHostRange > 0f && e.CanonHostRange <= MidRangeMeters ? JobMid : JobRanged;
        }

        /// <summary>
        /// 사거리 강화를 얹은 사거리 — 직업마다 레벨당 % 가 다르고 직업 상한에서 멈춘다.
        /// 기본 사거리가 이미 상한보다 길면 줄이지 않는다.
        /// </summary>
        /// <param name="baseRange">강화 전 사거리(단위는 부르는 쪽 그대로 — px 이든 m 이든)</param>
        /// <param name="unitsPerMeter">상한(m)을 baseRange 단위로 바꾸는 값</param>
        public static float UpgradedRange(GameConfig config, HostEntry host, float baseRange,
                                          int upgradeLevels, float unitsPerMeter)
        {
            if (config == null || host == null) return baseRange;
            int job = JobIndex(host);
            float grown = baseRange * (1f + config.RangePercentPerLevel(job) * Mathf.Max(0, upgradeLevels) / 100f);
            float cap = config.RangeGrowthMax(job) * unitsPerMeter;
            return Mathf.Min(grown, Mathf.Max(baseRange, cap));
        }

        /// <summary>
        /// Lv1 → 만렙 사이의 성장 배율.
        ///
        /// 성장폭(`GameConfig`)은 **만렙에서의 배율**이므로 Lv1 은 언제나 1 이다.
        /// 보간은 여기 한 줄뿐이라 비선형으로 갈아끼울 자리도 여기다.
        /// </summary>
        public static float GrowthMul(GameConfig config, HostEntry host,
                                      HostStat stat, int level, int levelMax)
        {
            if (config == null || host == null) return 1f;
            float atMax = config.StatGrowth(stat, host.IsPrimary(stat));
            if (levelMax <= 1) return 1f;
            float t = Mathf.Clamp01((float)(level - 1) / (levelMax - 1));
            return Mathf.Lerp(1f, atMax, t);
        }

        /// <summary>
        /// 이 레벨의 치명타 확률(%).
        ///
        /// ⚠ 치명타 성장폭은 **만렙에서의 확률**이지 레벨당 증분이 아니다.
        ///   증분으로 적으면 `10 + 2.5 × 49 = 132.5%` 로 폭주해 "확률" 이 아니게 된다.
        ///   시작값(`_critPercent`)에서 그 끝점까지 보간한다 — 주 40% · 부 22%.
        /// </summary>
        public static float CritPercent(GameConfig config, HostEntry host, int level, int levelMax)
        {
            if (config == null || host == null) return 0f;
            // 시작값은 **등급**에서 나온다. 확정본의 `_critPercent` 가 23명 전원 10 으로
            // 같아 차등이 없었다 — 등급이 그 자리를 대신한다(기획 2026-09-15).
            float start = config.CritOfGrade(host.CritGrade);
            float atMax = config.StatGrowth(HostStat.Crit, host.IsPrimary(HostStat.Crit));
            if (levelMax <= 1) return start;
            float t = Mathf.Clamp01((float)(level - 1) / (levelMax - 1));
            return Mathf.Clamp(Mathf.Lerp(start, Mathf.Max(start, atMax), t), 0f, 100f);
        }

        /// <summary>
        /// 이 레벨의 실제 사거리(m). **직업 밴드 상한에서 잘린다.**
        ///
        /// 자르지 않으면 중거리 몸이 6.0m 를 넘겨 원거리로 재분류되고,
        /// 그 순간 착탄 범위(중거리 상시 규칙)가 조용히 사라진다.
        /// </summary>
        public static float Range(GameConfig config, HostEntry host, int jobIndex,
                                  int level, int levelMax)
        {
            if (host == null) return 0f;
            float grown = host.CanonHostRange * GrowthMul(config, host, HostStat.Range, level, levelMax);
            return config == null ? grown : Mathf.Min(grown, config.RangeGrowthMax(jobIndex));
        }
    }
}
