using System;
using Bravo.Core.Telemetry;
using Bravo.Core.Updates;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.WebView;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Desktop.Telemetry;

internal static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds the telemetry of the host. A release build sends it to the resource of the releases; a development
    /// build only to the endpoint that its command line names, and without one it sends nothing.
    /// </summary>
    /// <param name="services">The services of the host. Call it before any other hosted service: it starts
    /// first and stops last, after the services whose exceptions it sends.</param>
    /// <param name="identity">The build that was started.</param>
    /// <param name="dataFolder">The data folder, which holds the store of the telemetry.</param>
    /// <param name="session">The run, which the page shares.</param>
    /// <param name="developmentEndpoint">The endpoint that the command line of a development build named.</param>
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        BuildIdentity identity,
        DataFolder dataFolder,
        TelemetrySession session,
        string? developmentEndpoint)
    {
        var options = TelemetryOptions.For(identity, dataFolder, developmentEndpoint);
        services.AddSingleton(options);
        services.AddSingleton(provider => TelemetryContext.Create(
            identity,
            session,
            provider.GetRequiredService<IInstallationReader>().Read(),
            WebViewRuntime.GetVersion()));
        services.AddSingleton(provider => provider.GetRequiredService<TelemetryContext>().ForPage(options.ConnectionString));
        services.AddSingleton<Func<PersonalNames>>(PersonalNames.OfCurrentUser);

        // Called only for a build that has a connection string.
        services.AddSingleton<Func<ITelemetryPipeline>>(
            () => TelemetryPipeline.Create(options.ConnectionString!, options.StorageFolder));
        services.AddSingleton<HostExceptionTelemetry>();
        services.AddSingleton<IExceptionTelemetry>(provider => provider.GetRequiredService<HostExceptionTelemetry>());
        services.AddHostedService(provider => provider.GetRequiredService<HostExceptionTelemetry>());

        return services;
    }
}
