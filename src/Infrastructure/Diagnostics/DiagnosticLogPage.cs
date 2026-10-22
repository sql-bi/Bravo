using System.Collections.Generic;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// The records added after a sequence number, read by the diagnostics pane.
/// </summary>
/// <param name="LastSequence">The sequence number to pass to the next read.</param>
/// <param name="DroppedCount">Records added after the requested sequence that the read does not return: evicted, or beyond its maximum count.</param>
public sealed record DiagnosticLogPage(IReadOnlyList<DiagnosticLogEntry> Entries, long LastSequence, long DroppedCount);
