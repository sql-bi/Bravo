using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// A log record kept for the diagnostics pane.
/// </summary>
/// <param name="Template">The message template, when the record has <paramref name="Properties"/>.</param>
/// <param name="Properties">The values of the template placeholders, as text.</param>
/// <param name="Exception">The exception and its inner exceptions, outermost first, depth first.</param>
public sealed record DiagnosticLogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    int EventId,
    string? EventName,
    string Message,
    string? Template,
    IReadOnlyDictionary<string, string?>? Properties,
    IReadOnlyList<DiagnosticLogException>? Exception);
