using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Binds the generic single-instance component to Bravo: it owns the instance name, the wire format
/// of the activation message and the reporting of failures. The component underneath knows none of
/// these things.
/// </summary>
internal sealed class BravoApplicationInstance : IDisposable
{
    private readonly SingleInstanceOptions _options;
    private readonly SingleInstanceServer? _server;

    private bool _disposed;

    // Not a primary constructor: it has to stay private (Create is the only entry point) and it
    // subscribes to the component's events, which a primary constructor cannot do.
    private BravoApplicationInstance(SingleInstanceOptions options, SingleInstanceServer? server)
    {
        _options = options;
        _server = server;

        if (_server is not null)
        {
            _server.Activated += OnActivated;
            _server.Error += OnError;
        }
    }

    /// <summary>
    /// Determines whether this process is the primary instance — the one that runs the application.
    /// When it is not, another instance is already running and <see cref="RequestActivation"/>
    /// should be called before exiting.
    /// </summary>
    public bool IsPrimary => _server is not null;

    /// <summary>
    /// Occurs on the primary instance when another instance asks it to come forward, carrying that
    /// instance's startup arguments. Raised on a thread pool thread: handlers that touch the UI must
    /// marshal. Nothing has been activated yet when this fires — honouring the request is up to the
    /// subscribers.
    /// </summary>
    /// <remarks>
    /// Not buffered. Requests that arrive before the subscribers exist are dropped. The pipe accepts
    /// connections before the UI is ready to process them, so early requests can be lost even though
    /// the secondary instance is told the payload was delivered. Buffering and replaying these requests
    /// was considered but rejected because the added complexity is not justified by the short startup
    /// window.
    /// </remarks>
    public event EventHandler<ActivationRequestedEventArgs>? ActivationRequested;

    /// <summary>
    /// Takes the role of primary instance if no other process holds it. The returned object is
    /// valid either way: inspect <see cref="IsPrimary"/> to know which role this process got.
    /// </summary>
    public static BravoApplicationInstance Create()
    {
        var options = new SingleInstanceOptions
        {
            PipeName = InstancePipeName.Create(),
        };

        _ = SingleInstanceServer.TryStart(options, out var server);

        return new BravoApplicationInstance(options, server);
    }

    /// <summary>
    /// Asks the primary instance to come forward, handing it the startup arguments of this process.
    /// Called by a secondary instance, which then exits.
    /// </summary>
    public void RequestActivation()
    {
        var startupSettings = StartupSettings.CreateFromCommandLineArguments();
        var startupMessage = AppInstanceStartupMessage.CreateFrom(startupSettings);
        var json = JsonSerializer.Serialize(startupMessage);
        var payload = Encoding.UTF8.GetBytes(json);

        var result = SingleInstanceClient.Send(_options, payload);
        if (!result.IsDelivered && result.Exception is not null)
        {
            // The primary instance cannot be reached (for example, an elevated "Run as administrator"
            // launch against a non-elevated instance), activation fails silently. This can happen because
            // PipeOptions.CurrentUserOnly compares WindowsIdentity.Owner, which differs when elevation changes
            // the identity to BUILTIN\Administrators. The failure is recorded in telemetry and the event log
            // (BravoApplicationInstance.RequestActivation), but no user-facing error is shown.

            // TODO: Consider surfacing activation failures to the user.
            Report(result.Exception);
        }
    }

    private void OnActivated(object? sender, SingleInstanceActivatedEventArgs e)
    {
        var startupMessage = default(AppInstanceStartupMessage?);
        try
        {
            var json = Encoding.UTF8.GetString(e.Payload);
            startupMessage = JsonSerializer.Deserialize<AppInstanceStartupMessage>(json);
        }
        catch (JsonException)
        {
            // An unreadable payload still activates the window, with no document to open.
        }

        // ACCEPTED LIMITATION — requests arriving while this instance is still starting up are
        // dropped here, on purpose. The pipe answers from the moment the process starts, but the
        // subscribers appear later: AppWindow attaches the bring-to-front handler in OnLoad, and the
        // one that forwards the startup message only in OnWebViewDOMContentLoaded, seconds later.
        // So a request that lands before the window exists is lost entirely, and one that lands
        // between the window and a loaded WebView brings Bravo to the front but does not open what
        // was asked for. The secondary instance is told the payload was delivered either way, so
        // nothing records the loss.
        // TODO: Consider buffering the requests and replaying them once the UI is ready.
        ActivationRequested?.Invoke(this, new ActivationRequestedEventArgs(startupMessage));
    }

    private void OnError(object? sender, SingleInstanceErrorEventArgs e) => Report(e.Exception);

    private static void Report(Exception exception)
    {
        ExceptionHelper.WriteToEventLog(exception, EventLogEntryType.Warning);
        TelemetryService.Instance.TrackException(exception);
    }

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
