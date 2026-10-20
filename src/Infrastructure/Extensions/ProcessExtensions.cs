using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Windows;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;

namespace Sqlbi.Bravo.Infrastructure.Extensions;

internal static class ProcessExtensions
{
    //[DebuggerStepThrough]
    [Obsolete("Use GetParentProcess")]
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

    public static IReadOnlyList<int> GetChildProcessIds(this Process process, ProcessTree processTree, string? childProcessImageName = null)
    {
        var sessionId = process.SessionId;

        var childProcessIds = new List<int>();
        foreach (var childProcessId in processTree.GetChildIds(process.Id, childProcessImageName))
        {
            if (!Process.TryGetById(childProcessId, sessionId, out var childProcess))
                continue;

            var isChild = IsStartedBefore(process, childProcess);
            childProcess.Dispose();

            if (!isChild)
                continue;

            childProcessIds.Add(childProcessId);
        }

        return childProcessIds;
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
            var sessionId = process.SessionId;
            var processTree = ProcessTree.Capture();

            var parentProcessId = processTree.GetParentId(process.Id);
            if (parentProcessId is null)
                return null;

            if (!Process.TryGetById(parentProcessId.Value, sessionId, out var parentProcess))
                return null;

            if (!IsStartedBefore(parentProcess, process))
            {
                parentProcess.Dispose();
                return null;
            }

            return parentProcess;
        }
    }

    // A process identifier is reused once its process exits, so the recorded parent or child can be an unrelated
    // process: a real parent always started before its child.
    private static bool IsStartedBefore(Process earlier, Process later)
    {
        try
        {
            // StartTime is local time, which repeats an hour when daylight saving time ends: only UTC keeps the order.
            var earlierStartTime = earlier.StartTime.ToUniversalTime();
            var laterStartTime = later.StartTime.ToUniversalTime();

            return earlierStartTime <= laterStartTime;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // The start time of a process that has exited, or that cannot be opened, is unknown.
            return false;
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
