using System;
using Sqlbi.Bravo.Infrastructure.Messages;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Provides data for the <see cref="IInstanceActivationEvents.ActivationRequested"/> event.
/// </summary>
internal class InstanceActivationRequestedEventArgs(AppInstanceStartupMessage? startupMessage) : EventArgs
{
    /// <summary>
    /// Gets the decoded startup message, or <see langword="null"/> when the payload could not be read.
    /// </summary>
    public AppInstanceStartupMessage? StartupMessage { get; } = startupMessage;
}
