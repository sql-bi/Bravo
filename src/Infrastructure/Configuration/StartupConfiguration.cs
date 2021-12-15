using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.IO;
using System.Net;

namespace Sqlbi.Bravo.Infrastructure.Configuration
{
    public static class StartupConfiguration
    {
        public static void SetupEnvironment()
        {
            ConfigureSecurityProtocols();
            ConfiguretCurrentDirectory();
            ConfigureProcessDPIAware();
        }

        private static void ConfiguretCurrentDirectory()
        {
            var path = AppContext.BaseDirectory;
            Directory.SetCurrentDirectory(path);
        }

        private static void ConfigureSecurityProtocols()
        {
            var includeTls = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

#pragma warning disable CS0618 // Type or member is obsolete - Justification is that we are removing SecurityProtocolType.Ssl3
            var excludeSsl = ServicePointManager.SecurityProtocol & ~SecurityProtocolType.Ssl3;
#pragma warning restore CS0618 // Type or member is obsolete

            ServicePointManager.SecurityProtocol = excludeSsl | includeTls;
        }

        private static void ConfigureProcessDPIAware()
        {
            NativeMethods.SetProcessDPIAware();
        }
    }
}
