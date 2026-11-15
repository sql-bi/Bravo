using System;
using System.IO;
using Bravo.Setup.ExternalTools;
using Bravo.Setup.Telemetry;
using Bravo.Setup.UserData;
using NSubstitute;
using Xunit;

namespace Bravo.Setup.Tests;

public sealed class ProcessBoundaryTests
{
    [Fact]
    public void Run_AProgramThatEnds_ReturnsItsExitCodeAndWritesNothing()
    {
        using var error = new StringWriter();

        var exitCode = ProcessBoundary.Run(() => ExitCode.Failed, error);

        Assert.Equal(ExitCode.Failed, exitCode);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void Run_AnExceptionThatNothingExpected_BecomesAnExitCodeAndALineOfText()
    {
        using var error = new StringWriter();

        var exitCode = ProcessBoundary.Run(() => throw new NotSupportedException("The path has a format that is not valid."), error);

        Assert.Equal(ExitCode.Unexpected, exitCode);
        Assert.Equal("NotSupportedException: The path has a format that is not valid." + Environment.NewLine, error.ToString());
    }

    // The program as Main runs it: a command that throws what no code of the program foresaw.
    [Fact]
    public void Run_ACommandOfTheApplicationThatThrows_EndsWithTheCodeOfADefect()
    {
        using var error = new StringWriter();
        var userData = Substitute.For<IUserDataFolder>();
        userData.When(folder => folder.Remove(Arg.Any<string>())).Do(_ => throw new InvalidOperationException("A defect."));
        var reporter = new InstallationEventReporter(new NullInstallationEventSink(), new SetupEnvironment("PC", "user", "Windows"));
        var application = new SetupApplication(Substitute.For<IExternalToolRegistration>(), reporter, userData, error);

        var exitCode = ProcessBoundary.Run(() => application.Run(["remove-user-data", "--local-application-data", @"C:\Local"]), error);

        Assert.Equal(ExitCode.Unexpected, exitCode);
        Assert.Contains("InvalidOperationException: A defect.", error.ToString(), StringComparison.Ordinal);
    }
}