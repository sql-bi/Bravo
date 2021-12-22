using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Helpers;
using System;

namespace Sqlbi.Bravo
{
    /*
     * Toast notification 
     * https://github.com/CommunityToolkit/WindowsCommunityToolkit
     * https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
     */
    internal partial class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                StartupConfiguration.Configure();

                using var instance = new AppInstance();

                if (instance.IsOwned)
                {
                    using var host = CreateHost();
                    host.Start();
                    {
                        using var window = new AppWindow(host);
                        window.WaitForClose();
                    }
                    host.StopAsync().Wait();
                }
                else
                {
                    instance.NotifyOwner();
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackException(ex);

                throw;
            }
        }
    }
}
