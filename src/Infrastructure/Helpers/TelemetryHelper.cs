using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Sqlbi.Bravo.Infrastructure.Security;
using System;
using System.Diagnostics;
using System.Threading;

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

        public static void TrackException(Exception exception)
        {
            var telemetry = CreateTelemetryClient();
            telemetry.TrackException(exception);
            telemetry.Flush();

            // Flush is not blocking when not using InMemoryChannel so wait a bit.
            // There is an active issue regarding the need for Sleep/Delay which is tracked here: https://github.com/microsoft/ApplicationInsights-dotnet/issues/407
            Thread.Sleep(1000);
        }
    }
}
