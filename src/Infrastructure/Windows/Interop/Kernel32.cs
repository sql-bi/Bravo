namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static class Kernel32
{
    public const int MAX_PATH = 260;

    public static IEnumerable<ProcessEntry32> SnapshotProcess(int parentProcessId) => SnapshotProcess().Where((e) => e.th32ParentProcessID == parentProcessId);

    public static IEnumerable<ProcessEntry32> SnapshotProcess()
    {
        using var handle = CreateToolhelp32Snapshot(Toolhelp32Flags.TH32CS_SNAPPROCESS, processId: 0);
        if (handle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var dwSize = (uint)Marshal.SizeOf(typeof(ProcessEntry32));
        var entry = new ProcessEntry32 { dwSize = dwSize };
        var success = Process32First(handle, ref entry);

        while (success)
        {
            yield return entry;

            entry = new ProcessEntry32 { dwSize = dwSize };
            success = Process32Next(handle, ref entry);
        }
    }

    public static string? GetCurrentPackageFullName(bool throwOnError = true)
    {
        // https://docs.microsoft.com/en-us/windows/msix/detect-package-identity

        var length = 0;
        var error  = GetCurrentPackageFullName(ref length, packageFullName: null);
        if (error == WIN32ERROR.ERROR_INSUFFICIENT_BUFFER)
        {
            var builder = new StringBuilder(length);
            error = GetCurrentPackageFullName(ref length, builder);

            if (error == WIN32ERROR.ERROR_SUCCESS)
                return builder.ToString();
        }

        if (throwOnError && error != WIN32ERROR.APPMODEL_ERROR_NO_PACKAGE)
            throw new Win32Exception(error);

        return null;
    }

    public static string GetFinalPathNameByHandle(SafeFileHandle handle)
    {
        var builder = new StringBuilder(MAX_PATH);
        var length = GetFinalPathNameByHandle(handle, builder, builder.Capacity, FinalPathNameFlags.FILE_NAME_NORMALIZED);
        if (length == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return builder.ToString();
    }

    public static bool DuplicateHandle(SafeProcessHandle sourceProcessHandle, SafeHandle sourceHandle, SafeProcessHandle targetProcessHandle, out IntPtr targetHandle)
    {
        return DuplicateHandle(sourceProcessHandle, sourceHandle.DangerousGetHandle(), targetProcessHandle, out targetHandle, dwDesiredAccess: 0 /* ignored */, bInheritHandle: false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
    }

    [DllImport(ExternDll.Kernel32, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr LocalFree(IntPtr hMem);

    [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);

    [Flags]
    private enum DUPLICATE_HANDLE_OPTIONS : uint
    {
        DUPLICATE_SAME_ACCESS = 0x00000002,
    }

    [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(SafeProcessHandle hSourceProcessHandle, IntPtr hSourceHandle, SafeProcessHandle hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, DUPLICATE_HANDLE_OPTIONS dwOptions);

    private enum Toolhelp32Flags : uint
    {
        TH32CS_INHERIT = 0x80000000,
        TH32CS_SNAPHEAPLIST = 0x00000001,
        TH32CS_SNAPMODULE = 0x00000008,
        TH32CS_SNAPMODULE32 = 0x00000010,
        TH32CS_SNAPPROCESS = 0x00000002,
        TH32CS_SNAPTHREAD = 0x00000004,
        TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST | TH32CS_SNAPMODULE | TH32CS_SNAPPROCESS | TH32CS_SNAPTHREAD,
    };

    [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeProcessHandle CreateToolhelp32Snapshot(Toolhelp32Flags flags, [Optional] int processId);

    [DllImport(ExternDll.Kernel32, SetLastError = false, ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, [Optional] StringBuilder? packageFullName);

    [Flags]
    private enum FinalPathNameFlags : uint
    {
        FILE_NAME_NORMALIZED = 0x0,
    }

    [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int GetFinalPathNameByHandle(SafeFileHandle hFile, StringBuilder lpszFilePath, int cchFilePath, FinalPathNameFlags dwFlags);

    [DebuggerDisplay("{szExeFile,nq}({th32ProcessID})")]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct ProcessEntry32
    {
        public uint dwSize;
        public uint cntUsage;
        public int th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public int th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string szExeFile;
    }

    [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(SafeProcessHandle snapshotHandle, ref ProcessEntry32 entry);

    [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(SafeProcessHandle snapshotHandle, ref ProcessEntry32 entry);
}
