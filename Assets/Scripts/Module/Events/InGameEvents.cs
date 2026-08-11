using GameFramework.Core.Common;

namespace Game.Module.Events
{
    /// <summary>룸에 입장했다. `RoomIndex` 는 0-base, `IsBossRoom` 이면 마지막 룸이다.</summary>
    /// <summary>
    /// 방의 성격 (기획서 A 06 ROOM TYPE).
    ///
    /// 기획서의 `분기 선택`은 넣지 않았다. 지금 진행이 스테이지 3개 직선이라
    /// 갈림길을 놓을 자리가 없다. 챕터가 길어지면 그때 되살린다.
    /// </summary>
    public enum RoomKind
    {
        /// <summary>일반 전투</summary>
        Normal,
        /// <summary>정예 — 적이 적게 나오지만 하나하나가 세다</summary>
        Elite,
        /// <summary>회복·보상 — 적이 없다</summary>
        Rest,
        /// <summary>보스</summary>
        Boss,
    }

    public struct RoomEnteredEvent : IEvent
    {
        public int RoomIndex;
        public int RoomTotal;
        public bool IsBossRoom;
        public RoomKind Kind;
    }

    /// <summary>빙의할 대상이 없어 긴급 호스트가 나왔다 (기획서 A 8-3).</summary>
    public struct EmergencyHostEvent : IEvent
    {
        public string HostKey;
        public int GhostCost;
    }

    /// <summary>방을 비워 출구가 열렸다. 통과해야 다음 스테이지로 넘어간다.</summary>
    public struct ExitOpenedEvent : IEvent
    {
        public int StageIndex;
    }

    /// <summary>런 EXP·레벨이 변했다. 게이지 표시용.</summary>
    public struct RunExpChangedEvent : IEvent
    {
        public int Level;
        public int Exp;
        public int ExpToNext;
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
        /// <summary>비어 있으면 이름 표기를 건드리지 않는다(체력만 갱신하는 호출).</summary>
        public string BossName;
        /// <summary>1~3. 0 이면 페이즈 표기를 건드리지 않는다.</summary>
        public int Phase;
    }

    /// <summary>빙의 가능한 대상이 사거리에 들어오거나 벗어났다.</summary>
    public struct PossessTargetChangedEvent : IEvent
    {
        public bool HasTarget;
    }

    /// <summary>
    /// 룸을 클리어해 버프 3택1을 제시한다. 선택 전까지 전투는 멈춘다.
    /// 배열 길이는 3이지만 남은 버프가 모자라면 더 짧을 수 있다.
    /// </summary>
    public struct BuffOfferEvent : IEvent
    {
        public string[] OfferedKeys;
        /// <summary>이번에 오른 레벨. 화면이 "무엇 때문에 열렸는지"를 말할 수 있어야 한다.</summary>
        public int Level;
    }

    /// <summary>버프를 골랐다. 화면을 닫고 다음 룸으로 넘어간다.</summary>
    public struct BuffChosenEvent : IEvent
    {
        public string ChosenKey;
        public int TotalBuffCount;
    }

    /// <summary>스테이지가 끝났다. 클리어·전멸 공통.</summary>
    public struct StageFinishedEvent : IEvent
    {
        public bool IsCleared;
        public int RewardGold;
        public int RewardGhostExp;
    }
}
