using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Lokman
{
    public class EventLogger : IEventLogger
    {
        internal static readonly Func<LogEvent, Exception, string> _formatter = (l, _) => l.ToString();
        internal static readonly ObjectPropertiesReaderFactory _objectReaderGenerator = new ObjectPropertiesReaderFactory();

        private readonly ILogger _innerLogger;
        private readonly IExternalScopeProvider _scopeProvider;

        public EventLogger(ILogger innerLogger, IExternalScopeProvider scopeProvider)
        {
            _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        }

        public IDisposable? BeginScope<TState>(TState state)
        {
            if (state is null)
                return default;
            return _scopeProvider.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel)
            => _innerLogger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _innerLogger.Log(logLevel, eventId, state, exception, formatter);

        public void LogEvent(LogLevel level, string eventName, Exception? exception, IReadOnlyDictionary<string, object>? data)
        {
            if (data is not Dictionary<string, object> values)
                values = data?.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);

            _scopeProvider.ForEachScope((scope, dict) => {
                if (scope is IEnumerable<KeyValuePair<string, object>> properties)
                {
                    foreach (var kv in properties)
                    {
                        dict[kv.Key] = kv.Value;
                    }
                }
                else if (scope != null)
                {
                    var reader = _objectReaderGenerator.GetReader(scope.GetType());
                    reader(dict, scope);
                }
            }, values);

            var logEvent = new LogEvent(eventName, values);
            _innerLogger.Log(level, default, logEvent, exception, _formatter);
        }
    }

    public class EventLogger<TCategoryName> : EventLogger, IEventLogger<TCategoryName>
    {
        public EventLogger(ILogger<TCategoryName> innerLogger, IExternalScopeProvider scopeProvider) : base(innerLogger, scopeProvider) { }
    }
}
