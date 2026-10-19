using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Provides global exception handling for the application.
/// </summary>
internal static class GlobalExceptionHandling
{
    private static GlobalExceptionHandler? s_handler;
    private static int s_isHandlingFatalException;

    /// <summary>
    /// Configures global exception handling for the application.
    /// </summary>
    public static void Configure(GlobalExceptionHandler handler)
    {
        if (s_handler is not null)
            throw new InvalidOperationException("Global exception handling has already been configured.");

        s_handler = handler;

        // mode: ThrowException: Prevents WinForms from swallowing UI exceptions or showing its "Continue/Quit" dialog.
        // - Preserves the exact stack trace at the throw site for crash dumps and debugger break.
        // - Routes UI exceptions to AppDomain.UnhandledException (consistent with background threads).
        // threadScope: false: Sets this mode globally across all threads, ensuring additional STA threads are covered.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: false);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        // ExceptionObject may contain a non-CLS exception value.
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException($"Non-CLS-compliant exception object of type '{e.ExceptionObject?.GetType()}': {e.ExceptionObject}.");

        // Prevent fatal exception handling from being reentered, which could lead to stack overflow or deadlock.
        if (Interlocked.CompareExchange(ref s_isHandlingFatalException, value: 1, comparand: 0) != 0)
            return;

        try
        {
            InvokeHandler(exception, ExceptionOrigin.AppDomain);
        }
        finally
        {
            Interlocked.Exchange(ref s_isHandlingFatalException, 0);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Prevent the task exception from being raised again if handling fails.
        e.SetObserved();

        InvokeHandler(e.Exception, ExceptionOrigin.UnobservedTask);
    }

    private static void InvokeHandler(Exception exception, ExceptionOrigin origin)
    {
        try
        {
            s_handler?.Invoke(exception, origin);
        }
        catch (Exception handlerException)
        {
            Environment.FailFast($"The unhandled exception handler failed while handling: {exception}", handlerException);
        }
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
