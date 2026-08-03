using System;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo;

internal partial class Program
{
    [STAThread]
    public static void Main()
    {
        try
        {
            StartupConfiguration.Configure();

            using var instance = BravoApplicationInstance.Create();
            if (!instance.IsPrimary)
            {
                instance.RequestActivation();
                return;
            }

            using var host = CreateHost();
            host.Start();
            {
                var window = new AppWindow(host.Services, instance);
                Application.Run(window);
            }
            host.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            TelemetryService.Instance.TrackException(ex);
            ExceptionHelper.ShowDialog(ex);
            throw;
        }
    }
}
