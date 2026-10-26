namespace Game.Module.Common
{
    /// <summary>
    /// 판에서 로비로 돌아갈 때 「로비에서 바로 열어 줄 화면」 — 씬을 건너 한 번만 쓰고 지운다(RunModeContext 와 같은 방식).
    /// 실패 결과 「성장 ›」 → 그 몸의 성장 화면(시안 result-fail 「가장 오래 쓴 몸」).
    /// </summary>
    public static class LobbyRequest
    {
        /// <summary>로비가 열리면 이 몸의 성장 화면을 연다. 비어 있으면 평소대로.</summary>
        public static string GrowthHostKey { get; set; }

        /// <summary>요청을 꺼내고 지운다.</summary>
        public static string TakeGrowthHost()
        {
            string key = GrowthHostKey;
            GrowthHostKey = null;
            return key;
        }
    }
}
