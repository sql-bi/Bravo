using System;
using System.Net;
using System.Net.Http;
using Bravo.Core.Errors;

namespace Bravo.Core.Network;

/// <summary>
/// Why a request did not get the answer of its server, as one of the errors that the user interface tells
/// apart. Every connection of the application reports these causes with the same codes.
/// </summary>
public static class NetworkFailure
{
    /// <summary>
    /// The error for an exception of an HTTP request, or <see langword="null"/> when the exception tells none
    /// of the causes: the caller then goes on with the exception.
    /// </summary>
    /// <param name="exception">What the request failed with.</param>
    /// <param name="destination">The address that was asked for: the error names its host.</param>
    public static BravoException? TryCreate(HttpRequestException exception, Uri destination)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(destination);

        var host = destination.Host;

        return exception.HttpRequestError switch
        {
            HttpRequestError.ProxyTunnelError when exception.StatusCode == HttpStatusCode.ProxyAuthenticationRequired
                => ProxyAuthenticationRequired(destination, exception),
            HttpRequestError.ProxyTunnelError
                => new BravoException(ErrorCode.ProxyConnectionRefused, $"The proxy server refused the connection to {host}.", exception),
            HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError
                => new BravoException(ErrorCode.ServerUnreachable, $"The server {host} cannot be reached.", exception),
            HttpRequestError.SecureConnectionError
                => new BravoException(ErrorCode.SecureConnectionFailed, $"The secure connection to {host} could not be established.", exception),
            _ => null,
        };
    }

    /// <summary>
    /// The error of a proxy server that answered 407: it asks for credentials.
    /// </summary>
    /// <param name="destination">The address that was asked for.</param>
    /// <param name="innerException">What the request failed with, when it failed with an exception.</param>
    public static BravoException ProxyAuthenticationRequired(Uri destination, Exception? innerException = null)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var message = $"The proxy server asks for credentials to reach {destination.Host}.";

        return new BravoException(ErrorCode.ProxyAuthenticationRequired, message, innerException);
    }

    /// <summary>
    /// The error of a server that did not answer within the time that its client waits.
    /// </summary>
    /// <param name="destination">The address that was asked for.</param>
    /// <param name="innerException">The cancellation that the client ended the request with.</param>
    public static BravoException TimedOut(Uri destination, Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(innerException);

        return new BravoException(ErrorCode.ServerUnreachable, $"The server {destination.Host} did not answer in time.", innerException);
    }
}
