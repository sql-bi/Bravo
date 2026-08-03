using System;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Outcome of an attempt to notify the owning instance.
/// </summary>
internal enum SingleInstanceSendStatus
{
    /// <summary>
    /// The payload was written to the owner.
    /// </summary>
    Delivered = 0,

    /// <summary>
    /// No owner accepted the connection within the configured timeout. Either the owner is gone, or
    /// it is alive but not listening — which the caller may want to surface rather than ignore.
    /// </summary>
    OwnerUnavailable = 1,

    /// <summary>
    /// The connection succeeded but the payload could not be written.
    /// </summary>
    Failed = 2,
}

/// <summary>
/// Result of a <see cref="SingleInstanceClient"/> send. Failing to reach the owner is an expected
/// runtime condition, not an exceptional one, so it is returned rather than thrown.
/// </summary>
internal readonly record struct SingleInstanceSendResult(SingleInstanceSendStatus Status, Exception? Exception)
{
    public bool IsDelivered => Status == SingleInstanceSendStatus.Delivered;

    public static SingleInstanceSendResult Delivered()
        => new(SingleInstanceSendStatus.Delivered, Exception: null);

    public static SingleInstanceSendResult OwnerUnavailable(Exception exception)
        => new(SingleInstanceSendStatus.OwnerUnavailable, exception);

    public static SingleInstanceSendResult Failed(Exception exception)
        => new(SingleInstanceSendStatus.Failed, exception);
}
