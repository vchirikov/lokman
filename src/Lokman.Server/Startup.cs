using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Lokman.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddResponseCompression(opts => {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
            services.AddGrpcHttpApi();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lokman http api", Version = "v1" });
            });
            services.AddGrpcSwagger();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(spa => spa.RootPath = Path.Combine("ClientApp/build"));

            services.AddEventLogging();

            // TODO: place these lines to new extension method in Lokman project
            services.TryAddSingleton<IDistributedLockStoreCleanupStrategy>(NoOpDistributedLockStoreCleanupStrategy.Instance);
            services.TryAddSingleton<IDistributedLockStore, DistributedLockStore>();
            services.TryAddSingleton<IExpirationQueue, ExpirationQueue>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            // for swagger mostly
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lokman http api v1");
            });

            app.UseRouting();
            app.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });

            app.UseEndpoints(endpoints => {
                endpoints.MapGrpcService<GrpcDistributedLockService>().EnableGrpcWeb();
                //endpoints.MapFallbackToPage("404.html");
            });
            if (!env.IsDevelopment())
            {
                app.UseSpa(spa => {
                    spa.Options.SourcePath = "ClientApp";
                    // UseProxyToSpaDevelopmentServer is too slow for react-fast reload, better doing another way
                });
            }
        }
    }
}
