using System;
using System.Security.Principal;
using Sqlbi.Bravo.Infrastructure;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Builds the named-pipe name that identifies a running Bravo instance.
/// </summary>
/// <remarks>
/// <para>
/// One instance per Windows session and per account, across elevation levels and regardless of how
/// Bravo was installed: Portable, both MSI variants and the MSIX/Store package are the same
/// application to the user and must not run side by side.
/// </para>
/// </remarks>
internal static class InstancePipeName
{
    // Hardcoded constants (stable across releases)
    private const string ApplicationName = "Bravo";
    private const string ScopeId = "8D4D9F1D39F94C7789D84729480D8198";

    /// <summary>
    /// Builds the name for the current user and session.
    /// </summary>
    public static string Create()
    {
        using var identity = WindowsIdentity.GetCurrent();

        // User, not the token owner. A "run as" with a different user gets its own instance: that
        // account has its own %LOCALAPPDATA%, MSAL cache and HKCU, so nothing is shared. A "run as
        // administrator" must not: UAC elevation keeps the same account, so usersettings.json, the
        // WebView2 user data folder and .msalcache are the same files and need a single writer.
        // Elevation changes the owner to BUILTIN\Administrators, while User stays the same.
        var userSid = identity.User?.Value
            ?? throw new InvalidOperationException("The current Windows identity has no user SID.");

        return Create(AppEnvironment.SessionId, userSid);
    }

    /// <summary>
    /// Builds the name for the given session and account.
    /// </summary>
    /// <param name="sessionId">
    /// The Windows session. Every Terminal Services session has its own window, so it is a different instance.
    /// </param>
    /// <param name="userSid">
    /// The Windows account. Must be stable across UAC elevation; see <see cref="Create()"/>.
    /// </param>
    public static string Create(int sessionId, string userSid)
        => $"{ApplicationName}.{ScopeId}.{sessionId}.{userSid}";
}
