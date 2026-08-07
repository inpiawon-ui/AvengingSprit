using GameFramework.Core.Common;

namespace Game.Module.Events
{
    // IEvent 는 public struct 로 선언한다. readonly struct 로 하면 객체 초기화 구문에서
    // CS0191 이 난다 (coding_conventions.md 1절).
    // 필드명은 발행자 프로퍼티명과 다르게 짓는다 — { Gold = Gold } 형태를 피하기 위함.

    /// <summary>유저 데이터가 로드되어 사용 가능해졌다. UI 초기 표시의 기준 시점.</summary>
    public struct UserDataReadyEvent : IEvent
    {
        public int LoadedGhostLevel;
    }

    /// <summary>재화가 변했다. 상단 HUD 갱신용.</summary>
    public struct CurrencyChangedEvent : IEvent
    {
        public int NewStamina;
        public int NewGold;
        public int NewGem;
    }

    /// <summary>고스트 레벨·EXP 가 변했다.</summary>
    public struct GhostProgressChangedEvent : IEvent
    {
        public int NewLevel;
        public int NewExp;
        public int NewExpMax;
    }

    /// <summary>챕터·스테이지 진행도가 변했다. 호스트 해금이 여기서 갈린다.</summary>
    public struct ProgressChangedEvent : IEvent
    {
        public int NewChapter;
        public int NewStage;
        public int NewClearedChapter;
    }

    /// <summary>선택 호스트가 바뀌었다. 상세 패널 갱신용.</summary>
    public struct HostSelectedEvent : IEvent
    {
        public string SelectedKey;
        public bool IsUnlockedHost;
    }
}
