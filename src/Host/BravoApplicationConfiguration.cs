using System;
using System.IO;
using System.Net.Http;
using System.Runtime;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Services;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Configures the application environment before the application starts.
/// </summary>
internal static class BravoApplicationConfiguration
{
    public static void Initialize()
    {
        ConfigureDirectories();
        ConfigureWebProxy();
        ConfigureRuntimeOptimization();

        WebView2Helper.EnsureRuntimeIsInstalled();
    }

    private static void ConfigureWebProxy()
    {
        HttpClient.DefaultProxy = WebProxyWrapper.Current;
    }

    private static void ConfigureDirectories()
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Directory.CreateDirectory(AppEnvironment.ApplicationDataPath);
        Directory.CreateDirectory(AppEnvironment.ApplicationTempPath);
        Directory.CreateDirectory(TelemetryService.DefaultStorageFolder);
    }

    private static void ConfigureRuntimeOptimization()
    {
        ProfileOptimization.SetProfileRoot(AppEnvironment.ApplicationCachePath);
        ProfileOptimization.StartProfile(".jitprofile");
    }
}
