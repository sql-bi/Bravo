using System;
using System.IO;
using Bravo.Setup.ExternalTools;
using Bravo.Setup.Telemetry;
using Bravo.Setup.UserData;
using NSubstitute;
using Xunit;

namespace Bravo.Setup.Tests;

/// <summary>
/// The program as <c>Main</c> composes it, with the sink of the events replaced: real files, in the folder
/// of the test.
/// </summary>
public sealed class SetupApplicationTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly IInstallationEventSink _sink = Substitute.For<IInstallationEventSink>();
    private readonly StringWriter _error = new();
    private readonly SetupApplication _application;

    public SetupApplicationTests()
    {
        var environment = new SetupEnvironment("LAPTOP-12C9A7VU", "SYSTEM", "Microsoft Windows NT 10.0.26100.0");
        _application = new SetupApplication(
            new ExternalToolRegistration(),
            new InstallationEventReporter(_sink, environment),
            new UserDataFolder(),
            _error);
    }

    public void Dispose()
    {
        _error.Dispose();
        _folder.Dispose();
    }

    [Fact]
    public void Run_RegisterThenUnregister_WritesTheFileThenRemovesIt()
    {
        var toolFile = _folder.Combine("External Tools", "bravo.pbitool.json");

        var registered = _application.Run(["register-external-tool", "--tool-file", toolFile, "--executable", @"C:\Bravo\Bravo.exe"]);
        var wasWritten = File.Exists(toolFile);
        var unregistered = _application.Run(["unregister-external-tool", "--tool-file", toolFile, "--executable", @"C:\Bravo\Bravo.exe"]);

        Assert.Equal((ExitCode.Success, ExitCode.Success), (registered, unregistered));
        Assert.True(wasWritten);
        Assert.False(File.Exists(toolFile));
    }

    [Fact]
    public void Run_ReportInstall_SendsTheEventAndSucceeds()
    {
        var exitCode = _application.Run(
        [
            "report-install",
            "--scope", "perUser",
            "--product-name", "Bravo",
            "--product-version", "1.1.0",
            "--product-build", "1.1.0.0",
            "--telemetry-enabled", "1",
        ]);

        Assert.Equal(ExitCode.Success, exitCode);
        var product = new InstalledProduct("perUser", "Bravo", "1.1.0", "1.1.0.0");
        _sink.Received(1).Send(Arg.Is<InstallationEvent>(sent => sent.Name == "Install" && sent.Product == product));
    }

    [Fact]
    public void Run_RemoveUserData_RemovesTheDataFolderOfTheApplication()
    {
        var dataFolder = _folder.Combine("Bravo");
        Directory.CreateDirectory(dataFolder);
        File.WriteAllText(Path.Combine(dataFolder, "usersettings.json"), "{}");

        var exitCode = _application.Run(["remove-user-data", "--local-application-data", _folder.Path]);

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.False(Directory.Exists(dataFolder));
    }

    [Fact]
    public void Run_ACommandLineThatIsNotValid_SaysWhyAndDoesNothing()
    {
        var exitCode = _application.Run(["register-external-tool", "--tool-file", _folder.Combine("bravo.pbitool.json")]);

        Assert.Equal(ExitCode.InvalidCommandLine, exitCode);
        Assert.Contains("--executable", _error.ToString(), StringComparison.Ordinal);
        Assert.Empty(Directory.GetFileSystemEntries(_folder.Path));
    }

    // The package ignores the exit code: a command that cannot be carried out never fails an installation.
    [Fact]
    public void Run_AFileThatCannotBeWritten_FailsWithoutAnException()
    {
        var toolFile = _folder.Combine("bravo.pbitool.json");
        Directory.CreateDirectory(toolFile);

        var exitCode = _application.Run(["register-external-tool", "--tool-file", toolFile, "--executable", @"C:\Bravo\Bravo.exe"]);

        Assert.Equal(ExitCode.Failed, exitCode);
        Assert.NotEmpty(_error.ToString());
    }
}
