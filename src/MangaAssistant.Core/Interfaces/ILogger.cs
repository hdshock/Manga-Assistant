using System;

namespace MangaAssistant.Core.Interfaces
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public interface ILogger
    {
        void Log(string message, LogLevel level = LogLevel.Info);
        void LogException(Exception ex, string? message = null, LogLevel level = LogLevel.Error);
    }
}
