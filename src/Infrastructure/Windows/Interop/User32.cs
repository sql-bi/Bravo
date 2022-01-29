namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class User32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }

        [Flags]
        public enum MODEKEY : uint
        {
            MOD_NONE = 0x0000,
            MOD_ALT = 0x0001,
            MOD_CONTROL = 0x0002,
            MOD_SHIFT = 0x0004,
            MOD_WIN = 0x0008,
            //MOD_NOREPEAT = 0x4000
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/shellscalingapi/ne-shellscalingapi-process_dpi_awareness
        /// </summary>
        public enum PROCESS_DPI_AWARENESS
        {
            PROCESS_DPI_UNAWARE = 0,
            PROCESS_SYSTEM_DPI_AWARE = 1,
            PROCESS_PER_MONITOR_DPI_AWARE = 2
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context
        /// </summary>
        public enum DPI_AWARENESS_CONTEXT
        {
            DPI_AWARENESS_CONTEXT_UNAWARE = -1,
            DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = -2,
            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = -3,
            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4
        }

        public const int SW_HIDE = 0;
        public const int SW_NORMAL = 1;
        public const int SW_SHOWNORMAL = SW_NORMAL;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = SW_MAXIMIZE;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;

        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, MODEKEY modifiers, System.Windows.Forms.Keys keys);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowMessage uMsg, int wParam, ref COPYDATASTRUCT lParam);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowMessage uMsg, int wParam, StringBuilder lParam);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowMessage uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        [DllImport(ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(ExternDll.User32)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport(ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT value);

        [DllImport(ExternDll.Shcore, SetLastError = true)]
        internal static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        [DllImport(ExternDll.User32, SetLastError = true)]
        private static extern bool EnableNonClientDpiScaling(IntPtr hWnd);

        [DllImport(ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
    }
}
