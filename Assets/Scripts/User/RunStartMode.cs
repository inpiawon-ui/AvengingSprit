namespace Game.User
{
    /// <summary>
    /// 판을 어떻게 시작했나(기획서 `AVSR_Content_Store.md` B-12 「출전 방식」).
    /// 로비가 정하고 판이 끝날 때 클리어 보너스가 읽는다. 저장하지 않는다.
    /// </summary>
    public enum RunStartMode
    {
        /// <summary>몸 골라 — 행동력 + 골드 출전비(오늘의 몸은 무료).</summary>
        Host,
        /// <summary>? 랜덤 — 행동력만. 깨면 행운 보너스(조각 · 골드).</summary>
        Random,
        /// <summary>유령 — 행동력만. 깨면 유령 경험치 보너스.</summary>
        Ghost,
    }
}
