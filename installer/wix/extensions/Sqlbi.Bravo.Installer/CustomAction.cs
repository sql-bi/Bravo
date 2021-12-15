using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Sqlbi.Bravo.Installer
{
    public class CustomActions
    {
        private const string CustomActionDataInstalledExecutablePath = "INSTALLEDEXECUTABLEPATH";
        private const string CustomActionDataInstallerTelemetryEnabled = "INSTALLERTELEMETRYENABLED";
        private const string CustomActionDataInstallerProductVersion = "INSTALLERPRODUCTVERSION";
        private const string ExternalToolsPbiToolFileName = @"bravo.pbitool.json";
        private const string ExternalToolsFolderPath = @"Microsoft Shared\Power BI Desktop\External Tools";

        [CustomAction]
        public static ActionResult PowerBIDesktopRegisterExternalTool(Session session)
        {
            session.Log($"BRAVOLOG BEGIN ({ nameof(PowerBIDesktopRegisterExternalTool) })");
            try
            {
                foreach (var pairs in session.CustomActionData)
                    session.Log($"BRAVOLOG LOG ({ nameof(PowerBIDesktopRegisterExternalTool) }) - CustomActionData({ pairs.Key }, { pairs.Value })");

                var installedExecutablePath = session.CustomActionData[CustomActionDataInstalledExecutablePath];
                installedExecutablePath = installedExecutablePath.Replace("\\", "\\\\");

                var commonProgramPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
                var path = Path.Combine(commonProgramPath, ExternalToolsFolderPath, ExternalToolsPbiToolFileName);
                session.Log($"BRAVOLOG LOG ({ nameof(PowerBIDesktopRegisterExternalTool) }) - pbitool.json ({ path })");

                var content = File.ReadAllText(path);
                content = content.Replace("<#!PATH!#>", installedExecutablePath);
                File.WriteAllText(path, content);
            }
            catch (Exception ex)
            {
                session.Log($"BRAVOLOG ERROR ({ nameof(PowerBIDesktopRegisterExternalTool) }) - { ex }");

                var telemetryClient = GetTelemetryClient(session);
                telemetryClient.TrackException(ex);
                telemetryClient.Flush();
            }

            session.Log($"BRAVOLOG END ({ nameof(PowerBIDesktopRegisterExternalTool) })");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult AfterInstall(Session session)
        {
            session.Log($"BRAVOLOG BEGIN ({ nameof(AfterInstall) })");
            try
            {
                foreach (var pairs in session.CustomActionData)
                    session.Log($"BRAVOLOG LOG ({ nameof(AfterInstall) }) - CustomActionData({ pairs.Key }, { pairs.Value })");

                if (IsTelemetryEnabled(session))
                {
                    session.Log($"BRAVOLOG LOG ({ nameof(AfterInstall) }) - TrackEvent");
                    var telemetryEvent = new EventTelemetry(name: "Install");
                    var telemetryClient = GetTelemetryClient(session);
                    telemetryClient.TrackEvent(telemetryEvent);
                    telemetryClient.Flush();
                }
            }
            catch (Exception ex)
            {
                session.Log($"BRAVOLOG ERROR ({ nameof(AfterInstall) }) { ex }");
            }

            session.Log($"BRAVOLOG END ({ nameof(AfterInstall) })");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult AfterUninstall(Session session)
        {
            session.Log($"BRAVOLOG BEGIN ({ nameof(AfterUninstall) })");
            try
            {
                foreach (var pairs in session.CustomActionData)
                    session.Log($"BRAVOLOG LOG ({ nameof(AfterUninstall) }) - CustomActionData({ pairs.Key }, { pairs.Value })");

                if (IsTelemetryEnabled(session))
                { 
                    session.Log($"BRAVOLOG LOG ({ nameof(AfterUninstall) }) - TrackEvent");
                    var telemetryClient = GetTelemetryClient(session);
                    var telemetryEvent = new EventTelemetry(name: "Uninstall");
                    telemetryClient.TrackEvent(telemetryEvent);
                    telemetryClient.Flush();
                }
            }
            catch (Exception ex)
            {
                session.Log($"BRAVOLOG ERROR ({ nameof(AfterUninstall) }) { ex }");
            }

            session.Log($"BRAVOLOG END ({ nameof(AfterUninstall) })");
            return ActionResult.Success;
        }

        private static TelemetryClient GetTelemetryClient(Session session)
        {
            var productVersion = GetProductVersion(session);

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = "47a8970c-6293-408a-9cce-5b7b311574d3";
            telemetryConfiguration.DisableTelemetry = false;
            
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            telemetryClient.Context.Component.Version = productVersion;
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            telemetryClient.Context.User.Id = ToHashSHA256($"{ Environment.MachineName }\\{ Environment.UserName }");

            return telemetryClient;
        }

        private static bool IsTelemetryEnabled(Session session)
        {
            if (session.CustomActionData.TryGetValue(CustomActionDataInstallerTelemetryEnabled, out var value))
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

        private static string GetProductVersion(Session session)
        {
            var version = session.CustomActionData[CustomActionDataInstallerProductVersion];

            return version;
        }

        private static string ToHashSHA256(string value)
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
    }
}
