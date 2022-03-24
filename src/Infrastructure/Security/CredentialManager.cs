namespace Sqlbi.Bravo.Infrastructure.Security
{
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class CredentialManager
    {
        public static GenericCredential? ReadCredential(string targetName)
        {
            var retval = Advapi32.CredReadW(targetName, Advapi32.CREDENTIAL_TYPE.CRED_TYPE_GENERIC, flags: 0, out var handle);
            try
            {
                if (retval == true)
                {
                    var credentialStruct = handle.GetCredential();

                    var userName = Marshal.PtrToStringUni(credentialStruct.UserName);
                    var password = credentialStruct.CredentialBlob != IntPtr.Zero 
                        ? Marshal.PtrToStringUni(credentialStruct.CredentialBlob, (int)credentialStruct.CredentialBlobSize / 2) 
                        : null;

                    var credential = new GenericCredential(targetName, userName, password);
                    return credential;
                }
            }
            finally
            {
                handle.Dispose();
            }

            return null;
        }

        public static void WriteCredential(string targetName, string userName, string password, Advapi32.CRED_PERSIST persist = Advapi32.CRED_PERSIST.CRED_PERSIST_LOCAL_MACHINE)
        {
            var targetNameLength = targetName.Length * UnicodeEncoding.CharSize;
            var userNameLength = userName.Length * UnicodeEncoding.CharSize;
            var passwordLength = password.Length * UnicodeEncoding.CharSize;

            if (targetNameLength > Advapi32.CRED_MAX_GENERIC_TARGET_NAME_LENGTH) throw new ArgumentOutOfRangeException(nameof(targetName));
            if (userNameLength > Advapi32.CRED_MAX_USERNAME_LENGTH) throw new ArgumentOutOfRangeException(nameof(userName));
            if (passwordLength > Advapi32.CRED_MAX_CREDENTIAL_BLOB_SIZE) throw new ArgumentOutOfRangeException(nameof(password));

            var targetNamePtr = Marshal.StringToCoTaskMemUni(targetName);
            var userNamePtr = Marshal.StringToCoTaskMemUni(userName);
            var passwordbPtr = Marshal.StringToCoTaskMemUni(password);
            try
            {
                var credential = new Advapi32.CREDENTIAL
                {
                    // Flags = ,
                    Type = Advapi32.CREDENTIAL_TYPE.CRED_TYPE_GENERIC,
                    TargetName = targetNamePtr,
                    Comment = IntPtr.Zero,
                    // LastWritten = ,
                    CredentialBlobSize = (uint)passwordLength,
                    CredentialBlob = passwordbPtr,
                    Persist = persist,
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero,
                    TargetAlias = IntPtr.Zero,
                    UserName = userNamePtr,
                };

                var written = Advapi32.CredWriteW(ref credential, flags: 0);
                if (written == false)
                {
                    var win32Error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(win32Error, $"Advapi32.CredWriteW failed [{ win32Error }]");
                }
            }
            finally
            {
                if (targetNamePtr != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUnicode(targetNamePtr);
                if (passwordbPtr != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUnicode(passwordbPtr);
                if (userNamePtr != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUnicode(userNamePtr);
            }
        }

        public static void DeleteCredential(string targetName)
        {
            var success = Advapi32.CredDeleteW(targetName, Advapi32.CREDENTIAL_TYPE.CRED_TYPE_GENERIC, flags: 0);
            if (success == false)
            {
                var win32Error = Marshal.GetLastWin32Error();
                throw new Win32Exception(win32Error, $"Advapi32.CredDeleteW failed [{ win32Error }]");
            }
        }
    }

    /// <summary>
    /// The credential is a generic credential. The credential will not be used by any particular authentication package. The credential will be stored securely but has no other significant characteristics.
    /// </summary>
    internal class GenericCredential
    {
        public GenericCredential(string targetName, string? userName, string? password)
        {
            TargetName = targetName;
            UserName = userName;
            Password = password;
        }

        public string TargetName { get; init; }

        public string? UserName { get; init; }

        public string? Password { get; init; }
    }
}
