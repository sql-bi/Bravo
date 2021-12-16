using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.Windows;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Infrastructure.Configuration.Settings;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Sqlbi.Bravo.Infrastructure
{
    internal class AppInstance : IDisposable
    {
        private readonly EventWaitHandle _instanceEventWait;
        private readonly Mutex _instanceMutex;
        private readonly bool _instanceOwned;

        private PhotinoWindowSubclass? _photinoWindowSubclass;
        private bool _disposed;

        public AppInstance()
        {
            _instanceEventWait = new EventWaitHandle(initialState: false, mode: EventResetMode.AutoReset, name: $"{ nameof(Bravo) }|{ nameof(EventWaitHandle) }");
            _instanceMutex = new Mutex(initiallyOwned: true, name: $"{ nameof(Bravo) }|{ nameof(Mutex) }", out _instanceOwned);

            GC.KeepAlive(_instanceMutex);

            if (!IsOwned) NotifyOwner();
        }

        public bool IsOwned
        {
            get
            {
                //if (!_instanceOwned)
                    //_instanceEventWait.Set();

                return _instanceOwned;
            }
        }

        internal void TryHook(object? sender)
        {
            if (sender is PhotinoNET.PhotinoWindow window)
            {
                _photinoWindowSubclass = new PhotinoWindowSubclass(window);
            }
        }

        private void NotifyOwner()
        {
            var startupSettings = StartupSettings.Get();

            var message = new AppInstanceStartedMessage
            {
                ConnectionName = startupSettings.ParentProcessMainWindowTitle,
                ServerName = startupSettings.ArgumentServerName,
                DatabaseName = startupSettings.ArgumentDatabaseName,
            };

            var hWnd = NativeMethods.FindWindow(lpClassName: null, lpWindowName: AppConstants.ApplicationMainWindowTitle);
            if (hWnd != IntPtr.Zero)
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.Unicode.GetBytes(json);

                NativeMethods.COPYDATASTRUCT copyData;
                copyData.dwData = (IntPtr)100;
                copyData.lpData = json;
                copyData.cbData = bytes.Length + 1;

                _ = NativeMethods.SendMessage(hWnd, NativeMethods.WM_COPYDATA, wParam: 0, ref copyData);
            }
        }

        public AppInstanceStartedMessage? ReceiveConnectionFromSecondaryInstance(IntPtr ptr)
        {


            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_instanceOwned)
                        _instanceMutex.ReleaseMutex();

                    _instanceMutex.Dispose();
                    _instanceEventWait.Dispose();
                }

                _disposed = true;
            }
        }

        ~AppInstance()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class PhotinoWindowSubclass : WindowSubclass
    {
        private readonly PhotinoNET.PhotinoWindow _window;
        private readonly IntPtr MSG_HANDLED = new(1);

        public PhotinoWindowSubclass(PhotinoNET.PhotinoWindow window)
            : base(hWnd: window.WindowHandle)
        {
            _window = window;
        }

        protected override IntPtr WndProcHooked(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
        {
            switch (uMsg)
            {
                case NativeMethods.WM_COPYDATA:
                    {
                        // Restore original size and position only if the window is minimized, otherwise keep current position
                        if (_window.Minimized)
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);

                        // Regardless of current status, bring to front and activate the window
                        NativeMethods.SetForegroundWindow(hWnd);

                        var copyDataObject = Marshal.PtrToStructure(ptr: lParam, typeof(NativeMethods.COPYDATASTRUCT));
                        if (copyDataObject != null)
                        {
                            var copyData = (NativeMethods.COPYDATASTRUCT)copyDataObject;
                            if (copyData.cbData != 0)
                            {
                                // TODO: incomplete (WIP)
                                //var message = JsonSerializer.Deserialize<AppInstanceStartedMessage>(json: copyData.lpData);

                                System.Diagnostics.Trace.WriteLine($"::Bravo:INF:WndProcHooked[WM_COPYDATA]:{ copyData.lpData }");

                                //_window.SendNotification("Native message received [WM_COPYDATA]", copyData.lpData);
                                _window.OpenAlertWindow("Native message received [WM_COPYDATA]", copyData.lpData);
                            }
                        }

                        return MSG_HANDLED;
                    }
                default:
                    break;
            }

            return base.WndProcHooked(hWnd, uMsg, wParam, lParam, id, data);
        }
    }
}
