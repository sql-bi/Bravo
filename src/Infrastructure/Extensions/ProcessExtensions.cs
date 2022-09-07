namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Management;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class ProcessExtensions
    {
        //[DebuggerStepThrough]
        [Obsolete("Use WMI query")]
        public static Process? InteropGetParent(this Process process)
        {
            Ntdll.PROCESS_BASIC_INFORMATION processInformation;
            int? retval;
            try
            {
                retval = Ntdll.NtQueryInformationProcess(process.Handle, Ntdll.PROCESSINFOCLASS.ProcessBasicInformation, out processInformation, processInformationLength: (uint)Marshal.SizeOf(typeof(Ntdll.PROCESS_BASIC_INFORMATION)), returnLength: out _);
            }
            catch (Win32Exception ex) when (ex.ErrorCode == -2147467259) // System.ComponentModel.Win32Exception {"Access is denied."}
            {
                return null;
            }
            
            if (retval.Value == (int)Ntdll.NTSTATUS.STATUS_SUCCESS)
            {
                var parentProcessId = (int)(uint)processInformation.InheritedFromUniqueProcessId;
                var parentProcess = ProcessHelper.SafeGetProcessById(parentProcessId);

                return parentProcess;
            }

            // TODO: How NTSTATUS codes are translated into Win32 errors ?
            // throw new Win32Exception(retval);

            return null;
        }

        public static IEnumerable<int> GetChildrenPIDs(this Process process, string? childProcessImageName = null)
        {
            // ManagementObjectSearcher.Get() raises a System.InvalidCastException when executed on the current thread, this regardless of the apartment state of the current thread (which is STA)
            //
            // System.InvalidCastException "Specified cast is not valid."
            //    at System.StubHelpers.InterfaceMarshaler.ConvertToNative(Object objSrc, IntPtr itfMT, IntPtr classMT, Int32 flags)
            //    at System.Management.SecuredIWbemServicesHandler.ExecQuery_(String strQueryLanguage, String strQuery, Int32 lFlags, IWbemContext pCtx, IEnumWbemClassObject& ppEnum)
            //    at System.Management.ManagementObjectSearcher.Get()

            var pids = new List<int>();
            
            ProcessHelper.RunOnSTAThread(GetImpl);
            
            return pids;

            void GetImpl()
            {
                var queryString = $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = { process.Id } AND SessionId = { AppEnvironment.SessionId }";

                if (childProcessImageName is not null)
                    queryString += $" AND Name = '{ childProcessImageName }'";

                using var searcher = new ManagementObjectSearcher(queryString);
                using var collection = searcher.Get();

                foreach (var @object in collection)
                {
                    if (@object is not null)
                    {
                        var processId = (int)(uint)@object.GetPropertyValue("ProcessId");
                        pids.Add(processId);
                    }
                }
            }
        }

        public static string GetMainWindowTitle(this Process process, Func<string, bool>? predicate = default)
        {
            if (process.MainWindowTitle.Length > 0)
                return process.MainWindowTitle;
            
            var builder = new StringBuilder(capacity: 1000);

            foreach (ProcessThread thread in process.Threads)
            {
                User32.EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                {
                    if (User32.IsWindowVisible(hWnd))
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

        public static string? GetPBIDesktopMainWindowTitle(this Process process)
        {
            var windowTitle = process.GetMainWindowTitle((windowTitle) => windowTitle.IsPBIDesktopMainWindowTitle());

            if (windowTitle.IsNullOrWhiteSpace())
            {
                // PBIDesktop is opening or the SSAS instance/model is not yet ready
                return null;
            }

            foreach (var suffix in AppEnvironment.PBIDesktopMainWindowTitleSuffixes)
            {
                var index = windowTitle.LastIndexOf(suffix);
                if (index >= 0)
                {
                    windowTitle = windowTitle[..index];
                    return windowTitle;
                }
            }

            return null;
        }
    }
}
