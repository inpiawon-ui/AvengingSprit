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

    /// <summary>열어서 받은 것. 화면에 뭘 받았는지 보여 주려고 그대로 돌려준다.</summary>
    public readonly struct ChestReward
    {
        public readonly int Gold, SpiritCore, HostMemory, Gem, Shards;
        public readonly string ShardHostKey;

        public ChestReward(int gold, int core, int memory, int gem, int shards, string shardHostKey)
        {
            Gold = gold;
            SpiritCore = core;
            HostMemory = memory;
            Gem = gem;
            Shards = shards;
            ShardHostKey = shardHostKey;
        }
    }

    public interface IChestService
    {
        /// <summary>칸 수. 화면이 3칸으로 그려져 있다.</summary>
        int SlotCount { get; }

        ChestSlotState Get(int slot);

        /// <summary>빈 칸에 담고 그 순간부터 시간을 센다. 칸이 다 차면 false.</summary>
        bool TryGrant(string chestKey, out int slot);

        /// <summary>젬을 치르고 즉시 완료로. 젬이 모자라거나 열 게 없으면 false.</summary>
        bool TryOpenNow(int slot);

        /// <summary>완료된 칸을 열어 보상을 지급하고 칸을 비운다.</summary>
        bool TryClaim(int slot, out ChestReward reward);

        /// <summary>표에 그런 상자가 있는가. 인게임이 상자를 고를 때 쓴다.</summary>
        bool Has(string chestKey);

        /// <summary>그 상자의 아틀라스 스프라이트 이름. 없으면 빈 문자열.</summary>
        string SpriteOf(string chestKey);
    }
}
