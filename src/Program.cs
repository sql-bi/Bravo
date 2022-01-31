namespace Sqlbi.Bravo
{
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Hosting;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using System;

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
                    host.StopAsync().GetAwaiter().GetResult();
                }
                else
                {
                    instance.NotifyOwner();
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackException(ex);
                ExceptionHelper.ShowDialog(ex);
                throw;
            }
        }
    }
}
