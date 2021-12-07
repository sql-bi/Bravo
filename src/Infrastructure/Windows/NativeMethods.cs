using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Sqlbi.Bravo.Infrastructure.Windows
{
    internal static class NativeMethods
    {
        private class ExternDll
        {
            public const string User32 = "user32.dll";
            public const string Gdi32 = "gdi32.dll";
            public const string Iphlpapi = "iphlpapi.dll";
        }

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
        public const int SPI_GETWORKAREA = 48;
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YXVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYXVIRTUALSCREEN = 79;
        public const int SM_CMONITORS = 80;

        public const uint WM_GETTEXT = 0x000D;
        public const int WM_COPYDATA = 0x004A;

        public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);
        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        public static readonly HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        [DllImport(ExternDll.User32)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MONITORINFOEX info);

        [DllImport(ExternDll.User32, ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        public static extern bool EnumDisplayMonitors(HandleRef hdc, COMRECT rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport(ExternDll.User32, ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        public static extern IntPtr MonitorFromWindow(HandleRef handle, int flags);

        [DllImport(ExternDll.User32, ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        public static extern IntPtr MonitorFromPoint(POINTSTRUCT pt, int flags);

        [DllImport(ExternDll.User32, ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        public static extern IntPtr MonitorFromRect(ref RECT rect, int flags);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern bool SystemParametersInfo(int nAction, int nParam, ref RECT rc, int nUpdate);

        [DllImport(ExternDll.Iphlpapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, uint reserved = 0);

        [DllImport(ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public static RECT FromXYWH(int x, int y, int width, int height) => new RECT(x, y, x + width, y + height);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTSTRUCT
        {
            public int X;
            public int Y;

            public POINTSTRUCT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public class MONITORINFOEX
        {
            internal int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            internal RECT rcMonitor = new RECT();
            internal RECT rcWork = new RECT();
            internal int dwFlags = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            internal char[] szDevice = new char[32];
        }

        [StructLayout(LayoutKind.Sequential)]
        public class COMRECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;
            public uint remoteAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] remotePort;
            public uint owningPid;

            public IPEndPoint LocalEndPoint => new IPEndPoint(localAddress, port: BitConverter.ToUInt16(new byte[2] { localPort[1], localPort[0] }, 0));

            public IPEndPoint RemoteEndPoint => new IPEndPoint(remoteAddress, port: BitConverter.ToUInt16(new byte[2] { remotePort[1], remotePort[0] }, 0));

            public TcpState TcpState => (state > 0 && state < 13) ? (TcpState)state : TcpState.Unknown;

            public int ProcessId => (int)owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPTABLE_OWNER_PID 
        {
            public uint dwNumEntries;
            MIB_TCPROW_OWNER_PID table;
        }
    }
}
