using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MangaAssistant.Common.Logging
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;
        private static bool _initialized = false;

        public static void Initialize(string logDirectory)
        {
            if (_initialized) return;

            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                _logFilePath = Path.Combine(logDirectory, $"MangaAssistant_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                _initialized = true;

                Log("Logger initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!_initialized) return;

            try
            {
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
                Debug.WriteLine(logMessage);

                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log message: {ex.Message}");
            }
        }

        public static void LogException(Exception ex, string context = "")
        {
            if (!_initialized) return;

            var sb = new StringBuilder();
            sb.AppendLine($"Exception in {context}:");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Source: {ex.Source}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine("Inner Exception:");
                sb.AppendLine($"Message: {ex.InnerException.Message}");
                sb.AppendLine($"StackTrace: {ex.InnerException.StackTrace}");
            }

            Log(sb.ToString(), LogLevel.Error);
        }

        public static async Task LogAsync(string message, LogLevel level = LogLevel.Info)
        {
            if (!_initialized) return;

            try
            {
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
                Debug.WriteLine(logMessage);

                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log message asynchronously: {ex.Message}");
            }
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}
