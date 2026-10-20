using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.ToolHelp;

namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

internal static class ProcessInterop
{
    /// <summary>
    /// Returns the processes running now, each with the identifier of the process that created it.
    /// </summary>
    /// <exception cref="Win32Exception">The processes cannot be read.</exception>
    public static IReadOnlyList<ProcessEntry> GetProcesses()
    {
        using var snapshot = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, th32ProcessID: 0);
        if (snapshot.IsInvalid)
            throw new Win32Exception();

        var entry = new PROCESSENTRY32W
        {
            dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32W>(),
        };

        if (!PInvoke.Process32FirstW(snapshot, ref entry))
            throw new Win32Exception();

        var processes = new List<ProcessEntry>();
        do
        {
            var process = new ProcessEntry((int)entry.th32ProcessID, (int)entry.th32ParentProcessID, entry.szExeFile.ToString());
            processes.Add(process);
        }
        while (PInvoke.Process32NextW(snapshot, ref entry));

        var error = (WIN32_ERROR)Marshal.GetLastPInvokeError();
        if (error != WIN32_ERROR.ERROR_NO_MORE_FILES)
            throw new Win32Exception((int)error);

        return processes;
    }
}
