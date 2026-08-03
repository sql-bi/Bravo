using System;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Carries the raw payload sent by a secondary instance. The component is deliberately unaware of
/// how the payload is encoded: interpreting it is the composition layer's responsibility.
/// </summary>
internal sealed class SingleInstanceActivatedEventArgs(byte[] payload) : EventArgs
{
    /// <summary>
    /// The bytes received from the secondary instance. Never empty.
    /// </summary>
    public byte[] Payload { get; } = payload;
}
