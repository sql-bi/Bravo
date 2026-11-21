using System;
using System.ComponentModel;
using System.Net;
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
    private const char DomainSeparator = '\\';

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

        return ToNetworkCredential(credential.UserName, credential.Secret);
    }

    /// <exception cref="Win32Exception">Windows could not keep the credentials.</exception>
    public void Write(NetworkCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        CredentialStoreInterop.WriteGeneric(_targetName, ToStoredUserName(credential), credential.Password);
    }

    /// <exception cref="Win32Exception">Windows could not delete the credentials.</exception>
    public void Delete()
    {
        CredentialStoreInterop.DeleteGeneric(_targetName);
    }

    /// <summary>
    /// The credentials for a user name as it is kept: <c>domain\user</c> is a user of a domain, any other text
    /// is the user name as it is, such as a name with <c>@</c>.
    /// </summary>
    internal static NetworkCredential ToNetworkCredential(string storedUserName, string password)
    {
        var parts = storedUserName.Split(DomainSeparator);
        if (parts.Length != 2)
        {
            return new NetworkCredential(storedUserName, password);
        }

        return new NetworkCredential(parts[1], password, parts[0]);
    }

    internal static string ToStoredUserName(NetworkCredential credential)
    {
        if (string.IsNullOrEmpty(credential.Domain))
        {
            return credential.UserName;
        }

        return $"{credential.Domain}{DomainSeparator}{credential.UserName}";
    }
}
