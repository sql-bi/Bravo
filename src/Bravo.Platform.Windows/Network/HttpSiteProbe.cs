using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using Bravo.Core.Settings;

namespace Bravo.Platform.Windows.Network;

/// <summary>
/// Asks a site for an answer over HTTP, through the proxy of the settings as the user keeps them now, which
/// may not be the proxy of the run yet.
/// </summary>
public sealed class HttpSiteProbe(IUserSettingsStore settings, ProxyCredentials credentials) : ISiteProbe
{
    // A user waits for the test: a site that is this slow does not serve the application either.
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private readonly IUserSettingsStore _settings = settings;
    private readonly ProxyCredentials _credentials = credentials;

    public async Task<SiteProbeResult> ProbeAsync(Uri address, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);

        // A client of its own, and not one of the host: those go through the proxy that was read at the start.
        var selection = ProxySelection.For(_settings.Current.Proxy);
        using var client = new HttpClient(selection.CreateHandler(_credentials)) { Timeout = s_timeout };

        // On a thread of the pool: Windows may take seconds to find the proxy of a script.
        var proxy = await Task.Run(() => selection.FindProxy(address), cancellationToken).ConfigureAwait(false);
        var failure = await SendAsync(client, address, cancellationToken).ConfigureAwait(false);

        return new SiteProbeResult(proxy, failure);
    }

    // The answer of the site is not read: that it answered, with any status, is all that the test asks.
    private static async Task<ErrorCode?> SendAsync(HttpClient client, Uri address, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, address);
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            return null;
        }
        catch (BravoException exception)
        {
            return exception.Code;
        }
        catch (Exception exception) when (exception is HttpRequestException or NotSupportedException)
        {
            // A failure that tells no cause, or a proxy address of a kind that .NET does not send through.
            return ErrorCode.ServerUnreachable;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The time of the client ran out, and not the patience of the caller.
            return ErrorCode.ServerUnreachable;
        }
    }
}
