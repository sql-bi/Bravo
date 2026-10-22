using System;
using Sqlbi.Bravo.Host;

namespace Sqlbi.Bravo;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (BravoCrashReporter.RunIfRequested(args))
            return;

        var exceptionHandler = BravoGlobalExceptionHandling.Configure();

#if DEBUG
        CrashTest.RunIfRequested(args, out args);
#endif

        using var instanceServer = BravoSingleInstance.ClaimOrRedirect(args);
        if (instanceServer is null)
            return;

        BravoApplicationConfiguration.Initialize();

        using var context = BootstrapContextFactory.CreateDefault(exceptionHandler);

        using var app = BravoApplication
            .CreateBuilder(context, instanceServer, args)
            .Build();

        app.Run();
    }
}
