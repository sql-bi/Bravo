using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Owner side of the single-instance protocol: holds ownership of the application instance and
/// receives activation payloads from secondary instances.
/// </summary>
/// <remarks>
/// <para>
/// Ownership is arbitrated by the named pipe itself, which is created allowing a single server
/// instance: only one process at a time can hold the name. There is deliberately no separate mutex,
/// so there is no second piece of state that can disagree with the listener. If this process stops
/// listening for any reason, the name is released and the next process to start becomes the owner,
/// instead of every later instance failing to reach an owner that no longer answers.
/// </para>
/// <para>
/// The pipe is disconnected — not recreated — between connections: recreating it would release the
/// name for an instant, during which another process could claim ownership.
/// </para>
/// </remarks>
internal sealed class SingleInstanceServer : IDisposable
{
    private static readonly TimeSpan s_errorBackoff = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan s_shutdownTimeout = TimeSpan.FromSeconds(1);

    private readonly SingleInstanceOptions _options;
    private readonly NamedPipeServerStream _pipeServer;
    private readonly CancellationTokenSource _cancellation;

    private Task? _listener;
    private bool _disposed;

    private SingleInstanceServer(SingleInstanceOptions options, NamedPipeServerStream pipeServer)
    {
        _options = options;
        _pipeServer = pipeServer;
        _cancellation = new CancellationTokenSource();
    }

    /// <summary>
    /// Raised when a secondary instance sends an activation payload. Raised on a thread pool thread,
    /// never on the listener loop, so a subscriber is free to block: subscribers that need a specific
    /// thread — a UI thread, typically — marshal by themselves.
    /// </summary>
    public event EventHandler<SingleInstanceActivatedEventArgs>? Activated;

    /// <summary>
    /// Raised when a connection fails, a client does not finish sending within
    /// <see cref="SingleInstanceOptions.ReadTimeout"/>, or a subscriber of <see cref="Activated"/>
    /// throws. The loop keeps running: ownership is not given up because of a single bad connection.
    /// </summary>
    public event EventHandler<SingleInstanceErrorEventArgs>? Error;

    /// <summary>
    /// Attempts to take ownership of the application instance identified by
    /// <see cref="SingleInstanceOptions.PipeName"/> and, on success, starts listening.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if ownership was acquired by this process; <see langword="false"/> if
    /// another process already owns it, in which case the caller should notify the owner through
    /// <see cref="SingleInstanceClient"/> and exit.
    /// </returns>
    public static bool TryStart(SingleInstanceOptions options, [NotNullWhen(true)] out SingleInstanceServer? server)
    {
        NamedPipeServerStream pipeServer;
        try
        {
            pipeServer = new NamedPipeServerStream(
                options.PipeName,
                PipeDirection.In,
                maxNumberOfServerInstances: 1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The name is already taken: another process owns this application instance.
            server = null;
            return false;
        }

        var instance = new SingleInstanceServer(options, pipeServer);
        instance._listener = Task.Run(() => instance.ListenAsync(instance._cancellation.Token));

        server = instance;
        return true;
    }

    /// <summary>
    /// Serves one connection at a time until shutdown. A bad connection degrades this loop, it never
    /// ends it: giving up would release the pipe name and hand ownership to the next process.
    /// </summary>
    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var payload = await AcceptAndReadAsync(cancellationToken).ConfigureAwait(false);
                if (payload is not null)
                    Dispatch(payload);
            }
            catch (Exception ex) when (IsShutdown(ex, cancellationToken))
            {
                break;
            }
            catch (Exception ex)
            {
                RaiseError(ex);

                if (!await TryBackOffAsync(cancellationToken).ConfigureAwait(false))
                    break;
            }
        }
    }

    /// <summary>
    /// Tells a shutdown apart from a connection that went wrong. The two arrive as the same exception
    /// types, and only the caller's token says which happened: a cancelled read is the ReadTimeout
    /// firing, unless shutdown was requested.
    /// </summary>
    private static bool IsShutdown(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        // The pipe was disposed underneath the loop, which only Dispose does.
        ObjectDisposedException => true,
        OperationCanceledException => cancellationToken.IsCancellationRequested,
        _ => false,
    };

    /// <summary>
    /// Accepts one connection and reads its payload, always releasing the pipe afterwards. Returns
    /// <see langword="null"/> when the client sent nothing usable.
    /// </summary>
    private async Task<byte[]?> AcceptAndReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Waiting for a connection has no deadline: an idle pipe with nobody connected is the
            // normal state, not a problem.
            await _pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

            // Once connected a deadline applies, otherwise a client that connects and never finishes
            // sending would hold the pipe's only server instance forever — see ReadTimeout.
            using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readCancellation.CancelAfter(_options.ReadTimeout);

            return await ReadPayloadAsync(readCancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Paces the loop after a failure, so that a pipe or a client failing immediately and repeatedly
    /// leaves the listener degraded rather than spinning at full speed.
    /// </summary>
    /// <returns><see langword="false"/> if shutdown was requested while waiting.</returns>
    private static async Task<bool> TryBackOffAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(s_errorBackoff, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<byte[]?> ReadPayloadAsync(CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];

        int count;
        while ((count = await _pipeServer.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            buffer.Write(chunk, 0, count);

            // Thrown rather than returned as null so that it travels the same path as any other bad
            // connection: reported through Error, then paced. Returning null would have made an
            // oversized payload indistinguishable from an empty one and dropped it silently.
            if (buffer.Length > _options.MaxPayloadBytes)
                throw new InvalidDataException($"Payload exceeds {_options.MaxPayloadBytes} bytes.");
        }

        return buffer.Length == 0 ? null : buffer.ToArray();
    }

    private void Dispatch(byte[] payload)
    {
        var handler = Activated;
        if (handler is null)
            return;

        _ = Task.Run(() =>
        {
            try
            {
                handler(this, new SingleInstanceActivatedEventArgs(payload));
            }
            catch (Exception ex)
            {
                RaiseError(ex);
            }
        });
    }

    private void RaiseError(Exception exception)
    {
        try
        {
            Error?.Invoke(this, new SingleInstanceErrorEventArgs(exception));
        }
        catch
        {
            // A failing error handler must not take down the listener loop.
        }
    }

    private void Disconnect()
    {
        try
        {
            // Unconditionally, and never guarded by IsConnected: when the client closes first the
            // property is already false while the pipe instance is still in a connected state, and
            // skipping the call makes every later WaitForConnectionAsync fail with
            // InvalidOperationException — the owner stops answering while still holding the name.
            _pipeServer.Disconnect();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // Not connected, or already gone: nothing to release.
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cancellation.Cancel();
        // Unblocks a WaitForConnectionAsync that is not observing cancellation yet.
        _pipeServer.Dispose();

        try
        {
            _listener?.Wait(s_shutdownTimeout);
        }
        catch (AggregateException)
        {
            // The loop faulted on its way out; there is nothing left to report at this point.
        }

        _cancellation.Dispose();
    }
}
