using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Reflection;

namespace Lokman.Client
{
    internal interface ILiveReloadService
    {
        ValueTask StartWatchAsync(CancellationToken cancellationToken = default);
    }

    internal class LiveReloadService : ILiveReloadService
    {
        private const int MillisecondsDelay = 200;
        private readonly HttpClient _httpClient;
        private readonly ILogger<LiveReloadService> _logger;
        private bool _isLastRequestFailed;
        private readonly string _url = "";
        private readonly Action _reload;

        public LiveReloadService(HttpClient httpClient, ILogger<LiveReloadService> logger, NavigationManager navigationManager)
            : this(
                httpClient,
                logger,
                $"{navigationManager.BaseUri}_framework/{Assembly.GetExecutingAssembly().Location}",
                () => navigationManager.NavigateTo(navigationManager.Uri, forceLoad: true))
        { }

        internal LiveReloadService(HttpClient httpClient, ILogger<LiveReloadService> logger, string url, Action reload)
        {
            _httpClient = httpClient;
            _logger = logger;
            _url = url;
            _reload = reload;
        }

        public async ValueTask StartWatchAsync(CancellationToken cancellationToken = default)
        {
            // we don't want to block rendering at all
            await Task.Yield().ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, _url).SetBrowserRequestCache(BrowserRequestCache.NoCache);
                    var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                    if (_isLastRequestFailed && result.StatusCode == HttpStatusCode.OK)
                    {
                        _isLastRequestFailed = false;
                        _reload();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (!_isLastRequestFailed)
                    {
                        _isLastRequestFailed = true;
                        _logger.LogWarning(ex, "Loading the base url failed, maybe it's caused by livereloading...");
                    }
                }
                await Task.Delay(MillisecondsDelay).ConfigureAwait(false);
            }
        }
    }
}
