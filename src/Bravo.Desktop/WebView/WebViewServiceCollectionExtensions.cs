using System;
using System.IO;
using Bravo.Api;
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
    /// <param name="identity">It tells where the data of this build is, and whether it is a development build.</param>
    public static IServiceCollection AddWebView(this IServiceCollection services, BuildIdentity identity)
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var options = new WebViewOptions
        {
            UserDataFolder = Path.Combine(localData, identity.DataFolderName, UserDataFolderName),
            ContentFolder = Path.Combine(AppContext.BaseDirectory, ContentFolderName),
            AreDeveloperToolsEnabled = identity.IsDevelopmentBuild,
        };

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.AddSingleton(new WebViewFailurePolicyOptions());
        services.AddSingleton<WebViewFailurePolicy>();
        services.AddSingleton<WebViewHost>();

        // The page comes from another origin than the API: a browser engine lets it read the answers only
        // when the API names that origin.
        services.Configure<ApiOptions>(api => api.AllowedOrigins.Add(PageAddress.Origin));

        return services;
    }
}
