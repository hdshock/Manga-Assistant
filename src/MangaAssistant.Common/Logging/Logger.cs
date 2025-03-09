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
        private static string _logFilePath = string.Empty;
        private static string _logDirectory = string.Empty;
        private static bool _initialized = false;
        private static readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private static readonly int MaxLogEntries = 5000; // Increased from 1000 to 5000

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

                Log("Logger initialized", LogLevel.Info, "Logger");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info, string category = "", string context = "", Dictionary<string, string>? tags = null)
        {
            if (!_initialized) return;

            try
            {
                var timestamp = DateTime.Now;
                var logEntry = CreateLogEntry(message, level, timestamp, category, context, tags);
                
                lock (_lock)
                {
                    // Write to file
                    File.AppendAllText(_logFilePath, logEntry.FullText + Environment.NewLine);
                    
                    // Add to in-memory log entries
                    _logEntries.Add(logEntry);
                    
                    // Keep only the most recent entries
                    while (_logEntries.Count > MaxLogEntries)
                    {
                        _logEntries.RemoveAt(0);
                    }
                    
                    // Raise the LogAdded event
                    OnLogAdded(new LogEventArgs(message, level, timestamp, category, context, tags));
                }

                // Write to debug output
                Debug.WriteLine(logEntry.FullText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log message: {ex.Message}");
            }
        }

        public static async Task LogAsync(string message, LogLevel level = LogLevel.Info, string category = "", string context = "", Dictionary<string, string>? tags = null)
        {
            if (!_initialized) return;

            try
            {
                var timestamp = DateTime.Now;
                var logEntry = CreateLogEntry(message, level, timestamp, category, context, tags);

                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        // Write to file
                        File.AppendAllText(_logFilePath, logEntry.FullText + Environment.NewLine);
                        
                        // Add to in-memory log entries
                        _logEntries.Add(logEntry);
                        
                        // Keep only the most recent entries
                        while (_logEntries.Count > MaxLogEntries)
                        {
                            _logEntries.RemoveAt(0);
                        }
                        
                        // Raise the LogAdded event
                        OnLogAdded(new LogEventArgs(message, level, timestamp, category, context, tags));
                    }
                });

                // Write to debug output
                Debug.WriteLine(logEntry.FullText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log message asynchronously: {ex.Message}");
            }
        }

        public static void LogException(Exception ex, string context = "", string category = "", Dictionary<string, string>? tags = null)
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

            var exceptionTags = new Dictionary<string, string>
            {
                { "ExceptionType", ex.GetType().Name },
                { "Source", ex.Source ?? "Unknown" }
            };

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    exceptionTags[tag.Key] = tag.Value;
                }
            }

            Log(sb.ToString(), LogLevel.Error, category, context, exceptionTags);
        }
        
        // Methods for the LogWindow
        public static List<LogEntry> GetLogs(int maxLines = 5000)
        {
            if (!_initialized) return new List<LogEntry>();
            
            lock (_lock)
            {
                return _logEntries.Take(maxLines).ToList();
            }
        }
        
        public static async Task<List<LogEntry>> GetLogsAsync(int maxLines = 5000)
        {
            if (!_initialized) return new List<LogEntry>();
            
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _logEntries.Take(maxLines).ToList();
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
                Log("Logs cleared by user", LogLevel.Info, "Logger");
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
                    Log("Logs cleared by user", LogLevel.Info, "Logger");
                }
            });
        }
        
        // Event for real-time log updates
        public static event EventHandler<LogEventArgs>? LogAdded;
        
        private static void OnLogAdded(LogEventArgs e)
        {
            LogAdded?.Invoke(null, e);
        }

        private static LogEntry CreateLogEntry(string message, LogLevel level, DateTime timestamp, string category, string context, Dictionary<string, string>? tags)
        {
            var fullText = $"[{level.ToString().ToUpper()}] {timestamp:yyyy-MM-dd HH:mm:ss}";
            if (!string.IsNullOrEmpty(category))
            {
                fullText += $" [{category}]";
            }
            fullText += $" {message}";
            if (!string.IsNullOrEmpty(context))
            {
                fullText += $" (Context: {context})";
            }
            if (tags != null && tags.Count > 0)
            {
                fullText += $" Tags: {string.Join(", ", tags.Select(t => $"{t.Key}={t.Value}"))}";
            }

            return new LogEntry
            {
                Level = level.ToString().ToUpper(),
                Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                Message = message,
                FullText = fullText,
                Category = category,
                Context = context,
                Tags = tags ?? new Dictionary<string, string>()
            };
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
        public string Category { get; }
        public string Context { get; }
        public Dictionary<string, string>? Tags { get; }
        
        public LogEventArgs(string message, LogLevel level, DateTime timestamp, string category = "", string context = "", Dictionary<string, string>? tags = null)
        {
            Message = message;
            Level = level;
            Timestamp = timestamp;
            Category = category;
            Context = context;
            Tags = tags;
        }
    }
    
    public class LogEntry
    {
        public string Level { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}
