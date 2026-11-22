using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests;

/// <summary>
/// The executable as the build of the tests wrote it, a development build, for the tests that start it. The
/// application has one instance: the classes of those tests are one collection, and run one after the other.
/// </summary>
internal static class BravoExecutable
{
    /// <summary>
    /// The collection of the test classes that start the executable.
    /// </summary>
    public const string Collection = "Bravo.exe";

    public static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(60);

    // The time the host has to stop: a closed window is an ended process within it.
    public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private static readonly string[] s_proxyVariables = ["HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "NO_PROXY"];

    // The build of the tests writes where the project of the executable built it: there it has beside it all
    // that it loads, which the folder of the tests does not hold. Its data goes to the folder of the test.
    public static ProcessStartInfo CreateStartInfo(string dataFolderPath, params string[] arguments)
    {
        string[] allArguments = [$"--dev-data-folder={dataFolderPath}", .. arguments];
        var start = new ProcessStartInfo(ReadPath(), allArguments) { UseShellExecute = false };

        // No test depends on the proxy of the machine: the proxy variables of the process that runs the tests do
        // not reach the application, which reads them before the settings of Windows.
        foreach (var name in s_proxyVariables)
        {
            start.Environment.Remove(name);
        }

        return start;
    }

    /// <summary>
    /// The executable started as the reporter of a crash, as the process that crashed starts it: with no data
    /// folder, which the reporter does not use.
    /// </summary>
    public static ProcessStartInfo CreateReporterStartInfo(string reportPath)
    {
        return new ProcessStartInfo(ReadPath(), [CrashReporter.SwitchName, reportPath]) { UseShellExecute = false };
    }

    public static Task WaitForMainWindowAsync(Process process)
    {
        return WaitForStartAsync(process, () => HasMainWindow(process), "show its window");
    }

    /// <summary>
    /// Waits for what tells that the application has started, such as its window or a file that it writes.
    /// </summary>
    /// <param name="process">The application.</param>
    /// <param name="hasStarted">Whether it is there.</param>
    /// <param name="whatItDoes">What the application does to start, as a failure says it: "show its window".</param>
    public static async Task WaitForStartAsync(Process process, Func<bool> hasStarted, string whatItDoes)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var start = Stopwatch.GetTimestamp();
        while (!hasStarted())
        {
            if (process.HasExited)
            {
                // A start that ends at once with success handed itself over: these are the names of every
                // development build, and one of this user already runs on this machine.
                Assert.SkipWhen(process.ExitCode == 0, "A development build of Bravo is running.");
                Assert.Fail($"Bravo ended with exit code {process.ExitCode} before it could {whatItDoes}.");
            }

            var waited = Stopwatch.GetElapsedTime(start);
            Assert.True(waited < StartTimeout, $"Bravo did not {whatItDoes} in time.");
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
    }

    public static async Task<int> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"The process {process.Id} did not end within {timeout}.");
        }

        return process.ExitCode;
    }

    private static string ReadPath()
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "BravoExecutable.txt")).Trim();
    }

    private static bool HasMainWindow(Process process)
    {
        process.Refresh();

        return process.MainWindowHandle != nint.Zero;
    }

    public static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);

                // The test deletes its data folder next: the application has let go of it by then.
                process.WaitForExit(ShutdownTimeout);
            }
        }
        catch (InvalidOperationException)
        {
            // The process was never started.
        }
    }
}
