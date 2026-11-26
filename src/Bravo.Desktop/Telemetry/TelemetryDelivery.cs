namespace Bravo.Desktop.Telemetry;

/// <summary>
/// What became of an item that was sent and waited for.
/// </summary>
internal enum TelemetryDelivery
{
    /// <summary>
    /// The endpoint took it.
    /// </summary>
    Sent,

    /// <summary>
    /// It waits in the store, for the next start that has the consent.
    /// </summary>
    Kept,

    /// <summary>
    /// It was neither sent nor kept.
    /// </summary>
    NotSent,
}
