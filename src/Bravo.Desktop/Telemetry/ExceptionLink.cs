using System.Collections.Generic;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// One exception of a chain, as the telemetry sends it: its texts lose what is personal or marked as
/// sensitive.
/// </summary>
/// <param name="Id">Its number in the chain, from 1.</param>
/// <param name="OuterId">The number of the exception that holds it, or 0 for the first one.</param>
/// <param name="TypeName">The full name of its type.</param>
/// <param name="Message">Its message.</param>
/// <param name="HasFullStack">False when frames in the middle of its stack were left out, for its length.</param>
/// <param name="Frames">Its stack, from where it was thrown.</param>
internal sealed record ExceptionLink(
    int Id,
    int OuterId,
    string TypeName,
    string Message,
    bool HasFullStack,
    IReadOnlyList<ExceptionFrame> Frames);
