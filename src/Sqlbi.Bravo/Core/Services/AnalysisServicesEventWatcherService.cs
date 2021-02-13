using Microsoft.AnalysisServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Sqlbi.Bravo.Core.Services
{
    internal class AnalysisServicesEventWatcherService : IAnalysisServicesEventWatcherService, IDisposable
    {
        private static readonly HashSet<TraceEventSubclass> WatcherSubclasses = new HashSet<TraceEventSubclass>
        {
            TraceEventSubclass.Create,
            TraceEventSubclass.TabularCreate,
            TraceEventSubclass.Alter,
            TraceEventSubclass.TabularAlter,
            TraceEventSubclass.Delete,
            TraceEventSubclass.TabularDelete,
            TraceEventSubclass.TabularRename,
            TraceEventSubclass.Batch,
            TraceEventSubclass.CommitTransaction
        };

        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly Server _server;
        private readonly AutoResetEvent _connectionManuallyChangedEvent = new AutoResetEvent(initialState: false);
        private readonly CancellationTokenRegistration _applicationStoppingRegistration;
        private Trace _trace;
        private bool _disposed;

        public event EventHandler<AnalysisServicesEventWatcherEventArgs> OnEvent;
        public event EventHandler<AnalysisServicesEventWatcherConnectionStateArgs> OnConnectionStateChanged;

        public AnalysisServicesEventWatcherService(ILogger<AnalysisServicesEventWatcherService> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;

            _logger.Trace();
            _server = new Server();
            _applicationStoppingRegistration = _lifetime.ApplicationStopping.Register(async () => await DisconnectAsync());

            StartConnectionStateMonitorTask();
        }

        private void StartConnectionStateMonitorTask()
        {
            _logger.Trace();

            _ = Task.Factory.StartNew(() =>
            {
                var waitHandles = new[]
                {
                    _lifetime.ApplicationStopping.WaitHandle,
                    _connectionManuallyChangedEvent
                };

                var previus = ConnectionState.Closed;
                do
                {
                    WaitHandle.WaitAny(waitHandles, AppConstants.AnalysisServicesEventWatcherServiceConnectionStateWaitDelay);
                    var current = _server.GetConnectionState(pingServer: true);

                    _logger.Debug(LogEvents.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus, "ConnectionStateMonitorTask(current<{CurrentStatus}>|previus<{PreviusStatus}>)", args: new object[] { current, previus });

                    if (current != previus)
                    {
                        var args = new AnalysisServicesEventWatcherConnectionStateArgs(previus, current);
                        OnConnectionStateChanged?.Invoke(this, args);
                        previus = current;
                    }
                }
                while (!_lifetime.ApplicationStopping.IsCancellationRequested);

                _logger.Debug(LogEvents.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted, "ConnectionStateMonitorTask(Completed)");
            },
            TaskCreationOptions.LongRunning).ContinueWith((t) =>
            {
                if (t.Exception != null)
                {
                    _logger.Error(LogEvents.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException, t.Exception);
                }
            },
            TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task ConnectAsync(RuntimeSummary runtimeSummary)
        {
            _logger.Trace();

            await Task.Run(Connect).ConfigureAwait(false);

            void Connect()
            {
                if (_server.Connected)
                    throw new InvalidOperationException("Server already connected");

                var connectionString = AnalysisServicesHelper.BuildConnectionString(runtimeSummary.ServerName, runtimeSummary.DatabaseName);

                _server.Connect(connectionString);
                _connectionManuallyChangedEvent.Set();
                _trace = CreateTrace();
                _trace.Start();
            }

            Trace CreateTrace()
            {
                var trace = _server.Traces.Add(AppConstants.ApplicationInstanceUniqueName);
                {
                    var traceEvent = trace.Events.Add(TraceEventClass.CommandEnd);
                    {
                        traceEvent.Columns.Add(TraceColumn.EventSubclass);
                        traceEvent.Columns.Add(TraceColumn.Success);
                        traceEvent.Columns.Add(TraceColumn.DatabaseName);
                        traceEvent.Columns.Add(TraceColumn.ApplicationName);
                        traceEvent.Columns.Add(TraceColumn.TextData);
                    }
                }

                trace.Filter = CreateFilter();
                trace.OnEvent += OnTraceEvent;
                trace.StopTime = DateTime.UtcNow.AddDays(1);
                trace.AutoRestart = false;
                //trace.Audit = false;
                trace.Update();

                return trace;
            }

            XmlNode CreateFilter()
            {
                var filter = "<And xmlns=\"http://schemas.microsoft.com/analysisservices/2003/engine\">" +
                                "<And>" +
                                    "<Equal>" +
                                        $"<ColumnID>{ (int)TraceColumn.Success }</ColumnID>" +
                                        $"<Value xsi:type=\"xsd:int\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">{ (int)TraceEventSuccess.Success }</Value>" +
                                    "</Equal>" +
                                    "<Like>" +
                                        $"<ColumnID>{ (int)TraceColumn.DatabaseName }</ColumnID>" +
                                        $"<Value>{ runtimeSummary.DatabaseName }</Value>" +
                                    "</Like>" +
                                "</And>" +
                                "<NotLike>" +
                                    $"<ColumnID>{ (int)TraceColumn.ApplicationName }</ColumnID>" +
                                    $"<Value>{ AppConstants.ApplicationInstanceUniqueName }</Value>" +
                                "</NotLike>" +
                            "</And>";

                var doc = new XmlDocument();
                doc.LoadXml(filter);

                return doc.FirstChild;
            }
        }

        public async Task DisconnectAsync()
        {
            _logger.Trace();

            await Task.Run(Disconnect).ConfigureAwait(false);

            void Disconnect()
            {
                if (_trace != null)
                {
                    if (_trace.IsStarted)
                        _trace.Stop();

                    if (_server.Connected && _server.GetConnectionState(pingServer: true) == ConnectionState.Open)
                        _trace.Drop();

                    _trace.Dispose();
                    _trace = null;
                }

                if (_server.Connected)
                {
                    _server.Disconnect(endSession: true);
                    _connectionManuallyChangedEvent.Set();
                }
            }
        }

        private void OnTraceEvent(object sender, TraceEventArgs e)
        {
            _logger.Trace("OnTraceEvent(eventClass<{EventClass}>|eventSubclass<{EventSubclass}>)", args: new object[] { e.EventClass, e.EventSubclass });

            if (!WatcherSubclasses.Contains(e.EventSubclass))
                return;

            var eventType = e.TextData.GetEventType();
            if (eventType == AnalysisServicesEventWatcherEvent.Unknown)
                return;

            _logger.Debug(LogEvents.AnalysisServicesEventWatcherServiceOnTraceEvent, "OnTraceEvent(eventType<{EventType}>)", args: eventType);

            var args = new AnalysisServicesEventWatcherEventArgs(eventType, text: string.Empty);
            OnEvent?.Invoke(this, args);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _applicationStoppingRegistration.Dispose();
                    _server.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
