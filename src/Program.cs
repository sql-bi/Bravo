using System;
using Sqlbi.Bravo.Host;

namespace Sqlbi.Bravo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var exceptionHandler = BravoGlobalExceptionHandling.Configure();

        using var instanceServer = BravoSingleInstance.ClaimOrRedirect();
        if (instanceServer is null)
            return;

        BravoApplicationConfiguration.Initialize();

        using var context = BootstrapContextFactory.CreateDefault(exceptionHandler);

        using var app = BravoApplication
            .CreateBuilder(context, instanceServer)
            .Build();

        app.Run();
    }
}
