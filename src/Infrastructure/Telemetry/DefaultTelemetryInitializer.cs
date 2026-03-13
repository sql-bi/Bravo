namespace Sqlbi.Bravo.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

internal sealed class DefaultTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        // Remark: Keep telemetry context configuration synchronized
        // with Sqlbi.Bravo.Installer.Wix.Helpers.GetTelemetryClient()

        telemetry.Context.Component.Version = TelemetrySessionInfo.ComponentVersion;
        telemetry.Context.Device.OperatingSystem = TelemetrySessionInfo.DeviceOperatingSystem;
        telemetry.Context.Session.Id = TelemetrySessionInfo.SessionId;
        telemetry.Context.User.Id = TelemetrySessionInfo.UserId;

        foreach (var property in TelemetrySessionInfo.GlobalProperties)
        {
            telemetry.Context.GlobalProperties.Add(property.Key, property.Value);
        }
    }
}
