using System;

namespace Sqlbi.Bravo.Host;

internal interface IInstanceEvents
{
    /// <summary>
    /// Occurs on the primary instance when another instance asks it to come forward.
    /// </summary>
    /// <remarks>
    /// Raised on a thread pool thread; handlers that touch the UI must marshal.
    /// </remarks>
    event EventHandler<InstanceActivationRequestedEventArgs>? ActivationRequested;
}
