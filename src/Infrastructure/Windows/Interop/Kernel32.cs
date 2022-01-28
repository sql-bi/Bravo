namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using System;
    using System.Runtime.InteropServices;

    internal static class Kernel32
    {
        public delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern int GetCurrentProcessId();

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern int GetCurrentThreadId();
    }
}
