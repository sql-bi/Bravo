using System;
using System.Buffers;
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
/// instance: only one process at a time can hold the name. There is no separate mutex, so there is
/// no second piece of state that can disagree with the listener. If this process stops listening
/// for any reason, the name is released and the next process to start becomes the owner.
/// </para>
/// <para>
/// The pipe is disconnected, not recreated, between connections: recreating it would release the
/// name for an instant, during which another process could claim ownership.
/// </para>
/// </remarks>
internal sealed class SingleInstanceServer : IDisposable
{
    private const int ErrorPipeBusy = unchecked((int)0x800700E7);

    private static readonly TimeSpan s_acceptFailureBackoff = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan s_shutdownTimeout = TimeSpan.FromSeconds(1);

    private readonly SingleInstanceOptions _options;
    private readonly NamedPipeServerStream _pipeServer;
    private readonly CancellationTokenSource _cancellation;
    private readonly Task _listener;

    private bool _disposed;

    private SingleInstanceServer(SingleInstanceOptions options, NamedPipeServerStream pipeServer)
    {
        _options = options;
        _pipeServer = pipeServer;
        _cancellation = new CancellationTokenSource();
        // Started on the thread pool so that the loop runs with no SynchronizationContext, whatever
        // thread called TryStart: every continuation stays on the pool, and the blocking wait in
        // Dispose cannot deadlock against the caller's context.
        _listener = Task.Run(() => ListenAsync(_cancellation.Token));
    }

    /// <summary>
    /// Raised when a secondary instance sends an activation payload.
    /// </summary>
    /// <remarks>
    /// Raised on a thread pool thread, not on the listener loop: a blocking handler does not stop
    /// the listener. Handlers for different payloads may run concurrently and in any order.
    /// </remarks>
    public event EventHandler<SingleInstanceActivatedEventArgs>? Activated;

    /// <summary>
    /// Raised when a connection fails, a client sends no valid payload within
    /// <see cref="SingleInstanceOptions.ReadTimeout"/>, or an <see cref="Activated"/> handler throws.
    /// </summary>
    /// <remarks>
    /// The listener continues after the event. Handlers may be invoked concurrently.
    /// </remarks>
    public event EventHandler<ErrorEventArgs>? Error;

    /// <summary>
    /// Attempts to take ownership of the application instance identified by
    /// <see cref="SingleInstanceOptions.PipeName"/> and, on success, starts listening.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if ownership was acquired by this process; <see langword="false"/> if
    /// another process already owns it.
    /// </returns>
    public static bool TryStart(SingleInstanceOptions options, [NotNullWhen(true)] out SingleInstanceServer? server)
    {
        NamedPipeServerStream pipeServer;
        try
        {
            // The default zero-sized buffer is deliberate: a client's Write then blocks until this
            // process reads, which can only happen after the connection is accepted. With a buffer,
            // a client that writes and closes before ConnectNamedPipe completes makes the accept fail
            // with ERROR_NO_DATA, and the payload in the buffer is lost.
            pipeServer = new NamedPipeServerStream(
                options.PipeName,
                PipeDirection.In,
                maxNumberOfServerInstances: 1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || (ex is IOException && ex.HResult == ErrorPipeBusy))
        {
            // ERROR_PIPE_BUSY: the single allowed instance is held by another process. Access denied:
            // it is held by a process whose pipe ACL excludes this one. Any other failure propagates.
            server = null;
            return false;
        }

        server = new SingleInstanceServer(options, pipeServer);
        return true;
    }

    /// <summary>
    /// Serves one connection at a time until shutdown.
    /// </summary>
    /// <remarks>
    /// A failed connection is reported and the loop continues: ending the loop would release the
    /// pipe name. Only a failure to accept is paced, since a repeatedly failing pipe would otherwise
    /// spin. A client that fails after connecting is not paced: the pipe is intact and the next
    /// client may already be waiting for the instance.
    /// </remarks>
    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                try
                {
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);
                }
                catch (Exception ex) when (!IsShutdown(ex, cancellationToken))
                {
                    Disconnect();
                    RaiseError(ex);
                    await Task.Delay(s_acceptFailureBackoff, cancellationToken);
                    continue;
                }

                try
                {
                    Dispatch(await ReadPayloadAsync(cancellationToken));
                }
                catch (Exception ex) when (!IsShutdown(ex, cancellationToken))
                {
                    RaiseError(ex);
                }
                finally
                {
                    Disconnect();
                }
            }
        }
        catch (Exception ex) when (IsShutdown(ex, cancellationToken))
        {
        }
    }

    /// <summary>
    /// Returns whether <paramref name="exception"/> was caused by shutdown rather than by a failed
    /// connection.
    /// </summary>
    /// <remarks>
    /// Disposing the pipe under a pending operation surfaces as an <see cref="IOException"/>, an
    /// <see cref="ObjectDisposedException"/> or an <see cref="OperationCanceledException"/> depending
    /// on timing, so the shutdown token decides, not the exception type.
    /// </remarks>
    private static bool IsShutdown(Exception exception, CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested || exception is ObjectDisposedException;

    /// <summary>
    /// Reads the payload of the connected client, up to the client closing its end of the pipe.
    /// </summary>
    /// <exception cref="TimeoutException">The client did not finish within <see cref="SingleInstanceOptions.ReadTimeout"/>.</exception>
    /// <exception cref="InvalidDataException">The payload is empty or exceeds <see cref="SingleInstanceOptions.MaxPayloadBytes"/>.</exception>
    private async Task<byte[]> ReadPayloadAsync(CancellationToken cancellationToken)
    {
        // Waiting for a connection has no deadline; once connected a deadline applies, otherwise a
        // client that never finishes sending would hold the pipe's only server instance.
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readCancellation.CancelAfter(_options.ReadTimeout);

        var limit = _options.MaxPayloadBytes;
        var buffer = ArrayPool<byte>.Shared.Rent(limit + 1);
        try
        {
            var length = 0;
            while (length <= limit)
            {
                var count = await _pipeServer.ReadAsync(buffer.AsMemory(length, buffer.Length - length), readCancellation.Token);
                if (count == 0)
                    break;

                length += count;
            }

            if (length > limit)
                throw new InvalidDataException($"The payload exceeds {limit} bytes.");

            if (length == 0)
                throw new InvalidDataException("The payload is empty.");

            return buffer.AsSpan(0, length).ToArray();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The client did not finish sending within {_options.ReadTimeout}.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
            Error?.Invoke(this, new ErrorEventArgs(exception));
        }
        catch
        {
            // An exception from an Error handler does not end the listener loop.
        }
    }

    private void Disconnect()
    {
        try
        {
            // Not guarded by IsConnected: when the client closes first, IsConnected is already false
            // while the instance is still connected, and a skipped Disconnect makes every later
            // WaitForConnectionAsync throw InvalidOperationException.
            _pipeServer.Disconnect();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // Not connected, or already disposed: nothing to release.
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cancellation.Cancel();
        _pipeServer.Dispose();

        // ListenAsync catches every exception, so this only returns or times out.
        _listener.Wait(s_shutdownTimeout);

        _cancellation.Dispose();
    }
}
