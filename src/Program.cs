using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Helpers;
using System;
using System.Threading;

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

                var instance = new AppInstance();
                if (instance.IsOwned == false)
                {
                    // Another instance of the application is already running, notify the owner and exit gracefully
                    instance.NotifyOwner();
                    return;
                }

                using var host = CreateHost();
                host.Start();
                {
                    using var window = new AppWindow(host);
                    window.WaitForClose();
                }
                host.StopAsync().Wait();
            }
            catch (Exception ex)
            {
                var telemetry = TelemetryHelper.CreateTelemetryClient();
                {
                    telemetry.TrackException(ex);
                    telemetry.Flush();
                    Thread.Sleep(3000);
                }
                throw;
            }
        }
    }
}
