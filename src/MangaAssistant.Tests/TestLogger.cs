using System;
using System.IO;

namespace MangaAssistant.Tests
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static class TestLogger
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

                _logFilePath = Path.Combine(logDirectory, $"MangaAssistant_Test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                _initialized = true;

                Log("TestLogger initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!_initialized)
            {
                Console.WriteLine(message);
                return;
            }

            try
            {
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
                Console.WriteLine(logMessage);

                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log: {ex.Message}");
            }
        }

        public static void LogException(Exception ex, string context)
        {
            Log($"Exception in {context}: {ex.Message}", LogLevel.Error);
            if (ex.StackTrace != null)
            {
                Log($"Stack trace: {ex.StackTrace}", LogLevel.Error);
            }
        }
    }
}
