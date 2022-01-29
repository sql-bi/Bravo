namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.Runtime.InteropServices;

    internal static class Advapi32
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw
        /// </summary>
        public const uint CRED_MAX_STRING_LENGTH = 256;
        public const uint CRED_MAX_USERNAME_LENGTH = 513;
        public const uint CRED_MAX_GENERIC_TARGET_NAME_LENGTH = 32767;
        public const uint CRED_MAX_CREDENTIAL_BLOB_SIZE = 5 * 512;

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentiala
        /// </summary>
        public enum CREDENTIAL_TYPE
        {
            CRED_TYPE_GENERIC = 1,
            CRED_TYPE_DOMAIN_PASSWORD = 2,
            CRED_TYPE_DOMAIN_CERTIFICATE = 3,
            CRED_TYPE_DOMAIN_VISIBLE_PASSWORD = 4,
            CRED_TYPE_GENERIC_CERTIFICATE = 5,
            CRED_TYPE_DOMAIN_EXTENDED = 6,
            CRED_TYPE_MAXIMUM = 7,
            CRED_TYPE_MAXIMUM_EX = CRED_TYPE_MAXIMUM + 1000,
        }

        public enum CRED_PERSIST : uint
        {
            CRED_PERSIST_SESSION = 1,
            CRED_PERSIST_LOCAL_MACHINE = 2,
            CRED_PERSIST_ENTERPRISE = 3,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CREDENTIAL
        {
            public uint Flags;
            public CREDENTIAL_TYPE Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public CRED_PERSIST Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        [DllImport(ExternDll.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredReadW(string targetName, CREDENTIAL_TYPE type, int flags, out CredentialSafeHandle handle);

        [DllImport(ExternDll.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWriteW([In] ref CREDENTIAL credential, [In] uint flags);

        [DllImport(ExternDll.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredDeleteW(string targetName, CREDENTIAL_TYPE type, int flags);

        [DllImport(ExternDll.Advapi32, SetLastError = true)]
        public static extern bool CredFree([In] IntPtr buffer);
    }

    internal sealed class CredentialSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public CredentialSafeHandle()
            : base(ownsHandle: true)
        {
        }

        public Advapi32.CREDENTIAL GetCredential()
        {
            if (IsInvalid)
                throw new InvalidOperationException("Invalid handle");

            var credential = Marshal.PtrToStructure<Advapi32.CREDENTIAL>(handle);
            return credential;
        }

        protected override bool ReleaseHandle()
        {
            var released =  Advapi32.CredFree(handle);
            return released;
        }
    }
}
