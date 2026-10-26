using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Provides global exception handling for the application.
/// </summary>
internal static class GlobalExceptionHandling
{
    private static GlobalExceptionDispatcher? s_dispatcher;

    /// <summary>
    /// Configures global exception handling for the application.
    /// </summary>
    public static void Configure(GlobalExceptionHandler handler)
    {
        if (s_dispatcher is not null)
            throw new InvalidOperationException("Global exception handling has already been configured.");

        var dispatcher = new GlobalExceptionDispatcher(handler);
        s_dispatcher = dispatcher;

        // mode: ThrowException: Prevents WinForms from swallowing UI exceptions or showing its "Continue/Quit" dialog.
        // - Preserves the exact stack trace at the throw site for crash dumps and debugger break.
        // - Routes UI exceptions to AppDomain.UnhandledException (consistent with background threads).
        // threadScope: false: Sets this mode globally across all threads, ensuring additional STA threads are covered.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: false);

        AppDomain.CurrentDomain.UnhandledException += dispatcher.OnUnhandledException;
        TaskScheduler.UnobservedTaskException += dispatcher.OnUnobservedTaskException;
    }
}

/// <summary>
/// Handles an exception captured by <see cref="GlobalExceptionHandling"/>.
/// </summary>
internal delegate void GlobalExceptionHandler(Exception exception, ExceptionOrigin origin);

/// <summary>
/// Identifies where an exception was captured and the constraints of handling it.
/// </summary>
internal enum ExceptionOrigin
{
    /// <summary>
    /// An unhandled exception from any thread. The runtime terminates the process after the handler returns.
    /// </summary>
    AppDomain,

    /// <summary>
    /// An unobserved task exception raised on the finalizer thread. Do not block or show UI.
    /// </summary>
    UnobservedTask,
}
