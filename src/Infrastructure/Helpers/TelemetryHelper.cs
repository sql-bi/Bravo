using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Sqlbi.Bravo.Infrastructure.Security;
using System;
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
            configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached || AppConstants.IsDebug;
            configuration.InstrumentationKey = AppConstants.TelemetryInstrumentationKey;
            configuration.DisableTelemetry = false;
            
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
        private static readonly string DeviceOperatingSystem = Environment.OSVersion.ToString();
        private static readonly string ComponentVersion = AppConstants.ApplicationFileVersion;
        private static readonly string SessionId = Guid.NewGuid().ToString();
        private static readonly string? UserId = $"{ Environment.MachineName }\\{ Environment.UserName }".ToSHA256Hash();

        public void Initialize(ITelemetry telemetry)
        {
            //if (telemetry is Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry exceptionTelemetry)
            //{
            //    if (exceptionTelemetry.Properties.ContainsKey("customProperty") == false)
            //        exceptionTelemetry.Properties.Add("customProperty", "value");
            //}

            var context = telemetry.Context;
            {
                context.Device.OperatingSystem = DeviceOperatingSystem;
                context.Component.Version = ComponentVersion;
                context.Session.Id = SessionId;
                context.User.Id = UserId;
            }
        }
    }
}
