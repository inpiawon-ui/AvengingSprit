namespace Game.Module.Common.Chest
{
    /// <summary>칸 하나가 지금 어떤 상태인지. 화면은 이것만 보고 그린다.</summary>
    public readonly struct ChestSlotState
    {
        /// <summary>빈 칸.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(ChestKey);
        /// <summary>시간이 다 돼 열 수 있다.</summary>
        public bool IsReady => !IsEmpty && RemainSeconds <= 0;

        public readonly string ChestKey;
        public readonly int RemainSeconds;
        public readonly int TotalSeconds;
        /// <summary>지금 즉시 열려면 드는 젬. 비었거나 이미 완료면 0.</summary>
        public readonly int GemCost;

        public ChestSlotState(string chestKey, int remainSeconds, int totalSeconds, int gemCost)
        {
            ChestKey = chestKey;
            RemainSeconds = remainSeconds;
            TotalSeconds = totalSeconds;
            GemCost = gemCost;
        }
    }

    /// <summary>
    /// 열어서 받은 것. 화면(보상 카드 목록)에 그대로 늘어놓는다.
    /// 조각은 호스트마다 한 줄 — <see cref="ShardHostKeys"/> 와 <see cref="ShardCounts"/> 가 짝이다.
    /// </summary>
    public readonly struct ChestReward
    {
        public readonly string ChestKey;
        public readonly int Gold;
        public readonly string[] ShardHostKeys;
        public readonly int[] ShardCounts;

        public ChestReward(string chestKey, int gold, string[] shardHostKeys, int[] shardCounts)
        {
            ChestKey = chestKey;
            Gold = gold;
            ShardHostKeys = shardHostKeys ?? System.Array.Empty<string>();
            ShardCounts = shardCounts ?? System.Array.Empty<int>();
        }
    }

    public interface IChestService
    {
        /// <summary>칸 수. 화면이 3칸으로 그려져 있다.</summary>
        int SlotCount { get; }

        ChestSlotState Get(int slot);

        /// <summary>
        /// 예전: 빈 칸에 담고 시간을 셌다. **2026-10-17 로비 상자 칸 폐지** — 지금은 그 자리에서 바로 열어 지급한다(slot = -1).
        /// 챕터 클리어는 <see cref="GrantChapterClear"/> 를 쓴다.
        /// </summary>
        bool TryGrant(string chestKey, out int slot);

        /// <summary>그 등급 상자의 골드 범위(챕터 창 「클리어 보상」 표시용). 표에 없으면 (0, 0).</summary>
        (int min, int max) GoldRange(string chestKey);

        /// <summary>그 등급 상자의 표 줄(골드 · 조각 범위 · 등급 무게) — 상점 「확률 보기」. 표에 없으면 null.</summary>
        ChestEntry Entry(string chestKey);

        /// <summary>
        /// 챕터 클리어 보상(PD 2026-10-17 「상자 대신 만난 호스트 조각 + 골드」) — 그 챕터 등급 상자의 골드 · 조각 총량을
        /// **이번 판에서 만난 호스트**에게 나눠 바로 지급한다. 만난 몸이 없으면 예전 상자처럼 고를 수 있는 몸에서 뽑는다.
        /// </summary>
        bool GrantChapterClear(string chestKey, System.Collections.Generic.IReadOnlyList<string> metHostKeys, out ChestReward reward);

        /// <summary>젬을 치르고 즉시 완료로. 젬이 모자라거나 열 게 없으면 false.</summary>
        bool TryOpenNow(int slot);

        /// <summary>완료된 칸을 열어 보상을 지급하고 칸을 비운다.</summary>
        bool TryClaim(int slot, out ChestReward reward);

        /// <summary>
        /// 칸을 안 거치고 바로 열어 보상을 지급한다 — 상점에서 산 상자(기획서 B-3). 굴리기는 칸 열기와 같다.
        /// 표에 없는 상자면 false.
        /// </summary>
        bool TryOpenImmediate(string chestKey, out ChestReward reward);

        /// <summary>표에 그런 상자가 있는가. 인게임이 상자를 고를 때 쓴다.</summary>
        bool Has(string chestKey);

        /// <summary>그 상자의 아틀라스 스프라이트 이름. 없으면 빈 문자열.</summary>
        string SpriteOf(string chestKey);
    }
}
