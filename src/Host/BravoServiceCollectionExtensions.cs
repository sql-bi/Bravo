using Dax.Formatter;
using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.PowerBI;
using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Sqlbi.Bravo.Services;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Provides the application-composition registrations for the Bravo host.
/// </summary>
internal static class BravoServiceCollectionExtensions
{
    /// <summary>
    /// Publishes what <see cref="BravoApplicationInitializer"/> produced. These are instance
    /// registrations: the container resolves them but never disposes them — their owner is the
    /// initialization context.
    /// </summary>
    public static IServiceCollection AddBravoInitializationServices(this IServiceCollection services, BravoApplicationInitializationContext context)
    {
        // The single instance goes in as its activation events only: IsPrimary and the activation
        // redirection belong to the entry point and stay out of the container.
        services.AddSingleton<IInstanceActivationEvents>(context.Instance);

        services.AddSingleton<IPolicies>(PoliciesFactory.Create());
        services.AddSingleton<ITelemetryService>(TelemetryService.Instance);

        return services;
    }

    /// <summary>
    /// Registers the REST API surface.
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
    /// Registers the core services that implement the application's functionality.
    /// </summary>
    public static IServiceCollection AddBravoServices(this IServiceCollection services)
    {
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
}
