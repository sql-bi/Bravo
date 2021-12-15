using Dax.Formatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Services;
using Sqlbi.Infrastructure;
using System;
using System.Text.Json.Serialization;
using static Sqlbi.Bravo.Infrastructure.Configuration.StartupConfiguration;

namespace Sqlbi.Bravo
{
    internal class Startup
    {
        private const string CorsLocalhostOnlyPolicy = "AllowLocalWebAPI";
        private const string CorsLocalhostOrigin = "null";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions((jsonOptions) =>
            {
                jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddCors((corsOptions) =>
            {
                corsOptions.AddPolicy(CorsLocalhostOnlyPolicy, (policyBuilder) =>
                {
                    // for security, default to only accepting calls from the local machine
                    policyBuilder.AllowAnyMethod().AllowAnyHeader().WithOrigins(CorsLocalhostOrigin);
                });
            });
#if DEBUG
            services.AddSwaggerGenCustomized();
#endif
            // Options
            services.AddWritableOptions<AppOptions>(section: Configuration.GetSection(nameof(AppOptions)), file: "appsettings.json"); //.ValidateDataAnnotations();
            services.AddOptions<AppStartupOptions>().Configure(FromCommandLineArguments); //.ValidateDataAnnotations();
            // Services
            services.AddSingleton<IPBICloudAuthenticationService, PBICloudAuthenticationService>();
            services.AddSingleton<IPBIDesktopService, PBIDesktopService>();
            services.AddSingleton<IPBICloudService, PBICloudService>();
            services.AddSingleton<IDaxFormatterClient, DaxFormatterClient>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            // TODO: see 'force IPV6 and randomise the listening port'
            var addressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>() ?? throw new Exception($"ServerFeature not found { nameof(IServerAddressesFeature) }");
            System.Diagnostics.Trace.WriteLine("::Bravo:INF:KestrelListeningAddresses:" + string.Join(", ", addressesFeature.Addresses ?? Array.Empty<string>()));
            Program.HostAddresses = addressesFeature.Addresses;

            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
#if DEBUG
            app.UseSwagger();
            app.UseSwaggerUI();
#endif
            app.UseRouting();
            app.UseCors(CorsLocalhostOnlyPolicy); // The call to UseCors must be placed after UseRouting, but before UseAuthorization and UseEndpoints

            // TODO: do we need https authz/authn ? 
            //app.UseAuthentication();
            //app.UseAuthorization(); e.g. FilterAttribute, IActionFilter .OnActionExecuting => if (filterContext.HttpContext.Request.IsLocal == false) filterContext.Result = new HttpForbiddenResult(); 

            app.UseEndpoints((endpoints) =>
            {
                endpoints.MapControllers();
                //endpoints.MapGet("/", async context =>
                //{
                //    await context.Response.WriteAsync($"Sqlbi.Bravo API on {Environment.MachineName}");
                //});
            });
        }
    }
}
