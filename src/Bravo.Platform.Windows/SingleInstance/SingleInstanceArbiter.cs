using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// Decides what a starting process is: the owner of the instance, or a second process that hands its request
/// over to the owner. A process becomes the owner only by acquiring the ownership, never because the owner
/// could not be reached.
/// </summary>
public sealed class SingleInstanceArbiter
{
    private readonly InstanceNames _names;
    private readonly SingleInstanceOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly HandOverClient _client;

    public SingleInstanceArbiter(InstanceNames names, SingleInstanceOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _names = names;
        _options = options;
        _timeProvider = timeProvider;
        _client = new HandOverClient(names, options, timeProvider);
    }

    /// <summary>
    /// Acquires the ownership or delivers <paramref name="payload"/> to the owner. While the owner may be
    /// starting, busy or ending it tries again, for at most <see cref="SingleInstanceOptions.ArbitrationTimeout"/>.
    /// </summary>
    /// <remarks>
    /// Blocks the calling thread, which is the one that disposes the ownership.
    /// </remarks>
    public ArbitrationResult Arbitrate(ReadOnlyMemory<byte> payload)
    {
        var start = _timeProvider.GetTimestamp();
        var wait = TimeSpan.Zero;

        while (true)
        {
            InstanceOwnership? ownership;
            try
            {
                ownership = InstanceOwnership.TryAcquire(_names, wait);
            }
            catch (Exception exception) when (IsAccessDenied(exception))
            {
                return ArbitrationResult.Failed(HandOverFailure.AccessDenied);
            }

            if (ownership is not null)
            {
                return ArbitrationResult.Owner(ownership);
            }

            var result = Send(payload);
            if (result.IsAccepted)
            {
                return ArbitrationResult.HandedOver();
            }

            if (!result.CanRetry)
            {
                return ArbitrationResult.Failed(ToFailure(result));
            }

            if (_timeProvider.GetElapsedTime(start) >= _options.ArbitrationTimeout)
            {
                return ArbitrationResult.Failed(HandOverFailure.NotResponding);
            }

            // The pause before the next attempt is a wait on the mutex, which an owner that ends releases. An
            // owner that ends during an attempt is noticed when the attempt gives up: see the connect timeout.
            wait = _options.RetryInterval;
        }
    }

    private static bool IsAccessDenied(Exception exception)
    {
        return exception is UnauthorizedAccessException or WaitHandleCannotBeOpenedException;
    }

    // On the thread pool: the calling thread may have a synchronization context, and nothing resumes on it.
    private HandOverResult Send(ReadOnlyMemory<byte> payload)
    {
        return Task.Run(() => _client.SendAsync(payload)).GetAwaiter().GetResult();
    }

    private static HandOverFailure ToFailure(HandOverResult result)
    {
        if (result.Status == HandOverStatus.AccessDenied)
        {
            return HandOverFailure.AccessDenied;
        }

        return HandOverFailure.Refused;
    }
}
