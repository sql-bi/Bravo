using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.Core.Windows;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class ApplicationInstanceService : IApplicationInstanceService, IDisposable
    {
        private class ConnectionInfoMessage
        {
            [JsonPropertyName("d")]
            public string DatabaseName { get; set; }

            [JsonPropertyName("s")]
            public string ServerName { get; set; }

            [JsonPropertyName("p")]
            public string ParentProcessName { get; set; }

            [JsonPropertyName("t")]
            public string ParentProcessMainWindowTitle { get; set; }
        }

        private readonly IGlobalSettingsProviderService _settings;
        private readonly ILogger _logger;
        private readonly EventWaitHandle _instanceEventWait;
        private readonly Mutex _instanceMutex;
        private readonly bool _instanceOwned;
        private bool _disposed;

        public ApplicationInstanceService(IGlobalSettingsProviderService settings, ILogger<ApplicationInstanceService> logger)
        {
            _settings = settings;
            _logger = logger;

            _logger.Trace();
            _instanceEventWait = new EventWaitHandle(initialState: false, mode: EventResetMode.AutoReset, name: $"{ nameof(Sqlbi.Bravo) }|{ nameof(EventWaitHandle) }");
            _instanceMutex = new Mutex(initiallyOwned: true, name: $"{ nameof(Sqlbi.Bravo) }|{ nameof(Mutex) }", out _instanceOwned);

            GC.KeepAlive(_instanceMutex);
        }

        public void NotifyConnectionToPrimaryInstance()
        {
            _logger.Trace();

            var message = new ConnectionInfoMessage
            {
                DatabaseName = _settings.Runtime.DatabaseName,
                ServerName = _settings.Runtime.ServerName,
                ParentProcessName = _settings.Runtime.ParentProcessName,
                ParentProcessMainWindowTitle = _settings.Runtime.ParentProcessMainWindowTitle
            };

            var hWnd = NativeMethods.FindWindow(lpClassName: null, lpWindowName: AppConstants.ApplicationNameLabel);
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

        public RuntimeSummary ReceiveConnectionFromSecondaryInstance(IntPtr ptr)
        {
            var copyData = (NativeMethods.COPYDATASTRUCT)Marshal.PtrToStructure(ptr, typeof(NativeMethods.COPYDATASTRUCT));
            if (copyData.cbData == 0)
                return null;

            var json = copyData.lpData;
            var message = JsonSerializer.Deserialize<ConnectionInfoMessage>(json);

            var runtimeSummary = new RuntimeSummary
            {
                IsExecutedAsExternalTool = true,
                ServerName = message.ServerName,
                DatabaseName = message.DatabaseName,
                ParentProcessName = message.ParentProcessName,
                ParentProcessMainWindowTitle = message.ParentProcessMainWindowTitle,
            };

            return runtimeSummary;
        }

        public void RegisterCallbackForMultipleInstanceStarted(Action<IntPtr> callback)
        {
            _logger.Trace();

            _ = Task.Factory.StartNew(() =>
            {
                while (_instanceEventWait.WaitOne())
                    callback?.Invoke(_settings.Runtime.ParentProcessMainWindowHandle);
            },
            TaskCreationOptions.LongRunning).ContinueWith((t) =>
            {
                if (t.Exception != null)
                {
                    _logger.Error(LogEvents.ApplicationInstanceServiceException, t.Exception);
                }
            },
            TaskContinuationOptions.OnlyOnFaulted).ConfigureAwait(false);
        }

        public bool IsCurrentInstanceOwned
        {
            get
            {
                _logger.Trace();

                if (!_instanceOwned)
                    _instanceEventWait.Set();

                return _instanceOwned;
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

        ~ApplicationInstanceService()
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