using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Lokman.Client
{
    public interface ILoggingInterop : IJsInterop
    {
        ValueTask LogAsync(LogLevel logLevel, params object[] logParts);
    }
    public partial class JsInteropService : IJsInteropService
    {
        public async ValueTask LogAsync(LogLevel logLevel, params object[] logParts)
        {
            var jsLogMethod = GetLogJsMethod(logLevel);
            if (jsLogMethod == null)
                return;

            if (logLevel == LogLevel.Critical && logParts?.Length == 1 && logParts[0] is string formattedMessage)
            {
                // Writing to Console.Error should cause the error UI (in #blazor-error-ui) to appear
                // message will be default, but we can change it if add a js script here
                Console.Error.WriteLine(formattedMessage);
            }

            // js add extra space if you send args like this: console.log("test ", "hello")
            var trimmed = logParts.Select(x => x is string str ? str.Trim(' ') : x).ToArray();

            await _jsRuntime.InvokeVoidAsync(jsLogMethod, trimmed).ConfigureAwait(false);

            static string? GetLogJsMethod(LogLevel logLevel)
                => logLevel switch
                {
                    LogLevel.Trace => "console.trace",
                    LogLevel.Debug => "console.debug",
                    LogLevel.Information => "console.log",
                    LogLevel.Warning => "console.warn",
                    LogLevel.Error => "console.error",
                    LogLevel.Critical => "console.error",
                    // special reserved for EventLogging, real LogLevel.None already filtered before
                    LogLevel.None => "console.table",
                    _ => "console.log",
                };
        }
    }
}
