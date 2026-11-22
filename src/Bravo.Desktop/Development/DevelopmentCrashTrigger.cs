using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bravo.Desktop.Shell;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Development;

/// <summary>
/// Throws an exception that nothing catches at the place that the command line of a development build named.
/// </summary>
internal sealed class DevelopmentCrashTrigger(DevelopmentCrash crash, MainWindowDispatcher dispatcher)
    : BackgroundService
{
    private static readonly TimeSpan s_windowPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly DevelopmentCrash _crash = crash;
    private readonly MainWindowDispatcher _dispatcher = dispatcher;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (_crash == DevelopmentCrash.Start)
        {
            throw CreateException();
        }

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Off the thread that starts the host.
        await Task.Yield();

        switch (_crash)
        {
            case DevelopmentCrash.Window:
                await RunOnWindowAsync(Post, stoppingToken).ConfigureAwait(false);
                break;

            case DevelopmentCrash.Thread:
                new Thread(() => throw CreateException()) { IsBackground = true }.Start();
                break;

            case DevelopmentCrash.Service:
                // While the application runs: once the window is shown, the host has started.
                await RunOnWindowAsync(_ => string.Empty, stoppingToken).ConfigureAwait(false);
                throw CreateException();

            case DevelopmentCrash.Unobserved:
                LeaveFailedTask();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                break;
        }
    }

    private InvalidOperationException CreateException()
    {
        return new InvalidOperationException($"A crash that the command line asked for: {_crash}.");
    }

    // On the thread of the window, once it is shown.
    private async Task RunOnWindowAsync(Func<nint, string> action, CancellationToken cancellationToken)
    {
        while (await _dispatcher.RunAsync(action, cancellationToken).ConfigureAwait(false) is null)
        {
            await Task.Delay(s_windowPollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    // A delegate posted to the message loop, as a callback of the window is.
    private string Post(nint handle)
    {
        Control.FromHandle(handle)!.BeginInvoke(() => throw CreateException());
        return string.Empty;
    }

    // Not inlined: no frame of the caller keeps the task, which the collection of the memory then finalizes.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LeaveFailedTask()
    {
        _ = Task.FromException(CreateException());
    }
}
