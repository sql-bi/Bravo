using System;
using System.Threading;
using System.Windows.Forms;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Gives every exception that escapes, on any thread, to one handler. The runtime then ends the process: the
/// application does not go on after an error that nothing expected.
/// </summary>
internal sealed class LastChanceExceptionHandling
{
    private readonly IFatalExceptionHandler _handler;
    private int _isHandling;

    internal LastChanceExceptionHandling(IFatalExceptionHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Call before the first window exists: Windows Forms refuses to change the mode after that.
    /// </summary>
    public static void Install(IFatalExceptionHandler handler)
    {
        var handling = new LastChanceExceptionHandling(handler);

        // An exception of an event handler leaves the message loop, on every thread, instead of being offered
        // to the user as something to continue from.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: false);
        AppDomain.CurrentDomain.UnhandledException += handling.OnUnhandledException;
    }

    internal void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // The process is ending: an exception that escapes while the first one is handled is not reported.
        if (Interlocked.Exchange(ref _isHandling, 1) != 0)
        {
            return;
        }

        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException($"An object that is not an exception was thrown: {e.ExceptionObject}.");
        try
        {
            _handler.Handle(exception);
        }
        catch (Exception handlerException)
        {
            Environment.FailFast($"The handler failed for the fatal exception: {exception}", handlerException);
        }
    }
}
