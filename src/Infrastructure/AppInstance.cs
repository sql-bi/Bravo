using Sqlbi.Bravo.Infrastructure.Helpers;
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
        private readonly Mutex _instanceMutex;
        private readonly bool _instanceOwned;

        private bool _disposed;

        public AppInstance()
        {
            var prefix = DesktopBridgeHelpers.IsPackagedAppInstance ? AppConstants.ApplicationStoreAliasName : AppConstants.ApplicationName;

            _instanceMutex = new Mutex(initiallyOwned: true, name: $"{ prefix }|4f9wB", out _instanceOwned);

            GC.KeepAlive(_instanceMutex);
        }

        /// <summary>
        ///  Determines if the current instance is the only running instance of the application or if another instance is already running
        /// </summary>
        /// <returns>true if the current instance is the only running instance of the application; otherwise, false</returns>
        public bool IsOwned => _instanceOwned;

        /// <summary>
        /// Sends a message to the primary instance owner notifying it of startup arguments for the current instance
        /// </summary>
        public void NotifyOwner()
        {
            var startupSettings = StartupSettings.Get();

            var message = new AppInstanceStartedMessage
            {
                ParentProcessId = startupSettings.ParentProcessId,
                ParentProcessName = startupSettings.ParentProcessName,
                ParentProcessMainWindowTitle = startupSettings.ParentProcessMainWindowTitle,
                ArgumentServerName = startupSettings.ArgumentServerName,
                ArgumentDatabaseName = startupSettings.ArgumentDatabaseName,
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

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_instanceOwned)
                        _instanceMutex.ReleaseMutex();

                    _instanceMutex.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
