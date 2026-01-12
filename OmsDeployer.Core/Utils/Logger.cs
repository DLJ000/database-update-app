using System;
using System.IO;

namespace OmsDeployer.Core
{
    public class Logger
    {
        private readonly string _logPath;
        private readonly object _lock = new object();

        public Logger()
        {
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logPath = Path.Combine(logsDir, $"deploy_{timestamp}.log");
        }

        public void Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_logPath, logEntry + Environment.NewLine);
            }
        }
    }
}

