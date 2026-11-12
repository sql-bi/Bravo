using System;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// The limits of the arbitration and of the hand-over. The owner and a second process use the same values.
/// </summary>
public sealed record SingleInstanceOptions
{
    /// <summary>
    /// The largest request that the owner reads.
    /// </summary>
    public int MaxPayloadBytes { get; init; } = 1024 * 1024;

    /// <summary>
    /// How long one attempt waits for the pipe of the owner.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long a request and its answer may take once the two processes are connected.
    /// </summary>
    public TimeSpan ExchangeTimeout { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long a second process keeps trying before it gives up. Longer than the time the owner takes to
    /// shut down, so that a start that follows a close finds the instance free.
    /// </summary>
    public TimeSpan ArbitrationTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long a second process waits for the ownership between two attempts.
    /// </summary>
    public TimeSpan RetryInterval { get; init; } = TimeSpan.FromMilliseconds(100);
}
