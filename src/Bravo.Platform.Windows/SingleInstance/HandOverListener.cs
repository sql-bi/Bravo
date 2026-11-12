using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// The side of the owner: reads the request of one second process at a time and answers it.
/// </summary>
public sealed partial class HandOverListener
{
    // A request is small: with a buffer, neither process waits for the other to read.
    private const int BufferSize = 4096;

    private static readonly TimeSpan s_recreateDelay = TimeSpan.FromMilliseconds(250);

    private readonly string _pipeName;
    private readonly SingleInstanceOptions _options;
    private readonly IHandOverReceiver _receiver;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HandOverListener> _logger;

    /// <summary>
    /// Only the owner of the instance listens: the pipe takes its name from the ownership.
    /// </summary>
    public HandOverListener(
        InstanceOwnership ownership,
        SingleInstanceOptions options,
        IHandOverReceiver receiver,
        TimeProvider timeProvider,
        ILogger<HandOverListener> logger)
    {
        ArgumentNullException.ThrowIfNull(ownership);

        _pipeName = ownership.Names.PipeName;
        _options = options;
        _receiver = receiver;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Listens until <paramref name="cancellationToken"/> is canceled. Returns at once, after logging it, when
    /// the pipe cannot be created: the owner goes on without a hand-over.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream pipe;
            try
            {
                pipe = CreatePipe();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                LogPipeUnavailable(exception, _pipeName);
                return;
            }

            await using (pipe.ConfigureAwait(false))
            {
                await ServeAsync(pipe, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(s_recreateDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private NamedPipeServerStream CreatePipe()
    {
        // The first and only instance with this name: no other process can answer for the owner.
        const PipeOptions Options =
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance;

        return new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            Options,
            inBufferSize: BufferSize,
            outBufferSize: BufferSize);
    }

    // Returns when the listener is stopped, or when the pipe failed and has to be created again.
    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException exception)
            {
                LogConnectionFailed(exception);
                return;
            }

            await ExchangeAsync(pipe, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                pipe.Disconnect();
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            {
                LogConnectionFailed(exception);
                return;
            }
        }
    }

    private async Task ExchangeAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        // A client that does not finish in time leaves the pipe to the next one.
        using var timeout = new CancellationTokenSource(_options.ExchangeTimeout, _timeProvider);
        using var exchange = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            var reply = await ReadRequestAsync(pipe, exchange.Token).ConfigureAwait(false);
            await pipe.WriteAsync(new[] { (byte)reply }, exchange.Token).ConfigureAwait(false);
            await WaitForClientToCloseAsync(pipe, exchange.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LogExchangeFailed(exception);
            }
        }
    }

    private async Task<HandOverReply> ReadRequestAsync(Stream pipe, CancellationToken cancellationToken)
    {
        var header = new byte[HandOverFrame.HeaderLength];
        await pipe.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);

        var (version, payloadLength) = HandOverFrame.ReadHeader(header);
        if (version != HandOverFrame.Version)
        {
            return HandOverReply.UnsupportedVersion;
        }

        if (payloadLength <= 0)
        {
            return HandOverReply.InvalidRequest;
        }

        if (payloadLength > _options.MaxPayloadBytes)
        {
            return HandOverReply.PayloadTooLarge;
        }

        var payload = new byte[payloadLength];
        await pipe.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);

        return _receiver.Receive(payload);
    }

    // Disconnecting discards what the client has not read yet, so the client closes first, after it has read
    // the answer. What it sent beyond a refused header is read and dropped here.
    private static async Task WaitForClientToCloseAsync(Stream pipe, CancellationToken cancellationToken)
    {
        var discarded = new byte[BufferSize];
        while (await pipe.ReadAsync(discarded, cancellationToken).ConfigureAwait(false) > 0)
        {
        }
    }
}
