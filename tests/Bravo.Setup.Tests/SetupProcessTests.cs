using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Setup.Tests;

/// <summary>
/// The executable as the build of the tests wrote it, started as an installer package starts it: a process
/// with a command line, which shows nothing and ends with an exit code.
/// </summary>
public sealed class SetupProcessTests : IDisposable
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    private readonly TemporaryFolder _folder = new();

    public void Dispose()
    {
        _folder.Dispose();
    }

    // The paths have spaces, as the folders that Windows Installer installs in.
    [Fact]
    public async Task Start_RegisterExternalTool_WritesTheFileAndEndsWithSuccess()
    {
        var toolFile = _folder.Combine("External Tools", "bravo.pbitool.json");
        var executable = _folder.Combine("Program Files", "Bravo.exe");

        var result = await RunAsync("register-external-tool", "--tool-file", toolFile, "--executable", executable);

        Assert.Equal((0, ""), result);
        using var tool = JsonDocument.Parse(await File.ReadAllBytesAsync(toolFile, TestContext.Current.CancellationToken));
        Assert.Equal(executable, tool.RootElement.GetProperty("path").GetString());
    }

    [Fact]
    public async Task Start_ReportInstall_SendsNothingYetAndEndsWithSuccess()
    {
        var result = await RunAsync(
            "report-install",
            "--scope", "perMachine",
            "--product-name", "Bravo",
            "--product-version", "1.1.0",
            "--product-build", "1.1.0.0",
            "--telemetry-enabled", "1");

        Assert.Equal((0, ""), result);
    }

    [Fact]
    public async Task Start_NoCommand_EndsWithTheCodeOfACommandLineThatIsNotValid()
    {
        var (exitCode, error) = await RunAsync();

        Assert.Equal(2, exitCode);
        Assert.NotEmpty(error);
    }

    // The build of the tests writes where the project of the executable built it.
    private static async Task<(int ExitCode, string Error)> RunAsync(params string[] arguments)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var pathFile = Path.Combine(AppContext.BaseDirectory, "SetupExecutable.txt");
        var path = (await File.ReadAllTextAsync(pathFile, cancellationToken)).Trim();
        var startInfo = new ProcessStartInfo(path, arguments)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("The program did not start.");
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(s_timeout);
        try
        {
            var error = await process.StandardError.ReadToEndAsync(timeoutSource.Token);
            await process.WaitForExitAsync(timeoutSource.Token);
            return (process.ExitCode, error.Trim());
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
    }
}
