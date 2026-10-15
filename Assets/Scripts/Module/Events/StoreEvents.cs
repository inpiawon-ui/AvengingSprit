using GameFramework.Core.Common;

namespace Game.Module.Events
{
    /// <summary>로비 상점이 바뀌었다(표가 왔다 · 샀다 · 계약을 받았다). 상점 화면이 다시 그린다.</summary>
    public struct StoreChangedEvent : IEvent
    {
    }

    /// <summary>시즌 패스 · 미션 · 프로필이 바뀌었다(경험치 · 받기 · 진행 · 장착). 패스 화면 · 로비 배지가 다시 그린다.</summary>
    public struct SeasonChangedEvent : IEvent
    {
    }

    /// <summary>육성에서 능력치를 올렸다 — 미션 「강화」.</summary>
    public struct HostStatUpgradedEvent : IEvent
    {
        public string UpgradedHostKey;
        public bool IsGhostTab;
    }

    /// <summary>
    /// 적을 쓰러뜨렸다 — 미션 「유리한 상성 처치」 · 「보스 처치」. 인게임(`BattleDirector`)이 발행한다.
    /// ⚠ 판 안에서 자주 나온다 — 받는 쪽은 카운터만 올리고 저장하지 않는다(B-5).
    /// </summary>
    public struct EnemyKilledEvent : IEvent
    {
        public string KilledKey;
        public bool WithAdvantage;
        public bool WasBoss;
    }
}
