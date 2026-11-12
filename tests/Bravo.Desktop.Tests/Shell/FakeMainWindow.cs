using System;
using System.Threading;
using Bravo.Desktop.Shell;

namespace Bravo.Desktop.Tests.Shell;

/// <summary>
/// A window without a window: its message loop is a wait that ends when the window is closed.
/// </summary>
internal sealed class FakeMainWindow : IMainWindow, IDisposable
{
    private readonly ManualResetEventSlim _closed = new();
    private int _closeRequests;
    private int _foregroundRequests;
    private int _messageLoops;

    public event EventHandler? Shown;

    /// <summary>
    /// What happens while the window is open, as when the user or the host acts on it.
    /// </summary>
    public Action WhileOpen { get; set; } = () => { };

    public int CloseRequests => Volatile.Read(ref _closeRequests);

    public int ForegroundRequests => Volatile.Read(ref _foregroundRequests);

    public int MessageLoops => Volatile.Read(ref _messageLoops);

    public void RunMessageLoop()
    {
        Interlocked.Increment(ref _messageLoops);
        Shown?.Invoke(this, EventArgs.Empty);
        WhileOpen();

        if (!_closed.Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Nothing closed the window.");
        }
    }

    public void RequestClose()
    {
        Interlocked.Increment(ref _closeRequests);
        _closed.Set();
    }

    public void BringToForeground()
    {
        Interlocked.Increment(ref _foregroundRequests);
    }

    /// <summary>
    /// What the user does to end the application.
    /// </summary>
    public void Close()
    {
        _closed.Set();
    }

    public void Dispose()
    {
        _closed.Dispose();
    }
}
