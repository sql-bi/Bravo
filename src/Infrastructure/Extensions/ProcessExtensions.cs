using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;

namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    public static class ProcessExtensions
    {
        public static Process? SafeGetProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return null;
            }
        }

        public static Process? GetParent(this Process process)
        {
            var queryString = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = { process.Id }";

            using var query = new ManagementObjectSearcher(queryString);
            using var collection = query.Get();
            using var item = collection.OfType<ManagementObject>().Single();

            var parentProcessId = (int)(uint)item["ParentProcessId"];
            var parentProcess = SafeGetProcessById(parentProcessId);

            return parentProcess;
        }

        public static IEnumerable<int> GetChildProcessIds(this Process process, string? name = null)
        {
            var queryString = $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = { process.Id }";

            if (name is not null)
                queryString += $" AND Name = '{ name }'";

            using var query = new ManagementObjectSearcher(queryString);
            using var collection = query.Get();

            foreach (var item in collection)
            {
                if (item is not null)
                    yield return (int)(uint)item["ProcessId"];
            }
        }

        public static string GetMainWindowTitle(this Process process, Func<string, bool>? predicate = default)
        {
            if (process.MainWindowTitle.Length > 0)
                return process.MainWindowTitle;
            
            var builder = new StringBuilder(capacity: 1000);

            foreach (ProcessThread thread in process.Threads)
            {
                NativeMethods.EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                {
                    if (NativeMethods.IsWindowVisible(hWnd))
                    {
                        User32.SendMessage(hWnd, WindowMessage.WM_GETTEXT, builder.Capacity, builder);

                        if (builder.Length > 0)
                        {
                            var windowTitle = builder.ToString();

                            if (predicate?.Invoke(windowTitle) == true)
                                return false;

                            builder.Clear();
                        }
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
