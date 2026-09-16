using GameFramework.Core.Common;

namespace Game.Module.Events
{
    /// <summary>상자 칸이 달라졌다. 화면은 이것만 받고 세 칸을 다시 그린다.</summary>
    public struct ChestChangedEvent : IEvent
    {
    }

    /// <summary>판이 끝나 상자를 받았다. 칸이 없어 못 받았으면 <see cref="Accepted"/> 가 false.</summary>
    public struct ChestGrantedEvent : IEvent
    {
        public string GrantedChestKey;
        public int Slot;
        public bool Accepted;
    }

    /// <summary>상자를 열었다.</summary>
    public struct ChestOpenedEvent : IEvent
    {
        public string OpenedChestKey;
        public int RewardGold;
        public int RewardSpiritCore;
        public int RewardHostMemory;
        public int RewardGem;
        public int RewardShards;
        public string RewardShardHostKey;
    }
}
