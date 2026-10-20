using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Turns the payloads received by the primary instance into activation requests.
/// </summary>
/// <remarks>
/// The server is owned by <c>Program.Main</c>, which claims it through <see cref="BravoSingleInstance"/>: disposing
/// this instance stops the events but keeps the pipe.
/// </remarks>
internal sealed partial class BravoApplicationInstance : IInstanceEvents, IDisposable
{
    private readonly SingleInstanceServer _server;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger _logger;

    public BravoApplicationInstance(
        SingleInstanceServer server,
        ITelemetryService telemetry,
        ILoggerFactory loggerFactory)
    {
        _server = server;
        _telemetry = telemetry;
        _logger = loggerFactory.CreateLogger<BravoApplicationInstance>();

        _server.Activated += OnActivated;
        _server.Error += OnError;
    }

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

        // Requests are not buffered: those that arrive before the subscribers exist are lost; see the activation
        // limits in docs/design/architecture.md.
        // TODO: Consider buffering the requests and replaying them once the UI is ready.
        ActivationRequested?.Invoke(this, new InstanceActivationRequestedEventArgs(startupMessage));
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        var exception = e.GetException();

        LogListenerFailed(exception);
        _telemetry.TrackException(exception);
    }

    public void Dispose()
    {
        _server.Activated -= OnActivated;
        _server.Error -= OnError;
    }
}
