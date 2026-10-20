using Sqlbi.Bravo.Infrastructure.Diagnostics;

namespace Sqlbi.Bravo.Host;

internal static class BravoGlobalExceptionHandling
{
    public static UnhandledExceptionHandler Configure()
    {
        var handler = new UnhandledExceptionHandler(new ErrorReportTaskDialog());

        GlobalExceptionHandling.Configure(handler.Handle);

#if DEBUG
        CrashTest.RunIfRequested();
#endif

        return handler;
    }
}
