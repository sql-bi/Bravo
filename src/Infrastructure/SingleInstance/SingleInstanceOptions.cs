using System;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Configuration shared by the two sides of the single-instance protocol: the owner
/// (<see cref="SingleInstanceServer"/>) and any secondary instance (<see cref="SingleInstanceClient"/>).
/// Both must be configured with the same <see cref="PipeName"/>.
/// </summary>
internal sealed record SingleInstanceOptions
{
    /// <summary>
    /// Name of the named pipe that both identifies the application and arbitrates ownership of the
    /// running instance. It must be stable across releases, and unique per user and per session:
    /// everything sharing this name is considered the same application instance.
    /// </summary>
    public required string PipeName { get; init; }

    /// <summary>
    /// How long a secondary instance waits for the owner to accept its connection before giving up.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the owner waits, once a client has connected, for that client to finish sending its
    /// payload. The counterpart of <see cref="ConnectTimeout"/> on the owner's side: a legitimate
    /// client writes its whole payload in one call right after connecting, so this bounds how long a
    /// stalled or malfunctioning client can hold the pipe's only server instance, which would
    /// otherwise leave the owner unreachable to every later instance while it stays alive.
    /// </summary>
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Upper bound, in bytes, for a single activation payload. The limit is checked after each read,
    /// so a rejected payload is buffered up to one read block past it and never more, keeping a
    /// malfunctioning or hostile client from growing the owner's memory. The rejection is reported
    /// through <see cref="SingleInstanceServer.Error"/>.
    /// </summary>
    public int MaxPayloadBytes { get; init; } = 64 * 1024;
}
