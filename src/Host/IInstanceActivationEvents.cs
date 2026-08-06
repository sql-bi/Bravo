using System;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Defines the activation events this process instance raises for interested subscribers.
/// </summary>
internal interface IInstanceActivationEvents
{
    /// <summary>
    /// Occurs on the primary instance when another instance asks it to come forward.
    /// </summary>
    /// <remarks>
    /// Raised on a thread pool thread; handlers that touch the UI must marshal.
    /// </remarks>
    event EventHandler<InstanceActivationRequestedEventArgs>? ActivationRequested;
}
