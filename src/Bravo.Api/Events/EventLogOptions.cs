using System;

namespace Bravo.Api.Events;

internal sealed record EventLogOptions
{
    /// <summary>
    /// How many events are kept for a page that has not asked for them yet. Events are few: these are more
    /// than a page misses while it loads.
    /// </summary>
    public int Capacity { get; init; } = 64;

    /// <summary>
    /// How long a request waits for an event before it is answered with none. A page that waits asks again
    /// twice a minute, which also tells it that the host is still there.
    /// </summary>
    public TimeSpan WaitLimit { get; init; } = TimeSpan.FromSeconds(30);
}
