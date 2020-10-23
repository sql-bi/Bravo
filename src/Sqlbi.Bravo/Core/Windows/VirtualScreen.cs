using System.Drawing;

namespace Sqlbi.Bravo.Core.Windows
{
    /// <summary>
    /// https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/SystemInformation.cs
    /// </summary>
    public static class VirtualScreen
    {
        private static readonly bool MultiMonitorSupport = NativeMethods.GetSystemMetrics(NativeMethods.SM_CMONITORS) != 0;

        public static Rectangle Bounds
        {
            get
            {
                if (MultiMonitorSupport)
                {
                    var x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
                    var y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YXVIRTUALSCREEN);
                    var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
                    var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYXVIRTUALSCREEN);
                    return new Rectangle(x, y, width, height);
                }
                else
                {
                    var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                    var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
                    return new Rectangle(0, 0, width, height);
                }
            }
        }

        public static Rectangle WorkingArea
        {
            get
            {
                var rc = new NativeMethods.RECT();
                NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref rc, 0);
                var width = rc.Right - rc.Left;
                var height = rc.Bottom - rc.Top;
                return new Rectangle(rc.Left, rc.Top, width, height);
            }
        }
    }
}
