using System;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// What the owner does with the request of a second process.
/// </summary>
public interface IHandOverReceiver
{
    /// <summary>
    /// Takes a request, or refuses it. The answer goes back to the process that sent it.
    /// </summary>
    HandOverReply Receive(ReadOnlyMemory<byte> payload);
}
