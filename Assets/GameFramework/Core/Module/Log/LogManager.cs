using System.Collections.Generic;

namespace GameFramework.Core.Module.Log
{
    public sealed class LogManager : ILog
    {
        private readonly List<ILogHandler>      _handlers        = new();
        private readonly HashSet<string>        _disabledChannels = new();
        private LogLevel                        _minLevel        = LogLevel.Debug;

        // ──────────────────────────────────────────────
        // Handler 관리
        // ──────────────────────────────────────────────

        public void AddHandler(ILogHandler handler)
        {
            if (handler != null && !_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        public void RemoveHandler(ILogHandler handler) => _handlers.Remove(handler);

        // ──────────────────────────────────────────────
        // ILog
        // ──────────────────────────────────────────────

        public void Debug  (string msg, string channel = "General") => Write(LogLevel.Debug,   msg, channel);
        public void Warning(string msg, string channel = "General") => Write(LogLevel.Warning, msg, channel);
        public void Error  (string msg, string channel = "General") => Write(LogLevel.Error,   msg, channel);

        public void SetChannelEnabled(string channel, bool enabled)
        {
            if (enabled) _disabledChannels.Remove(channel);
            else         _disabledChannels.Add(channel);
        }

        public void SetMinLevel(LogLevel level) => _minLevel = level;

        // ──────────────────────────────────────────────
        // 내부
        // ──────────────────────────────────────────────

        private void Write(LogLevel level, string msg, string channel)
        {
            if (level < _minLevel)               return;
            if (_disabledChannels.Contains(channel)) return;

            var entry = new LogEntry(level, channel, msg);
            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i].Handle(entry);
        }
    }
}
