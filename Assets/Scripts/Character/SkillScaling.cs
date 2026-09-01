using System;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 숙련도에 따라 자라는 수치 한 벌.
    ///
    /// 기획서 §5 의 표가 그대로 이 모양이다 — "무적 0.8 → 1.5초" 는
    /// 시작값과 끝값 두 개일 뿐이고, 사이는 계산으로 채운다.
    /// 레벨마다 값을 적으면 10칸 × 23명 × 두 구간이 되어 손으로 관리할 수 없다.
    ///
    /// **구간이 둘인 이유**는 Lv5 에서 특수 효과가 열리기 때문이다.
    ///   `base`  Lv1 → Lv4   기본 효과가 자라는 구간
    ///   `spec`  Lv5 → Lv10  특수 효과가 열린 뒤 자라는 구간
    ///
    /// ⚠ **켜짐/꺼짐은 코드가, 수치는 데이터가** 갖는다.
    ///   "Lv5 부터 보스에게 고정피해" 같은 분기를 데이터에 넣으려 하지 말 것 —
    ///   넣는 순간 표가 스크립트가 되고, 어느 쪽도 읽을 수 없게 된다.
    /// </summary>
    [Serializable]
    public struct SkillScaling
    {
        [Tooltip("기본 구간 시작값 (Lv1).")]
        [SerializeField] private float _baseValue;
        [Tooltip("기본 구간 끝값 (Lv4).")]
        [SerializeField] private float _baseValueMax;
        [Tooltip("특수 구간 시작값 (Lv5, 특수 효과가 열리는 단계).")]
        [SerializeField] private float _specValue;
        [Tooltip("특수 구간 끝값 (Lv10).")]
        [SerializeField] private float _specValueMax;

        public float BaseValue    => _baseValue;
        public float BaseValueMax => _baseValueMax;
        public float SpecValue    => _specValue;
        public float SpecValueMax => _specValueMax;

        /// <summary>특수 효과가 열리는 숙련도. 두 구간이 여기서 갈린다.</summary>
        public const int SpecLevel = 5;

        /// <summary>
        /// **기본 축**의 값. Lv1 → Lv4 에 자라고, **Lv5 부터는 끝값에서 멈춘다.**
        ///
        /// ⚠ 두 축을 한 함수로 합치지 않는다. 예전에 `At(level)` 하나가
        ///   Lv5 미만이면 기본 축을, 이상이면 특수 축을 돌려줬다 — 그래서
        ///   아마존 숙련도를 5로 올리는 순간 대시 피해 배율이 ×2.2 에서
        ///   ×1.0(무적 초)으로 **떨어졌다.** 레벨을 올렸는데 약해지는 것이다.
        ///   축이 둘이면 읽는 곳도 둘이어야 한다.
        /// </summary>
        public float BaseAt(int level)
            => Lerp(_baseValue, _baseValueMax, Mathf.Clamp(level, 1, SpecLevel - 1),
                    1, SpecLevel - 1);

        /// <summary>
        /// **특수 축**의 값. Lv5 에 열려 Lv10 까지 자란다.
        /// **Lv5 미만이면 0** — 아직 열리지 않았다는 뜻이다.
        /// </summary>
        public float SpecAt(int level, int masteryMax)
        {
            if (level < SpecLevel) return 0f;
            if (masteryMax < SpecLevel) masteryMax = SpecLevel;
            return Lerp(_specValue, _specValueMax,
                        Mathf.Clamp(level, SpecLevel, masteryMax), SpecLevel, masteryMax);
        }

        /// <summary>특수 효과가 열렸는가.</summary>
        public static bool IsSpecOpen(int level) => level >= SpecLevel;

        /// <summary>구간 안에서의 선형 보간. 구간이 한 칸이면 시작값을 그대로 쓴다.</summary>
        private static float Lerp(float from, float to, int level, int levelFrom, int levelTo)
        {
            int span = levelTo - levelFrom;
            if (span <= 0) return from;
            return Mathf.Lerp(from, to, (float)(level - levelFrom) / span);
        }

        /// <summary>값이 하나도 안 채워졌는가. 채우기 전에는 스킬이 기본값으로 돈다.</summary>
        public bool IsEmpty =>
            _baseValue == 0f && _baseValueMax == 0f && _specValue == 0f && _specValueMax == 0f;
    }
}
