using Dax.Formatter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.PowerBI;
using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Sqlbi.Bravo.Services;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Provides the application-composition registrations for the Bravo host.
/// </summary>
internal static class BravoServiceCollectionExtensions
{
    /// <summary>
    /// Registers and configures the services required for the Bravo REST API.
    /// </summary>
    public static IServiceCollection AddBravoRestApi(this IServiceCollection services)
    {
        services.AddAndConfigureControllers();
        services.AddAndConfigureCors();
        services.AddAndConfigureAuthorization();
        services.AddAndConfigureAuthentication();
        services.AddAndConfigureProblemDetails();
#if DEBUG
        services.AddAndConfigureSwaggerGen();
#endif
        return services;
    }

    /// <summary>
    /// Registers and configures the services required for the Bravo application.
    /// </summary>
    public static IServiceCollection AddBravoServices(this IServiceCollection services, BootstrapContext bootstrap)
    {
        services.AddBootstrapServices(bootstrap);
        services.AddBravoApplicationInstance();
        services.AddHttpClient();
        services.AddPowerBI();
        services.AddSingleton<IPBIDesktopService, PBIDesktopService>();
        services.AddSingleton<IDaxFormatterClient, DaxFormatterClient>();

        services.AddSingleton<IFormatDaxService, FormatDaxService>();
        services.AddSingleton<IExportDataService, ExportDataService>();
        services.AddSingleton<IManageDatesService, ManageDatesService>();
        services.AddSingleton<IAnalyzeModelService, AnalyzeModelService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ITemplateDevelopmentService, TemplateDevelopmentService>();
        services.AddSingleton<IBestPracticeAnalyzerService, BestPracticeAnalyzerService>();

        return services;
    }

    private static IServiceCollection AddBravoApplicationInstance(this IServiceCollection services)
    {
        services.AddSingleton<IBravoApplicationInstance>((provider) =>
        {
            var telemetry = provider.GetRequiredService<ITelemetryService>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var options = new SingleInstanceOptions
            {
                PipeName = InstancePipeName.Create(),
            };
            _ = SingleInstanceServer.TryStart(options, out var server);

            return new BravoApplicationInstance(options, server, telemetry, loggerFactory);
        });

        return services;
    }

    private static IServiceCollection AddBootstrapServices(this IServiceCollection services, BootstrapContext bootstrap)
    {
        // Instance registrations: the container resolves them without creating or disposing them.

        // Replace the default ILoggerFactory registered by the WebApplicationBuilder
        services.Replace(ServiceDescriptor.Singleton(bootstrap.LoggerFactory));

        services.AddSingleton(bootstrap.PolicyService);
        services.AddSingleton(bootstrap.Settings);
        services.AddSingleton(bootstrap.Telemetry);

        return services;
    }
}
