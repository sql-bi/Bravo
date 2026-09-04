using System;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Configuration shared by <see cref="SingleInstanceServer"/> and <see cref="SingleInstanceClient"/>.
/// </summary>
/// <remarks>
/// Both sides must use the same <see cref="PipeName"/>.
/// </remarks>
internal sealed record SingleInstanceOptions
{
    /// <summary>
    /// Name of the named pipe that identifies the application instance.
    /// </summary>
    /// <remarks>
    /// Everything sharing this name is the same application instance, so it must be stable across
    /// releases and unique per user and per session.
    /// </remarks>
    public required string PipeName { get; init; }

    /// <summary>
    /// How long a secondary instance waits for the owner to accept its connection.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the owner waits for a connected client to finish sending its payload.
    /// </summary>
    /// <remarks>
    /// A client that connects and never finishes would otherwise hold the pipe's only server
    /// instance, leaving the owner unreachable while it stays alive.
    /// </remarks>
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Upper bound, in bytes, for a single activation payload.
    /// </summary>
    /// <remarks>
    /// A larger payload is rejected and reported through <see cref="SingleInstanceServer.Error"/>.
    /// </remarks>
    public int MaxPayloadBytes { get; init; } = 64 * 1024;
}
