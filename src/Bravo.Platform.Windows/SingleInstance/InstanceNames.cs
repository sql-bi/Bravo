using System;
using System.Diagnostics;
using System.Security.Principal;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// The names that make processes one instance: the processes that use the same names are the same instance.
/// </summary>
public sealed record InstanceNames(string MutexName, string PipeName)
{
    /// <summary>
    /// The names for the current user in the current Windows session, the same at every privilege level.
    /// </summary>
    /// <param name="applicationId">What tells this application from every other one.</param>
    public static InstanceNames ForCurrentUserSession(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        // The user of the token, which elevation does not change: an elevated start is the same instance.
        using var identity = WindowsIdentity.GetCurrent();
        var userSid = identity.User?.Value
            ?? throw new InvalidOperationException("The current Windows identity has no user.");

        using var process = Process.GetCurrentProcess();

        // The options of the mutex scope it to the session; pipes have one namespace for the whole machine.
        var mutexName = $"{applicationId}.{userSid}";
        var pipeName = $"{applicationId}.{process.SessionId}.{userSid}";
        return new InstanceNames(mutexName, pipeName);
    }
}
