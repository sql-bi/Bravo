using Sqlbi.Bravo.Core.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class ManagementHelpers
    {
        public static Process GetParent(this Process process)
        {
            var queryString = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = { process.Id }";

            using var query = new ManagementObjectSearcher(queryString);
            using var collection = query.Get();
            using var item = collection.OfType<ManagementObject>().Single();
           
            var parentProcessId = (int)(uint)item["ParentProcessId"];
            return Process.GetProcessById(parentProcessId);
        }

        public static string GetMainWindowTitle(this Process process)
        {
            if (process.MainWindowTitle.Length > 0)
                return process.MainWindowTitle;

            var builder = new StringBuilder(1000);

            foreach (ProcessThread thread in process.Threads)
            {
                NativeMethods.EnumThreadWindows(thread.Id, (hWnd, lParam) => 
                {
                    if (NativeMethods.IsWindowVisible(hWnd))
                    {
                        NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETTEXT, builder.Capacity, builder);

                        if (builder.Length > 0)
                            return false;
                    }

                    return true;
                }, 
                IntPtr.Zero);

                if (builder.Length > 0)
                    break;
            }

            return builder.ToString();
        }
    }
}