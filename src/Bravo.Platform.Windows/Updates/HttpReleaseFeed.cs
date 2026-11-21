using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using Bravo.Core.Updates;

namespace Bravo.Platform.Windows.Updates;

/// <summary>
/// Reads the update feed over HTTP, with the client and the proxy that the host gives every request.
/// </summary>
public sealed class HttpReleaseFeed(HttpClient httpClient, TimeProvider timeProvider) : IReleaseFeed
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PublishedRelease> ReadAsync(Uri address, CancellationToken cancellationToken)
    {
        string text;
        try
        {
            // Decoded as the headers say or, without a charset, by the byte order mark: the feed of today is
            // UTF-16 with one.
            text = await _httpClient.GetStringAsync(AddCacheBuster(address), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            // A charset that names no encoding is an InvalidOperationException of the content.
            throw new BravoException(ErrorCode.UpdateCheckFailed, "The update feed cannot be read.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw NetworkFailure.TimedOut(address, exception);
        }

        return ReleaseFeedDocument.Parse(text);
    }

    // Every earlier version adds it, so that no cache between the application and the storage answers with
    // a feed that a release has replaced.
    private Uri AddCacheBuster(Uri address)
    {
        var seconds = _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        return new UriBuilder(address) { Query = $"nocache={seconds}" }.Uri;
    }
}
