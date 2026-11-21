using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Core.Network;

/// <summary>
/// Asks the user for the credentials of the proxy server.
/// </summary>
public interface IProxyCredentialDialog
{
    /// <summary>
    /// Shows the dialog, and returns when the user has closed it. The dialog opens with a user name in it, as
    /// <see cref="CredentialUserName"/> writes it, or with none when the user name is <see langword="null"/>.
    /// </summary>
    /// <returns>
    /// What the user entered, or <see langword="null"/> when the user entered nothing: the dialog was
    /// cancelled, or the application has no window to show it in.
    /// </returns>
    Task<NetworkCredential?> ShowAsync(string? userName, CancellationToken cancellationToken);
}
