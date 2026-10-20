using System.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Xunit;

namespace Bravo.Tests.Infrastructure.Extensions;

public class ProcessExtensionsTests
{
    [Fact]
    public void TryGetById_RunningProcessInTheSession_ReturnsTheProcess()
    {
        using var currentProcess = Process.GetCurrentProcess();

        Assert.True(Process.TryGetById(currentProcess.Id, currentProcess.SessionId, out var process));

        using (process)
        {
            Assert.Equal(currentProcess.Id, process.Id);
        }
    }

    [Fact]
    public void TryGetById_ProcessInAnotherSession_ReturnsFalse()
    {
        using var currentProcess = Process.GetCurrentProcess();

        Assert.False(Process.TryGetById(currentProcess.Id, currentProcess.SessionId + 1, out var process));
        Assert.Null(process);
    }

    [Fact]
    public void TryGetById_ProcessNotRunning_ReturnsFalse()
    {
        using var currentProcess = Process.GetCurrentProcess();
        var startInfo = new ProcessStartInfo("cmd.exe", "/c exit")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        // The handle held until the end of the test keeps Windows from reusing the identifier for another process.
        using var exitedProcess = Process.Start(startInfo);
        Assert.NotNull(exitedProcess);
        exitedProcess.WaitForExit();

        Assert.False(Process.TryGetById(exitedProcess.Id, currentProcess.SessionId, out var process));
        Assert.Null(process);
    }

    [Fact]
    public void GetParentProcess_ChildProcess_ReturnsTheProcessThatStartedIt()
    {
        using var currentProcess = Process.GetCurrentProcess();
        // Redirected input keeps "pause" waiting until the test ends the process.
        var startInfo = new ProcessStartInfo("cmd.exe", "/c pause")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
        };
        using var childProcess = Process.Start(startInfo);
        Assert.NotNull(childProcess);

        try
        {
            using var parentProcess = childProcess.GetParentProcess();

            Assert.NotNull(parentProcess);
            Assert.Equal(currentProcess.Id, parentProcess.Id);
        }
        finally
        {
            childProcess.Kill();
            childProcess.WaitForExit();
        }
    }
}
