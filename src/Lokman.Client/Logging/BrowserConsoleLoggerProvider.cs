using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lokman.Client
{
    /// <summary>
    /// A provider for <seealso cref="BrowserConsoleLogger"/>
    /// The analog of default <see cref="Microsoft.AspNetCore.Components.WebAssembly.Services.WebAssemblyConsoleLoggerProvider"/>)
    /// </summary>
    [ProviderAlias("BrowserConsole")]
    internal class BrowserConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ILoggingInterop _jsInterop;

        private readonly ConcurrentDictionary<string, BrowserConsoleLogger> _loggers
            = new ConcurrentDictionary<string, BrowserConsoleLogger>();

        public BrowserConsoleLoggerProvider(ILoggingInterop loggingInterop) => _jsInterop = loggingInterop;

        public ILogger CreateLogger(string? categoryName)
            => _loggers.GetOrAdd(categoryName ?? "", category => new BrowserConsoleLogger(_jsInterop, category));

        public void Dispose() => _loggers.Clear();
    }
}
