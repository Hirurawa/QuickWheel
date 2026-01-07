using System;
using System.IO;
using System.Diagnostics;
using QuickWheel.Interfaces;

namespace QuickWheel.Services
{
    public class FileLogger : ILogger
    {
        private readonly string _logPath = "app.log";

        public void Log(string message)
        {
#if DEBUG
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}";
                Console.WriteLine(line);
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch { /* Best effort */ }
#endif
        }

        public void LogError(string message, Exception? ex = null)
        {
#if DEBUG
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message} {ex?.Message}";
                Console.WriteLine(line);
                File.AppendAllText(_logPath, line + Environment.NewLine);
                if (ex != null)
                {
                    File.AppendAllText(_logPath, ex.StackTrace + Environment.NewLine);
                }
            }
            catch { /* Best effort */ }
#endif
        }
    }
}
