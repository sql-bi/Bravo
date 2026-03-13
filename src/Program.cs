namespace Sqlbi.Bravo
{
    using Microsoft.Extensions.Hosting;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Telemetry;
    using System;
    using System.Windows.Forms;

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
                        var window = new AppWindow(host.Services, instance);
                        Application.Run(window);
                    }
                    host.StopAsync().GetAwaiter().GetResult();
                }
                else
                {
                    instance.NotifyOwner();
                }
            }
            catch (Exception ex)
            {
                TelemetryService.TrackFatalException(ex);
                ExceptionHelper.ShowDialog(ex);
                throw;
            }
        }
    }
}
