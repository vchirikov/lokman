using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Lokman.Client
{
    /// <inheritdoc cref="BrowserConsoleLogger"/>
    public class BrowserConsoleLogger<TCategoryName> : BrowserConsoleLogger, ILogger<TCategoryName>
    {
        public BrowserConsoleLogger(ILoggingInterop jsInterop) : base(jsInterop, typeof(TCategoryName).FullName) { }
    }

    /// <summary>
    /// The better implementation of console logging
    /// than <see cref="Microsoft.AspNetCore.Components.WebAssembly.Services.WebAssemblyConsoleLogger{T}"/>
    /// The main added feature are supporting <see cref="IEventLogger"/>, testing via injected service <see cref="ILoggingInterop"/>
    /// and inserting a object in logging message
    /// </summary>
    public class BrowserConsoleLogger : ILogger
    {
        private readonly ILoggingInterop _jsInterop;
        private readonly string _category;
        private static readonly JsLoggerValuesFormatter _loggerValuesFormatter = new JsLoggerValuesFormatter();

        public BrowserConsoleLogger(ILoggingInterop jsInterop, string category)
        {
            _jsInterop = jsInterop;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            // Event logging support
            if (state is LogEvent logEvent)
            {
                _jsInterop.LogAsync(logLevel, "Event ", logEvent._eventName, ":");
                _jsInterop.LogAsync(LogLevel.None, logEvent._data);
                return;
            }
            // hot path - FormattedLogValues
            if (state is IReadOnlyList<KeyValuePair<string, object>> list)
            {
                var last = list[^1];
                // {OriginalFormat} expected in the last KV-pair of FormattedLogValues
                if (last.Key == "{OriginalFormat}")
                {
                    // with FormattedLogValues will be "[null]" if format is null
                    var format = last.Value.ToString();
                    // For debug:
                    // Console.WriteLine($"Format: \"{format}\" Dump: {string.Join(", ", list.Select(x => $"{x.Key} = {x.Value ?? "(null)"} "))}");
                    SendLogIntoJs(logLevel, exception, list, format);
                    return;
                }
            }
            else if (state is IEnumerable<KeyValuePair<string, object>> tuples)
            {
                var format = tuples.FirstOrDefault(x => x.Key.Equals("{OriginalFormat}", StringComparison.Ordinal)).Value?.ToString();
                if (!string.IsNullOrWhiteSpace(format))
                {
                    SendLogIntoJs(logLevel, exception, tuples, format);
                    return;
                }
            }
            var msg = formatter(state, exception);
            _jsInterop.LogAsync(logLevel, msg);

            void SendLogIntoJs(LogLevel logLevel, Exception exception, IEnumerable<KeyValuePair<string, object>> tuples, string format)
            {
                var args = _loggerValuesFormatter.Parse(format, tuples);
                if (exception != null)
                {
                    args.Add("\nException: ");
                    args.Add(exception.ToString());
                }
                _jsInterop.LogAsync(logLevel, args.ToArray());
            }
        }
    }
}
