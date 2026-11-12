using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Xunit;

namespace Bravo.Desktop.Tests.Activation;

public sealed class ActivationCommandLineTests
{
    [Fact]
    public void Parse_NoArguments_ReturnsTheEmptyRequest()
    {
        var request = ActivationCommandLine.Parse([]);

        Assert.Same(ActivationRequest.Empty, request);
    }

    [Theory]
    [InlineData("--server", "--database")]
    [InlineData("-s", "-d")]
    public void Parse_ServerAndDatabase_ReturnsBothWithNoError(string serverOption, string databaseOption)
    {
        var request = ActivationCommandLine.Parse([serverOption, "localhost:12345", databaseOption, "db"]);

        Assert.Equal(("localhost:12345", "db"), (request.ServerName, request.DatabaseName));
        Assert.Null(request.ParentProcessId);
        Assert.Empty(request.CommandLineErrors);
    }

    // What the external tool registration passes, as Windows splits it:
    // --server="%server%" --database="%database%".
    [Fact]
    public void Parse_ValuesAfterAnEqualsSign_ReturnsThem()
    {
        var request = ActivationCommandLine.Parse(["--server=localhost:12345", "--database=db"]);

        Assert.Equal(("localhost:12345", "db"), (request.ServerName, request.DatabaseName));
        Assert.Empty(request.CommandLineErrors);
    }

    [Fact]
    public void Parse_ParentProcessId_ReturnsIt()
    {
        var request = ActivationCommandLine.Parse(["--server", "localhost:12345", "--database", "db", "--ppid", "42"]);

        Assert.Equal(42, request.ParentProcessId);
        Assert.Empty(request.CommandLineErrors);
    }

    [Fact]
    public void Parse_ServerWithoutDatabase_KeepsTheServerAndReportsAnError()
    {
        var request = ActivationCommandLine.Parse(["--server", "localhost:12345"]);

        Assert.Equal("localhost:12345", request.ServerName);
        Assert.Null(request.DatabaseName);
        Assert.NotEmpty(request.CommandLineErrors);
    }

    [Theory]
    [InlineData("--ppid", "abc")]
    [InlineData("--ppid", "")]
    [InlineData("--ppid")]
    public void Parse_ParentProcessIdThatIsNotANumber_ReportsAnErrorAndKeepsTheRest(params string[] parentProcessId)
    {
        string[] args = ["--server", "localhost:12345", "--database", "db", .. parentProcessId];

        var request = ActivationCommandLine.Parse(args);

        Assert.Null(request.ParentProcessId);
        Assert.Equal(("localhost:12345", "db"), (request.ServerName, request.DatabaseName));
        Assert.NotEmpty(request.CommandLineErrors);
    }

    [Theory]
    [InlineData(@"C:\Reports\Contoso.pbix")]
    [InlineData("--help")]
    [InlineData("--version")]
    [InlineData("/server")]
    public void Parse_ArgumentThatIsNotAnOption_ReportsItAndReturnsNoValue(string argument)
    {
        var request = ActivationCommandLine.Parse([argument]);

        Assert.Null(request.ServerName);
        Assert.Null(request.DatabaseName);
        Assert.Contains(request.CommandLineErrors, error => error.Contains(argument));
    }

    [Fact]
    public void Parse_ResponseFile_IsAnArgumentLikeAnyOtherAndIsNotRead()
    {
        string[] args = ["@arguments.rsp", "--server", "localhost:12345", "--database", "db"];

        var request = ActivationCommandLine.Parse(args);

        var error = Assert.Single(request.CommandLineErrors);
        Assert.Contains("@arguments.rsp", error);
    }
}
