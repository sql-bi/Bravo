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
        internal const string CustomDataPbitoolPath = "CUSTOMDATAPBITOOLPATH";
        internal const string CustomDataProductName = "CUSTOMDATAPRODUCTNAME";
        internal const string CustomDataProductVersion = "CUSTOMDATAPRODUCTVERSION";
        internal const string CustomDataProductExecutablePath = "CUSTOMDATAPRODUCTEXECUTABLEPATH";
        internal const string CustomDataInstallerTelemetryEnabled = "CUSTOMDATAINSTALLERTELEMETRYENABLED";

        internal static void TrackEvent(Session session, string name)
        {
            var telemetryClient = GetTelemetryClient(session);
            var telemetryEvent = new EventTelemetry(name);
            telemetryClient.TrackEvent(telemetryEvent);
            telemetryClient.Flush();
            Thread.Sleep(500);
        }

        internal static void TrackException(Session session, Exception exception)
        {
            var telemetryClient = GetTelemetryClient(session);
            telemetryClient.TrackException(exception);
            telemetryClient.Flush();
            Thread.Sleep(500);
        }

        internal static TelemetryClient GetTelemetryClient(Session session)
        {
            var productName = session.CustomActionData[CustomDataProductName];
            var productVersion = session.CustomActionData[CustomDataProductVersion];

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = "47a8970c-6293-408a-9cce-5b7b311574d3";
            telemetryConfiguration.DisableTelemetry = false;

            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            telemetryClient.Context.Component.Version = productVersion;
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            telemetryClient.Context.User.Id = ToSHA256Hash($"{ Environment.MachineName }\\{ Environment.UserName }");
            telemetryClient.Context.GlobalProperties.Add("ProductName", productName);

            return telemetryClient;
        }

        internal static bool IsTelemetryEnabled(Session session)
        {
            if (session.CustomActionData.TryGetValue(CustomDataInstallerTelemetryEnabled, out var value))
            {
                if (value == string.Empty)
                    return false;
                else if (bool.TryParse(value, out var boolValue))
                    return boolValue;
                else if (int.TryParse(value, out var intValue))
                    return Convert.ToBoolean(intValue);
            }

            // In case of missing argument enable telemetry to further investigate
            return true;
        }

        internal static string ToSHA256Hash(string value)
        {
            if (value == null)
                return null;

            using (var algorithm = new SHA256Managed())
            {
                var stringBuilder = new StringBuilder();
                var buffer = Encoding.UTF8.GetBytes(value);
                var offset = 0;
                var count = Encoding.UTF8.GetByteCount(value);

                var bytes = algorithm.ComputeHash(buffer, offset, count);

                foreach (var @byte in bytes)
                    stringBuilder.Append(@byte.ToString("x2"));

                return stringBuilder.ToString();
            }
        }

        //internal static bool IsProductInstalled(string name)
        //{
        //    var value = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\SQLBI\{ name }", "installFolder", null);
        //    if (value != null)
        //    {
        //        var path = Convert.ToString(value);
        //        if (Directory.Exists(path))
        //            return true;
        //    }
        //    return false;
        //}
    }
}
