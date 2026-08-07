using UnityEngine;

namespace GameFramework.Core.Module.Log.Handlers
{
    /// <summary>
    /// Unity Console에 출력하는 핸들러.
    /// DEVELOPMENT_BUILD 또는 UNITY_EDITOR 에서만 Debug/Warning 출력.
    /// Error는 항상 출력.
    /// </summary>
    public sealed class UnityConsoleHandler : ILogHandler
    {
        public void Handle(LogEntry entry)
        {
            switch (entry.Level)
            {
                case LogLevel.Debug:
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log(entry.ToString());
#endif
                    break;
                case LogLevel.Warning:
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogWarning(entry.ToString());
#endif
                    break;
                case LogLevel.Error:
                    Debug.LogError(entry.ToString());
                    break;
            }
        }
    }
}
