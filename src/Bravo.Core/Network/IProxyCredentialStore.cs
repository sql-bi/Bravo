using System.Net;

namespace Bravo.Core.Network;

/// <summary>
/// The credentials that the user entered for the proxy server, kept from one start of the application to the
/// next.
/// </summary>
public interface IProxyCredentialStore
{
    /// <summary>
    /// The credentials, or <see langword="null"/> when none are kept or they cannot be read.
    /// </summary>
    NetworkCredential? Read();

    /// <summary>
    /// Keeps the credentials in place of the ones before.
    /// </summary>
    /// <exception cref="Errors.BravoException">
    /// <see cref="Errors.ErrorCode.ProxyCredentialsStoreFailed"/>: they could not be kept.
    /// </exception>
    void Write(NetworkCredential credential);

    /// <summary>
    /// Forgets the credentials. With none kept it does nothing.
    /// </summary>
    /// <exception cref="Errors.BravoException">
    /// <see cref="Errors.ErrorCode.ProxyCredentialsStoreFailed"/>: they could not be forgotten.
    /// </exception>
    void Delete();
}
