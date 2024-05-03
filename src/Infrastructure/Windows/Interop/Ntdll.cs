namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

internal static class Ntdll
{
    public static Dictionary<int, SYSTEM_PROCESS_INFORMATION> NtQuerySystemInformationProcessInformation(int sessionId, string? imageName = null, StringComparison comparisonType = StringComparison.Ordinal)
    {
        var query = NtQuerySystemInformationProcessInformation().Where((p) => p.Value.SessionId == sessionId);

        if (imageName is not null)
            query = query.Where((p) => p.Value.ImageName.Buffer.Equals(imageName, comparisonType));

        return query.ToDictionary(p => p.Key, p => p.Value);
    }

    public static string NtQueryInformationProcessCommandLineInformation(int processId)
    {
        using var handle = NtOpenProcess(processId, ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION);
        var buffer = IntPtr.Zero;
        try
        {
            buffer = NtQueryInformationProcess<UNICODE_STRING>(handle, PROCESSINFOCLASS.ProcessCommandLineInformation);
            return Marshal.PtrToStructure<UNICODE_STRING>(buffer).Buffer ?? string.Empty;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }
    }

    public static PROCESS_HANDLE_TABLE_ENTRY_INFO[] NtQueryInformationProcessHandleInformation(int processId)
    {
        // TODO: PROCESSINFOCLASS.ProcessHandleInformation is available on Windows 8 and later

        using var handle = NtOpenProcess(processId, ProcessAccess.PROCESS_QUERY_INFORMATION);
        var buffer = IntPtr.Zero;
        try
        {
            buffer = NtQueryInformationProcess<PROCESS_HANDLE_SNAPSHOT_INFORMATION>(handle, PROCESSINFOCLASS.ProcessHandleInformation);
            return Read(buffer);
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }

        static PROCESS_HANDLE_TABLE_ENTRY_INFO[] Read(IntPtr buffer) // Consider refactoring using SafeBuffer.ReadArray()
        {
            var snapshot = Marshal.PtrToStructure<PROCESS_HANDLE_SNAPSHOT_INFORMATION>(buffer);
            var handles = new PROCESS_HANDLE_TABLE_ENTRY_INFO[snapshot.NumberOfHandles.ToInt32()];

            var size = Marshal.SizeOf<PROCESS_HANDLE_TABLE_ENTRY_INFO>();
            var offset = Marshal.OffsetOf(typeof(PROCESS_HANDLE_SNAPSHOT_INFORMATION), nameof(PROCESS_HANDLE_SNAPSHOT_INFORMATION.Handles));
            var ptr = buffer + offset.ToInt32();

            for (var i = 0; i < handles.Length; i++)
            {
                handles[i] = Marshal.PtrToStructure<PROCESS_HANDLE_TABLE_ENTRY_INFO>(ptr);
                ptr += size;
            }

            return handles;
        }
    }

    public static OBJECT_TYPE_INFORMATION NtQueryObjectTypeInformation(SafeHandle handle)
    {
        return NtQueryObject<OBJECT_TYPE_INFORMATION>(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation);
    }

    public static string NtQueryObjectNameInformation(SafeHandle handle)
    {
        return NtQueryObject<OBJECT_NAME_INFORMATION>(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation).Name.Buffer;
    }

