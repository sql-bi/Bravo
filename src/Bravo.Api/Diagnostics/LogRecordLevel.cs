namespace Bravo.Api.Diagnostics;

/// <summary>
/// How serious what a record tells is, from the least to the most.
/// </summary>
public enum LogRecordLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
}
