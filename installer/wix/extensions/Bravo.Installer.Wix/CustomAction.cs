using Microsoft.Deployment.WindowsInstaller;
using System;
using System.IO;

namespace Sqlbi.Bravo.Installer.Wix
{
    public class CustomAction
    {
        [CustomAction]
        public static ActionResult SetPropertyTelemetryUserId(Session session)
        {
            // The telemetry user id is computed from the name of the user who is currently logged into the operating system.
            // Here we are using a dedicated custom action that must be executed with the Execute="immediate" Impersonate="yes" properties.
            // This is to ensure that the correct value from Environment.UserName is used instead of using the localsystem/administrator account used to run the installer when a per-machine install scope is used.

            session.Log($"::BRAVO<BEGIN> ({ nameof(SetPropertyTelemetryUserId) })");
            try
            {
                Helpers.Log(session, nameof(SetPropertyTelemetryUserId));

                var userId = $"{ Environment.MachineName }\\{ Environment.UserName }".ToSHA256Hash();
                session[Helpers.PropertyTelemetryUserId] = userId;
            }
            catch (Exception ex)
            {
                session.Log($"::BRAVO<ERROR> ({ nameof(SetPropertyTelemetryUserId) }) - { ex }");
                Helpers.TrackException(session, ex);
            }

            session.Log($"::BRAVO<END> ({ nameof(SetPropertyTelemetryUserId) })");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult RegisterExternalTool(Session session)
        {
            session.Log($"::BRAVO<BEGIN> ({ nameof(RegisterExternalTool) })");
            try
            {
                Helpers.Log(session, nameof(RegisterExternalTool));

                var pbitoolPath = session.CustomActionData[Helpers.PropertyPbitoolPath];
                var executablePath = session.CustomActionData[Helpers.PropertyProductExecutablePath].Replace("\\", "\\\\");
                var content = File.ReadAllText(pbitoolPath);
                {
                    content = content.Replace("<#!PATH!#>", executablePath);
                }
                File.WriteAllText(pbitoolPath, content);
            }
            catch (Exception ex)
            {
                session.Log($"::BRAVO<ERROR> ({ nameof(RegisterExternalTool) }) - { ex }");
                Helpers.TrackException(session, ex);
            }

            session.Log($"::BRAVO<END> ({ nameof(RegisterExternalTool) })");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult UnregisterExternalTool(Session session)
        {
            session.Log($"::BRAVO<BEGIN> ({ nameof(UnregisterExternalTool) })");
            session.Log($"::BRAVO<END> ({ nameof(UnregisterExternalTool) })");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult AfterInstall(Session session)
        {
            session.Log($"::BRAVO<BEGIN> ({ nameof(AfterInstall) })");
            try
            {
                Helpers.Log(session, nameof(AfterInstall));

                if (Helpers.IsTelemetryEnabled(session))
                {
                    session.Log($"::BRAVO<LOG> ({ nameof(AfterInstall) }) - TrackEvent");
                    Helpers.TrackEvent(session, name: "Install");
                }
            }
            catch (Exception ex)
            {
                session.Log($"::BRAVO<ERROR> ({ nameof(AfterInstall) }) { ex }");
            }

            session.Log($"::BRAVO<END> ({ nameof(AfterInstall) })");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult AfterUninstall(Session session)
        {
            session.Log($"::BRAVO<BEGIN> ({ nameof(AfterUninstall) })");
            try
            {
                Helpers.Log(session, nameof(AfterUninstall));

                if (Helpers.IsTelemetryEnabled(session))
                {
                    session.Log($"::BRAVO<LOG> ({ nameof(AfterUninstall) }) - TrackEvent");
                    Helpers.TrackEvent(session, name: "Uninstall");
                }
            }
            catch (Exception ex)
            {
                session.Log($"::BRAVO<ERROR> ({ nameof(AfterUninstall) }) { ex }");
            }

            session.Log($"::BRAVO<END> ({ nameof(AfterUninstall) })");
            return ActionResult.Success;
        }
    }
}
