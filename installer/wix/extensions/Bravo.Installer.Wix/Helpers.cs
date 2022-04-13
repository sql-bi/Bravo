using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Sqlbi.Bravo.Installer.Wix
{
    internal static class Helpers
    {
        internal const string PropertyPbitoolPath = "PBITOOLPATH";
        internal const string PropertyProductName = "PRODUCTNAME";
        internal const string PropertyProductVersion = "PRODUCTVERSION";
        internal const string PropertyProductBuild = "PRODUCTBUILD";
        internal const string PropertyProductExecutablePath = "PRODUCTEXECUTABLEPATH";
        internal const string PropertyInstallerTelemetryEnabled = "INSTALLERTELEMETRYENABLED";
        internal const string PropertyTelemetryUserId = "TELEMETRYUSERID";

        internal static void Log(Session session, string name)
        {
            foreach (var pairs in session.CustomActionData)
                session.Log($"::BRAVO<LOG> ({ name }) - CustomActionData({ pairs.Key }, { pairs.Value })");
        }

        internal static void TrackEvent(Session session, string name)
        {
            var telemetryClient = GetTelemetryClient(session);
            var telemetryEvent = new EventTelemetry(name);
            telemetryClient.TrackEvent(telemetryEvent);
            telemetryClient.Flush();
            Thread.Sleep(1000);
        }

        internal static void TrackException(Session session, Exception exception)
        {
            var telemetryClient = GetTelemetryClient(session);
            telemetryClient.TrackException(exception);
            telemetryClient.Flush();
            Thread.Sleep(1000);
        }

        internal static TelemetryClient GetTelemetryClient(Session session)
        {
            var productName = session.CustomActionData[PropertyProductName];
            var productVersion = session.CustomActionData[PropertyProductVersion];
            var productBuild = session.CustomActionData[PropertyProductBuild];
            var userId = session.CustomActionData[PropertyTelemetryUserId];

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = "47a8970c-6293-408a-9cce-5b7b311574d3";
            telemetryConfiguration.DisableTelemetry = false;

            // Keep telemetry context configuration synchronized with Sqlbi.Bravo.Infrastructure.Helpers.ContextTelemetryInitializer
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            telemetryClient.Context.Component.Version = productVersion;
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            telemetryClient.Context.User.Id = userId;
            telemetryClient.Context.GlobalProperties.Add("ProductName", productName);
            telemetryClient.Context.GlobalProperties.Add("Version", productVersion);
            telemetryClient.Context.GlobalProperties.Add("Build", productBuild);

            return telemetryClient;
        }

        internal static bool IsTelemetryEnabled(Session session)
        {
            if (session.CustomActionData.TryGetValue(PropertyInstallerTelemetryEnabled, out var value))
            {
                if (string.IsNullOrEmpty(value))
                    return false;
                
                if (int.TryParse(value, out var intValue))
                    return Convert.ToBoolean(intValue);
            }

            // In case of missing argument enable telemetry to further investigate
            return true;
        }

        internal static string ToSHA256Hash(this string value)
        {
            if (value == null)
                return null;

            using (var algorithm = SHA256.Create())
            {
                var stringBuilder = new StringBuilder();
                var buffer = Encoding.UTF8.GetBytes(value);
                var count = Encoding.UTF8.GetByteCount(value);
                var bytes = algorithm.ComputeHash(buffer, offset: 0, count);

                foreach (var @byte in bytes)
                    stringBuilder.Append(@byte.ToString("x2"));

                return stringBuilder.ToString();
            }
        }
    }
}
