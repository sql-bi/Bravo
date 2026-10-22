#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Raises a failure in a Debug build, to test crash handling: <c>--crash-test &lt;scenario&gt;</c>.
/// </summary>
/// <remarks>
/// The failure comes 3 seconds after startup, except for <see cref="CrashTestScenario.Startup"/>. A secondary instance
/// exits before that, so the test runs only when no other Bravo is open.
/// </remarks>
internal static class CrashTest
{
    public const string Option = "--crash-test";

    private static readonly TimeSpan s_delay = TimeSpan.FromSeconds(3);

    // Keeps the timer alive until it ticks.
    private static System.Windows.Forms.Timer? s_timer;

    /// <summary>
    /// Starts the test that the arguments request, and removes it from them so that the rest of the startup never
    /// sees it.
    /// </summary>
    /// <exception cref="ArgumentException">The option has no scenario, or an unknown one.</exception>
    public static void RunIfRequested(IReadOnlyList<string> args, out string[] remainingArgs)
    {
        if (Extract(args, out remainingArgs) is { } scenario)
        {
            Start(scenario);
        }
    }

    internal static CrashTestScenario? Extract(IReadOnlyList<string> args, out string[] remainingArgs)
    {
        var index = args.ToList().FindIndex((argument) => string.Equals(argument, Option, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            remainingArgs = [.. args];
            return null;
        }

        if (index + 1 == args.Count)
        {
            throw new ArgumentException($"{Option} needs a scenario.", nameof(args));
        }

        var scenario = ParseScenario(args[index + 1]);
        remainingArgs = [.. args.Take(index), .. args.Skip(index + 2)];

        return scenario;
    }

    private static CrashTestScenario ParseScenario(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "startup" => CrashTestScenario.Startup,
            "ui" => CrashTestScenario.Ui,
            "thread" => CrashTestScenario.Thread,
            "unobserved" => CrashTestScenario.Unobserved,
            "stackoverflow" => CrashTestScenario.StackOverflow,
            _ => throw new ArgumentException($"'{name}' is not a crash test scenario.", nameof(name)),
        };
    }

    private static void Start(CrashTestScenario scenario)
    {
        switch (scenario)
        {
            case CrashTestScenario.Startup:
                throw new InvalidOperationException("Crash test: before the message loop.");

            case CrashTestScenario.Ui:
                // Ticks on the thread running the message loop, so the exception escapes a window procedure.
                s_timer = new System.Windows.Forms.Timer { Interval = (int)s_delay.TotalMilliseconds };
                s_timer.Tick += (_, _) => throw new InvalidOperationException("Crash test: UI thread.");
                s_timer.Start();
                break;

            case CrashTestScenario.Thread:
                StartDelayed(() => throw new InvalidOperationException("Crash test: background thread."));
                break;

            case CrashTestScenario.Unobserved:
                StartDelayed(RaiseUnobservedTaskException);
                break;

            case CrashTestScenario.StackOverflow:
                StartDelayed(() => Recurse(0));
                break;
        }
    }

    private static void StartDelayed(Action action)
    {
        var thread = new Thread(() =>
        {
            Thread.Sleep(s_delay);
            action();
        })
        {
            IsBackground = true,
        };

        thread.Start();
    }

    private static void RaiseUnobservedTaskException()
    {
        ThrowUnobserved();

        // The event is raised when the faulted task is finalized, not when it faults.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    // In its own method: a local in the caller would keep the task alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnobserved()
    {
        var task = Task.Run(() => throw new InvalidOperationException("Crash test: unobserved task."));

        while (!task.IsCompleted)
        {
            Thread.Sleep(10);
        }
    }

    // Not a tail call, so that every call takes a stack frame.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Recurse(int depth)
    {
        return Recurse(depth + 1) + 1;
    }
}

internal enum CrashTestScenario
{
    /// <summary>
    /// Throws on the main thread before the message loop starts.
    /// </summary>
    Startup,

    /// <summary>
    /// Throws on the UI thread, from a window procedure.
    /// </summary>
    Ui,

    /// <summary>
    /// Throws on a background thread.
    /// </summary>
    Thread,

    /// <summary>
    /// Faults a task that is never observed: reported, and the process keeps running.
    /// </summary>
    Unobserved,

    /// <summary>
    /// Overflows the stack: the runtime ends the process without calling the handler.
    /// </summary>
    StackOverflow,
}
#endif
