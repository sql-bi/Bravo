namespace Sqlbi.Bravo.Infrastructure
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Messages;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using System.Text.Json;
    using System.Threading;

    internal class AppInstance : IDisposable
    {
        private readonly bool _owned;
        private readonly Mutex _mutex;
        private readonly string _pipeName;
        private readonly string _mutexName;

        private NamedPipeServerStream? _pipeServer;
        private bool _disposed;

        public AppInstance()
        {
            var appId = "8D4D9F1D39F94C7789D84729480D8198"; // Do not change !!
            var appName = AppEnvironment.DeploymentMode == AppDeploymentMode.Packaged ? AppEnvironment.ApplicationStoreAliasName : AppEnvironment.ApplicationName;
            var pipeNamePrefix = AppEnvironment.DeploymentMode == AppDeploymentMode.Packaged ? "LOCAL\\" : string.Empty; // Named pipes in packaged applications must use the syntax \\.\pipe\LOCAL\ for the pipe name

            _pipeName = $"{pipeNamePrefix}{appName}.{appId}";
            _mutexName = $"{appName}{appId}";
            _mutex = new Mutex(initiallyOwned: true, name: _mutexName, createdNew: out _owned);

            if (_owned)
            {
                StartPipeServer();
                GC.KeepAlive(_mutex);
            }
        }

        /// <summary>
        /// Determines if the current instance is the only running instance of the application or if another instance is already running
        /// </summary>
        /// <returns>true if the current instance is the only running instance of the application; otherwise, false</returns>
        public bool IsOwned => _owned;

        /// <summary>
        /// Occurs when a new (secondary) instance of the application is started and the notification is sent to the primary (owner) instance
        /// </summary>
        public event EventHandler<AppInstanceStartupEventArgs>? OnNewInstance;

        /// <summary>
        /// Sends a message to the primary instance owner notifying it of startup arguments for the current instance
        /// </summary>
        public void NotifyOwner()
        {
            using var pipeClient = new NamedPipeClientStream(serverName: ".", _pipeName, PipeDirection.InOut);
            try
            {
                pipeClient.Connect(timeout: 5_000);
            }
            catch (Exception ex) when (ex is IOException || ex is TimeoutException)
            {
                ExceptionHelper.WriteToEventLog(ex, EventLogEntryType.Warning);
                TelemetryHelper.TrackException(ex);
                return;
            }

            using var currentIdentity = WindowsIdentity.GetCurrent();
            var remotePipeSecurity = pipeClient.GetAccessControl();
            var remoteOwner = remotePipeSecurity.GetOwner(typeof(SecurityIdentifier));
            if (remoteOwner != currentIdentity.User)
            {
                throw new UnauthorizedAccessException("Bravo could not connect to the pipe because it was not owned by the current user.");
            }

            var startupSettings = StartupSettings.CreateFromCommandLineArguments();
            var startupMessage = AppInstanceStartupMessage.CreateFrom(startupSettings);
            var json = JsonSerializer.Serialize(startupMessage);
            var bytes = Encoding.Unicode.GetBytes(json).AsSpan();

            try
            {
                pipeClient.Write(bytes);
                pipeClient.Flush();
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is IOException)
            {
                ExceptionHelper.WriteToEventLog(ex, EventLogEntryType.Warning);
                TelemetryHelper.TrackException(ex);
                return;
            }
        }

        private void StartPipeServer(PipeSecurity? pipeSecurity = null)
        {
            if (pipeSecurity is null)
            {
                using var currentIdentity = WindowsIdentity.GetCurrent();
                BravoUnexpectedException.ThrowIfNull(currentIdentity.User);
                var currentUser = currentIdentity.User;

                // In order to restrict access to just this account we do not use PipeOptions.CurrentUserOnly but we use a custom ACL - see details here https://github.com/sql-bi/Bravo/issues/459
                // We set the PipeAccessRule identity specifically here and on the pipe client side they will check the owner against this one - they must have identical SIDs or the client will reject this server.
                var pipeAccessRule = new PipeAccessRule(currentUser, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);

                pipeSecurity = new PipeSecurity();
                pipeSecurity.AddAccessRule(pipeAccessRule);

                // Here we set the current user (WindowsIdentity.User) as owner for the security descriptor instead of using the token owner (WindowsIdentity.Owner) as PipeOptions.CurrentUserOnly would do.
                // This allows the user to connect even with different elevation levels.
                pipeSecurity.SetOwner(currentUser);
            }

            _pipeServer?.Dispose();
            _pipeServer = NamedPipeServerStreamAcl.Create(_pipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.None, inBufferSize: 0, outBufferSize: 0, pipeSecurity);
            _pipeServer.BeginWaitForConnection(OnPipeConnection, state: _pipeServer);
        }

        private void OnPipeConnection(IAsyncResult asyncResult)
        {
            BravoUnexpectedException.ThrowIfNull(asyncResult.AsyncState);

            var pipeServer = (NamedPipeServerStream)asyncResult.AsyncState;
            pipeServer.EndWaitForConnection(asyncResult);

            using var reader = new StreamReader(pipeServer, Encoding.Unicode);
            var json = reader.ReadToEnd();

            var startupMessage = default(AppInstanceStartupMessage?);
            try
            {
                startupMessage = JsonSerializer.Deserialize<AppInstanceStartupMessage>(json);
            }
            catch (JsonException)
            {
                // TODO: log JsonException ?
            }

            OnNewInstance?.Invoke(this, new AppInstanceStartupEventArgs(startupMessage));

            var pipeSecurity = pipeServer.GetAccessControl();
            StartPipeServer(pipeSecurity);
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_owned)
                        _mutex.ReleaseMutex();

                    _pipeServer?.Dispose();
                    _mutex.Dispose();
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

    internal class AppInstanceStartupEventArgs : EventArgs
    {
        public AppInstanceStartupEventArgs(AppInstanceStartupMessage? message)
        {
            Message = message;
        }

        public AppInstanceStartupMessage? Message { get; }
    }
}
