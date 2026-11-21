using System;
using Bravo.Core.Network;
using Bravo.Core.Settings;
using Bravo.Platform.Windows.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Desktop.Network;

internal static class NetworkServiceCollectionExtensions
{
    // The name under which 1.0.x keeps the credentials of the proxy server: the ones that it kept are found.
    private const string CredentialTargetName = Product.Name + "/proxy";

    // A development build keeps its own, and neither reads nor replaces the ones of the installed application.
    private const string DevelopmentCredentialTargetName = Product.Name + " (development)/proxy";

    /// <summary>
    /// Adds what takes the connections of the host through the proxy of the settings of the user: every HTTP
    /// client of the host gets it, with the credentials for a proxy server that asks for them.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="identity">The build that was started: a development build keeps credentials of its own.</param>
    public static IServiceCollection AddNetwork(this IServiceCollection services, BuildIdentity identity)
    {
        var targetName = identity.IsDevelopmentBuild ? DevelopmentCredentialTargetName : CredentialTargetName;

        services.TryAddSingleton(new ProxySelectionOptions());
        services.AddSingleton<IProxyCredentialStore>(
            provider => ActivatorUtilities.CreateInstance<WindowsProxyCredentialStore>(provider, targetName));
        services.AddSingleton<ProxyCredentials>();
        services.AddSingleton(SelectProxy);
        services.AddHostedService<ProxySelectionStartup>();
        services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(CreateHandler));

        return services;
    }

    private static ProxySelection SelectProxy(IServiceProvider provider)
    {
        var settings = provider.GetRequiredService<IUserSettingsStore>();
        var options = provider.GetRequiredService<ProxySelectionOptions>();

        return ProxySelection.For(settings.Current.Proxy, options);
    }

    private static NetworkFailureHandler CreateHandler(IServiceProvider provider)
    {
        var selection = provider.GetRequiredService<ProxySelection>();
        var credentials = provider.GetRequiredService<ProxyCredentials>();

        return selection.CreateHandler(credentials);
    }
}
