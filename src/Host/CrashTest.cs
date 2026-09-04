#if DEBUG
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Provides a mechanism to test the application's global exception handling
/// by throwing exceptions in different contexts.
/// </summary>
internal static class CrashTest
{
    private static System.Windows.Forms.Timer? s_timer;

    public static void RunIfRequested()
    {
        switch (Environment.GetEnvironmentVariable("BRAVO_CrashTest"))
        {
            case "startup":
                throw new InvalidOperationException("Crash test: before the message loop.");

            case "ui":
                // Ticks on the thread running the message loop, so the exception escapes a window procedure.
                s_timer = new System.Windows.Forms.Timer { Interval = 3_000 };
                s_timer.Tick += (_, _) => throw new InvalidOperationException("Crash test: UI thread.");
                s_timer.Start();
                break;

            case "thread":
                StartDelayed(() => throw new InvalidOperationException("Crash test: background thread."));
                break;

            case "unobserved":
                StartDelayed(() =>
                {
                    ThrowUnobserved();

                    // The event is raised when the faulted task is finalized, not when it faults.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                });
                break;
        }
    }

    private static void StartDelayed(Action action)
    {
        var thread = new Thread(() =>
        {
            Thread.Sleep(3_000);
            action();
        })
        {
            IsBackground = true,
        };

        thread.Start();
    }

    // In its own method: a local in the caller would keep the task alive.
    private static void ThrowUnobserved()
    {
        var task = Task.Run(() => throw new InvalidOperationException("Crash test: unobserved task."));

        // Wait for the task to complete so that it can be finalized
        while (!task.IsCompleted)
            Thread.Sleep(10);
    }
}
#endif
