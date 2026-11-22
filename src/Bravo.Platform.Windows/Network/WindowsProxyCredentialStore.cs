using System;
using System.ComponentModel;
using System.Net;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using Bravo.Platform.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.Network;

/// <summary>
/// Keeps the credentials of the proxy server as one generic credential of the Windows Credential Manager: the
/// user name as text and the password as its UTF-16 characters, as 1.0.x keeps them.
/// </summary>
public sealed partial class WindowsProxyCredentialStore(string targetName, ILogger<WindowsProxyCredentialStore> logger)
    : IProxyCredentialStore
{
    private readonly string _targetName = targetName;
    private readonly ILogger<WindowsProxyCredentialStore> _logger = logger;

    public NetworkCredential? Read()
    {
        (string UserName, string Secret)? stored;
        try
        {
            stored = CredentialStoreInterop.ReadGeneric(_targetName);
        }
        catch (Win32Exception exception)
        {
            // Asked while a request answers a proxy server: there, credentials that cannot be read are none.
            LogReadFailed(exception, _targetName);
            return null;
        }

        if (stored is not { } credential)
        {
            return null;
        }

        return CredentialUserName.ToCredential(credential.UserName, credential.Secret);
    }

    public void Write(NetworkCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        try
        {
            CredentialStoreInterop.WriteGeneric(
                _targetName, CredentialUserName.FromCredential(credential), credential.Password);
        }
        catch (Win32Exception exception)
        {
            throw new BravoException(
                ErrorCode.ProxyCredentialsStoreFailed, "Windows did not keep the proxy credentials.", exception);
        }
    }

    public void Delete()
    {
        try
        {
            CredentialStoreInterop.DeleteGeneric(_targetName);
        }
        catch (Win32Exception exception)
        {
            throw new BravoException(
                ErrorCode.ProxyCredentialsStoreFailed, "Windows did not forget the proxy credentials.", exception);
        }
    }
}
