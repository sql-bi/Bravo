using System;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            using var context = BravoApplicationInitializer.Initialize();

            if (!context.Instance.IsPrimary)
            {
                context.Instance.RedirectActivationToPrimary();
                return;
            }

            using var application = BravoApplication
                .CreateBuilder(context)
                .Build();

            application.Run();
        }
        catch (Exception ex)
        {
            TelemetryService.Instance.TrackException(ex);

            var report = ErrorReport.Create(ex);
            report.TrySave();
            ErrorReportDialog.Show(report);

            throw;
        }
    }
}
