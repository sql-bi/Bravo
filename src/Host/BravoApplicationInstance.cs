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
/// Defines the contract for a single-instance application.
/// </summary>
internal interface IBravoApplicationInstance : IInstanceActivationEvents, IDisposable
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

internal sealed class BravoApplicationInstance : IBravoApplicationInstance
{
    private readonly SingleInstanceOptions _options;
    private readonly SingleInstanceServer? _server;

    private bool _disposed;

    /// <summary>
    /// Creates a <see cref="BravoApplicationInstance"/>, claiming the primary role if no other process holds it.
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

    /// <inheritdoc/>
    public bool IsPrimary => _server is not null;

    /// <inheritdoc/>
    public event EventHandler<InstanceActivationRequestedEventArgs>? ActivationRequested;

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
        ActivationRequested?.Invoke(this, new InstanceActivationRequestedEventArgs(startupMessage));
    }

    private void OnError(object? sender, SingleInstanceErrorEventArgs e) => Report(e.Exception);

    /// <inheritdoc/>
    public void RedirectActivationToPrimary()
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
            // (BravoApplicationInstance.RedirectActivationToPrimary), but no user-facing error is shown.

            // TODO: Consider surfacing activation failures to the user.
            Report(result.Exception);
        }
    }

    private static void Report(Exception exception)
    {
        ExceptionHelper.WriteToEventLog(exception, EventLogEntryType.Warning);
        TelemetryService.Instance.TrackException(exception);
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
