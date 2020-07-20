using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Lokman.Protos;

namespace Lokman.Client
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            if (builder.HostEnvironment.IsDevelopment())
            {
                builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions() {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                }));
            }

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

            AddGrpcWeb(services);
            AddLokmanClient(services);

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

            // TODO: move this to separate extension method
            static void AddLokmanClient(IServiceCollection services)
            {
                services.AddSingleton(sp => new DistributedLockService.DistributedLockServiceClient(sp.GetRequiredService<GrpcChannel>()));
                services.AddSingleton<IDistributedLockStore, GrpcDistributedLockStore>();
            }

            static void AddGrpcWeb(IServiceCollection services)
            {
                services.AddSingleton(services => {
                    // If server streaming is not used in your app then GrpcWeb is recommended because it produces smaller messages.
                    // If you need streaming then GrpcWebText can be used
                    var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());
                    return GrpcChannel.ForAddress(services.GetRequiredService<AppSettings>().ServedUrl, new GrpcChannelOptions { HttpHandler = httpHandler });
                });
            }
        }
    }
}
