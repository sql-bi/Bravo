namespace Sqlbi.Bravo.Infrastructure.Configuration
{
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Runtime;

    internal static class StartupConfiguration
    {
        public static void Configure()
        {
            ApplicationConfiguration.Initialize();

            //if (AppEnvironment.IsOSVersionUnsupported)
            //{
            //    MessageDialog.Show(heading: "Unsupported Windows OS version", text: "This application is only supported on Windows 10 series operating systems version 1809 (build 17763) or higher.");
            //    Environment.Exit(NativeMethods.NO_ERROR);
            //}

            ConfigureProxy();
            ConfigureDirectories();
            ConfigureMulticoreJit();
            ConfigureSecurityProtocols();

            WebView2Helper.EnsureRuntimeIsInstalled();
        }

        private static void ConfigureProxy()
        {
            HttpClient.DefaultProxy = WebProxyWrapper.Current;
        }

        private static void ConfigureDirectories()
        {
#if !DEBUG_WWWROOT
            Directory.SetCurrentDirectory(System.AppContext.BaseDirectory);
#endif
            Directory.CreateDirectory(AppEnvironment.ApplicationDataPath);
            Directory.CreateDirectory(AppEnvironment.ApplicationTempPath);
        }

        private static void ConfigureSecurityProtocols()
        {
            var includeTls = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

#pragma warning disable CS0618 // Type or member is obsolete - Justification is that we are removing SecurityProtocolType.Ssl3
            var excludeSsl = ServicePointManager.SecurityProtocol & ~SecurityProtocolType.Ssl3;
#pragma warning restore CS0618 // Type or member is obsolete

            ServicePointManager.SecurityProtocol = excludeSsl | includeTls;
        }

        //private static void ConfigureProcessDpiAwareness()
        //{
        //    var windows8Version = new Version(6, 3, 0); // win 8.1 (build number 9600) added support for per monitor dpi 
        //    var windows10Version = new Version(10, 0, 15063); // Windows 10 version 1703 (build number 15063) added support for per monitor dpi v2
        //    var environmentOSVersion = Environment.OSVersion.Version;

        //    if (environmentOSVersion >= windows8Version)
        //    {
        //        if (environmentOSVersion >= windows10Version)
        //        {
        //            _ = User32.SetProcessDpiAwarenessContext(User32.DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        //            // Applications running at a DPI_AWARENESS_CONTEXT of DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 automatically scale their non-client areas by default.
        //            // Here we don't need to call User32.EnableNonClientDpiScaling function.
        //        }
        //        else
        //        {
        //            _ = User32.SetProcessDpiAwareness(User32.PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
        //            // TODO: need to call User32.EnableNonClientDpiScaling function, see comment above
        //        }
        //    }
        //    else
        //    {
        //        _ = User32.SetProcessDPIAware();
        //        // TODO: need to call User32.EnableNonClientDpiScaling function, see comment above
        //    }
        //}

        private static void ConfigureMulticoreJit()
        {
            ProfileOptimization.SetProfileRoot(AppEnvironment.ApplicationDataPath);
            ProfileOptimization.StartProfile(".jitprofile");
        }
    }
}
