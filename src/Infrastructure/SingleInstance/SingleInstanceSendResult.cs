using System;
using System.Diagnostics.CodeAnalysis;

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
    /// No owner accepted the connection within the configured timeout, or the owner refused it.
    /// </summary>
    OwnerUnavailable = 1,

    /// <summary>
    /// The connection succeeded but the payload could not be written.
    /// </summary>
    Failed = 2,
}

/// <summary>
/// Result of a <see cref="SingleInstanceClient"/> send.
/// </summary>
/// <remarks>
/// Failing to reach the owner is an expected runtime condition, so it is returned rather than
/// thrown. A failed result always carries the exception that caused it.
/// </remarks>
internal readonly record struct SingleInstanceSendResult
{
    private SingleInstanceSendResult(SingleInstanceSendStatus status, Exception? exception)
    {
        Status = status;
        Exception = exception;
    }

    public SingleInstanceSendStatus Status { get; }

    public Exception? Exception { get; }

    [MemberNotNullWhen(false, nameof(Exception))]
    public bool IsDelivered => Status == SingleInstanceSendStatus.Delivered;

    public static SingleInstanceSendResult Delivered()
        => new(SingleInstanceSendStatus.Delivered, exception: null);

    public static SingleInstanceSendResult OwnerUnavailable(Exception exception)
        => new(SingleInstanceSendStatus.OwnerUnavailable, exception);

    public static SingleInstanceSendResult Failed(Exception exception)
        => new(SingleInstanceSendStatus.Failed, exception);
}
