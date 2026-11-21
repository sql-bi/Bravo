using System;
using System.Net;
using Bravo.Core.Settings;

namespace Bravo.Core.Network;

/// <summary>
/// What the application answers a proxy server that asks for credentials: the ones that the user entered, when
/// the user chose to use them, else the Windows user. It is asked at every challenge, so a choice or an entry
/// of the user holds from the next one.
/// </summary>
public sealed class ProxyCredentials(IUserSettingsStore settings, IProxyCredentialStore store) : ICredentials
{
    private readonly IUserSettingsStore _settings = settings;
    private readonly IProxyCredentialStore _store = store;

    public NetworkCredential? GetCredential(Uri uri, string authType)
    {
        // The option before the store: with the default settings nothing reads the kept credentials.
        if (!_settings.Current.Proxy.UseDefaultCredentials && _store.Read() is { } stored)
        {
            return stored;
        }

        // The Windows user answers only the schemes that never send a password.
        return IsIntegrated(authType) ? CredentialCache.DefaultNetworkCredentials : null;
    }

    private static bool IsIntegrated(string authType)
    {
        return string.Equals(authType, "Negotiate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(authType, "NTLM", StringComparison.OrdinalIgnoreCase);
    }
}
