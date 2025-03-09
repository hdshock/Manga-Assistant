using System;
using MangaAssistant.Common.Logging;
using MangaAssistant.Core.Interfaces;
using LogLevel = MangaAssistant.Core.Interfaces.LogLevel;

namespace MangaAssistant.Infrastructure.Logging
{
    /// <summary>
    /// Adapter class that implements ILogger interface by wrapping the static Logger class
    /// </summary>
    public class LoggerAdapter : ILogger
    {
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            // Map Core.Interfaces.LogLevel to Common.Logging.LogLevel
            var commonLogLevel = MapLogLevel(level);
            Common.Logging.Logger.Log(message, commonLogLevel);
        }

        public void LogException(Exception ex, string? message = null, LogLevel level = LogLevel.Error)
        {
            var commonLogLevel = MapLogLevel(level);
            
            if (string.IsNullOrEmpty(message))
            {
                Common.Logging.Logger.Log($"Exception: {ex.Message}\n{ex.StackTrace}", commonLogLevel);
            }
            else
            {
                Common.Logging.Logger.Log($"{message}: {ex.Message}\n{ex.StackTrace}", commonLogLevel);
            }
        }

        private Common.Logging.LogLevel MapLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => Common.Logging.LogLevel.Debug,
                LogLevel.Info => Common.Logging.LogLevel.Info,
                LogLevel.Warning => Common.Logging.LogLevel.Warning,
                LogLevel.Error => Common.Logging.LogLevel.Error,
                LogLevel.Critical => Common.Logging.LogLevel.Critical,
                _ => Common.Logging.LogLevel.Info
            };
        }
    }
}
