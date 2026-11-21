using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Network;
using Bravo.Desktop.Interop;
using Bravo.Desktop.Shell;

namespace Bravo.Desktop.Network;

/// <summary>
/// Asks for the credentials of the proxy server with the dialog box of Windows for credentials, over the main
/// window.
/// </summary>
internal sealed class ProxyCredentialDialog(MainWindowDispatcher dispatcher) : IProxyCredentialDialog
{
    // The texts of 1.0.x, in English as the dialog box of Windows shows them in any language of the page.
    private const string Caption = "Enter proxy credentials";
    private const string Message = "Enter credentials to authenticate to the HTTP Proxy";

    private readonly MainWindowDispatcher _dispatcher = dispatcher;

    public Task<NetworkCredential?> ShowAsync(string? userName, CancellationToken cancellationToken)
    {
        return _dispatcher.RunAsync(windowHandle => Prompt(windowHandle, userName), cancellationToken);
    }

    private static NetworkCredential? Prompt(nint windowHandle, string? userName)
    {
        var entered = CredentialDialogInterop.Prompt(windowHandle, Caption, Message, userName);

        // An entry without a user name answers no proxy server: it is no entry.
        if (entered is not { UserName.Length: > 0 } credentials)
        {
            return null;
        }

        return CredentialUserName.ToCredential(credentials.UserName, credentials.Password);
    }
}
