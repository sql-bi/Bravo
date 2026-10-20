using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;

namespace Sqlbi.Bravo.Infrastructure.Extensions;

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
            var queryString = $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {process.Id} AND SessionId = {AppEnvironment.SessionId}";

            if (childProcessImageName is not null)
                queryString += $" AND Name = '{childProcessImageName}'";

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

    extension(Process)
    {
        /// <summary>
        /// Gets the running process with the given identifier in the given Windows session.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> when the process is not running, has exited, runs in another session or cannot be
        /// opened.
        /// </returns>
        public static bool TryGetById(int processId, int sessionId, [NotNullWhen(true)] out Process? process)
        {
            process = null;
            Process? candidate = null;
            try
            {
                candidate = Process.GetProcessById(processId); // Throws ArgumentException if the process is not running

                if (candidate.SessionId != sessionId)
                    return false;

                if (candidate.HasExited) // Throws Win32Exception if the process cannot be opened, for example another user's
                    return false;

                _ = candidate.ProcessName; // Throws InvalidOperationException if the process has exited in the meantime

                process = candidate;
                candidate = null; // Owned by the caller from here on, so the finally block must not dispose it
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
            {
                return false;
            }
            finally
            {
                candidate?.Dispose();
            }
        }
    }

    extension(Process process)
    {
        /// <summary>
        /// Gets the parent of the process when it runs in the same Windows session; otherwise, <see langword="null"/>.
        /// </summary>
        public Process? GetParentProcess()
        {
            // ManagementObjectSearcher.Get() raises a System.InvalidCastException when executed on the current thread, this regardless of the apartment state of the current thread (which is STA)
            //
            // System.InvalidCastException "Specified cast is not valid."
            //    at System.StubHelpers.InterfaceMarshaler.ConvertToNative(Object objSrc, IntPtr itfMT, IntPtr classMT, Int32 flags)
            //    at System.Management.SecuredIWbemServicesHandler.ExecQuery_(String strQueryLanguage, String strQuery, Int32 lFlags, IWbemContext pCtx, IEnumWbemClassObject& ppEnum)
            //    at System.Management.ManagementObjectSearcher.Get()

            var processId = process.Id;
            var sessionId = process.SessionId;
            var parentProcessId = (int?)null;

            ProcessHelper.RunOnSTAThread(GetImpl);

            if (parentProcessId is not int id)
                return null;

            if (!Process.TryGetById(id, sessionId, out var parentProcess))
                return null;

            return parentProcess;

            void GetImpl()
            {
                var queryString = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId} AND SessionId = {sessionId}";

                using var searcher = new ManagementObjectSearcher(queryString);
                using var collection = searcher.Get();
                using var @object = collection.OfType<ManagementObject>().SingleOrDefault();

                if (@object is not null)
                {
                    parentProcessId = (int)(uint)@object["ParentProcessId"];
                }
            }
        }
    }

    public static string GetMainWindowTitle(this Process process)
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

                    var windowTitle = builder.ToString();
                    if (windowTitle.Length > 0)
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
