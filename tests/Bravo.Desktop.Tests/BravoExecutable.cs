using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

    // The build of the tests writes where the project of the executable built it: there it has beside it all
    // that it loads, which the folder of the tests does not hold. Its data goes to the folder of the test.
    public static ProcessStartInfo CreateStartInfo(string dataFolderPath, params string[] arguments)
    {
        var pathFile = Path.Combine(AppContext.BaseDirectory, "BravoExecutable.txt");
        var path = File.ReadAllText(pathFile).Trim();
        string[] allArguments = [$"--dev-data-folder={dataFolderPath}", .. arguments];
        return new ProcessStartInfo(path, allArguments) { UseShellExecute = false };
    }

    public static async Task WaitForMainWindowAsync(Process process)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var start = Stopwatch.GetTimestamp();
        while (process.MainWindowHandle == nint.Zero)
        {
            if (process.HasExited)
            {
                // A start that ends at once with success handed itself over: these are the names of every
                // development build, and one of this user already runs on this machine.
                Assert.SkipWhen(process.ExitCode == 0, "A development build of Bravo is running.");
                Assert.Fail($"Bravo ended with exit code {process.ExitCode} before it showed its window.");
            }

            var waited = Stopwatch.GetElapsedTime(start);
            Assert.True(waited < StartTimeout, "Bravo did not show its window in time.");
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            process.Refresh();
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

    public static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process was never started.
        }
    }
}
