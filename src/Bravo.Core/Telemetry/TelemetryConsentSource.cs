namespace Bravo.Core.Telemetry;

/// <summary>
/// Who decided whether the application sends its telemetry.
/// </summary>
public enum TelemetryConsentSource
{
    /// <summary>
    /// A policy of an administrator, which applies over the option of the user.
    /// </summary>
    Policy,

    /// <summary>
    /// The option of the user, since no policy is set.
    /// </summary>
    Option,
}
