using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Core.Windows
{
    /// <summary>
    /// https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/Screen.cs
    /// </summary>
    public class Screen
    {
        private static readonly bool MultiMonitorSupport = NativeMethods.GetSystemMetrics(NativeMethods.SM_CMONITORS) != 0;

        private const int PRIMARY_MONITOR = unchecked((int)0xBAADF00D);
        private const int MONITORINFOF_PRIMARY = 0x00000001;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        private readonly IntPtr _hmonitor;

        private Screen(IntPtr monitor)
        {
            if (!MultiMonitorSupport || monitor == (IntPtr)PRIMARY_MONITOR)
            {
                Bounds = VirtualScreen.Bounds;
                Primary = true;
                DeviceName = "DISPLAY";
            }
            else
            {
                var info = new NativeMethods.MONITORINFOEX();
                NativeMethods.GetMonitorInfo(new HandleRef(null, monitor), info);
                Bounds = Rectangle.FromLTRB(info.rcMonitor.Left, info.rcMonitor.Top, info.rcMonitor.Right, info.rcMonitor.Bottom);
                Primary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;
                DeviceName = new string(info.szDevice).TrimEnd((char)0);
            }

            _hmonitor = monitor;
        }

        public static IEnumerable<Screen> AllScreens
        {
            get
            {
                if (MultiMonitorSupport)
                {
                    var closure = new MonitorEnumCallback();
                    var proc = new NativeMethods.MonitorEnumProc(closure.Callback);
                    NativeMethods.EnumDisplayMonitors(NativeMethods.NullHandleRef, null, proc, IntPtr.Zero);

                    if (closure.Screens.Count > 0)
                        return closure.Screens.Cast<Screen>();
                }

                return new[] { PrimaryScreen };
            }
        }
        public static Screen PrimaryScreen
        {
            get
            {
                if (MultiMonitorSupport)
                    return AllScreens.FirstOrDefault((s) => s.Primary);

                return new Screen((IntPtr)PRIMARY_MONITOR);
            }
        }

        public static Screen FromHandle(IntPtr hwnd)
        {
            if (MultiMonitorSupport)
            {
                var monitor = NativeMethods.MonitorFromWindow(new HandleRef(null, hwnd), MONITOR_DEFAULTTONEAREST);
                return new Screen(monitor);
            }

            return new Screen((IntPtr)PRIMARY_MONITOR);
        }

        public static Screen FromPoint(System.Drawing.Point point)
        {
            if (MultiMonitorSupport)
            {
                var pt = new NativeMethods.POINTSTRUCT(point.X, point.Y);
                var monitor = NativeMethods.MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
                return new Screen(monitor);
            }

            return new Screen((IntPtr)PRIMARY_MONITOR);
        }

        public static Screen FromRectangle(Rectangle rect)
        {
            if (MultiMonitorSupport)
            {
                var rc = NativeMethods.RECT.FromXYWH(rect.Left, rect.Top, rect.Width, rect.Height);
                var monitor = NativeMethods.MonitorFromRect(ref rc, MONITOR_DEFAULTTONEAREST);
                return new Screen(monitor);
            }

            return new Screen((IntPtr)PRIMARY_MONITOR);
        }

        public Rectangle Bounds { get; private set; }

        public string DeviceName { get; private set; }

        public bool Primary { get; private set; }

        public Rectangle WorkingArea
        {
            get
            {
                if (!MultiMonitorSupport || _hmonitor == (IntPtr)PRIMARY_MONITOR)
                    return VirtualScreen.WorkingArea;

                var info = new NativeMethods.MONITORINFOEX();
                NativeMethods.GetMonitorInfo(new HandleRef(null, _hmonitor), info);
                return Rectangle.FromLTRB(info.rcWork.Left, info.rcWork.Top, info.rcWork.Right, info.rcWork.Bottom);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Screen screen)
            {
                if (_hmonitor == screen._hmonitor)
                    return true;
            }

            return false;
        }

        public override int GetHashCode() => (int)_hmonitor;

        private class MonitorEnumCallback
        {
            public ArrayList Screens { get; private set; } = new ArrayList();

            public bool Callback(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lparam)
            {
                var screen = new Screen(monitor);
                Screens.Add(screen);
                return true;
            }
        }
    }
}
