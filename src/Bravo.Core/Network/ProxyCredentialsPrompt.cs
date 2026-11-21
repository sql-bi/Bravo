using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Core.Network;

/// <summary>
/// Asks the user for the credentials of the proxy server and keeps what the user enters: a proxy server that
/// asks for credentials is answered with them from its next challenge.
/// </summary>
public sealed class ProxyCredentialsPrompt(IProxyCredentialDialog dialog, IProxyCredentialStore store)
{
    private readonly IProxyCredentialDialog _dialog = dialog;
    private readonly IProxyCredentialStore _store = store;

    /// <summary>
    /// Shows the dialog with the user name that is kept, if any, so that the user sees whose credentials the
    /// application has.
    /// </summary>
    /// <returns>
    /// Whether the user entered credentials. When not, the ones that were kept stay as they were.
    /// </returns>
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var kept = _store.Read();
        var userName = kept is null ? null : CredentialUserName.FromCredential(kept);

        var entered = await _dialog.ShowAsync(userName, cancellationToken).ConfigureAwait(false);
        if (entered is null)
        {
            return false;
        }

        _store.Write(entered);

        return true;
    }
}