    public static FILE_DEVICE_TYPE NtQueryVolumeInformationFileFsDeviceInformation(SafeHandle handle)
    {
        var bufferSize = Marshal.SizeOf<FILE_FS_DEVICE_INFORMATION>();
        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            var ioStatusBlock = new IO_STATUS_BLOCK();
            var status = NtQueryVolumeInformationFile(handle, ioStatusBlock, buffer, bufferSize, FS_INFORMATION_CLASS.FileFsDeviceInformation);
            if ((int)status < 0)
                throw new InvalidOperationException($"NtQueryVolumeInformationFile failed (0x{status:X08})");

            return Marshal.PtrToStructure<FILE_FS_DEVICE_INFORMATION>(buffer).DeviceType;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static SafeProcessHandle NtOpenProcess(int processId, ProcessAccess access)
    {
        // Calling this function from a 32 bit process where the target process is 64 bit is not supported

        var clientId = new CLIENT_ID
        {
            UniqueProcess = new IntPtr(processId),
            UniqueThread = IntPtr.Zero,
        };

        var status = NtOpenProcess(out SafeProcessHandle handle, access, objectAttributes: default, clientId);
        if ((int)status < 0)
            throw new InvalidOperationException($"NtOpenProcess failed (0x{status:X08})");

        return handle;
    }

    [Flags]
    public enum ProcessAccess : uint
    {
        None = 0,
        PROCESS_DUP_HANDLE = 0x0040,
        PROCESS_QUERY_INFORMATION = 0x0400,
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OBJECT_ATTRIBUTES
    {
        public uint Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    [DllImport(ExternDll.Ntdll, SetLastError = false, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint NtOpenProcess(out SafeProcessHandle processHandle, ProcessAccess desiredAccess, [In] OBJECT_ATTRIBUTES objectAttributes, [In] CLIENT_ID clientId);

    private enum PROCESSINFOCLASS
    {
        ProcessBasicInformation = 0,
        ProcessHandleInformation = 51,
        ProcessCommandLineInformation = 60,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public uint ExitStatus;
        public IntPtr PebBaseAddress;
        public UIntPtr AffinityMask;
        public int BasePriority;
        public UIntPtr UniqueProcessId;
        public UIntPtr InheritedFromUniqueProcessId;
    }

    [Flags]
    private enum AttributeFlags : uint
    {
        None = 0,
        ProtectClose = 0x00000001,
        Inherit = 0x00000002,
        AuditObjectClose = 0x00000004,
        Permanent = 0x00000010,
        Exclusive = 0x00000020,
        CaseInsensitive = 0x00000040,
        OpenIf = 0x00000080,
        OpenLink = 0x00000100,
        KernelHandle = 0x00000200,
        ForceAccessCheck = 0x00000400,
        IgnoreImpersonatedDevicemap = 0x00000800,
        DontReparse = 0x00001000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_HANDLE_TABLE_ENTRY_INFO
    {
        public IntPtr HandleValue;
        private readonly IntPtr HandleCount;
        private readonly IntPtr PointerCount;
        public ACCESS_MASK GrantedAccess;
        public int ObjectTypeIndex;
        private readonly AttributeFlags HandleAttributes;
        private readonly int Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_HANDLE_SNAPSHOT_INFORMATION
    {
        public IntPtr NumberOfHandles;
        private readonly IntPtr Reserved;
        public PROCESS_HANDLE_TABLE_ENTRY_INFO Handles;
    }

    private static IntPtr NtQueryInformationProcess<T>(SafeProcessHandle handle, PROCESSINFOCLASS @class) where T : struct
    {
        // If we're on a 64 bit OS then the target process will have a 64 bit PEB if we are calling this function from a 64 bit process (regardless of whether or not the target process is 32 bit or 64 bit).
        // If we are calling this function from a 32 bit process and the target process is 32 bit then we will get a 32 bit PEB, even on a 64 bit OS.
        // The one situation we can't handle is if we are calling this function from a 32 bit process and the target process is 64 bit.
        // For that we need to use the undocumented NtWow64QueryInformationProcess64 and NtWow64ReadVirtualMemory64 APIs
        // See also https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess#ulong_ptr

        var bufferLength = Marshal.SizeOf(typeof(T)); // use base size before trying to reallocate
        var buffer = Marshal.AllocHGlobal(bufferLength);
        do
        {
            var status = NtQueryInformationProcess(handle, @class, buffer, (uint)bufferLength, out var bufferRequiredLength);
            if (status != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH /* && status != NTSTATUS.STATUS_BUFFER_TOO_SMALL && status != NTSTATUS.STATUS_BUFFER_OVERFLOW */)
            {
                if ((int)status < 0 || (int)bufferRequiredLength == 0)
                    throw new InvalidOperationException($"NtQueryInformationProcess failed (0x{status:X08})"); // TODO: Win32Exception as inner exception

                return buffer;
            }

            bufferRequiredLength += 1024 * 10; // add extra space for new information that may have been created since the last query
            bufferLength = (int)bufferRequiredLength;
            buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)bufferLength);
        }
        while (true);
    }

    [DllImport(ExternDll.Ntdll, SetLastError = false, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint NtQueryInformationProcess(SafeProcessHandle processHandle, PROCESSINFOCLASS processInformationClass, IntPtr processInformation, uint processInformationLength, out uint returnLength);

    private enum SYSTEM_INFORMATION_CLASS
    {
        SystemProcessInformation = 5
    }

    [DebuggerDisplay("{Buffer,nq}")]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        private readonly ushort MaximumLength;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CLIENT_ID
    {
        public IntPtr UniqueProcess;
        public IntPtr UniqueThread;
    }

    [DebuggerDisplay("{ImageName,nq}({(int)UniqueProcessId})")]
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_PROCESS_INFORMATION
    {
        public uint NextEntryOffset;
        private readonly uint NumberOfThreads;
        private readonly ulong Reserved1_1;
        private readonly ulong Reserved1_2;
        private readonly ulong Reserved1_3;
        private readonly ulong Reserved1_4;
        private readonly ulong Reserved1_5;
        private readonly ulong Reserved1_6;
        public UNICODE_STRING ImageName;
        private readonly int BasePriority;
        public IntPtr UniqueProcessId;
        private readonly IntPtr Reserved2;
        private readonly uint HandleCount;
        public uint SessionId;
        private readonly IntPtr Reserved3;
        private readonly UIntPtr PeakVirtualSize;
        private readonly UIntPtr VirtualSize;
        private readonly uint Reserved4;
        private readonly UIntPtr PeakWorkingSetSize;
        private readonly UIntPtr WorkingSetSize;
        private readonly IntPtr Reserved5;
        private readonly UIntPtr QuotaPagedPoolUsage;
        private readonly IntPtr Reserved6;
        private readonly UIntPtr QuotaNonPagedPoolUsage;
        private readonly UIntPtr PagefileUsage;
        private readonly UIntPtr PeakPagefileUsage;
        private readonly UIntPtr PrivatePageCount;
        private readonly long Reserved7_1;
        private readonly long Reserved7_2;
        private readonly long Reserved7_3;
        private readonly long Reserved7_4;
        private readonly long Reserved7_5;
        private readonly long Reserved7_6;

        //public readonly string ProcessName
        //{
        //    get
        //    {
        //        var name = ImageName.Buffer;

        //        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        //            name = name[..^4];

        //        var index = name.LastIndexOf('\\');
        //        if (index >= 0)
        //            name = name[(index + 1)..];

        //        return name;
        //    }
        //}
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_THREAD_INFORMATION
    {
        private readonly ulong Reserved1_1;
        private readonly ulong Reserved1_2;
        private readonly ulong Reserved1_3;
        private readonly uint Reserved2;
        public IntPtr StartAddress;
        public CLIENT_ID ClientId;
        public int Priority;
        public int BasePriority;
        private readonly uint Reserved3;
        public uint ThreadState;
        public uint WaitReason;
    }

    private static Dictionary<int, SYSTEM_PROCESS_INFORMATION> NtQuerySystemInformationProcessInformation()
    {
        long[] buffer = [];
        int bufferLength = 0;
        var bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            do
            {
                var pinnedBuffer = bufferHandle.AddrOfPinnedObject();
                var status = NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessInformation, pinnedBuffer, (uint)bufferLength, out var bufferRequiredLength);
                if (status != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
                {
                    if ((int)status < 0)
                        throw new InvalidOperationException($"NtQuerySystemInformation failed (0x{status:X08})"); // TODO: Win32Exception as inner exception

                    return Read(pinnedBuffer);
                }

                bufferRequiredLength += 1024 * 10; // add extra space for new processes that may have been created since the last query
                buffer = new long[(bufferRequiredLength + 7) / 8]; // 64-bit aligned
                bufferLength = buffer.Length * sizeof(long);

                bufferHandle.Free();
                bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            }
            while (true);
        }
        finally
        {
            if (bufferHandle.IsAllocated)
                bufferHandle.Free();
        }

        static Dictionary<int, SYSTEM_PROCESS_INFORMATION> Read(IntPtr buffer)
        {
            var processes = new Dictionary<int, SYSTEM_PROCESS_INFORMATION>(100);
            var address = buffer.ToInt64();
            var offset = 0L;
            do
            {
                var ptr = new IntPtr(address + offset);
                var spi = Marshal.PtrToStructure<SYSTEM_PROCESS_INFORMATION>(ptr);
                var processId = spi.UniqueProcessId.ToInt32();

                spi.ImageName.Buffer ??= processId switch
                {
                    0 => "Idle",
                    4 => "System",
                    _ => processId.ToString(CultureInfo.InvariantCulture)
                };

                processes.Add(processId, spi);

                if (spi.NextEntryOffset == 0)
                    break;

                offset += spi.NextEntryOffset;
            }
            while (true);

            return processes;
        }
    }

    [DllImport(ExternDll.Ntdll, SetLastError = false, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS systemInformationClass, IntPtr systemInformation, uint systemInformationLength, out uint returnLength);

    private enum FS_INFORMATION_CLASS
    {
        FileFsDeviceInformation = 4,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_STATUS_BLOCK
    {
        public UIntPtr Pointer;
        public IntPtr Information;
    }

    public enum FILE_DEVICE_TYPE
    {
        FILE_DEVICE_CD_ROM = 0x00000002,
        FILE_DEVICE_DISK = 0x00000007, // See https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/specifying-device-types
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_FS_DEVICE_INFORMATION
    {
        public FILE_DEVICE_TYPE DeviceType;
        public uint Characteristics;
    }

    [DllImport(ExternDll.Ntdll, SetLastError = false, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint NtQueryVolumeInformationFile(SafeHandle fileHandle, [Out] IO_STATUS_BLOCK ioStatusBlock, IntPtr fsInformation, int length, FS_INFORMATION_CLASS fsInformationClass);

    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_TYPE_INFORMATION
    {
        public UNICODE_STRING Name;
        private readonly uint TotalNumberOfObjects;
        private readonly uint TotalNumberOfHandles;
        private readonly uint TotalPagedPoolUsage;
        private readonly uint TotalNonPagedPoolUsage;
        private readonly uint TotalNamePoolUsage;
        private readonly uint TotalHandleTableUsage;
        private readonly uint HighWaterNumberOfObjects;
        private readonly uint HighWaterNumberOfHandles;
        private readonly uint HighWaterPagedPoolUsage;
        private readonly uint HighWaterNonPagedPoolUsage;
        private readonly uint HighWaterNamePoolUsage;
        private readonly uint HighWaterHandleTableUsage;
        private readonly AttributeFlags InvalidAttributes;
        private readonly GENERIC_MAPPING GenericMapping;
        private readonly uint ValidAccess;
        private readonly byte SecurityRequired;
        private readonly byte MaintainHandleCount;
        public byte TypeIndex;
        private readonly byte ReservedByte;
        private readonly uint PoolType;
        private readonly uint PagedPoolUsage;
        private readonly uint NonPagedPoolUsage;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OBJECT_NAME_INFORMATION
    {
        public UNICODE_STRING Name;
    }

    private enum OBJECT_INFORMATION_CLASS
    {
        ObjectNameInformation = 1,
        ObjectTypeInformation = 2,
    }

    private static T NtQueryObject<T>(SafeHandle handle, OBJECT_INFORMATION_CLASS @class) where T : struct
    {
        var bufferLength = 0;
        var buffer = Marshal.AllocHGlobal(bufferLength);
        try
        {
            var status = NtQueryObject(handle, @class, buffer, (uint)bufferLength, out var returnLength);
            if (status != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH && status != NTSTATUS.STATUS_BUFFER_TOO_SMALL)
                throw new InvalidOperationException($"NtQueryObject failed (0x{status:X08})");

            bufferLength = (int)returnLength;
            buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)bufferLength);

            status = NtQueryObject(handle, @class, buffer, (uint)bufferLength, out returnLength);
            if ((int)status < 0)
                throw new InvalidOperationException($"NtQueryObject failed (0x{status:X08})");

            return Marshal.PtrToStructure<T>(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport(ExternDll.Ntdll, SetLastError = false, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint NtQueryObject(SafeHandle objectHandle, OBJECT_INFORMATION_CLASS objectInformationClass, IntPtr objectInformation, uint objectInformationLength, out uint returnLength);
}
