using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Credentials;

namespace Bravo.Desktop.Interop;

/// <summary>
/// The dialog box of Windows that asks for a user name and a password, and gives them back as text.
/// </summary>
internal static class CredentialDialogInterop
{
    // The blocks are packed as text, the form that the dialog box reads and writes when it is asked for text.
    private const CRED_PACK_FLAGS PackFlags = CRED_PACK_FLAGS.CRED_PACK_GENERIC_CREDENTIALS;

    // Windows opens a block that it protected for the session of the user, as 1.0.x asks it to.
    private const CRED_PACK_FLAGS UnpackFlags = CRED_PACK_FLAGS.CRED_PACK_PROTECTED_CREDENTIALS;

    private const char DomainSeparator = '\\';

    /// <summary>
    /// Shows the dialog box over a window, which takes no input until the dialog box is closed, and returns
    /// then. It is called on the thread of the window.
    /// </summary>
    /// <param name="ownerHandle">The window that the dialog box belongs to.</param>
    /// <param name="caption">The title that the dialog box shows.</param>
    /// <param name="message">The text below the title.</param>
    /// <param name="userName">The user name that the dialog box opens with, or <see langword="null"/> for none.</param>
    /// <returns>What the user entered, or <see langword="null"/> when the user cancelled.</returns>
    /// <exception cref="Win32Exception">Windows could not show the dialog box, or read what the user entered.</exception>
    public static unsafe (string UserName, string Password)? Prompt(nint ownerHandle, string caption, string message, string? userName)
    {
        var shown = userName is null ? [] : Pack(userName, password: string.Empty);

        fixed (char* captionText = caption, messageText = message)
        {
            var info = new CREDUI_INFOW
            {
                cbSize = (uint)sizeof(CREDUI_INFOW),
                hwndParent = (HWND)ownerHandle,
                pszCaptionText = captionText,
                pszMessageText = messageText,
            };
            uint package = 0;

            var result = PInvoke.CredUIPromptForWindowsCredentials(
                info,
                dwAuthError: 0,
                ref package,
                shown,
                out var block,
                out var blockSize,
                CREDUIWIN_FLAGS.CREDUIWIN_GENERIC);

            var entered = new Span<byte>(block, (int)blockSize);
            try
            {
                return (WIN32_ERROR)result switch
                {
                    WIN32_ERROR.ERROR_SUCCESS => Unpack(entered),
                    WIN32_ERROR.ERROR_CANCELLED => null,
                    _ => throw new Win32Exception((int)result),
                };
            }
            finally
            {
                // What the user typed is not left readable until the memory is used again.
                CryptographicOperations.ZeroMemory(entered);
                Marshal.FreeCoTaskMem((nint)block);
            }
        }
    }

    /// <summary>
    /// The block that the dialog box reads the credentials that it opens with from.
    /// </summary>
    /// <exception cref="Win32Exception">Windows could not pack the credentials.</exception>
    internal static unsafe byte[] Pack(string userName, string password)
    {
        fixed (char* userNameText = userName, passwordText = password)
        {
            // Without a block Windows tells the size of the one that it needs.
            uint size = 0;
            _ = PInvoke.CredPackAuthenticationBuffer(PackFlags, userNameText, passwordText, default, ref size);

            var block = new byte[size];
            if (!PInvoke.CredPackAuthenticationBuffer(PackFlags, userNameText, passwordText, block, ref size))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            return block;
        }
    }

    /// <summary>
    /// The user name and the password of a block that the dialog box, or <see cref="Pack"/>, wrote. A domain
    /// that Windows gives apart is put back before the user name.
    /// </summary>
    /// <exception cref="Win32Exception">Windows could not read the block.</exception>
    internal static (string UserName, string Password) Unpack(ReadOnlySpan<byte> block)
    {
        // Without a text to write in, Windows tells the length of each one, with its terminator.
        uint userNameLength = 0;
        uint domainLength = 0;
        uint passwordLength = 0;
        _ = PInvoke.CredUnPackAuthenticationBuffer(
            UnpackFlags, block, default, ref userNameLength, default, ref domainLength, default, ref passwordLength);

        var userName = new char[userNameLength];
        var domain = new char[domainLength];
        var password = new char[passwordLength];
        try
        {
            var isRead = PInvoke.CredUnPackAuthenticationBuffer(
                UnpackFlags, block, userName, ref userNameLength, domain, ref domainLength, password, ref passwordLength);
            if (!isRead)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            return (JoinDomain(ToText(domain), ToText(userName)), ToText(password));
        }
        finally
        {
            Array.Clear(password);
        }
    }

    private static string JoinDomain(string domain, string userName)
    {
        if (domain.Length == 0)
        {
            return userName;
        }

        return $"{domain}{DomainSeparator}{userName}";
    }

    private static string ToText(ReadOnlySpan<char> terminated)
    {
        var end = terminated.IndexOf('\0');

        return new string(end < 0 ? terminated : terminated[..end]);
    }
}
