using System.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Xunit;

namespace Bravo.Tests.Infrastructure.Configuration.Settings;

public class StartupCommandLineTests
{
    [Fact]
    public void Parse_NoArguments_IsEmpty()
    {
        var commandLine = StartupCommandLine.Parse([]);

        Assert.True(commandLine.IsEmpty);
        Assert.False(commandLine.HasConnectionArguments);
        Assert.Empty(commandLine.Errors);
    }

    [Theory]
    [InlineData("--server", "--database")]
    [InlineData("-s", "-d")]
    public void Parse_ServerAndDatabase_ReturnsBoth(string serverOption, string databaseOption)
    {
        var commandLine = StartupCommandLine.Parse([serverOption, "localhost:12345", databaseOption, "db"]);

        Assert.False(commandLine.IsEmpty);
        Assert.True(commandLine.HasConnectionArguments);
        Assert.Equal("localhost:12345", commandLine.ServerName);
        Assert.Equal("db", commandLine.DatabaseName);
        Assert.Null(commandLine.ParentProcessId);
        Assert.Empty(commandLine.Errors);
    }

    [Fact]
    public void Parse_MissingDatabase_KeepsTheServerAndReportsAnError()
    {
        var commandLine = StartupCommandLine.Parse(["--server", "localhost:12345"]);

        Assert.True(commandLine.HasConnectionArguments);
        Assert.Equal("localhost:12345", commandLine.ServerName);
        Assert.Null(commandLine.DatabaseName);
        Assert.NotEmpty(commandLine.Errors);
    }

    [Fact]
    public void Parse_ParentProcessId_ReturnsIt()
    {
        var commandLine = StartupCommandLine.Parse(["--server", "localhost:12345", "--database", "db", "--ppid", "42"]);

        Assert.Equal(42, commandLine.ParentProcessId);
        Assert.Empty(commandLine.Errors);
    }

    [Theory]
    [InlineData("--ppid", "abc")]
    [InlineData("--ppid", "")]
    [InlineData("--ppid")]
    public void Parse_InvalidParentProcessId_ReportsAnErrorWithoutAValue(params string[] parentProcessIdArguments)
    {
        string[] args = ["--server", "localhost:12345", "--database", "db", .. parentProcessIdArguments];

        var commandLine = StartupCommandLine.Parse(args);

        Assert.Null(commandLine.ParentProcessId);
        Assert.Equal("localhost:12345", commandLine.ServerName);
        Assert.NotEmpty(commandLine.Errors);
    }

    [Fact]
    public void Parse_UnknownArgument_ReportsErrorsWithoutConnectionArguments()
    {
        var commandLine = StartupCommandLine.Parse([@"C:\Reports\Contoso.pbix"]);

        Assert.False(commandLine.IsEmpty);
        Assert.False(commandLine.HasConnectionArguments);
        Assert.Null(commandLine.ServerName);
        Assert.NotEmpty(commandLine.Errors);
    }

    [Fact]
    public void FromCommandLine_EmptyCommandLine_SetsOnlyIsEmpty()
    {
        // The activation payload of a launch without arguments carries null for every other value.
        var settings = new StartupSettings();
        var commandLine = StartupCommandLine.Parse([]);
        using var currentProcess = Process.GetCurrentProcess();

        settings.FromCommandLine(commandLine, currentProcess);

        Assert.True(settings.IsEmpty);
        Assert.Null(settings.CommandLineErrors);
        Assert.Null(settings.ParentProcessId);
    }

    [Fact]
    public void FromCommandLine_ConnectionArguments_SetsTheArguments()
    {
        var settings = new StartupSettings();
        var commandLine = StartupCommandLine.Parse(["--server", "localhost:12345", "--database", "db"]);
        using var currentProcess = Process.GetCurrentProcess();

        settings.FromCommandLine(commandLine, currentProcess);

        Assert.False(settings.IsEmpty);
        Assert.True(settings.IsExternalTool);
        Assert.Equal("localhost:12345", settings.ArgumentServerName);
        Assert.Equal("db", settings.ArgumentDatabaseName);
        Assert.NotNull(settings.CommandLineErrors);
        Assert.Empty(settings.CommandLineErrors);
    }
}
