using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Defines the contract for a single-instance application.
/// </summary>
internal interface IBravoApplicationInstance : IInstanceEvents, IDisposable
{
    /// <summary>
    /// Gets a value indicating whether this process is the primary instance, the one that runs the application.
    /// </summary>
    bool IsPrimary { get; }

    /// <summary>
    /// Redirects this process's activation to the primary instance, forwarding its startup arguments.
    /// </summary>
    void RedirectActivationToPrimary();
}

internal sealed partial class BravoApplicationInstance : IBravoApplicationInstance
{
    private readonly SingleInstanceOptions _options;
    private readonly SingleInstanceServer? _server;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger _logger;

    private bool _disposed;

    public BravoApplicationInstance(
        SingleInstanceOptions options,
        SingleInstanceServer? server,
        ITelemetryService telemetry,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _server = server;
        _telemetry = telemetry;
        _logger = loggerFactory.CreateLogger<BravoApplicationInstance>();

        if (_server is not null)
        {
            _server.Activated += OnActivated;
            _server.Error += OnError;
        }
    }

    /// <inheritdoc/>
    public bool IsPrimary => _server is not null;

    /// <inheritdoc/>
    public event EventHandler<InstanceActivationRequestedEventArgs>? ActivationRequested;

    private void OnActivated(object? sender, SingleInstanceActivatedEventArgs e)
    {
        var startupMessage = default(AppInstanceStartupMessage?);
        try
        {
            startupMessage = JsonSerializer.Deserialize<AppInstanceStartupMessage>(e.Payload);
        }
        catch (JsonException ex)
        {
            // An unreadable payload still activates the window, with no document to open.
            LogActivationPayloadUnreadable(ex);
            _telemetry.TrackException(ex);
        }

        // Known limitation: requests arriving while this instance is still starting up are dropped
        // here. The pipe answers from the moment the process starts, but the subscribers appear
        // later: AppWindow attaches the bring-to-front handler in OnLoad, and the one that forwards
        // the startup message only in OnWebViewDOMContentLoaded, seconds later. A request that lands
        // before the window exists is lost, and one that lands between the window and a loaded
        // WebView brings Bravo to the front but does not open what was asked for. The secondary
        // instance is told the payload was delivered either way.
        // TODO: Consider buffering the requests and replaying them once the UI is ready.
        ActivationRequested?.Invoke(this, new InstanceActivationRequestedEventArgs(startupMessage));
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        var exception = e.GetException();

        LogListenerFailed(exception);
        _telemetry.TrackException(exception);
    }

    /// <inheritdoc/>
    public void RedirectActivationToPrimary()
    {
        var startupSettings = StartupSettings.CreateFromCommandLineArguments();
        var startupMessage = AppInstanceStartupMessage.CreateFrom(startupSettings);
        var payload = JsonSerializer.SerializeToUtf8Bytes(startupMessage);

        var result = SingleInstanceClient.Send(_options, payload);
        if (!result.IsDelivered)
        {
            // The primary instance cannot be reached and activation fails silently. One known cause is
            // a launch whose elevation differs from the primary's: PipeOptions.CurrentUserOnly builds
            // and checks the pipe ACL on WindowsIdentity.Owner, which elevation changes to
            // BUILTIN\Administrators, while the pipe name is keyed on WindowsIdentity.User
            // (https://github.com/dotnet/runtime/issues/123903, open, milestone .NET 12).
            // TODO: Consider surfacing activation failures to the user.
            LogActivationRedirectFailed(result.Exception, result.Status);
            _telemetry.TrackException(result.Exception);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_server is not null)
        {
            _server.Activated -= OnActivated;
            _server.Error -= OnError;
            _server.Dispose();
        }
    }
}
