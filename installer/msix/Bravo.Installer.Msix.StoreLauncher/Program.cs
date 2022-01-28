namespace Bravo.Installer.Msix.StoreLauncher
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Runtime.InteropServices;

    internal class Program
    {
        private const string WindowsStoreAppExecutionAlias = "BravoStore";
        private const string BravoParentProcessIdArgument = "--ppid";

        internal static void Main(string[] args)
        {
            // Packaged apps cannot be directly launched via Process.Start API so we use the AppExecutionAlias which is a 0-byte EXE create on %LOCALAPPDATA%\Microsoft\WindowsApps folder
            // If the AppExecutionAlias file is missing check if the packaged app is installed and if the alias is active (Settings > Apps > Apps & features > App execution aliases)
            var windowsStoreAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps", Path.ChangeExtension(WindowsStoreAppExecutionAlias, ".exe"));
            var parentProcess = GetParentProcess();

            if (!File.Exists(windowsStoreAppPath))
            {
                var message = "The Microsoft Store version of the application is not installed or the application execution alias is disabled (see: Settings > Apps > Apps & features > App execution aliases)";

                using (var eventLog = new EventLog(logName: "Application", machineName: ".", source: "Application"))
                {
                    var extendedMessage = "=== SQLBI - Bravo for Power BI ===\r\n" + message + "\r\n===";
                    eventLog.WriteEntry(extendedMessage, EventLogEntryType.Warning, 100);
                }

                MessageBoxShow(hWnd: parentProcess.MainWindowHandle, text: message, caption: "SQLBI - Bravo for Power BI");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = windowsStoreAppPath,
                Arguments = string.Join(" ", args) + string.Format(" {0}={1}", BravoParentProcessIdArgument, parentProcess.Id),
            };

            // Fire and forget - errors are logged in the windows event log
            Process.Start(startInfo);
        }

        [DllImport("user32.dll")]
        private static extern int MessageBoxA(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        private static void MessageBoxShow(IntPtr hWnd, string text, string caption)
        {
            // MessageBox function (winuser.h)
            // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox

            uint buttons = 0x00000000; // MB_OK
            uint icon = 0x00000010;    // MB_ICONSTOP
            uint button = 0x00000000;  // MB_DEFBUTTON1
            uint modal = 0x00000000;   // MB_APPLMODAL

            MessageBoxA(hWnd, text, caption, (buttons | icon | button | modal));
        }

        private static Process GetParentProcess()
        {
            var currentProcess = Process.GetCurrentProcess();
            var queryString = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0} AND SessionId = {1}", currentProcess.Id, currentProcess.SessionId);

            using (var query = new ManagementObjectSearcher(queryString))
            using (var collection = query.Get())
            using (var item = collection.OfType<ManagementObject>().Single())
            {
                var parentProcessId = (int)(uint)item["ParentProcessId"];
                return Process.GetProcessById(parentProcessId);
            }
        }
    }
}
