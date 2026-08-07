namespace GameFramework.Core.Module.Log
{
    public interface ILog
    {
        void Debug  (string msg, string channel = "General");
        void Warning(string msg, string channel = "General");
        void Error  (string msg, string channel = "General");

        void SetChannelEnabled(string channel, bool enabled);
        void SetMinLevel(LogLevel level);
    }
}
