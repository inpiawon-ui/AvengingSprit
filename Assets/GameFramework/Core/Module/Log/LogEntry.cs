using System;

namespace GameFramework.Core.Module.Log
{
    public readonly struct LogEntry
    {
        public readonly LogLevel  Level;
        public readonly string    Channel;
        public readonly string    Message;
        public readonly DateTime  Timestamp;

        public LogEntry(LogLevel level, string channel, string message)
        {
            Level     = level;
            Channel   = channel;
            Message   = message;
            Timestamp = DateTime.Now;
        }

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss.fff}][{Level}][{Channel}] {Message}";
    }
}
