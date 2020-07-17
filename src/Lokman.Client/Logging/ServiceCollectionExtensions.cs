using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lokman.Client
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventLogging(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(ILogger), typeof(Logger<IEventLogger>));
            services.TryAddTransient(typeof(IExternalScopeProvider), typeof(LoggerExternalScopeProvider));
            services.TryAddSingleton(typeof(IEventLogger), typeof(EventLogger));
            services.TryAddSingleton(typeof(IEventLogger<>), typeof(EventLogger<>));
            return services;
        }

        /// <summary>
        /// Adds a logger that target the browser's console output
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// </summary>
        public static ILoggingBuilder AddBrowserConsole(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BrowserConsoleLoggerProvider>());

            // override the hardcoded ILogger<> injected by Blazor
            builder.Services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(BrowserConsoleLogger<>)));
            return builder;
        }
    }
}
