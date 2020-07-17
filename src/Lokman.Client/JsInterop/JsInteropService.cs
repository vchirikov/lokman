using System;
using Microsoft.JSInterop;

namespace Lokman.Client
{
    /// <summary>
    /// Marker for all interfaces with javascript interop
    /// Implementations Will be auto-registered as singletons
    /// </summary>
    public interface IJsInterop { }

    /// <summary>
    /// Combined interface with all <see cref="IJsInterop"/> interfaces
    /// </summary>
    public interface IJsInteropService :
        ILoggingInterop
    { }

    public partial class JsInteropService : IJsInteropService
    {
        private readonly IJSRuntime _jsRuntime;

        public JsInteropService(IJSRuntime jsRuntime)
            => _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }
}
