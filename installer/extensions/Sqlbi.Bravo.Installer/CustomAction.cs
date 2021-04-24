using Microsoft.Deployment.WindowsInstaller;
using System;

namespace Sqlbi.Bravo.Installer
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult PowerBIDesktopRegisterExternalTool(Session session)
        {
            session.Log($"BRAVO [{ nameof(PowerBIDesktopRegisterExternalTool) }] Begin");

            try
            {
                System.IO.File.WriteAllText(@"C:\Program Files (x86)\Common Files\Microsoft Shared\Power BI Desktop\External Tools\bravo.pbitool.json", "atolla");
            }
            catch(Exception ex)
            {
                session.Log($"BRAVO [{ nameof(PowerBIDesktopRegisterExternalTool) }] Error { ex }");
            }

            session.Log($"BRAVO [{ nameof(PowerBIDesktopRegisterExternalTool) }] End");

            return ActionResult.Success;
        }
    }
}
