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

    /// <summary>상자를 열었다. 보상 목록 창이 이것을 받아 카드로 늘어놓는다.</summary>
    public struct ChestOpenedEvent : IEvent
    {
        public string OpenedChestKey;
        public int RewardGold;
        /// <summary>조각을 받은 호스트들. <see cref="RewardShardCounts"/> 와 짝이다.</summary>
        public string[] RewardShardHostKeys;
        public int[] RewardShardCounts;
    }
}
