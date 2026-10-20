using System;
using Sqlbi.Bravo.Host;

namespace Sqlbi.Bravo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        BravoApplicationConfiguration.Initialize();

        using var context = BootstrapContextFactory.CreateDefault();

        BravoGlobalExceptionHandling.Configure(context);

        using var app = BravoApplication
            .CreateBuilder(context)
            .Build();

        app.Run();
    }
}
