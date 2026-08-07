using System.Collections.Generic;

namespace GameFramework.Core.Module.Analytics
{
    public interface IAnalyticsBackend
    {
        void LogEvent      (string eventName, Dictionary<string, object> parameters);
        void SetUserProperty(string key, string value);
        void SetUserId     (string userId);
    }
}
