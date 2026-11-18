using System;
using Bravo.Setup.CommandLine;
using Bravo.Setup.Telemetry;
using Xunit;

namespace Bravo.Setup.Tests.CommandLine;

public sealed class SetupCommandLineTests
{
    private const string ToolFile = @"C:\Tools\bravo.pbitool.json";
    private const string Executable = @"C:\Program Files\SQLBI\Bravo\Bravo.exe";

    [Fact]
    public void TryParse_RegisterExternalTool_ReturnsTheFileAndTheExecutable()
    {
        var command = Parse("register-external-tool", "--tool-file", ToolFile, "--executable", Executable);

        Assert.Equal(new RegisterExternalToolCommand(ToolFile, Executable), command);
    }

    [Fact]
    public void TryParse_OptionsInAnotherOrder_ReturnsTheSameCommand()
    {
        var command = Parse("register-external-tool", "--executable", Executable, "--tool-file", ToolFile);

        Assert.Equal(new RegisterExternalToolCommand(ToolFile, Executable), command);
    }

    [Fact]
    public void TryParse_UnregisterExternalTool_ReturnsTheFileAndTheExecutable()
    {
        var command = Parse("unregister-external-tool", "--tool-file", ToolFile, "--executable", Executable);

        Assert.Equal(new UnregisterExternalToolCommand(ToolFile, Executable), command);
    }

    [Theory]
    [InlineData("report-install", nameof(InstallationEventKind.Install))]
    [InlineData("report-uninstall", nameof(InstallationEventKind.Uninstall))]
    public void TryParse_Report_ReturnsTheKindAndWhatThePackageSaysOfItself(string name, string kindName)
    {
        var command = Parse([name, .. ReportOptions(telemetryEnabled: "1")]);

        var kind = Enum.Parse<InstallationEventKind>(kindName);
        var product = new InstalledProduct("perMachine", "Bravo", "1.1.0", "1.1.0.0");
        Assert.Equal(new ReportInstallationEventCommand(kind, product, IsTelemetryEnabled: true), command);
    }

    // The package passes the value of its property: "1" when who installs chose to send, nothing otherwise.
    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("true")]
    [InlineData(" 1")]
    public void TryParse_ReportWithATelemetryValueThatIsNotOne_ReturnsTelemetryDisabled(string telemetryEnabled)
    {
        var command = Parse(["report-install", .. ReportOptions(telemetryEnabled)]);

        var report = Assert.IsType<ReportInstallationEventCommand>(command);
        Assert.False(report.IsTelemetryEnabled);
    }

    [Fact]
    public void TryParse_RemoveUserData_ReturnsTheFolderOfTheLocalApplicationData()
    {
        var command = Parse("remove-user-data", "--local-application-data", @"C:\Users\someone\AppData\Local\");

        Assert.Equal(new RemoveUserDataCommand(@"C:\Users\someone\AppData\Local\"), command);
    }

    [Theory]
    [InlineData]
    [InlineData("install")]
    [InlineData("--tool-file", ToolFile)]
    [InlineData("REGISTER-EXTERNAL-TOOL", "--tool-file", ToolFile, "--executable", Executable)]
    public void TryParse_NoCommandOrAnUnknownOne_ReturnsAnError(params string[] arguments)
    {
        var isParsed = SetupCommandLine.TryParse(arguments, out var command, out var error);

        Assert.False(isParsed);
        Assert.Null(command);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("register-external-tool", "--tool-file", ToolFile)]
    [InlineData("register-external-tool", "--tool-file", ToolFile, "--executable")]
    [InlineData("register-external-tool", "--tool-file", ToolFile, "--executable", Executable, "--folder", @"C:\Data")]
    [InlineData("register-external-tool", "--tool-file", ToolFile, "--tool-file", ToolFile, "--executable", Executable)]
    [InlineData("register-external-tool", ToolFile, Executable)]
    [InlineData("unregister-external-tool")]
    [InlineData("unregister-external-tool", "--tool-file", ToolFile)]
    [InlineData("report-install", "--scope", "perUser", "--product-name", "Bravo", "--product-version", "1.1.0")]
    [InlineData("remove-user-data")]
    [InlineData("remove-user-data", "--folder", @"C:\Users\someone\AppData\Local\Bravo")]
    public void TryParse_OptionsThatAreNotTheOnesOfTheCommand_ReturnsAnError(params string[] arguments)
    {
        var isParsed = SetupCommandLine.TryParse(arguments, out var command, out var error);

        Assert.False(isParsed);
        Assert.Null(command);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    // A relative path would name another file for every folder that the program is started in.
    [Theory]
    [InlineData("register-external-tool", "--tool-file", "bravo.pbitool.json", "--executable", Executable)]
    [InlineData("register-external-tool", "--tool-file", ToolFile, "--executable", "Bravo.exe")]
    [InlineData("unregister-external-tool", "--tool-file", @"External Tools\bravo.pbitool.json", "--executable", Executable)]
    [InlineData("unregister-external-tool", "--tool-file", "", "--executable", Executable)]
    [InlineData("unregister-external-tool", "--tool-file", ToolFile, "--executable", "Bravo.exe")]
    [InlineData("remove-user-data", "--local-application-data", "Local")]
    [InlineData("remove-user-data", "--local-application-data", @"\AppData\Local")]
    public void TryParse_APathThatIsNotFull_ReturnsAnError(params string[] arguments)
    {
        var isParsed = SetupCommandLine.TryParse(arguments, out _, out var error);

        Assert.False(isParsed);
        Assert.Contains("full path", error);
    }

    private static string[] ReportOptions(string telemetryEnabled)
    {
        return
        [
            "--scope", "perMachine",
            "--product-name", "Bravo",
            "--product-version", "1.1.0",
            "--product-build", "1.1.0.0",
            "--telemetry-enabled", telemetryEnabled,
        ];
    }

    private static SetupCommand Parse(params string[] arguments)
    {
        var isParsed = SetupCommandLine.TryParse(arguments, out var command, out var error);

        Assert.True(isParsed, error);
        return command!;
    }
}
