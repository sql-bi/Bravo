namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using System;
    using System.Runtime.InteropServices;

    internal static class Comctl32
    {
        public delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data);

        [DllImport(ExternDll.Comctl32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, ref IntPtr pdwRefData);

        [DllImport(ExternDll.Comctl32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC callback, IntPtr id, IntPtr data);

        [DllImport(ExternDll.Comctl32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC callback, IntPtr id);

        [DllImport(ExternDll.Comctl32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    }
}
