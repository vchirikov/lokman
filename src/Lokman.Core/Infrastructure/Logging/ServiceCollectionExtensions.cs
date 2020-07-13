using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lokman
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventLogging(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(ILogger), typeof(Logger<IEventLogger>));
            services.TryAddTransient(typeof(IExternalScopeProvider), typeof(LoggerExternalScopeProvider));
            services.TryAddSingleton(typeof(IEventLogger), typeof(EventLogger));
            services.TryAddSingleton(typeof(IEventLogger<>), typeof(EventLogger<>));
            return services;
        }
    }
}
