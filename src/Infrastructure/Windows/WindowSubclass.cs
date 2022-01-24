using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Windows
{
    /// <summary>
    /// Installs a window subclass callback to hook messages sent to the specified window
    /// </summary>
    /// <remarks>The window must be owned by the calling thread</remarks>
    internal abstract class WindowSubclass : CriticalFinalizerObject, IDisposable
    {
        private static readonly uint _disposeMsg;

        private readonly User32.SUBCLASSPROC _wndProc;
        private readonly IntPtr _wndProcPtr;
        private IntPtr _hWnd;

        static WindowSubclass()
        {
            _disposeMsg = User32.RegisterWindowMessage("Sqlbi.Bravo.WindowSubclass");
        }

        public WindowSubclass(IntPtr hWnd)
        {
            if (!IsOwnerThread(hWnd)) throw new InvalidOperationException("The current thread does not own the window");

            _hWnd = hWnd;
            _wndProc = WndProcStub;
            _wndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProc);

            User32.SetWindowSubclass(hWnd, _wndProc, IntPtr.Zero, IntPtr.Zero);
        }

        protected virtual IntPtr WndProcHooked(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data) => User32.DefSubclassProc(hWnd, uMsg, wParam, lParam);

        private IntPtr WndProcStub(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data) => WndProc(hwnd, msg, wParam, lParam, id, data);

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
        {
            var retVal = IntPtr.Zero;

            try
            {
                retVal = WndProcHooked(hWnd, msg, wParam, lParam, id, data);
            }
            finally
            {
                if (_hWnd != IntPtr.Zero)
                {
                    Debug.Assert(_hWnd == hWnd);

                    if (msg == 0x0082) // NCDESTROY = 0x0082,
                    {
                        Dispose();
                    }
                    else if (msg == _disposeMsg && wParam == _wndProcPtr)
                    {
                        TryDispose(disposing: lParam != IntPtr.Zero);
                    }
                }
            }

            return retVal;
        }

        private bool IsOwnerThread(IntPtr hWnd)
        {
            // Retrieves the identifier of the thread and the process that created the window
            var threadId = User32.GetWindowThreadProcessId(hWnd, out var processId);

            var currentProcessId = User32.GetCurrentProcessId();
            var currentThreadId = User32.GetCurrentThreadId();

            if (processId == currentProcessId && threadId == currentThreadId)
                return true;

            // The window was not created by the current thread
            return false;
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (_hWnd == IntPtr.Zero || !IsOwnerThread(_hWnd))
                throw new InvalidOperationException("Dispose virtual should only be called by WindowSubclass once on the correct thread.");

            User32.RemoveWindowSubclass(_hWnd, _wndProc, IntPtr.Zero);

            _hWnd = IntPtr.Zero;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            TryDispose(disposing: true);
        }

        ~WindowSubclass()
        {
            // The finalizer is always on the wrong thread
            TryDispose(disposing: false);
        }

        private void TryDispose(bool disposing)
        {
            var hWnd = _hWnd;

            if (hWnd != IntPtr.Zero)
            {
                if (IsOwnerThread(hWnd))
                {
                    Dispose(disposing);
                }
                else
                {
                    // Here we are on the wrong thread so we send the dispose message to the WndProc to remove for us itself on the correct thread (it is dangerous, we risk a deadlock)

                    var lParam = disposing ? new IntPtr(1) : IntPtr.Zero;
                    User32.SendMessage(hWnd, _disposeMsg, _wndProcPtr, lParam);
                }
            }
        }

        #endregion
    }
}
