namespace Game.Character
{
    /// <summary>
    /// 성급 → 스킬 위력 배율(기획 `_exchange/doc_skill_concept` 성급 성장 표, PD 2026-10-15).
    ///   액티브 단계마다 +6%(10성 ×1.54) · 필살기 단계마다 +4%(10성 ×1.36). 1성 = ×1.00, 0(미보유)도 ×1.00 으로 본다.
    /// 육성 화면이 읽는다. ⚠ 전투(`BattleDirector.Skills` 의 ActivePowerPerLevel · UltPowerPerLevel)와 같은 값이어야 한다 —
    /// 전투 쪽이 이 상수를 읽게 바꾸면 자가 하나가 된다(인게임 세션에 요청함).
    /// </summary>
    public static class SkillPower
    {
        public const float ActivePerLevel = 0.06f;
        public const float UltPerLevel = 0.04f;

        public static float Active(int mastery) => 1f + ActivePerLevel * System.Math.Max(0, mastery - 1);
        public static float Ult(int mastery) => 1f + UltPerLevel * System.Math.Max(0, mastery - 1);
    }
}
