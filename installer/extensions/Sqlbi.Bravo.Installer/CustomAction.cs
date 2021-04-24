using Microsoft.Deployment.WindowsInstaller;
using System;
using System.IO;

namespace Sqlbi.Bravo.Installer
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult PowerBIDesktopRegisterExternalTool(Session session)
        {
            session.Log($"BRAVODEBUG [{ nameof(PowerBIDesktopRegisterExternalTool) }] Begin");

            try
            {
                var installedExecutablePath = session.CustomActionData["INSTALLEDEXECUTABLEPATH"];
                session.Log($"BRAVODEBUG [{ nameof(PowerBIDesktopRegisterExternalTool) }] CustomActionData<INSTALLEDEXECUTABLEPATH> '{ installedExecutablePath }'");

                var path1 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
                var path2 = @"Microsoft Shared\Power BI Desktop\External Tools";
                var file = "bravo.pbitool.json";
                var path = Path.Combine(path1, path2, file);

                session.Log($"BRAVODEBUG [{ nameof(PowerBIDesktopRegisterExternalTool) }] pbitool.json '{ path }'");

                var content = File.ReadAllText(path);
                content = content.Replace("<#!PATH!#>", installedExecutablePath);
                File.WriteAllText(path, content);
            }
            catch(Exception ex)
            {
                session.Log($"BRAVODEBUG [{ nameof(PowerBIDesktopRegisterExternalTool) }] Error { ex }");
            }

            session.Log($"BRAVODEBUG [{ nameof(PowerBIDesktopRegisterExternalTool) }] End");

            return ActionResult.Success;
        }
    }
}
