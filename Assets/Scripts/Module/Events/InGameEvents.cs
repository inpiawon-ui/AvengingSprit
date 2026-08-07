using GameFramework.Core.Common;

namespace Game.Module.Events
{
    /// <summary>룸에 입장했다. `RoomIndex` 는 0-base, `IsBossRoom` 이면 마지막 룸이다.</summary>
    public struct RoomEnteredEvent : IEvent
    {
        public int RoomIndex;
        public int RoomTotal;
        public bool IsBossRoom;
    }

    /// <summary>룸의 적이 전멸했다.</summary>
    public struct RoomClearedEvent : IEvent
    {
        public int ClearedRoomIndex;
        public bool IsLastRoom;
    }

    /// <summary>고스트가 호스트에 빙의했다.</summary>
    public struct PossessedEvent : IEvent
    {
        public string PossessedHostKey;
        public string DisplayName;
        public int HostHpMax;
    }

    /// <summary>호스트가 파괴되어 고스트로 돌아왔다.</summary>
    public struct HostLostEvent : IEvent
    {
        public string LostHostKey;
    }

    /// <summary>고스트 또는 호스트의 체력이 변했다.</summary>
    public struct CombatHpChangedEvent : IEvent
    {
        public int GhostHp;
        public int GhostHpMax;
        public int HostHp;
        public int HostHpMax;
        public bool HasHost;
    }

    /// <summary>보스 체력이 변했다. `Max` 가 0 이면 보스가 없다(게이지 숨김).</summary>
    public struct BossHpChangedEvent : IEvent
    {
        public int BossHp;
        public int BossHpMax;
    }

    /// <summary>빙의 가능한 대상이 사거리에 들어오거나 벗어났다.</summary>
    public struct PossessTargetChangedEvent : IEvent
    {
        public bool HasTarget;
    }

    /// <summary>스테이지가 끝났다. 클리어·전멸 공통.</summary>
    public struct StageFinishedEvent : IEvent
    {
        public bool IsCleared;
        public int RewardGold;
        public int RewardGhostExp;
    }
}
