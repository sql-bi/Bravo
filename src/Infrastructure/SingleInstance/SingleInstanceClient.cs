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
    /// The connection is opened with <see cref="PipeOptions.CurrentUserOnly"/>, which makes the
    /// client verify that the pipe is owned by the current user before writing to it.
    /// </remarks>
    public static SingleInstanceSendResult Send(SingleInstanceOptions options, byte[] payload)
    {
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
            pipeClient.Flush();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            return SingleInstanceSendResult.Failed(ex);
        }

        return SingleInstanceSendResult.Delivered();
    }
}
