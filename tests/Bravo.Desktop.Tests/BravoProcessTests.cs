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
            await BravoExecutable.WaitForMainWindowAsync(first);

            second.StartInfo = CreateStartInfo("--server", "localhost:12345", "--database", "db");
            second.Start();
            var secondExitCode = await BravoExecutable.WaitForExitAsync(second, BravoExecutable.StartTimeout);
            var firstWasRunning = !first.HasExited;

            first.CloseMainWindow();
            var firstExitCode = await BravoExecutable.WaitForExitAsync(first, BravoExecutable.ShutdownTimeout);

            Assert.Equal(0, secondExitCode);
            Assert.True(firstWasRunning);
            Assert.Equal(0, firstExitCode);
        }
        finally
        {
            BravoExecutable.Kill(second);
            BravoExecutable.Kill(first);
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
            await BravoExecutable.WaitForMainWindowAsync(bravo);
            var window = (HWND)bravo.MainWindowHandle;

            var answer = await SendAsync(window, QueryEndSession, default);
            await SendAsync(window, EndSession, new WPARAM(1));
            var exitCode = await BravoExecutable.WaitForExitAsync(bravo, BravoExecutable.ShutdownTimeout);

            Assert.Equal(1, answer);
            Assert.Equal(0, exitCode);
        }
        finally
        {
            BravoExecutable.Kill(bravo);
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
            await BravoExecutable.WaitForMainWindowAsync(bravo);
            var title = bravo.MainWindowTitle;

            bravo.CloseMainWindow();
            var exitCode = await BravoExecutable.WaitForExitAsync(bravo, BravoExecutable.ShutdownTimeout);

            Assert.Equal("Bravo for Power BI", title);
            Assert.Equal(ExitCode.PoliciesUnreadable, exitCode);
        }
        finally
        {
            BravoExecutable.Kill(bravo);
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
            await BravoExecutable.WaitForMainWindowAsync(first);
            second.StartInfo = CreateStartInfo(policyKey);
            second.Start();
            await BravoExecutable.WaitForMainWindowAsync(second);

            second.CloseMainWindow();
            first.CloseMainWindow();
            var secondExitCode = await BravoExecutable.WaitForExitAsync(second, BravoExecutable.ShutdownTimeout);
            var firstExitCode = await BravoExecutable.WaitForExitAsync(first, BravoExecutable.ShutdownTimeout);

            Assert.Equal(ExitCode.PoliciesUnreadable, secondExitCode);
            Assert.Equal(ExitCode.PoliciesUnreadable, firstExitCode);
        }
        finally
        {
            BravoExecutable.Kill(second);
            BravoExecutable.Kill(first);
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
            return await answer.WaitAsync(BravoExecutable.ShutdownTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            Assert.Fail($"The window did not answer the message 0x{message:X4} within {BravoExecutable.ShutdownTimeout}.");
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
}
