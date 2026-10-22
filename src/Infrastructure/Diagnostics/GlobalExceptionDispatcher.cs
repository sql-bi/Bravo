using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Passes the runtime callbacks for unhandled exceptions to a handler, handling only the first fatal exception.
/// </summary>
internal sealed class GlobalExceptionDispatcher(GlobalExceptionHandler handler)
{
    private static readonly ManualResetEvent s_neverSignaled = new(initialState: false);

    private readonly GlobalExceptionHandler _handler = handler;

    // The thread handling the fatal exception, zero until there is one: managed thread IDs start at one. It is never
    // reset, because the process ends once that thread is done.
    private int _fatalThreadId;

    public void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        // ExceptionObject may contain a non-CLS exception value.
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException($"Non-CLS-compliant exception object of type '{e.ExceptionObject?.GetType()}': {e.ExceptionObject}.");

        var threadId = Environment.CurrentManagedThreadId;
        var fatalThreadId = Interlocked.CompareExchange(ref _fatalThreadId, threadId, 0);

        if (fatalThreadId == threadId)
        {
            // Handling again on this thread could recurse until the stack overflows; the runtime ends the process.
            return;
        }

        if (fatalThreadId != 0)
        {
            // Returning would let the runtime end the process while another thread is still reporting its crash.
            // A wait, unlike Thread.Sleep, lets a UI thread answer the messages sent to its windows meanwhile.
            s_neverSignaled.WaitOne();
            return;
        }

        InvokeHandler(exception, ExceptionOrigin.AppDomain);
    }

    public void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Prevent the task exception from being raised again if handling fails.
        e.SetObserved();

        InvokeHandler(e.Exception, ExceptionOrigin.UnobservedTask);
    }

    private void InvokeHandler(Exception exception, ExceptionOrigin origin)
    {
        try
        {
            _handler(exception, origin);
        }
        catch (Exception handlerException)
        {
            Environment.FailFast($"The unhandled exception handler failed while handling: {exception}", handlerException);
        }
    }
}
