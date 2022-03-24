namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class Kernel32
    {
        public delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern int GetCurrentProcessId();

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern int GetCurrentThreadId();

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);
    }
}
