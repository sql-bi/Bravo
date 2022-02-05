namespace Sqlbi.Bravo.Infrastructure.Windows
{
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;

    /// <summary>
    /// Installs a window subclass callback to hook messages sent to the specified window
    /// </summary>
    internal abstract class WindowSubclass
    {
        private readonly Comctl32.SUBCLASSPROC _subclassProc;
        private readonly IntPtr _hWnd;

        public WindowSubclass(IntPtr hWnd)
        {
            _hWnd = hWnd;

            _subclassProc = SubclassProc;
            _ = Comctl32.SetWindowSubclass(hWnd, _subclassProc, IntPtr.Zero, IntPtr.Zero);
        }

        private IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            return WndProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
        }

        protected virtual IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            if (uMsg == (uint)WindowMessage.WM_NCDESTROY)
            {
                // The subclass must be removed before the window being subclassed is destroyed
                // This is a permanent subclass so can call RemoveWindowSubclass inside the subclass procedure itself
                _ = Comctl32.RemoveWindowSubclass(_hWnd, _subclassProc, IntPtr.Zero);
            }

            return Comctl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }
    }
}
