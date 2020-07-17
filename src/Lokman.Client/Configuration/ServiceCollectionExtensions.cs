using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lokman.Client
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Read settings from configuration file via <see cref="IConfiguration"/>
        /// and register POCO class of <typeparamref name="T"/> in <paramref name="services"/> as singleton
        /// This method is workaround for Get{T} that isn't work
        /// </summary>
        /// <typeparam name="T">POCO class of settings</typeparam>
        public static IServiceCollection AddSettings<T>(this IServiceCollection services, IConfiguration cfg) where T : class, new()
        {
            var result = new T();
            var type = typeof(T);
            // Indexed properties are not useful (or valid) for grabbing properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetIndexParameters().Length == 0 && prop.GetMethod != null).ToArray();

            if (properties.Length == 0)
                throw new NotSupportedException($"Type '{type.Name}' isn't a POCO class");

            var setterParams = new object?[1];
            cfg = cfg.GetSection(type.Name);
            if (cfg == null)
                return services;

            foreach (var prop in properties)
            {
                var setter = prop.GetSetMethod();
                if (setter == null)
                    continue;
                var untyped = cfg[prop.Name];
                if (string.IsNullOrWhiteSpace(untyped))
                    continue;
                var propType = prop.PropertyType;
                if (propType == typeof(string))
                    setterParams[0] = untyped;
                else if (propType == typeof(int))
                    setterParams[0] = int.Parse(untyped);
                else if (propType == typeof(bool))
                    setterParams[0] = bool.Parse(untyped.ToLowerInvariant().Trim('"', '\''));
                else if (propType == typeof(string[]))
                    setterParams[0] = JsonSerializer.Deserialize<string[]>(untyped);
                else
                    throw new NotSupportedException($"Property type '{propType}' isn't supported by configuration");

                prop.GetSetMethod()?.Invoke(result, setterParams);
                setterParams[0] = null;
            }
            services.AddSingleton<T>(result);
            services.AddSingleton(Options.Create(result));
            return services;
        }
    }
}
