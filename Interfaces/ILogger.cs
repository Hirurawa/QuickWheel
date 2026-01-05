namespace QuickWheel.Interfaces
{
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message, System.Exception ex = null);
    }
}
