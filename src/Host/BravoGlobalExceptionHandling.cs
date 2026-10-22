using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Diagnostics;

namespace Sqlbi.Bravo.Host;

internal static class BravoGlobalExceptionHandling
{
    public static UnhandledExceptionHandler Configure()
    {
        // The path is read on a crash: AppEnvironment initialization can itself be the failure being reported.
        var handler = new UnhandledExceptionHandler(new ErrorReportTaskDialog(), () => AppEnvironment.ApplicationDataPath);

        GlobalExceptionHandling.Configure(handler.Handle);

        return handler;
    }
}
