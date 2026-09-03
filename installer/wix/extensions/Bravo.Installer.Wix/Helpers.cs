using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Sqlbi.Bravo.Installer.Wix
{
    internal static class Helpers
    {
        internal const string PropertyPbitoolPath = "PBITOOLPATH";
        internal const string PropertyProductName = "PRODUCTNAME";
        internal const string PropertyProductVersion = "PRODUCTVERSION";
        internal const string PropertyProductBuild = "PRODUCTBUILD";
        internal const string PropertySelfContained = "SELFCONTAINED";
        internal const string PropertyProductExecutablePath = "PRODUCTEXECUTABLEPATH";
        internal const string PropertyInstallerTelemetryEnabled = "INSTALLERTELEMETRYENABLED";
        internal const string PropertyTelemetryUserId = "TELEMETRYUSERID";
        internal const string PropertyInstallScope = "INSTALLSCOPE";
        internal const string PropertyLocalAppDataSubfolder = "LOCALAPPDATASUBFOLDER";

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
            telemetryClient.Flush(); // Synchronous with InMemoryChannel
        }

        internal static void TrackException(Session session, Exception exception)
        {
            var telemetryClient = GetTelemetryClient(session);
            telemetryClient.TrackException(exception);
            telemetryClient.Flush(); // Synchronous with InMemoryChannel
        }

        /// <summary>
        /// Enables TLS 1.2, the only protocol accepted by the Application Insights ingestion endpoint.
        /// </summary>
        internal static void EnableTls12()
        {
            // The custom action runs inside the native SfxCA host, with no managed entry assembly: the CLR applies the
            // .NET Framework 4.0 compatibility quirks regardless of the target framework of this assembly, and the
            // default SecurityProtocol is SSL 3.0 and TLS 1.0 only. A failed handshake is swallowed by the SDK.
            //
            // SecurityProtocolType.SystemDefault is deliberately not used. It requires .NET Framework 4.7: on 4.5-4.6.2
            // the setter throws NotSupportedException, and this method is also reached from TrackException inside a
            // catch block, so the exception would fail the custom action. It also delegates to the Schannel defaults,
            // which on Windows 7 SP1 and Windows Server 2012 are TLS 1.0 unless KB3140245 and its registry key are
            // applied, so it would negotiate TLS 1.0 again on the very systems the launch condition still admits.
            // Tls12 is available since .NET Framework 4.5, is additive, and is what the endpoint requires.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        internal static TelemetryClient GetTelemetryClient(Session session)
        {
            EnableTls12();

            var productName = session.CustomActionData[PropertyProductName];
            var productVersion = session.CustomActionData[PropertyProductVersion];
            var productBuild = session.CustomActionData[PropertyProductBuild];
            var userId = session.CustomActionData[PropertyTelemetryUserId];
            var installScope = GetInstallScope(session.CustomActionData[PropertyInstallScope]);
            var publishMode = GetPublishMode(session.CustomActionData[PropertySelfContained]);

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = "47a8970c-6293-408a-9cce-5b7b311574d3";
            telemetryConfiguration.DisableTelemetry = false;

            // Keep telemetry context configuration synchronized with Sqlbi.Bravo.Infrastructure.Telemetry.TelemetrySessionInfo
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            telemetryClient.Context.Component.Version = productVersion;
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            telemetryClient.Context.User.Id = userId;
            telemetryClient.Context.GlobalProperties.Add("PublishMode", publishMode);
            telemetryClient.Context.GlobalProperties.Add("InstallScope", installScope);
            telemetryClient.Context.GlobalProperties.Add("ProductName", productName);
            telemetryClient.Context.GlobalProperties.Add("Version", productVersion);
            telemetryClient.Context.GlobalProperties.Add("Build", productBuild);

            return telemetryClient;
        }

        /// <summary>
        /// Maps the self-contained build flag to the publish mode reported by the application.
        /// </summary>
        internal static string GetPublishMode(string selfContained)
        {
            // This method must not throw because it can be called while reporting exceptions from
            // custom actions. An exception here would fail the custom action and roll back the installation.
            if (bool.TryParse(selfContained, out var value))
                return value ? "SelfContained" : "FrameworkDependent";

            // Unexpected values are reported as received, so that the telemetry shows what the build passed in
            return selfContained;
        }

        /// <summary>
        /// Maps the WiX Package/@InstallScope value to the deployment mode reported by the application.
        /// </summary>
        internal static string GetInstallScope(string installScope)
        {
            // The installer telemetry used to send the raw WiX values 'perMachine' and 'perUser'. The values are now
            // mapped to 'PerMachine' and 'PerUser', the AppDeploymentMode names sent by the application telemetry,
            // so that the InstallScope property has the same set of values for both sources.
            //
            // See GetPublishMode for why this method must not throw.
            if (string.Equals(installScope, "perMachine", StringComparison.OrdinalIgnoreCase))
                return "PerMachine";

            if (string.Equals(installScope, "perUser", StringComparison.OrdinalIgnoreCase))
                return "PerUser";

            // Unexpected values are reported as received, so that the telemetry shows what the build passed in
            return installScope;
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
