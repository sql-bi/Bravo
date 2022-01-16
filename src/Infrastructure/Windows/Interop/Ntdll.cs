using System;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    internal static class Ntdll
    {
        internal enum NTSTATUS
        {
            STATUS_SUCCESS = 0
        }

        internal enum PROCESSINFOCLASS
        {
            ProcessBasicInformation = 0,
            //ProcessDebugPort = 7,
            //ProcessWow64Information = 26,
            //ProcessImageFileName = 27,
            //ProcessBreakOnTermination = 29,
            //ProcessSubsystemInformation = 75
        }

        internal struct PROCESS_BASIC_INFORMATION
        {
            public uint ExitStatus;

            public IntPtr PebBaseAddress;

            public UIntPtr AffinityMask;

            public int BasePriority;

            public UIntPtr UniqueProcessId;

            public UIntPtr InheritedFromUniqueProcessId;
        }

        [DllImport(ExternDll.Ntdll, ExactSpelling = true, SetLastError = true)]
        internal static extern int NtQueryInformationProcess(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, out PROCESS_BASIC_INFORMATION processInformation, uint processInformationLength, out int returnLength);
    }
}
