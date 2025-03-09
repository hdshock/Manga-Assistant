using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaAssistant.Common.Logging
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;
        private static string _logDirectory;
        private static bool _initialized = false;
        private static readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private static readonly int MaxLogEntries = 1000;

        public static void Initialize(string logDirectory)
        {
            if (_initialized) return;

            try
            {
                _logDirectory = logDirectory;
                
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
                var timestamp = DateTime.Now;
                var logMessage = $"[{level.ToString().ToUpper()}] {timestamp:yyyy-MM-dd HH:mm:ss} {message}";
                Debug.WriteLine(logMessage);

                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                    
                    // Add to in-memory log entries for the LogWindow
                    _logEntries.Add(new LogEntry
                    {
                        Level = level.ToString().ToUpper(),
                        Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        Message = message,
                        FullText = logMessage
                    });
                    
                    // Keep only the most recent entries
                    if (_logEntries.Count > MaxLogEntries)
                    {
                        _logEntries.RemoveAt(0);
                    }
                    
                    // Raise the LogAdded event
                    OnLogAdded(new LogEventArgs(message, level, timestamp));
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
                var timestamp = DateTime.Now;
                var logMessage = $"[{level.ToString().ToUpper()}] {timestamp:yyyy-MM-dd HH:mm:ss} {message}";
                Debug.WriteLine(logMessage);

                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                        
                        // Add to in-memory log entries for the LogWindow
                        _logEntries.Add(new LogEntry
                        {
                            Level = level.ToString().ToUpper(),
                            Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            Message = message,
                            FullText = logMessage
                        });
                        
                        // Keep only the most recent entries
                        if (_logEntries.Count > MaxLogEntries)
                        {
                            _logEntries.RemoveAt(0);
                        }
                        
                        // Raise the LogAdded event
                        OnLogAdded(new LogEventArgs(message, level, timestamp));
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log message asynchronously: {ex.Message}");
            }
        }
        
        // Methods for the LogWindow
        public static List<string> GetLogs(int maxLines = 1000)
        {
            if (!_initialized) return new List<string>();
            
            lock (_lock)
            {
                return _logEntries.Select(e => e.FullText).ToList();
            }
        }
        
        public static async Task<List<string>> GetLogsAsync(int maxLines = 1000)
        {
            if (!_initialized) return new List<string>();
            
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _logEntries.Select(e => e.FullText).ToList();
                }
            });
        }
        
        public static void ClearLogs()
        {
            if (!_initialized) return;
            
            lock (_lock)
            {
                _logEntries.Clear();
                
                // Clear the log file
                File.WriteAllText(_logFilePath, string.Empty);
                
                // Add a log entry indicating logs were cleared
                Log("Logs cleared by user", LogLevel.Info);
            }
        }
        
        public static async Task ClearLogsAsync()
        {
            if (!_initialized) return;
            
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _logEntries.Clear();
                    
                    // Clear the log file
                    File.WriteAllText(_logFilePath, string.Empty);
                    
                    // Add a log entry indicating logs were cleared
                    Log("Logs cleared by user", LogLevel.Info);
                }
            });
        }
        
        // Event for real-time log updates
        public static event EventHandler<LogEventArgs> LogAdded;
        
        private static void OnLogAdded(LogEventArgs e)
        {
            LogAdded?.Invoke(null, e);
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
    
    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogLevel Level { get; }
        public DateTime Timestamp { get; }
        
        public LogEventArgs(string message, LogLevel level, DateTime timestamp)
        {
            Message = message;
            Level = level;
            Timestamp = timestamp;
        }
    }
    
    public class LogEntry
    {
        public string Level { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
    }
}
