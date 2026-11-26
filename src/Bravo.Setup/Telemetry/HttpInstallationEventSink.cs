using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Bravo.Setup.Telemetry;

/// <summary>
/// Posts an event to the ingestion of Application Insights, in one request, through the proxy of the system with the
/// Windows credentials of who installs. An installation does not wait for the network: the send returns after the
/// timeout at the latest, and an event that was not sent by then is lost.
/// </summary>
internal sealed class HttpInstallationEventSink : IInstallationEventSink
{
    /// <summary>
    /// How long an installation waits for the event to be sent.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly TelemetryDestination _destination;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _timeout;

    public HttpInstallationEventSink(TelemetryDestination destination, TimeProvider timeProvider, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _destination = destination;
        _timeProvider = timeProvider;
        _timeout = timeout;
    }

    public void Send(InstallationEvent installationEvent)
    {
        var item = InstallationEventEnvelope.Write(
            installationEvent, _destination.InstrumentationKey, _timeProvider.GetUtcNow(), Guid.NewGuid());

        // On a thread of the pool: a script of the proxy of the system runs before the request, and no timeout of
        // the request reaches it. The process ends after the wait, and a send that goes on with it.
        _ = Task.Run(() => PostAsync(item)).Wait(_timeout);
    }

    private async Task PostAsync(byte[] item)
    {
        try
        {
            using var handler = new SocketsHttpHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
            using var client = new HttpClient(handler) { Timeout = _timeout };
            using var content = new ByteArrayContent(item);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-json-stream");
            using var response = await client.PostAsync(_destination.TrackAddress, content).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Not sent, whatever failed, from the proxy to the authentication: the installation goes on, as with
            // 1.0.x.
        }
    }
}
