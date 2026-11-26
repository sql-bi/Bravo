using System;

namespace Bravo.Core.Telemetry;

/// <summary>
/// Sends an exception that the application did not foresee to the telemetry of Bravo.
/// </summary>
public interface IExceptionTelemetry
{
    /// <summary>
    /// Sends the exception, when the build has a destination and a policy or the user allows it; otherwise it
    /// does nothing. It never throws.
    /// </summary>
    void Track(Exception exception);
}
