using System;
using System.Security.Cryptography;
using System.Text;

namespace Bravo.Setup.Telemetry;

/// <summary>
/// The identifier that telemetry counts a user by: a hash of the names of the machine and of the account,
/// computed as 1.0.x computes it, so that an installation of this version counts as the same user.
/// </summary>
internal static class TelemetryUserId
{
    public static string Create(string machineName, string userName)
    {
        ArgumentNullException.ThrowIfNull(machineName);
        ArgumentNullException.ThrowIfNull(userName);

        var account = Encoding.UTF8.GetBytes($@"{machineName}\{userName}");
        return Convert.ToHexStringLower(SHA256.HashData(account));
    }
}
