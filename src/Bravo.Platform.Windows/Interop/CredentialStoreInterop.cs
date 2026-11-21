using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Credentials;

namespace Bravo.Platform.Windows.Interop;

/// <summary>
/// The generic credentials of the Windows Credential Manager: a user name and a secret under a target name.
/// The secret is text, kept as its UTF-16 characters with no terminator.
/// </summary>
internal static class CredentialStoreInterop
{
    /// <returns><see langword="null"/> when no credential has the target name.</returns>
    /// <exception cref="Win32Exception">Windows could not read the credential.</exception>
    public static unsafe (string UserName, string Secret)? ReadGeneric(string targetName)
    {
        var isRead = PInvoke.CredRead(targetName, CRED_TYPE.CRED_TYPE_GENERIC, out var handle);
        var error = Marshal.GetLastPInvokeError();
        using (handle)
        {
            if (!isRead)
            {
                if (error == (int)WIN32_ERROR.ERROR_NOT_FOUND)
                {
                    return null;
                }

                throw new Win32Exception(error);
            }

            using var lease = handle.Lease();

            return CopyAndClear(lease.Value);
        }
    }

    // The block that Windows handed over is read while its handle is held, and its secret is not left
    // readable until the memory is used again.
    private static unsafe (string UserName, string Secret) CopyAndClear(CREDENTIALW* credential)
    {
        var blob = new Span<byte>(credential->CredentialBlob, (int)credential->CredentialBlobSize);
        try
        {
            var userName = credential->UserName.ToString() ?? string.Empty;
            var secret = new string(MemoryMarshal.Cast<byte, char>(blob));

            return (userName, secret);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(blob);
        }
    }

    /// <exception cref="Win32Exception">Windows could not keep the credential.</exception>
    public static unsafe void WriteGeneric(string targetName, string userName, string secret)
    {
        fixed (char* target = targetName, user = userName, blob = secret)
        {
            var credential = new CREDENTIALW
            {
                Type = CRED_TYPE.CRED_TYPE_GENERIC,
                TargetName = target,
                UserName = user,
                CredentialBlob = (byte*)blob,
                CredentialBlobSize = (uint)(secret.Length * sizeof(char)),
                Persist = CRED_PERSIST.CRED_PERSIST_LOCAL_MACHINE,
            };

            if (!PInvoke.CredWrite(credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }
    }

    /// <summary>
    /// Deletes the credential of a target name. With no such credential it does nothing.
    /// </summary>
    /// <exception cref="Win32Exception">Windows could not delete the credential.</exception>
    public static void DeleteGeneric(string targetName)
    {
        if (PInvoke.CredDelete(targetName, CRED_TYPE.CRED_TYPE_GENERIC))
        {
            return;
        }

        var error = Marshal.GetLastPInvokeError();
        if (error != (int)WIN32_ERROR.ERROR_NOT_FOUND)
        {
            throw new Win32Exception(error);
        }
    }
}
