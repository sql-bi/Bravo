using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Platform.Windows.Interop;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// The side of a second process: sends its request to the owner and reads the answer.
/// </summary>
internal sealed class HandOverClient(InstanceNames names, SingleInstanceOptions options, TimeProvider timeProvider)
{
    private readonly string _pipeName = names.PipeName;
    private readonly SingleInstanceOptions _options = options;
    private readonly TimeProvider _timeProvider = timeProvider;

    public Task<HandOverResult> SendAsync(ReadOnlyMemory<byte> payload)
    {
        return SendAsync(HandOverFrame.Version, payload);
    }

    internal async Task<HandOverResult> SendAsync(int version, ReadOnlyMemory<byte> payload)
    {
        const PipeOptions Options = PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly;

        var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, Options);
        await using (pipe.ConfigureAwait(false))
        {
            try
            {
                await pipe.ConnectAsync(_options.ConnectTimeout, CancellationToken.None).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                return new HandOverResult(HandOverStatus.AccessDenied);
            }
            catch (Exception exception) when (exception is TimeoutException or IOException)
            {
                return new HandOverResult(HandOverStatus.Unreachable);
            }

            AllowOwnerToTakeTheForeground(pipe);

            var frame = HandOverFrame.Create(version, payload.Span);
            return await ExchangeAsync(pipe, frame).ConfigureAwait(false);
        }
    }

    private async Task<HandOverResult> ExchangeAsync(Stream pipe, byte[] frame)
    {
        using var timeout = new CancellationTokenSource(_options.ExchangeTimeout, _timeProvider);
        try
        {
            await pipe.WriteAsync(frame, timeout.Token).ConfigureAwait(false);

            var reply = new byte[1];
            await pipe.ReadExactlyAsync(reply, timeout.Token).ConfigureAwait(false);

            return new HandOverResult(HandOverStatus.Answered, (HandOverReply)reply[0]);
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException)
        {
            return new HandOverResult(HandOverStatus.NoAnswer);
        }
    }

    // Windows lets a process come to the foreground when a process that could do it grants the right: here
    // the one the user has just started. Without the grant the window of the owner flashes in the taskbar.
    private static void AllowOwnerToTakeTheForeground(NamedPipeClientStream pipe)
    {
        if (NamedPipeInterop.TryGetServerProcessId(pipe.SafePipeHandle, out var ownerProcessId))
        {
            _ = ForegroundInterop.TryAllowSetForeground(ownerProcessId);
        }
    }
}
