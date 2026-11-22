using System.Collections.Generic;

namespace Bravo.Api.Diagnostics;

/// <summary>
/// The answer to a request for the records of the log after a number.
/// </summary>
public sealed record LogBatch
{
    /// <summary>
    /// The records after the number of the request, oldest first. When the first number is not the one that
    /// follows the number of the request, the records between the two are no longer kept.
    /// </summary>
    public required IReadOnlyList<LogRecord> Records { get; init; }

    /// <summary>
    /// The number to send with the next request: the one of the last record of the host.
    /// </summary>
    public required int Next { get; init; }
}
