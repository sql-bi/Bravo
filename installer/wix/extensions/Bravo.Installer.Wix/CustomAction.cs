using Microsoft.Deployment.WindowsInstaller;
using System;
using System.IO;

namespace Sqlbi.Bravo.Installer.Wix
{
    public class CustomAction
    {
        [CustomAction]
        public static ActionResult RegisterExternalTool(Session session)
        {
            session.Log($"::BRAVO<BEGIN> ({ nameof(RegisterExternalTool) })");
            try
            {
                foreach (var pairs in session.CustomActionData)
                    session.Log($"::BRAVO<LOG> ({ nameof(RegisterExternalTool) }) - CustomActionData({ pairs.Key }, { pairs.Value })");

                var pbitoolPath = session.CustomActionData[Helpers.CustomDataPbitoolPath];
                var executablePath = session.CustomActionData[Helpers.CustomDataProductExecutablePath].Replace("\\", "\\\\");

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
                foreach (var pairs in session.CustomActionData)
                    session.Log($"::BRAVO<LOG> ({ nameof(AfterInstall) }) - CustomActionData({ pairs.Key }, { pairs.Value })");

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
                foreach (var pairs in session.CustomActionData)
                    session.Log($"::BRAVO<LOG> ({ nameof(AfterUninstall) }) - CustomActionData({ pairs.Key }, { pairs.Value })");

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
