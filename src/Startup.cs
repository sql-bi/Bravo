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
    using Sqlbi.Bravo.Services;
    using System.Text.Json.Serialization;

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
                //jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter()); // Macross.Json.Extensions https://github.com/dotnet/runtime/issues/31081#issuecomment-578459083
                jsonOptions.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumMemberConverter(
                        options: new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: null),
                        typeof(Sqlbi.Bravo.Infrastructure.Configuration.Settings.ThemeType),
                        typeof(Sqlbi.Bravo.Infrastructure.Configuration.Settings.DiagnosticLevelType),
                        typeof(Sqlbi.Bravo.Infrastructure.Configuration.Settings.UpdateChannelType),
                        typeof(Sqlbi.Bravo.Infrastructure.Messages.WebMessageType),
                        typeof(Sqlbi.Bravo.Infrastructure.BravoProblem),
                        typeof(Sqlbi.Bravo.Infrastructure.AppFeature),
                        typeof(Sqlbi.Bravo.Models.PBIDesktopReportConnectionMode),
                        typeof(Sqlbi.Bravo.Models.PBICloudDatasetConnectionMode),
                        typeof(Sqlbi.Bravo.Models.PBICloudDatasetEndorsement),
                        typeof(Sqlbi.Bravo.Models.DiagnosticMessageSeverity),
                        typeof(Sqlbi.Bravo.Models.DiagnosticMessageType),
                        typeof(Sqlbi.Bravo.Models.ExportData.ExportDataStatus),
                        typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudWorkspaceType),
                        typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudWorkspaceCapacitySkuType),
                        typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudSharedModelWorkspaceType),
                        typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudPromotionalStage),
                        typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudPermissions),
                        typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudOrganizationalGalleryItemStatus),
                        typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudEnvironmentType),
                        typeof(Dax.Formatter.Models.DaxFormatterLineStyle),
                        typeof(Dax.Formatter.Models.DaxFormatterSpacingStyle),
                        typeof(Dax.Template.Enums.AutoNamingEnum),
                        typeof(Dax.Template.Enums.AutoScanEnum),
                        typeof(Sqlbi.Bravo.Models.FormatDax.DaxLineBreakStyle), 
                        typeof(Sqlbi.Bravo.Models.ManageDates.TableValidation),
                        typeof(Sqlbi.Bravo.Models.ManageDates.TypeStartFiscalYear),
                        typeof(Sqlbi.Bravo.Models.ManageDates.WeeklyType)

                        //, typeof(Sqlbi.Bravo.Models.ManageDates.DayOfWeek)
                        //, typeof(Sqlbi.Bravo.Models.ManageDates.QuarterWeekType)
                        //, typeof(Sqlbi.Bravo.Models.AnalyzeModel.TabularTableFeature)
                        //, typeof(Sqlbi.Bravo.Models.AnalyzeModel.TabularTableFeatureUnsupportedReason)
                        )
                    );
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
            services.AddSingleton<IFormatDaxService, FormatDaxService>();
            services.AddSingleton<IExportDataService, ExportDataService>();
            services.AddSingleton<IDaxFormatterClient, DaxFormatterClient>();
            services.AddSingleton<IManageDatesService, ManageDatesService>();
            services.AddSingleton<IBestPracticeAnalyzerService, BestPracticeAnalyzerService>();
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
#if DEBUG
                endpoints.MapControllers();
#else
                // Map controllers and marks them as RequireAuthorization so that all requests must be authorized
                endpoints.MapControllers().RequireAuthorization();
#endif
                //endpoints.MapGet("/", async context =>
                //{
                //    await context.Response.WriteAsync($"Sqlbi.Bravo API on {Environment.MachineName}");
                //});
            });
        }
    }
}
