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
        /// <summary>이벤트 — 적이 없다. 대신 값을 묻는다</summary>
        Event,
        /// <summary>상점 — 모아 둔 골드를 쓰는 자리</summary>
        Shop,
        /// <summary>보스</summary>
        Boss,
    }

    /// <summary>이벤트 방에 들어섰다. UI 가 선택 창을 띄운다.</summary>
    public struct EventOfferEvent : IEvent
    {
        public string EventId;
        public string Title;
        public string Body;
        public string AcceptLabel;
        public string DeclineLabel;
        /// <summary>대가 한 줄. 없으면 빈 문자열이다.</summary>
        public string CostLabel;
        public string RewardLabel;
        /// <summary>대가를 못 치를 때 false — UI 가 수락 버튼을 잠근다.</summary>
        public bool CanAfford;

        /// <summary>못 고르는 이유. 비어 있으면 고를 수 있다.</summary>
        public string BlockedReason;
    }

    /// <summary>이벤트가 끝났다. 무슨 일이 있었는지 한 줄로 알린다.</summary>
    public struct EventResolvedEvent : IEvent
    {
        public string EventId;
        public bool Accepted;
        public string ResultLine;
    }

    /// <summary>상점에 들어섰다. UI 가 진열대를 띄운다.</summary>
    public struct ShopOpenedEvent : IEvent
    {
        /// <summary>진열된 물건 이름 (마지막 칸은 회복이다)</summary>
        public string[] Names;
        public string[] Descs;
        public int[] Prices;
        /// <summary>각 칸을 지금 살 수 있는가 (골드·구매 한도까지 본 결과)</summary>
        public bool[] CanBuy;
        /// <summary>칸마다 쓸 아이콘 이름(`buffcat_attack` …). 없으면 빈 문자열.</summary>
        public string[] Icons;
        public int Gold;
        public string LimitLine;
    }

    /// <summary>상점에서 하나를 샀다.</summary>
    public struct ShopPurchasedEvent : IEvent
    {
        public int Index;
        public string ResultLine;
    }

    /// <summary>판 안에서 쓰는 골드가 바뀌었다.</summary>
    public struct RunGoldChangedEvent : IEvent
    {
        public int Gold;
        public int Delta;

        /// <summary>
        /// 골드가 **어디서** 나왔는가 (월드 좌표). 동전이 그 자리에서 튀어
        /// HUD 로 날아간다 — 숫자만 늘면 무엇을 얻었는지가 안 읽힌다.
        ///
        /// 상점처럼 나가는 골드에는 자리가 없다(<see cref="HasSource"/> = false).
        /// 화면 좌표가 아니라 **월드 좌표**로 넘긴다 — 캔버스 렌더 모드가 바뀌어도
        /// 받는 쪽에서 `InverseTransformPoint` 한 번이면 끝난다.
        /// </summary>
        public UnityEngine.Vector3 SourceWorld;
        public bool HasSource;
    }

    public struct RoomEnteredEvent : IEvent
    {
        public int RoomIndex;      // 런 전체에서 몇 번째 방인가 (0-based)
        public int RoomTotal;      // 런 전체 방 수
        // 화면에는 **챕터와 그 안의 순번**을 보여 준다.
        // 런 통짜 번호(45 중 13)만 보이면 챕터가 어디서 갈리는지 알 수가 없다.
        public int Chapter;        // 1~3
        public int StageInChapter; // 챕터 안에서 몇 번째 방인가 (1-based)
        public int ChapterTotal;   // 이 챕터의 방 수
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
        /// <summary>영문 이름 — HUD 큰 글씨. 무기 구분은 붙이지 않는다.</summary>
        public string DisplayNameEn;
        /// <summary>한글 이름 — 영문 아래 작은 줄.</summary>
        public string DisplayNameKr;
        /// <summary>이 몸의 숙련도. HUD 의 LV 배지가 이 값을 쓴다.</summary>
        public int Mastery;
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
        /// <summary>지금 누르면 나갈 Ghost HP. 0 이면 공짜(유령 상태에서의 빙의)다.</summary>
        public int GhostCost;
        /// <summary>대상은 있는데 지금은 못 누른다 — 쿨다운 또는 Ghost HP 부족.</summary>
        public bool Blocked;
    }


    /// <summary>
    /// 보스가 다음 페이즈로 넘어갔다. 행동이 바뀌는 순간이라 화면이 알려야 한다.
    /// </summary>
    public struct BossPhaseEvent : IEvent
    {
        public int Phase;
        /// <summary>정본 AttackPattern 문자열. 표시·기록용.</summary>
        public string Pattern;
        public int MinionCount;
    }

    /// <summary>
    /// 전술 빙의 쿨다운이 흐른다. 남은 시간이 보이지 않으면 눌러 보고 나서야
    /// 못 쓴다는 것을 알게 된다.
    /// </summary>
    public struct RepossessLockEvent : IEvent
    {
        public float Remain;
        public float Total;
    }

    /// <summary>
    /// 룸을 클리어해 버프 3택1을 제시한다. 선택 전까지 전투는 멈춘다.
    /// 배열 길이는 3이지만 남은 버프가 모자라면 더 짧을 수 있다.
    /// </summary>
    /// <summary>
    /// 회복 제단이 열렸다. **셋 중 하나를 고른다 — 거절은 없다.**
    /// 셋 다 공짜라 안 고를 이유가 없고, 안 고르는 길을 두면 그 자리가 통로가 된다.
    /// </summary>
    public struct ShrineOpenedEvent : IEvent
    {
        public string[] Titles;
        public string[] Descs;
        /// <summary>칸마다 앞에 놓을 그림. 무엇을 주는지가 글보다 먼저 읽힌다.</summary>
        public string[] Icons;
    }

    /// <summary>제단에서 무엇을 받았는지 한 줄.</summary>
    public struct ShrineResolvedEvent : IEvent
    {
        public string ResultLine;
    }

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
        /// <summary>영구 재화 — 런이 끝나도 남는다 (정본 Growth Runtime).</summary>
        public int RewardSpiritCore;
        public int RewardHostMemory;
        public int RewardGem;
    }
}
