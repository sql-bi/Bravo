using System;
using System.IO;
using System.IO.Pipes;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Secondary-instance side of the single-instance protocol: hands the activation payload to the
/// process that owns the application instance.
/// </summary>
internal static class SingleInstanceClient
{
    /// <summary>
    /// Sends <paramref name="payload"/> to the owning instance.
    /// </summary>
    /// <remarks>
    /// <see cref="PipeOptions.CurrentUserOnly"/> makes the client verify that the pipe is owned by
    /// the current user before writing to it. The write completes when the owner has read the
    /// payload, so a delivered payload has reached the owner; an owner that is alive but not
    /// reading blocks the caller, with no timeout.
    /// </remarks>
    public static SingleInstanceSendResult Send(SingleInstanceOptions options, byte[] payload)
    {
        ArgumentOutOfRangeException.ThrowIfZero(payload.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(payload.Length, options.MaxPayloadBytes);

        using var pipeClient = new NamedPipeClientStream(
            serverName: ".",
            options.PipeName,
            PipeDirection.Out,
            PipeOptions.CurrentUserOnly);

        try
        {
            pipeClient.Connect(options.ConnectTimeout);
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return SingleInstanceSendResult.OwnerUnavailable(ex);
        }

        try
        {
            pipeClient.Write(payload, offset: 0, payload.Length);
        }
        catch (IOException ex)
        {
            return SingleInstanceSendResult.Failed(ex);
        }

        return SingleInstanceSendResult.Delivered();
    }
}
