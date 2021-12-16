using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Infrastructure.Configuration.Settings;
using System;
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

        private bool _disposed;

        public AppInstance()
        {
            _instanceEventWait = new EventWaitHandle(initialState: false, mode: EventResetMode.AutoReset, name: $"{ nameof(Bravo) }|{ nameof(EventWaitHandle) }");
            _instanceMutex = new Mutex(initiallyOwned: true, name: $"{ nameof(Bravo) }|{ nameof(Mutex) }", out _instanceOwned);

            GC.KeepAlive(_instanceMutex);
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

        public void NotifyToOwner()
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
}
