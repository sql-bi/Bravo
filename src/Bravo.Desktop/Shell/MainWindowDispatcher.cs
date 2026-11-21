using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Desktop.Shell;

/// <summary>
/// Reaches the main window from a thread that is not its own, such as the one of a request of the API. The
/// window is there while its message loop runs: a service never asks the container for the window, which is
/// created on its own thread.
/// </summary>
internal sealed class MainWindowDispatcher
{
    private volatile IMainWindow? _window;

    /// <summary>
    /// Runs a function on the thread of the window, with the handle of the window, and returns its result.
    /// </summary>
    /// <returns>
    /// The result of the function, or <see langword="null"/> when there is no window to run it on.
    /// </returns>
    public Task<T?> RunAsync<T>(Func<nint, T?> action, CancellationToken cancellationToken)
        where T : class
    {
        if (_window is not { } window)
        {
            return Task.FromResult<T?>(null);
        }

        return window.RunOnWindowAsync(action, cancellationToken);
    }

    public void Attach(IMainWindow window)
    {
        _window = window;
    }

    public void Detach()
    {
        _window = null;
    }
}
