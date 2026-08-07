namespace GameFramework.Core.Module.EventBus
{
    public enum EventPriority
    {
        Lowest  = 0,    // 애널리틱스·후처리 — 항상 마지막
        Low     = 50,   // UI 업데이트 — 로직 처리 후 반영
        Normal  = 100,  // 기본값 — 대부분의 게임 로직
        High    = 200,  // 유효성 검사, 선처리 필요 시스템
        Highest = 300,  // LogModule — 모든 이벤트 가장 먼저 기록
    }
}
