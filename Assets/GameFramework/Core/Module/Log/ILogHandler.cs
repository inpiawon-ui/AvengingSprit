namespace GameFramework.Core.Module.Log
{
    public interface ILogHandler
    {
        void Handle(LogEntry entry);
    }
}