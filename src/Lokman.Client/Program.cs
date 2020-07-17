using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace Lokman.Client
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            builder.RootComponents.Add<LiveReload>("livereload");
            builder.Logging.ClearProviders();
            builder.Logging.AddBrowserConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
            Console.WriteLine($"HostEnvironment.BaseAddress is {builder.HostEnvironment.BaseAddress}");
            var services = builder.Services;
            services
                .AddTransient(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), })
                .AddEventLogging()
                .AddSingleton<ILiveReloadService, LiveReloadService>()
                .AddSettings<AppSettings>(builder.Configuration)
                ;

            var jsInteropInterfaces = typeof(Program).Assembly.GetTypes().Where(t =>
                t.IsInterface &&
                t != typeof(IJsInterop) &&
                t != typeof(IJsInteropService) &&
                typeof(IJsInterop).IsAssignableFrom(t)
            );

            services.AddSingleton<IJsInteropService, JsInteropService>();

            foreach (var type in jsInteropInterfaces)
                services.AddSingleton(type, sp => sp.GetRequiredService<IJsInteropService>());

            var webAssemblyHost = builder.Build();

            var logger = webAssemblyHost.Services.GetRequiredService<IEventLogger<App>>();
            var opt = webAssemblyHost.Services.GetRequiredService<IOptions<AppSettings>>();
            logger.LogInformation("We are runned on {Environment} environment", opt.Value.Environment);

            await webAssemblyHost.RunAsync().ConfigureAwait(false);
        }
    }
}
