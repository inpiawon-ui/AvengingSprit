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
            float atMax = config.StatGrowth(HostStat.Crit, host.IsPrimary(HostStat.Crit));
            if (levelMax <= 1) return host.CritPercent;
            float t = Mathf.Clamp01((float)(level - 1) / (levelMax - 1));
            return Mathf.Clamp(Mathf.Lerp(host.CritPercent, atMax, t), 0f, 100f);
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
