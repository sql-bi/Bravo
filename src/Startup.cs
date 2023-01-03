namespace Sqlbi.Bravo
{
    using Dax.Formatter;
    using Hellang.Middleware.ProblemDetails;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Services;

    internal class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAndConfigureControllers();
            services.AddAndConfigureCors();
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
            services.AddSingleton<IFormatDaxService, FormatDaxService>();
            services.AddSingleton<IExportDataService, ExportDataService>();
            services.AddSingleton<IDaxFormatterClient, DaxFormatterClient>();
            services.AddSingleton<IManageDatesService, ManageDatesService>();
            services.AddSingleton<IAnalyzeModelService, AnalyzeModelService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<ITemplateDevelopmentService, TemplateDevelopmentService>();
            services.AddSingleton<IBestPracticeAnalyzerService, BestPracticeAnalyzerService>();
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment environment)
        {
#if DEBUG
            application.UseSwagger();
            application.UseSwaggerUI();
#endif
            application.UseProblemDetails();
            application.UseRouting();
            application.UseCors(); // this call must appear after UseRouting(), but before UseAuthorization() and UseEndpoints() for the middleware to function correctly
            application.UseAuthentication();
            application.UseAuthorization(); // this call must appear after UseRouting(), but before UseEndpoints() for the middleware to function correctly

            application.UseEndpoints((endpoints) =>
            {
#if DEBUG
                endpoints.MapControllers();
#else
                // Map controllers and marks them as RequireAuthorization so that all requests must be authorized
                endpoints.MapControllers().RequireAuthorization();
#endif
            });
        }
    }
}
