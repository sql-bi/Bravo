using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

internal sealed class DefaultTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        // Remark: Keep telemetry context configuration synchronized
        // with Sqlbi.Bravo.Installer.Wix.Helpers.GetTelemetryClient()

        // A value the item already carries is kept: the reporter tracks a crash on behalf of the process that crashed.
        telemetry.Context.Device.OperatingSystem ??= TelemetrySessionInfo.DeviceOperatingSystem;
        telemetry.Context.Cloud.RoleInstance ??= TelemetrySessionInfo.CloudRoleInstance;
        telemetry.Context.Component.Version ??= TelemetrySessionInfo.ComponentVersion;
        telemetry.Context.Session.Id ??= TelemetrySessionInfo.SessionId;
        telemetry.Context.User.Id ??= TelemetrySessionInfo.UserId;

        foreach (var property in TelemetrySessionInfo.GlobalProperties)
        {
            telemetry.Context.GlobalProperties.TryAdd(property.Key, property.Value);
        }
    }
}
