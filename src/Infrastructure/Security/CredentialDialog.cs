namespace Sqlbi.Bravo.Infrastructure.Security
{
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class CredentialDialog
    {
        public static NetworkCredential? PromptForCredentials(CredentialDialogOptions options)
        {
            var uiInfo = new Credui.CREDUI_INFO
            {
                hwndParent = options.HwndParent,
                pszMessageText = options.Message,
                pszCaptionText = options.Caption,
                //hbmBanner = options.HbmBanner,
            };
            uiInfo.cbSize = Marshal.SizeOf(uiInfo);

            var authPackage = 0;
            var inAuthBuffer = IntPtr.Zero;
            var inAuthBufferSize = 0;
            var flags = options.Flags;
            var save = options.IsSaveChecked;
            var authError = options.AuthError;

            var retval = Credui.CredUIPromptForWindowsCredentialsW(ref uiInfo, authError, ref authPackage, inAuthBuffer, inAuthBufferSize, out var outAuthBuffer, out var outAuthBufferSize, ref save, flags);
            try
            {
                switch (retval)
                {
                    case NativeMethods.ERROR_SUCCESS:
                        {
                            var networkCredential = CredUnPackNetworkCredential(outAuthBuffer, outAuthBufferSize);
                            return networkCredential;
                        }
                    case NativeMethods.ERROR_CANCELLED:
                        return null;
                }

                throw new Win32Exception(retval);
            }
            finally
            {
                if (outAuthBuffer != IntPtr.Zero)
                {
                    // Make sure buffer is zeroed out. TODO: where is SecureZeroMem ??
                    var zeroedBuffer = new byte[outAuthBufferSize];
                    Marshal.Copy(zeroedBuffer, 0, outAuthBuffer, outAuthBufferSize);
                }

                Marshal.FreeCoTaskMem(inAuthBuffer);
                Marshal.FreeCoTaskMem(outAuthBuffer);
            }
        }

        private static NetworkCredential CredUnPackNetworkCredential(IntPtr authBufferPtr, int authBufferSize)
        {
            var domainBuffer = new StringBuilder(Credui.CREDUI_MAX_DOMAIN_LENGTH);
            var userNameBuffer = new StringBuilder(Credui.CREDUI_MAX_USERNAME_LENGTH);
            var passwordBuffer = new StringBuilder(Credui.CREDUI_MAX_PASSWORD_LENGTH);
            var domainBufferSize = domainBuffer.Capacity;
            var userNameBufferSize = userNameBuffer.Capacity;
            var passwordBufferSize = passwordBuffer.Capacity;

            //#define CRED_PACK_PROTECTED_CREDENTIALS      0x1
            //#define CRED_PACK_WOW_BUFFER                 0x2
            //#define CRED_PACK_GENERIC_CREDENTIALS        0x4
            var dwFlags = 0x1;

            var result = Credui.CredUnPackAuthenticationBufferW(dwFlags, authBufferPtr, authBufferSize, userNameBuffer, ref userNameBufferSize, domainBuffer, ref domainBufferSize, passwordBuffer, ref passwordBufferSize);
            if (result == false)
            {
                var win32Error = Marshal.GetLastWin32Error();
                throw new Win32Exception(win32Error);
            }

            var networkCredential = new NetworkCredential
            {
                Domain = domainBuffer.ToString(),
                UserName = userNameBuffer.ToString(),
                Password = passwordBuffer.ToString()
            };

            if (string.IsNullOrWhiteSpace(networkCredential.Domain))
            {
                userNameBuffer.Clear();
                domainBuffer.Clear();

                var retval = Credui.CredUIParseUserNameW(networkCredential.UserName, userNameBuffer, userNameBuffer.Capacity, domainBuffer, domainBuffer.Capacity);

                switch (retval)
                {
                    case NativeMethods.ERROR_SUCCESS:
                        networkCredential.Domain = domainBuffer.ToString();
                        networkCredential.UserName = userNameBuffer.ToString();
                        break;
                    case NativeMethods.ERROR_INVALID_ACCOUNT_NAME:
                        break;
                    //case NativeMethods.ERROR_INSUFFICIENT_BUFFER:
                    //case NativeMethods.ERROR_INVALID_PARAMETER:
                    default:
                        throw new Win32Exception((int)retval);
                }
            }

            return networkCredential;
        }
    }

    internal class CredentialDialogOptions
    {
        public CredentialDialogOptions(string caption, string message)
        {
            if (caption.Length > Credui.CREDUI_MAX_CAPTION_LENGTH) throw new ArgumentOutOfRangeException(nameof(caption));
            if (message.Length > Credui.CREDUI_MAX_MESSAGE_LENGTH) throw new ArgumentOutOfRangeException(nameof(message));

            Caption = caption;
            Message = message;

            AuthError = 0;
            IsSaveChecked = false;
            Flags = Credui.CREDUIWIN.CREDUIWIN_GENERIC;
        }

        public string Caption { get; set; }

        public string Message { get; set; }

        public IntPtr HwndParent { get; set; }

        //public IntPtr HbmBanner { get; set; }

        public bool IsSaveChecked { get; private set; }

        public int AuthError { get; private set; }

        public Credui.CREDUIWIN Flags { get; private set; }
    }
}
