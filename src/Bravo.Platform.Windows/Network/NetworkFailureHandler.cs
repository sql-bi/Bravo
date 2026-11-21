using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Network;

namespace Bravo.Platform.Windows.Network;

/// <summary>
/// Makes a request that does not get the answer of its server fail with the error of its cause, for every
/// client that sends through it.
/// </summary>
public sealed class NetworkFailureHandler : DelegatingHandler
{
    /// <exception cref="BravoException">
    /// With one of the codes of <see cref="NetworkFailure"/>: the request did not reach its server.
    /// </exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var destination = GetDestination(request);
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            return RequireNoProxyChallenge(response, destination);
        }
        catch (HttpRequestException exception) when (NetworkFailure.TryCreate(exception, destination) is { } failure)
        {
            throw failure;
        }
    }

    /// <summary>
    /// The same for a client that sends without a task: no way of sending goes past the codes.
    /// </summary>
    /// <inheritdoc cref="SendAsync"/>
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var destination = GetDestination(request);
        try
        {
            return RequireNoProxyChallenge(base.Send(request, cancellationToken), destination);
        }
        catch (HttpRequestException exception) when (NetworkFailure.TryCreate(exception, destination) is { } failure)
        {
            throw failure;
        }
    }

    private static Uri GetDestination(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.RequestUri ?? throw new ArgumentException("The request has no address.", nameof(request));
    }

    // Only a proxy server answers 407: on a destination without TLS its answer comes as a response.
    private static HttpResponseMessage RequireNoProxyChallenge(HttpResponseMessage response, Uri destination)
    {
        if (response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired)
        {
            return response;
        }

        response.Dispose();
        throw NetworkFailure.ProxyAuthenticationRequired(destination);
    }
}
