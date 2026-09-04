using Sqlbi.Bravo.Infrastructure.Diagnostics;

namespace Sqlbi.Bravo.Host;

internal static class BravoGlobalExceptionHandling
{
    public static void Configure(BootstrapContext context)
    {
        var dialog = new ErrorReportTaskDialog(context.Telemetry);

        var handler = new UnhandledExceptionHandler(
            dialog, context.Telemetry, context.LoggerFactory);

        GlobalExceptionHandling.Configure(handler.Handle);

#if DEBUG
        CrashTest.RunIfRequested();
#endif
    }
}
