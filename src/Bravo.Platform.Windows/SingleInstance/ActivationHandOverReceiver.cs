using System;
using Bravo.Core.Activation;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// Puts the request of a second process on the activation queue. The answer says whether the queue took it:
/// an accepted request is one that the consumer of the queue will read.
/// </summary>
public sealed class ActivationHandOverReceiver(IActivationChannel channel) : IHandOverReceiver
{
    private readonly IActivationChannel _channel = channel;

    public HandOverReply Receive(ReadOnlyMemory<byte> payload)
    {
        if (!ActivationPayload.TryDecode(payload.Span, out var request))
        {
            return HandOverReply.InvalidRequest;
        }

        return _channel.Post(request) switch
        {
            ActivationPostResult.Accepted => HandOverReply.Accepted,
            ActivationPostResult.Full => HandOverReply.Busy,
            _ => HandOverReply.ShuttingDown,
        };
    }
}
