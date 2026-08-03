using System;
using Sqlbi.Bravo.Infrastructure.Messages;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Carries the startup arguments of the secondary instance that requested the activation.
/// </summary>
internal class ActivationRequestedEventArgs(AppInstanceStartupMessage? startupMessage) : EventArgs
{
    /// <summary>
    /// The decoded startup message, or <see langword="null"/> when the payload could not be read.
    /// The event is raised either way: the user asked for Bravo, so the window is brought to the
    /// foreground even when there is nothing to open.
    /// </summary>
    public AppInstanceStartupMessage? StartupMessage { get; } = startupMessage;
}
