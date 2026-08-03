using System;
using System.Security.Principal;
using Sqlbi.Bravo.Infrastructure;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Builds the named-pipe name that identifies a running Bravo instance.
/// </summary>
/// <remarks>
/// <para>
/// THE RULE: one instance per Windows session and per account, across elevation levels, and
/// regardless of how Bravo was installed — Portable, both MSI variants and the MSIX/Store package
/// are the same application to the user, and must not run side by side just because one of them
/// happens to come from a different install mechanism.
/// </para>
/// <para>
/// The format is a frozen contract: changing it makes a new build unable to see an instance started
/// by an older one. It is kept as a pure function so that it can be pinned by tests without touching
/// the registry, the process or the current Windows identity.
/// </para>
/// </remarks>
internal static class InstancePipeName
{
    /// <summary>
    /// Builds the name for the current user and session.
    /// </summary>
    public static string Create()
    {
        using var identity = WindowsIdentity.GetCurrent();

        // User, not the token's default object owner. A "run as" with a different user gets its own
        // instance — that account has its own %LOCALAPPDATA%, MSAL cache and HKCU, so nothing is
        // shared with it. A "run as administrator" must not get its own instance: UAC elevation keeps
        // the same Windows account, so usersettings.json, the WebView2 user data folder and
        // .msalcache are the same files, and they need a single writer. Owner would get this backwards
        // — elevation changes it to BUILTIN\Administrators, while User stays stable across elevation.
        var userSid = identity.User?.Value
            ?? throw new InvalidOperationException("The current Windows identity has no user SID.");

        return Create(AppEnvironment.SessionId, userSid);
    }

    /// <param name="sessionId">
    /// Identifies the Windows session. Every Terminal Services session needs its own window and its
    /// own Bravo instance, so a different session is always a different instance.
    /// </param>
    /// <param name="userSid">
    /// Identifies the Windows account and must remain stable across UAC elevation — see
    /// <see cref="Create()"/> for why this is <c>User</c> rather than the token owner.
    /// </param>
    public static string Create(int sessionId, string userSid)
    {
        // Both constants below are hardcoded; they are part of the frozen
        // contract that makes the instance name stable across releases.
        const string ApplicationName = "Bravo";
        const string ScopeId = "8D4D9F1D39F94C7789D84729480D8198";

        return $"{ApplicationName}.{ScopeId}.{sessionId}.{userSid}";
    }
}
