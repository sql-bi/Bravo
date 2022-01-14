using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System.IO;
using System.Net;
using System.Runtime;

namespace Sqlbi.Bravo.Infrastructure.Configuration
{
    public static class StartupConfiguration
    {
        public static void Configure()
        {
            ConfigureDirectories();
            ConfigureMulticoreJit();
            ConfigureSecurityProtocols();
            ConfigureProcessDpiAware();
        }

        private static void ConfigureDirectories()
        {
#if !DEBUG_WWWROOT
            Directory.SetCurrentDirectory(System.AppContext.BaseDirectory);
#endif
            Directory.CreateDirectory(AppConstants.ApplicationFolderLocalDataPath);
        }

        private static void ConfigureSecurityProtocols()
        {
            var includeTls = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

#pragma warning disable CS0618 // Type or member is obsolete - Justification is that we are removing SecurityProtocolType.Ssl3
            var excludeSsl = ServicePointManager.SecurityProtocol & ~SecurityProtocolType.Ssl3;
#pragma warning restore CS0618 // Type or member is obsolete

            ServicePointManager.SecurityProtocol = excludeSsl | includeTls;
        }

        private static void ConfigureProcessDpiAware()
        {
            NativeMethods.SetProcessDPIAware();
        }

        private static void ConfigureMulticoreJit()
        {
            ProfileOptimization.SetProfileRoot(AppConstants.ApplicationFolderLocalDataPath);
            ProfileOptimization.StartProfile(".jitprofile");
        }
    }
}
