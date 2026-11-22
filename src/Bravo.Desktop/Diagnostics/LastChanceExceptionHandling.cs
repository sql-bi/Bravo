using System;
using System.Threading;
using System.Windows.Forms;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Gives the first exception that escapes, on any thread, to one handler. The runtime then ends the process:
/// the application does not go on after an error that nothing expected.
/// </summary>
internal sealed class LastChanceExceptionHandling
{
    private const int NoThread = 0;

    private readonly IFatalExceptionHandler _handler;
    private readonly Action _waitForTheEnd;
    private int _handlingThreadId;

    /// <param name="handler">What is done with the exception.</param>
    /// <param name="waitForTheEnd">
    /// What another thread whose exception escapes does meanwhile: it never returns, so that the runtime does not
    /// end the process before the handler is done.
    /// </param>
    internal LastChanceExceptionHandling(IFatalExceptionHandler handler, Action waitForTheEnd)
    {
        _handler = handler;
        _waitForTheEnd = waitForTheEnd;
    }

    /// <summary>
    /// Call before the first window exists: Windows Forms refuses to change the mode after that.
    /// </summary>
    public static void Install(IFatalExceptionHandler handler)
    {
        var handling = new LastChanceExceptionHandling(handler, static () => Thread.Sleep(Timeout.Infinite));

        // An exception of an event handler leaves the message loop, on every thread, instead of being offered
        // to the user as something to continue from.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: false);
        AppDomain.CurrentDomain.UnhandledException += handling.OnUnhandledException;
    }

    internal void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // The runtime calls this on every thread whose exception escapes, at the same time.
        var threadId = Environment.CurrentManagedThreadId;
        var handlingThreadId = Interlocked.CompareExchange(ref _handlingThreadId, threadId, NoThread);
        if (handlingThreadId == threadId)
        {
            // An exception that escapes from the handler itself: the process is ending.
            return;
        }

        if (handlingThreadId != NoThread)
        {
            _waitForTheEnd();
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
