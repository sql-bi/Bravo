using System;
using System.Collections.Generic;
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
    internal const string DevelopmentCredentialTargetName = Product.Name + " (development)/proxy";

    // The sites that the features of the application need, each at an address that answers without sending
    // the request on to another site. The update feed is added by the test: it is the one of the build.
    private static readonly IReadOnlyList<ConnectionTestSite> s_sites =
    [
        new(NetworkSite.SignIn, new Uri("https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration")),
        new(NetworkSite.PowerBIService, new Uri("https://api.powerbi.com/")),
        new(NetworkSite.DaxFormatter, new Uri("https://www.daxformatter.com/")),
    ];

    /// <summary>
    /// Adds what takes the connections of the host through the proxy of the settings of the user: every HTTP
    /// client of the host gets it, with the credentials for a proxy server that asks for them. Adds the test
    /// of the connection to the sites that the application needs.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="identity">
    /// The build that was started. A development build keeps credentials of its own, and tests no site but the
    /// update feed that it was given: no test of the application leaves the machine.
    /// </param>
    public static IServiceCollection AddNetwork(this IServiceCollection services, BuildIdentity identity)
    {
        var targetName = identity.IsDevelopmentBuild ? DevelopmentCredentialTargetName : CredentialTargetName;
        var connectionTest = new ConnectionTestOptions { Sites = identity.IsDevelopmentBuild ? [] : s_sites };

        services.TryAddSingleton(new ProxySelectionOptions());
        services.AddSingleton<IProxyCredentialStore>(
            provider => ActivatorUtilities.CreateInstance<WindowsProxyCredentialStore>(provider, targetName));
        services.AddSingleton<ProxyCredentials>();
        services.AddSingleton(SelectProxy);
        services.AddHostedService<ProxySelectionStartup>();
        services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(CreateHandler));

        services.AddSingleton(connectionTest);
        services.AddSingleton<ISiteProbe, HttpSiteProbe>();
        services.AddTransient<ConnectionTest>();

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
