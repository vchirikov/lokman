using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Lokman
{
    public interface IEventLogger : ILogger
    {
        /// <summary>
        /// Log event
        /// For example, <c>logger.LogEvent("JobFailed", ex, LogLevel.Critical, new Dictionary{string,object} { {"val1" = command.InputData} })</c>
        /// </summary>
        void LogEvent(LogLevel level, string eventName, Exception? exception, IReadOnlyDictionary<string, object>? data);
    }

    public interface IEventLogger<TCategoryName> : ILogger<TCategoryName>, IEventLogger
    {
    }

    public static class IEventLoggerExtensions
    {
        public static void LogEvent(this IEventLogger logger, LogLevel level, string eventName, Exception? exception, object dataObj)
        {
            var reader = EventLogger._objectReaderGenerator.GetReader(dataObj.GetType());
            var dict = new Dictionary<string, object>();
            reader(dict, dataObj);
            logger.LogEvent(level, eventName, exception, data: dict);
        }

        public static void LogEvent(this IEventLogger logger, LogLevel level, string eventName, object dataObj)
            => logger.LogEvent(level, eventName, exception: null, dataObj: dataObj);

        public static void LogEvent(this IEventLogger logger, LogLevel level, string eventName, IReadOnlyDictionary<string, object>? data)
            => logger.LogEvent(level, eventName, exception: null, data: data);

        public static void InformationEvent(this IEventLogger logger, string eventName, object data)
            => logger.LogEvent(LogLevel.Information, eventName, exception: null, data);

        public static void DebugEvent(this IEventLogger logger, string eventName, object data)
            => logger.LogEvent(LogLevel.Debug, eventName, exception: null, data);

        public static void WarningEvent(this IEventLogger logger, string eventName, object data)
            => logger.LogEvent(LogLevel.Warning, eventName, exception: null, data);

        public static void WarningEvent(this IEventLogger logger, Exception exception, string eventName, object data)
            => logger.LogEvent(LogLevel.Warning, eventName, exception, data);

        public static void ErrorEvent(this IEventLogger logger, string eventName, object data)
            => logger.LogEvent(LogLevel.Error, eventName, exception: null, data);

        public static void ErrorEvent(this IEventLogger logger, Exception exception, string eventName, object data)
            => logger.LogEvent(LogLevel.Error, eventName, exception, data);

        public static void CriticalEvent(this IEventLogger logger, string eventName, object data)
            => logger.LogEvent(LogLevel.Critical, eventName, exception: null, data);

        public static void CriticalEvent(this IEventLogger logger, Exception exception, string eventName, object data)
            => logger.LogEvent(LogLevel.Critical, eventName, exception, data);

        // ToDo: add IReadOnlyDictionary methods
    }
}
