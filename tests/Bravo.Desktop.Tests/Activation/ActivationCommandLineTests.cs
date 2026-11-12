using System.IO;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Development;
using Xunit;

namespace Bravo.Desktop.Tests.Activation;

public sealed class ActivationCommandLineTests
{
    private static readonly BuildIdentity s_release = BuildIdentity.Release("1.1.0");
    private static readonly BuildIdentity s_development = BuildIdentity.Development("1.1.0");

    [Fact]
    public void Parse_NoArguments_ReturnsTheEmptyRequest()
    {
        var request = Parse([]);

        Assert.Same(ActivationRequest.Empty, request);
    }

    [Theory]
    [InlineData("--server", "--database")]
    [InlineData("-s", "-d")]
    public void Parse_ServerAndDatabase_ReturnsBothWithNoError(string serverOption, string databaseOption)
    {
        var request = Parse([serverOption, "localhost:12345", databaseOption, "db"]);

        Assert.Equal(("localhost:12345", "db"), (request.ServerName, request.DatabaseName));
        Assert.Null(request.ParentProcessId);
        Assert.Empty(request.CommandLineErrors);
    }

    // What the external tool registration passes, as Windows splits it:
    // --server="%server%" --database="%database%".
    [Fact]
    public void Parse_ValuesAfterAnEqualsSign_ReturnsThem()
    {
        var request = Parse(["--server=localhost:12345", "--database=db"]);

        Assert.Equal(("localhost:12345", "db"), (request.ServerName, request.DatabaseName));
        Assert.Empty(request.CommandLineErrors);
    }

    [Fact]
    public void Parse_ParentProcessId_ReturnsIt()
    {
        var request = Parse(["--server", "localhost:12345", "--database", "db", "--ppid", "42"]);

        Assert.Equal(42, request.ParentProcessId);
        Assert.Empty(request.CommandLineErrors);
    }

    [Fact]
    public void Parse_ServerWithoutDatabase_KeepsTheServerAndReportsAnError()
    {
        var request = Parse(["--server", "localhost:12345"]);

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

        var request = Parse(args);

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
        var request = Parse([argument]);

        Assert.Null(request.ServerName);
        Assert.Null(request.DatabaseName);
        Assert.Contains(request.CommandLineErrors, error => error.Contains(argument));
    }

    [Fact]
    public void Parse_ResponseFile_IsAnArgumentLikeAnyOtherAndIsNotRead()
    {
        string[] args = ["@arguments.rsp", "--server", "localhost:12345", "--database", "db"];

        var request = Parse(args);

        var error = Assert.Single(request.CommandLineErrors);
        Assert.Contains("@arguments.rsp", error);
    }

    [Fact]
    public void Parse_ReleaseBuild_HasNoDevelopmentOptions()
    {
        var start = ActivationCommandLine.Parse(["--server", "localhost:12345", "--database", "db"], s_release);

        Assert.Same(DevelopmentOptions.None, start.Development);
    }

    [Theory]
    [InlineData("--dev-origin=http://127.0.0.1:8098")]
    [InlineData("--dev-connection-file=host.json")]
    public void Parse_DevelopmentOptionInAReleaseBuild_IsAnErrorLikeAnyArgumentThatIsNotKnown(string argument)
    {
        var start = ActivationCommandLine.Parse([argument], s_release);

        Assert.Same(DevelopmentOptions.None, start.Development);
        Assert.Contains(start.Request.CommandLineErrors, error => error.Contains(argument));
    }

    [Fact]
    public void Parse_DevelopmentBuildWithNoArguments_ReturnsTheEmptyRequestAndNoDevelopmentOption()
    {
        var start = ActivationCommandLine.Parse([], s_development);

        Assert.Same(ActivationRequest.Empty, start.Request);
        Assert.Equal(DevelopmentOptions.None, start.Development);
    }

    [Theory]
    [InlineData("--dev-origin=http://127.0.0.1:8098", "--dev-connection-file=host.json")]
    [InlineData("--dev-origin", "http://127.0.0.1:8098", "--dev-connection-file", "host.json")]
    public void Parse_DevelopmentOptionsInADevelopmentBuild_ReturnsThemAndARequestWithNoError(params string[] args)
    {
        var start = ActivationCommandLine.Parse(args, s_development);

        Assert.Equal("http://127.0.0.1:8098", start.Development.AllowedOrigin);
        Assert.Equal(Path.GetFullPath("host.json"), start.Development.ConnectionFilePath);
        Assert.Same(ActivationRequest.Empty, start.Request);
    }

    [Fact]
    public void Parse_DevelopmentOptionsAmongTheArgumentsOfTheExternalTool_ReturnsBoth()
    {
        string[] args = ["--server=localhost:12345", "--dev-origin=http://127.0.0.1:8098", "--database=db"];

        var start = ActivationCommandLine.Parse(args, s_development);

        Assert.Equal("http://127.0.0.1:8098", start.Development.AllowedOrigin);
        Assert.Equal(("localhost:12345", "db"), (start.Request.ServerName, start.Request.DatabaseName));
        Assert.Empty(start.Request.CommandLineErrors);
    }

    [Theory]
    [InlineData("localhost:8098")]
    [InlineData("http://127.0.0.1:8098/")]
    [InlineData("http://127.0.0.1:8098/index.html")]
    [InlineData("file:///C:/index.html")]
    [InlineData("*")]
    public void Parse_DevelopmentOriginThatIsNotAnOrigin_AllowsNoOriginAndReportsAnError(string origin)
    {
        var start = ActivationCommandLine.Parse(["--dev-origin", origin], s_development);

        Assert.Null(start.Development.AllowedOrigin);
        Assert.Contains(start.Request.CommandLineErrors, error => error.Contains(origin));
    }

    [Fact]
    public void Parse_DevelopmentOptionWithoutAValue_ReportsAnErrorAndStartsWithoutIt()
    {
        var start = ActivationCommandLine.Parse(["--dev-connection-file"], s_development);

        Assert.Null(start.Development.ConnectionFilePath);
        Assert.NotEmpty(start.Request.CommandLineErrors);
    }

    // The request of a release build: the command line that users and the external tool registration have.
    private static ActivationRequest Parse(string[] args)
    {
        return ActivationCommandLine.Parse(args, s_release).Request;
    }
}
