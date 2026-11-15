using System;
using Bravo.Setup.Telemetry;
using NSubstitute;
using Xunit;

namespace Bravo.Setup.Tests.Telemetry;

public sealed class InstallationEventReporterTests
{
    private static readonly SetupEnvironment s_environment =
        new("LAPTOP-12C9A7VU", "SYSTEM", "Microsoft Windows NT 10.0.26100.0");

    private static readonly InstalledProduct s_product = new("perMachine", "Bravo", "1.1.0", "1.1.0.0");

    private readonly IInstallationEventSink _sink = Substitute.For<IInstallationEventSink>();

    [Theory]
    [InlineData(nameof(InstallationEventKind.Install), "Install")]
    [InlineData(nameof(InstallationEventKind.Uninstall), "Uninstall")]
    public void Report_TelemetryEnabled_SendsTheEventUnderTheNameOfVersion10(string kindName, string name)
    {
        var reporter = new InstallationEventReporter(_sink, s_environment);

        reporter.Report(Enum.Parse<InstallationEventKind>(kindName), s_product, isTelemetryEnabled: true);

        _sink.Received(1).Send(Arg.Is<InstallationEvent>(sent => sent.Name == name));
    }

    [Fact]
    public void Report_TelemetryEnabled_SendsTheDataThatVersion10SendsWithTheEvent()
    {
        var reporter = new InstallationEventReporter(_sink, s_environment);

        reporter.Report(InstallationEventKind.Install, s_product, isTelemetryEnabled: true);

        var expected = new InstallationEvent(
            "Install",
            "e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0",
            "Microsoft Windows NT 10.0.26100.0",
            s_product);
        _sink.Received(1).Send(expected);
    }

    [Fact]
    public void Report_TelemetryDisabled_SendsNothing()
    {
        var reporter = new InstallationEventReporter(_sink, s_environment);

        reporter.Report(InstallationEventKind.Install, s_product, isTelemetryEnabled: false);

        _sink.DidNotReceiveWithAnyArgs().Send(default!);
    }
}
