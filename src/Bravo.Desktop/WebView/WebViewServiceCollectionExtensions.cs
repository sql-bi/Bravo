using System;
using System.IO;
using Bravo.Api;
using Bravo.Core.Network;
using Bravo.Desktop.Telemetry;
using Bravo.Platform.Windows.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Desktop.WebView;

internal static class WebViewServiceCollectionExtensions
{
    // Beside the executable: the installation puts the files of the user interface there.
    private const string ContentFolderName = "wwwroot";

    private const string UserDataFolderName = "WebView2";

    /// <summary>
    /// Adds the webview of the main window, and lets its page call the API.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="identity">It tells whether the build is a development build.</param>
    /// <param name="dataFolder">The folder of the data of the user, which holds the one of the webview.</param>
    public static IServiceCollection AddWebView(this IServiceCollection services, BuildIdentity identity, DataFolder dataFolder)
    {
        var options = new WebViewOptions
        {
            UserDataFolder = Path.Combine(dataFolder.Path, UserDataFolderName),
            ContentFolder = Path.Combine(AppContext.BaseDirectory, ContentFolderName),
            AreDeveloperToolsEnabled = identity.IsDevelopmentBuild,
        };

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.AddSingleton(CreateProxy);
        services.AddSingleton(new WebViewFailurePolicyOptions());
        services.AddSingleton<WebViewFailurePolicy>();
        services.AddSingleton<WebViewHost>();

        // The page comes from another origin than the API: a browser engine lets it read the answers only
        // when the API names that origin.
        services.Configure<ApiOptions>(api => api.AllowedOrigins.Add(PageAddress.Origin));

        return services;
    }

    // The server that the page calls outside the machine is the endpoint of its telemetry.
    private static WebViewProxy CreateProxy(IServiceProvider provider)
    {
        return new WebViewProxy(
            provider.GetRequiredService<ProxySelection>(),
            provider.GetRequiredService<ProxyCredentials>(),
            provider.GetRequiredService<TelemetryOptions>().IngestionEndpoint);
    }
}
