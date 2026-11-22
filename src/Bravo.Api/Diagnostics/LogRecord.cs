using System;

namespace Bravo.Api.Diagnostics;

/// <summary>
/// What the host recorded, of its own work or of a library that it uses.
/// </summary>
public sealed record LogRecord
{
    /// <summary>
    /// The number of the record, in the order that the host wrote them: the first of each process is 1.
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// When the host wrote the record, in UTC.
    /// </summary>
    public required DateTimeOffset Time { get; init; }

    /// <summary>
    /// How serious what the record tells is.
    /// </summary>
    public required LogRecordLevel Level { get; init; }

    /// <summary>
    /// What wrote the record: the full name of a type of the host or of a library.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// What happened, as the number that the category gives it, or 0.
    /// </summary>
    public required int EventId { get; init; }

    /// <summary>
    /// What happened, as the name that the category gives it.
    /// </summary>
    public string? EventName { get; init; }

    /// <summary>
    /// What happened, in English.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The trace of the work that the record belongs to, as 32 hexadecimal digits: for a request, the middle part
    /// of the traceId of its error answer.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// The exception that the record tells of, with its causes and where each one was thrown.
    /// </summary>
    public string? Exception { get; init; }
}
