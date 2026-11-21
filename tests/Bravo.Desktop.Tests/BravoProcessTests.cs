using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Xunit;

namespace Bravo.Desktop.Tests;

/// <summary>
/// The executable as the build of the tests wrote it, a development build: real processes, the names of the
/// instance of a development build, a real window.
/// </summary>
[Collection(BravoExecutable.Collection)]
public sealed class BravoProcessTests : IDisposable
{
    private static readonly TimeSpan s_startTimeout = BravoExecutable.StartTimeout;
    private static readonly TimeSpan s_shutdownTimeout = BravoExecutable.ShutdownTimeout;

    // WM_QUERYENDSESSION and WM_ENDSESSION, with ENDSESSION_CLOSEAPP: the application is asked to end so
    // that its files can be replaced, and not because the session of the user ends.
    private const uint QueryEndSession = 0x0011;
    private const uint EndSession = 0x0016;
    private const int CloseApplication = 0x1;

    private readonly TemporaryFolder _dataFolder = new();

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

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

    // What Restart Manager sends to the window of an application that holds the files that an installation
    // replaces: the question, then the order. An application that answers yes and ends is replaced with no
    // restart of Windows.
    [Fact]
    public async Task EndSession_AskedAsRestartManagerAsks_IsAllowedAndEndsTheProcessWithSuccess()
    {
        using var bravo = StartBravo();
        try
        {
            await WaitForMainWindowAsync(bravo);
            var window = (HWND)bravo.MainWindowHandle;

            var answer = await SendAsync(window, QueryEndSession, default);
            await SendAsync(window, EndSession, new WPARAM(1));
            var exitCode = await WaitForExitAsync(bravo, s_shutdownTimeout);

            Assert.Equal(1, answer);
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Kill(bravo);
        }
    }

    // The message is the only window of the process: the host is never built.
    [Fact]
    public async Task Start_WithPoliciesThatCannotBeRead_ShowsAMessageAndEndsWithItsCode()
    {
        using var registry = new TemporaryRegistryKey();
        var keyName = registry.DenyReading("Policies");
        using var bravo = StartBravo($"--dev-policy-key={keyName}");
        try
        {
            await WaitForMainWindowAsync(bravo);
            var title = bravo.MainWindowTitle;

            bravo.CloseMainWindow();
            var exitCode = await WaitForExitAsync(bravo, s_shutdownTimeout);

            Assert.Equal("Bravo for Power BI", title);
            Assert.Equal(ExitCode.PoliciesUnreadable, exitCode);
        }
        finally
        {
            Kill(bravo);
        }
    }

    // The instance is released before the message: a second start reads the policies itself, and is not handed
    // over to a process that has no host to take it.
    [Fact]
    public async Task Start_WhileTheMessageOfPoliciesThatCannotBeReadIsShown_ShowsItsOwnMessage()
    {
        using var registry = new TemporaryRegistryKey();
        var policyKey = $"--dev-policy-key={registry.DenyReading("Policies")}";
        using var first = StartBravo(policyKey);
        using var second = new Process();
        try
        {
            await WaitForMainWindowAsync(first);
            second.StartInfo = CreateStartInfo(policyKey);
            second.Start();
            await WaitForMainWindowAsync(second);

            second.CloseMainWindow();
            first.CloseMainWindow();
            var secondExitCode = await WaitForExitAsync(second, s_shutdownTimeout);
            var firstExitCode = await WaitForExitAsync(first, s_shutdownTimeout);

            Assert.Equal(ExitCode.PoliciesUnreadable, secondExitCode);
            Assert.Equal(ExitCode.PoliciesUnreadable, firstExitCode);
        }
        finally
        {
            Kill(second);
            Kill(first);
        }
    }

    // SendMessage returns when the window has answered, and a window that hangs never does. The wait has an
    // end: the test then fails, and the end of the process, which the test asks for, ends the call as well.
    private static async Task<nint> SendAsync(HWND window, uint message, WPARAM wParam)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var answer = Task.Run(() => PInvoke.SendMessage(window, message, wParam, new LPARAM(CloseApplication)).Value, cancellationToken);
        try
        {
            return await answer.WaitAsync(s_shutdownTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            Assert.Fail($"The window did not answer the message 0x{message:X4} within {s_shutdownTimeout}.");
            throw;
        }
    }

    private Process StartBravo(params string[] arguments)
    {
        return Process.Start(CreateStartInfo(arguments)) ?? throw new InvalidOperationException("Bravo did not start.");
    }

    private ProcessStartInfo CreateStartInfo(params string[] arguments)
    {
        return BravoExecutable.CreateStartInfo(_dataFolder.Path, arguments);
    }

    private static Task WaitForMainWindowAsync(Process process)
    {
        return BravoExecutable.WaitForMainWindowAsync(process);
    }

    private static Task<int> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        return BravoExecutable.WaitForExitAsync(process, timeout);
    }

    private static void Kill(Process process)
    {
        BravoExecutable.Kill(process);
    }
}
