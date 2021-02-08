using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class ApplicationInstanceService : IApplicationInstanceService, IDisposable
    {
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
                    _logger.Error(LogEvents.ApplicationInstanceServiceMultipleInstanceTaskException, t.Exception);
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
