using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// The SDK of Application Insights, configured to send what it is given and nothing of its own. What it could
/// not send waits in the folder of the store, and goes with the next pipeline on that folder.
/// </summary>
internal sealed class TelemetryPipeline : ITelemetryPipeline
{
    private readonly TelemetryConfiguration _configuration;
    private readonly ServerTelemetryChannel _channel;
    private readonly TelemetryClient _client;

    // Set on a thread of the channel, once the endpoint accepted an item that this pipeline was given.
    private volatile bool _hasSent;

    private TelemetryPipeline(TelemetryConfiguration configuration, ServerTelemetryChannel channel)
    {
        _configuration = configuration;
        _channel = channel;
        _client = new TelemetryClient(configuration);

        // The channel tells it of what this pipeline gives it, not of what waited in the store.
        _channel.TransmissionStatusEvent = OnTransmissionStatus;
    }

    /// <param name="connectionString">The connection string of the resource.</param>
    /// <param name="storageFolder">The folder of the store, which the pipeline creates.</param>
    /// <exception cref="IOException">The folder of the store cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">The folder of the store cannot be created.</exception>
    public static TelemetryPipeline Create(string connectionString, string storageFolder)
    {
        // The channel keeps nothing in a folder that does not exist, and chooses no other.
        Directory.CreateDirectory(storageFolder);
        var channel = new ServerTelemetryChannel { StorageFolder = storageFolder };

        // Not CreateDefault, which adds the modules and the sampling of a server: this configuration sends the
        // items it is given, as they are.
        var configuration = new TelemetryConfiguration
        {
            ConnectionString = connectionString,
            TelemetryChannel = channel,
        };
        channel.Initialize(configuration);

        return new TelemetryPipeline(configuration, channel);
    }

    /// <summary>
    /// Deletes the folder of the store, with what waits there.
    /// </summary>
    /// <exception cref="IOException">A file of the folder is in use.</exception>
    /// <exception cref="UnauthorizedAccessException">The folder cannot be deleted.</exception>
    public static void DeleteStore(string storageFolder)
    {
        if (Directory.Exists(storageFolder))
        {
            Directory.Delete(storageFolder, recursive: true);
        }
    }

    public void Send(ExceptionTelemetry item)
    {
        _client.TrackException(item);

        // Out of the buffer of the channel at once, without waiting for the sending: an exception is often
        // followed by the end of the process.
        _client.Flush();
    }

    /// <summary>
    /// Passes the item to the channel and waits, for the time given at most, until it is sent or kept in the
    /// store.
    /// </summary>
    public async Task<TelemetryDelivery> SendAsync(ExceptionTelemetry item, TimeSpan timeout)
    {
        Send(item);
        var isKept = await FlushAsync(timeout, CancellationToken.None).ConfigureAwait(false);

        if (_hasSent)
        {
            return TelemetryDelivery.Sent;
        }

        return isKept ? TelemetryDelivery.Kept : TelemetryDelivery.NotSent;
    }

    public async Task<bool> FlushAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(timeout);
        try
        {
            return await _client.FlushAsync(cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _configuration.Dispose();

        // The configuration does not own a channel that it was given.
        _channel.Dispose();
    }

    // An item of a batch that the endpoint did not take has the status of the batch, 206 or worse.
    private void OnTransmissionStatus(object? sender, TransmissionStatusEventArgs e)
    {
        if (e.Response?.StatusCode == 200)
        {
            _hasSent = true;
        }
    }
}
