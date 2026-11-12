using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace Bravo.Core.Activation;

public sealed class ActivationChannel : IActivationChannel
{
    private readonly Channel<ActivationRequest> _channel;
    private int _hasConsumer;
    private volatile bool _isClosed;

    /// <param name="capacity">How many requests can wait for the consumer.</param>
    public ActivationChannel(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        };
        _channel = Channel.CreateBounded<ActivationRequest>(options);
    }

    public ActivationPostResult Post(ActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_channel.Writer.TryWrite(request))
        {
            return ActivationPostResult.Accepted;
        }

        if (_isClosed)
        {
            return ActivationPostResult.Closed;
        }

        return ActivationPostResult.Full;
    }

    public IAsyncEnumerable<ActivationRequest> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _hasConsumer, 1) != 0)
        {
            throw new InvalidOperationException("The activation queue already has its consumer.");
        }

        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        _isClosed = true;
        _channel.Writer.TryComplete();
    }
}
