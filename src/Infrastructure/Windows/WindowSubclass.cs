namespace Sqlbi.Bravo.Infrastructure.Windows
{
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Runtime.ConstrainedExecution;

    /// <summary>
    /// Installs a window subclass callback to hook messages sent to the specified window
    /// </summary>
    internal abstract class WindowSubclass : CriticalFinalizerObject, IDisposable
    {
        private readonly Comctl32.SUBCLASSPROC _subclassProc;
        private readonly IntPtr _hWnd;
        private readonly object _lockSync = new();
        private bool _subclassInstalled;

        public WindowSubclass(IntPtr hWnd)
        {
            _hWnd = hWnd;

            _subclassProc = SubclassProc;
            _subclassInstalled = Comctl32.SetWindowSubclass(hWnd, _subclassProc, IntPtr.Zero, IntPtr.Zero);
        }

        private IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            try
            {
                return WndProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
            }
            finally
            {
                if (uMsg == (uint)WindowMessage.WM_NCDESTROY)
                {
                    DetachSubclass();
                }
            }
        }

        private void DetachSubclass()
        {
            if (_subclassInstalled)
            {
                lock (_lockSync)
                {
                    if (_subclassInstalled)
                    {
                        _ = Comctl32.RemoveWindowSubclass(_hWnd, _subclassProc, IntPtr.Zero);
                        _subclassInstalled = false;
                    }
                }
            }
        }

        protected virtual IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData) => Comctl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);

        #region IDisposable

        public void Dispose()
        {
            DetachSubclass();
        }

        #endregion
    }
}
