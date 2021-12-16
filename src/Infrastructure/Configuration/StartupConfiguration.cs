using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System.Net;

namespace Sqlbi.Bravo.Infrastructure.Configuration
{
    public static class StartupConfiguration
    {
        public static void Configure()
        {
            ConfigureSecurityProtocols();
            ConfiguretCurrentDirectory();
            ConfigureProcessDPIAware();
        }

        private static void ConfiguretCurrentDirectory()
        {
#if !DEBUG
            var path = System.AppContext.BaseDirectory;
            System.IO.Directory.SetCurrentDirectory(path);
#endif
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
