using System;

namespace Bravo.Setup.Telemetry;

/// <summary>
/// Reports what a package did, when who installs chose to send the telemetry of the installer. As in 1.0.x, the
/// choice alone decides: the policies of an administrator are for the application.
/// </summary>
internal sealed class InstallationEventReporter(IInstallationEventSink sink, SetupEnvironment environment)
{
    private readonly IInstallationEventSink _sink = sink;
    private readonly SetupEnvironment _environment = environment;

    public void Report(InstallationEventKind kind, InstalledProduct product, bool isTelemetryEnabled)
    {
        if (!isTelemetryEnabled)
        {
            return;
        }

        var userId = TelemetryUserId.Create(_environment.MachineName, _environment.UserName);
        _sink.Send(new InstallationEvent(GetName(kind), userId, _environment.OperatingSystem, product));
    }

    // The names of the events of 1.0.x: the counts of one version and of the other are compared by them.
    private static string GetName(InstallationEventKind kind)
    {
        return kind switch
        {
            InstallationEventKind.Install => "Install",
            InstallationEventKind.Uninstall => "Uninstall",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null),
        };
    }
}
