using System;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class PBIExternalToolHelper
    {
        public const string RegisterCommandLineArgument = "etreg"; // External Tool Register
        public const string UnregisterCommandLineArgument = "etunreg"; // External Tool Unregister

        public enum ActionType
        {
            None = 0,
            Register,
            Unregister
        }

        public static ActionType RequestedAction
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length == 2)
                {
                    var argument = args[1];

                    if (argument.Equals(RegisterCommandLineArgument, StringComparison.OrdinalIgnoreCase))
                        return ActionType.Register;

                    if (argument.Equals(UnregisterCommandLineArgument, StringComparison.OrdinalIgnoreCase))
                        return ActionType.Unregister;
                }

                return ActionType.None;
            }
        }

        public static void RegisterExternalTool()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "Sqlbi.Bravo.Assets.bravo.pbitool.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new System.IO.StreamReader(stream!);

            var pbitool = reader.ReadToEnd();
            var path = @"C:\Program Files (x86)\Common Files\Microsoft Shared\Power BI Desktop\External Tools\bravo.pbitool.json";

            System.IO.File.WriteAllText(path, contents: pbitool);
        }

        public static void UnregisterExternalTool()
        {
            if (IsCurrentProcessAdmin() && DesktopBridgeHelpers.IsPackagedAppInstance)
            { }

            var path = @"C:\Program Files (x86)\Common Files\Microsoft Shared\Power BI Desktop\External Tools\bravo.pbitool.json";

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        private static bool IsCurrentProcessAdmin()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
