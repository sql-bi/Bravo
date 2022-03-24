namespace Sqlbi.Bravo.Infrastructure
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Sqlbi.Bravo.Infrastructure.Security;
    using System;
    using System.Collections.Generic;

    internal class AppTelemetryInitializer : ITelemetryInitializer
    {
        public static readonly string DeviceOperatingSystem = Environment.OSVersion.ToString();
        public static readonly string ComponentVersion = AppEnvironment.ApplicationProductVersion;
        public static readonly string SessionId = Guid.NewGuid().ToString();
        public static readonly string? UserId = $"{ Environment.MachineName }\\{ Environment.UserName }".ToSHA256Hash();
        public static readonly IReadOnlyDictionary<string, string> GlobalProperties = new Dictionary<string, string>
        {
            { "ProductName", AppEnvironment.ApplicationName },
            { "Version", AppEnvironment.ApplicationProductVersion },
            { "Build", AppEnvironment.ApplicationFileVersion },
            { "IsPackaged", AppEnvironment.IsPackagedAppInstance.ToString().ToLowerInvariant() },
            { "WebView2Version", AppEnvironment.WebView2VersionInfo ?? string.Empty },
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

    internal class AppTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public AppTelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            _next.Process(item);
        }
    }
}
