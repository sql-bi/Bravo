using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Sqlbi.Bravo.Infrastructure.Security;
using System;
using System.Diagnostics;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class TelemetryHelper
    {
        public static TelemetryClient CreateTelemetryClient()
        {
            var telemetryChannel = new ServerTelemetryChannel
            {
                DeveloperMode = Debugger.IsAttached || AppConstants.IsDebug
            };

            var telemetryConfiguration = new TelemetryConfiguration
            {
                InstrumentationKey = AppConstants.TelemetryInstrumentationKey,
                DisableTelemetry = false, // TOFIX: telemetry enabled/disabled
                TelemetryChannel = telemetryChannel
            };

            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            telemetryClient.Context.Component.Version = AppConstants.ApplicationFileVersion;
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            telemetryClient.Context.User.Id = $"{ Environment.MachineName }\\{ Environment.UserName }".ToSHA256Hash();

            return telemetryClient;
        }
    }
}
