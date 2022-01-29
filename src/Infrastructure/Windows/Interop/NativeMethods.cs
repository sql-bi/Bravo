namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    internal static class NativeMethods
    {
        public const int NO_ERROR = 0;
        public const int ERROR_SUCCESS = NO_ERROR;
        public const int ERROR_INVALID_PARAMETER = 87;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int ERROR_NO_DATA = 232;
        public const int ERROR_INVALID_FLAGS = 1004;
        public const int ERROR_NOT_FOUND = 1168;
        public const int ERROR_CANCELLED = 1223;
        public const int ERROR_NO_SUCH_LOGON_SESSION = 1312;
        public const int ERROR_INVALID_ACCOUNT_NAME = 1315;

        //public const int SPI_GETWORKAREA = 48;
        //public const int SM_CXSCREEN = 0;
        //public const int SM_CYSCREEN = 1;
        //public const int SM_XVIRTUALSCREEN = 76;
        //public const int SM_YXVIRTUALSCREEN = 77;
        //public const int SM_CXVIRTUALSCREEN = 78;
        //public const int SM_CYXVIRTUALSCREEN = 79;
        //public const int SM_CMONITORS = 80;

        //public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        //public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

        //public static readonly HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        //[DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        //[ResourceExposure(ResourceScope.None)]
        //public static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MONITORINFOEX info);

        //[DllImport(ExternDll.User32, ExactSpelling = true)]
        //[ResourceExposure(ResourceScope.None)]
        //public static extern bool EnumDisplayMonitors(HandleRef hdc, COMRECT rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        //[DllImport(ExternDll.User32, ExactSpelling = true)]
        //[ResourceExposure(ResourceScope.None)]
        //public static extern IntPtr MonitorFromWindow(HandleRef handle, int flags);

        //[DllImport(ExternDll.User32, ExactSpelling = true)]
        //[ResourceExposure(ResourceScope.None)]
        //public static extern IntPtr MonitorFromPoint(POINTSTRUCT pt, int flags);

        //[DllImport(ExternDll.User32, ExactSpelling = true)]
        //[ResourceExposure(ResourceScope.None)]
        //public static extern IntPtr MonitorFromRect(ref RECT rect, int flags);

        //[DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        //[ResourceExposure(ResourceScope.None)]
        //public static extern int GetSystemMetrics(int nIndex);

        //[DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        //[ResourceExposure(ResourceScope.None)]
        //public static extern bool SystemParametersInfo(int nAction, int nParam, ref RECT rc, int nUpdate);

        //[StructLayout(LayoutKind.Sequential)]
        //public struct RECT
        //{
        //    public int Left;
        //    public int Top;
        //    public int Right;
        //    public int Bottom;

        //    public RECT(int left, int top, int right, int bottom)
        //    {
        //        Left = left;
        //        Top = top;
        //        Right = right;
        //        Bottom = bottom;
        //    }

        //    public static RECT FromXYWH(int x, int y, int width, int height)
        //    {
        //        return new RECT(x, y, x + width, y + height);
        //    }
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //public struct POINTSTRUCT
        //{
        //    public int X;
        //    public int Y;

        //    public POINTSTRUCT(int x, int y)
        //    {
        //        X = x;
        //        Y = y;
        //    }
        //}

        //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        //public class MONITORINFOEX
        //{
        //    internal int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
        //    internal RECT rcMonitor = new RECT();
        //    internal RECT rcWork = new RECT();
        //    internal int dwFlags = 0;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        //    internal char[] szDevice = new char[32];
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //public class COMRECT
        //{
        //    public int Left;
        //    public int Top;
        //    public int Right;
        //    public int Bottom;
        //}
    }
}
