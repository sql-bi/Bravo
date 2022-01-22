using Dax.Formatter;
using Hellang.Middleware.ProblemDetails;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Services;
using Sqlbi.Infrastructure.Configuration.Settings;
using System.Text.Json.Serialization;

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
                jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter()); // Macross.Json.Extensions https://github.com/dotnet/runtime/issues/31081#issuecomment-578459083
                jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddAndConfigureCors(CorsLocalhostOnlyPolicy, CorsLocalhostOrigin);
            services.AddAndConfigureAuthorization();
            services.AddAndConfigureAuthentication();
            services.AddAndConfigureProblemDetails();
#if DEBUG
            services.AddAndConfigureSwaggerGen();
#endif
            services.AddHttpClient();
            services.AddOptions<StartupSettings>().Configure((settings) => settings.FromCommandLineArguments()); //.ValidateDataAnnotations();
            services.AddOptions<TelemetryConfiguration>().Configure((configuration) => TelemetryHelper.Configure(configuration));
            services.AddSingleton<IPBICloudAuthenticationService, PBICloudAuthenticationService>();
            services.AddSingleton<IPBICloudSettingsService, PBICloudSettingsService>();
            services.AddSingleton<IPBIDesktopService, PBIDesktopService>();
            services.AddSingleton<IPBICloudService, PBICloudService>();
            services.AddSingleton<IExportDataService, ExportDataService>();
            services.AddSingleton<IDaxFormatterClient, DaxFormatterClient>();
            // services.AddHostedService<ApplicationInstanceHostedService>();
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment environment)
        {
#if DEBUG
            application.UseSwagger();
            application.UseSwaggerUI();
#endif
            application.UseProblemDetails();
            application.UseRouting();
            application.UseCors(CorsLocalhostOnlyPolicy); // this call must appear after UseRouting(), but before UseAuthorization() and UseEndpoints() for the middleware to function correctly
            application.UseAuthentication();
            application.UseAuthorization(); // this call must appear after UseRouting(), but before UseEndpoints() for the middleware to function correctly

            application.UseEndpoints((endpoints) =>
            {
                // Map controllers and marks them as RequireAuthorization so that all requests must be authorized
                endpoints.MapControllers().RequireAuthorization();

                //endpoints.MapGet("/", async context =>
                //{
                //    await context.Response.WriteAsync($"Sqlbi.Bravo API on {Environment.MachineName}");
                //});
            });
        }
    }
}
