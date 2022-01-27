namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class Credui
    {
        public const int CREDUI_MAX_MESSAGE_LENGTH = 32767;
        public const int CREDUI_MAX_CAPTION_LENGTH = 128;
        public const int CREDUI_MAX_DOMAIN_LENGTH = 256;
        public const int CREDUI_MAX_USERNAME_LENGTH = 256;
        public const int CREDUI_MAX_PASSWORD_LENGTH = (512 / 2);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string? pszMessageText;
            public string? pszCaptionText;
            public IntPtr hbmBanner;
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/wincred/nf-wincred-creduipromptforwindowscredentialsa
        /// </summary>
        [Flags]
        public enum CREDUIWIN : uint
        {
            CREDUIWIN_GENERIC = 0x1,
            CREDUIWIN_CHECKBOX = 0x2,
            CREDUIWIN_AUTHPACKAGE_ONLY = 0x10,
            CREDUIWIN_IN_CRED_ONLY = 0x20,
            CREDUIWIN_ENUMERATE_ADMINS = 0x100,
            CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200,
            CREDUIWIN_SECURE_PROMPT = 0x1000,
            CREDUIWIN_PREPROMPTING = 0x2000,
            // 0x40000
            CREDUIWIN_PACK_32_WOW = 0x10000000,
            // 0x80000000
        }

        [DllImport(ExternDll.Credui, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint CredUIParseUserNameW(string userName, StringBuilder user, int userBufferSize, StringBuilder domain, int domainBufferSize);

        [DllImport(ExternDll.Credui, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredPackAuthenticationBuffer(int dwFlags, IntPtr pszUserName, IntPtr pszPassword, IntPtr pPackedCredentials, ref int pcbPackedCredentials);

        [DllImport(ExternDll.Credui, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool CredUnPackAuthenticationBufferW(int dwFlags, IntPtr pAuthBuffer, int cbAuthBuffer, StringBuilder pszUserName, ref int pcchMaxUserName, StringBuilder pszDomainName, ref int pcchMaxDomainame, StringBuilder pszPassword, ref int pcchMaxPassword);

        [DllImport(ExternDll.Credui, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int CredUIPromptForWindowsCredentialsW(ref CREDUI_INFO pUiInfo, int dwAuthError, ref int pulAuthPackage, IntPtr pvInAuthBuffer, int ulInAuthBufferSize, out IntPtr ppvOutAuthBuffer, out int pulOutAuthBufferSize, ref bool pfSave, CREDUIWIN dwFlags);
    }
}
