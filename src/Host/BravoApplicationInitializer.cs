using System;
using System.IO;
using System.Net.Http;
using System.Runtime;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Services;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Provides the initialization phase that precedes the application.
/// </summary>
internal static class BravoApplicationInitializer
{
    /// <summary>
    /// Initializes the process: applies the process-wide settings and composes what the process owns
    /// before a host exists — as explicit objects in dependency order, no container — returning them
    /// in the context. <see cref="BravoApplicationBuilder"/> later publishes them to the one real
    /// service provider.
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
