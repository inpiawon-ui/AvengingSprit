using System.Collections.Generic;

namespace GameFramework.Core.Module.Analytics.Backends
{
    /// <summary>아무 동작도 하지 않는 기본 백엔드. 프로덕션에서는 Firebase 등으로 교체하세요.</summary>
    public sealed class NoOpAnalyticsBackend : IAnalyticsBackend
    {
        public void LogEvent      (string eventName, Dictionary<string, object> parameters) { }
        public void SetUserProperty(string key, string value) { }
        public void SetUserId     (string userId) { }
    }
}
