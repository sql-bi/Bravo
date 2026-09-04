using System;
using System.IO;
using System.Net.Http;
using System.Runtime;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Services;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Initializes the application process, applying process-wide settings
/// and composing the necessary components before the host is created.
/// </summary>
internal static class BravoApplicationInitializer
{
    /// <summary>
    /// Initializes the application process, applying process-wide settings
    /// </summary>
    public static BravoApplicationInitializationContext Initialize()
    {
        ConfigureWebProxy();
        ConfigureDirectories();
        ConfigureRuntimeOptimization();

        WebView2Helper.EnsureRuntimeIsInstalled();

        var instance = BravoApplicationInstance.Create();

        return new BravoApplicationInitializationContext(instance);
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
    }

    private static void ConfigureRuntimeOptimization()
    {
        ProfileOptimization.SetProfileRoot(AppEnvironment.ApplicationDataPath);
        ProfileOptimization.StartProfile(".jitprofile");
    }
}
