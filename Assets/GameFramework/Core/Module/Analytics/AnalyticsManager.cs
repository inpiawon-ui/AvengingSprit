using System.Collections.Generic;

namespace GameFramework.Core.Module.Analytics
{
    public sealed class AnalyticsManager : IAnalyticsManager
    {
        private readonly IAnalyticsBackend _backend;

        public AnalyticsManager(IAnalyticsBackend backend)
        {
            _backend = backend;
        }

        public void LogEvent(string eventName) =>
            _backend.LogEvent(eventName, null);

        public void LogEvent(string eventName, Dictionary<string, object> parameters) =>
            _backend.LogEvent(eventName, parameters);

        public void SetUserProperty(string key, string value) =>
            _backend.SetUserProperty(key, value);

        public void SetUserId(string userId) =>
            _backend.SetUserId(userId);
    }
}
