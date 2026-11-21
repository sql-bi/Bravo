using System;
using System.Net;

namespace Bravo.Core.Network;

/// <summary>
/// The user name of credentials as one text, the way a user types it and Windows keeps it:
/// <c>domain\user</c> is a user of a domain, any other text is the user name as it is, such as a name with
/// <c>@</c>.
/// </summary>
public static class CredentialUserName
{
    private const char DomainSeparator = '\\';

    /// <summary>
    /// The credentials of a user name as text and of a password.
    /// </summary>
    public static NetworkCredential ToCredential(string userName, string password)
    {
        ArgumentNullException.ThrowIfNull(userName);

        var parts = userName.Split(DomainSeparator);
        if (parts.Length != 2)
        {
            return new NetworkCredential(userName, password);
        }

        return new NetworkCredential(parts[1], password, parts[0]);
    }

    /// <summary>
    /// The user name of credentials as text, which <see cref="ToCredential"/> reads back as the same user.
    /// </summary>
    public static string FromCredential(NetworkCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (string.IsNullOrEmpty(credential.Domain))
        {
            return credential.UserName;
        }

        return $"{credential.Domain}{DomainSeparator}{credential.UserName}";
    }
}
