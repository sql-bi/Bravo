using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class TelemetryHelper
    {
        private static readonly Lazy<TelemetryConfiguration> _telemetryConfiguration = new(Configure(new TelemetryConfiguration()));

        public static TelemetryConfiguration Configure(TelemetryConfiguration configuration)
        {
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            configuration.TelemetryInitializers.Add(new ContextTelemetryInitializer());
            configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached || AppConstants.VersionInfo.IsDebug;
            configuration.InstrumentationKey = AppConstants.TelemetryInstrumentationKey;
            configuration.DisableTelemetry = UserPreferences.Current.TelemetryEnabled == false;
            
            return configuration;
        }

        public static TelemetryConfiguration TelemetryConfigurationInstance => _telemetryConfiguration.Value;

        public static void TrackException(Exception exception)
        {
            if (exception is AggregateException aex)
                exception = aex.GetBaseException();

            var telemetryClient = new TelemetryClient(TelemetryConfigurationInstance);
            telemetryClient.TrackException(exception);
            telemetryClient.Flush(); // Flush is blocking when using InMemoryChannel, no need for Sleep/Delay
        }
    }

    internal class ContextTelemetryInitializer : ITelemetryInitializer
    {
        public static readonly string DeviceOperatingSystem = Environment.OSVersion.ToString();
        public static readonly string ComponentVersion = AppConstants.ApplicationProductVersion;
        public static readonly string SessionId = Guid.NewGuid().ToString();
        public static readonly string? UserId = $"{ Environment.MachineName }\\{ Environment.UserName }".ToSHA256Hash();
        public static readonly IReadOnlyDictionary<string, string> GlobalProperties = new Dictionary<string, string>
        {
            { "ProductName", AppConstants.ApplicationName },
            { "Version", AppConstants.ApplicationProductVersion },
            { "Build", AppConstants.ApplicationFileVersion },
        };

        public void Initialize(ITelemetry telemetry)
        {
            // Keep telemetry context configuration synchronized with Sqlbi.Bravo.Installer.Wix.Helpers.GetTelemetryClient()

            telemetry.Context.Device.OperatingSystem = DeviceOperatingSystem;
            telemetry.Context.Component.Version = ComponentVersion;
            telemetry.Context.Session.Id = SessionId;
            telemetry.Context.User.Id = UserId;

            foreach (var property in GlobalProperties)
            {
                telemetry.Context.GlobalProperties.Add(property.Key, property.Value);
            }
        }
    }
}
