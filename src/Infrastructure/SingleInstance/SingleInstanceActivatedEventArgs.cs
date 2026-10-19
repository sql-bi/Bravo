using System;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Carries the payload sent by a secondary instance.
/// </summary>
/// <remarks>
/// The payload is not interpreted by the server. It is never empty.
/// </remarks>
internal sealed class SingleInstanceActivatedEventArgs(byte[] payload) : EventArgs
{
    public byte[] Payload { get; } = payload;
}
