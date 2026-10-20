using System;
using Sqlbi.Bravo.Host;

namespace Sqlbi.Bravo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Visual styles first: the crash dialog is a task dialog, which requires them.
        ApplicationConfiguration.Initialize();

        var exceptionHandler = BravoGlobalExceptionHandling.Configure();

        BravoApplicationConfiguration.Initialize();

        using var context = BootstrapContextFactory.CreateDefault();

        exceptionHandler.AttachServices(context.LoggerFactory, context.Telemetry);

        using var app = BravoApplication
            .CreateBuilder(context)
            .Build();

        app.Run();
    }
}
