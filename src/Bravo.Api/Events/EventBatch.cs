using System.Collections.Generic;

namespace Bravo.Api.Events;

/// <summary>
/// The answer to a request for the events after a number.
/// </summary>
internal sealed record EventBatch
{
    /// <summary>
    /// The events after the number of the request, oldest first. Empty when none arrived within the limit.
    /// When the first number is not the one that follows the number of the request, the events between the
    /// two are no longer kept.
    /// </summary>
    public required IReadOnlyList<HostEvent> Events { get; init; }

    /// <summary>
    /// The number to send with the next request: the one of the last event of the host.
    /// </summary>
    public required int Next { get; init; }
}
