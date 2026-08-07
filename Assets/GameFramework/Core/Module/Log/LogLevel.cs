namespace GameFramework.Core.Module.Log
{
    public enum LogLevel
    {
        Debug   = 0,  // 개발 중 흐름 추적 — 릴리즈 차단
        Warning = 1,  // 비정상이지만 진행 가능 — 릴리즈 선택
        Error   = 2,  // 처리 실패 — 릴리즈에서도 항상 출력
    }
}
