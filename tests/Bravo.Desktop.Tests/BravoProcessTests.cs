using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Desktop.Tests;

/// <summary>
/// The executable as the build of the tests wrote it, a development build: real processes, the names of the
/// instance of a development build, a real window.
/// </summary>
public sealed class BravoProcessTests
{
    private static readonly TimeSpan s_startTimeout = TimeSpan.FromSeconds(60);

    // The time the host has to stop: a closed window is an ended process within it.
    private static readonly TimeSpan s_shutdownTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Start_WhileAnInstanceRuns_HandsOverAndExitsAndTheInstanceGoesOnUntilItIsClosed()
    {
        using var first = StartBravo();
        using var second = new Process();
        try
        {
            await WaitForMainWindowAsync(first);

            second.StartInfo = CreateStartInfo("--server", "localhost:12345", "--database", "db");
            second.Start();
            var secondExitCode = await WaitForExitAsync(second, s_startTimeout);
            var firstWasRunning = !first.HasExited;

            first.CloseMainWindow();
            var firstExitCode = await WaitForExitAsync(first, s_shutdownTimeout);

            Assert.Equal(0, secondExitCode);
            Assert.True(firstWasRunning);
            Assert.Equal(0, firstExitCode);
        }
        finally
        {
            Kill(second);
            Kill(first);
        }
    }

    private static Process StartBravo()
    {
        return Process.Start(CreateStartInfo()) ?? throw new InvalidOperationException("Bravo did not start.");
    }

    // The build of the tests writes where the project of the executable built it: there it has beside it all
    // that it loads, which the folder of the tests does not hold.
    private static ProcessStartInfo CreateStartInfo(params string[] arguments)
    {
        var pathFile = Path.Combine(AppContext.BaseDirectory, "BravoExecutable.txt");
        var path = File.ReadAllText(pathFile).Trim();
        return new ProcessStartInfo(path, arguments) { UseShellExecute = false };
    }

    private static async Task WaitForMainWindowAsync(Process process)
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
            Assert.True(waited < s_startTimeout, "Bravo did not show its window in time.");
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            process.Refresh();
        }
    }

    private static async Task<int> WaitForExitAsync(Process process, TimeSpan timeout)
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

    private static void Kill(Process process)
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
