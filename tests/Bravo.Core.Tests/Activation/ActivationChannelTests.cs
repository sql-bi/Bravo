using System;
using System.Linq;
using System.Threading.Tasks;
using Bravo.Core.Activation;
using Xunit;

namespace Bravo.Core.Tests.Activation;

public sealed class ActivationChannelTests
{
    private static readonly ActivationRequest s_first = new() { ServerName = "first" };
    private static readonly ActivationRequest s_second = new() { ServerName = "second" };
    private static readonly ActivationRequest s_third = new() { ServerName = "third" };

    [Fact]
    public async Task Post_BeforeAConsumerExists_KeepsTheRequestsInOrder()
    {
        var channel = new ActivationChannel(capacity: 4);

        channel.Post(s_first);
        channel.Post(s_second);
        channel.Complete();

        var read = await channel.ReadAllAsync(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([s_first, s_second], read);
    }

    [Fact]
    public async Task Post_ChannelThatHoldsAllItCan_RefusesAndKeepsWhatItHolds()
    {
        var channel = new ActivationChannel(capacity: 2);
        channel.Post(s_first);
        channel.Post(s_second);

        var result = channel.Post(s_third);

        channel.Complete();
        var read = await channel.ReadAllAsync(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ActivationPostResult.Full, result);
        Assert.Equal([s_first, s_second], read);
    }

    [Fact]
    public async Task Post_AfterTheConsumerMadeRoom_Accepts()
    {
        var channel = new ActivationChannel(capacity: 1);
        channel.Post(s_first);
        var reader = channel.ReadAllAsync(TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await reader.MoveNextAsync();

        var result = channel.Post(s_second);

        Assert.Equal(ActivationPostResult.Accepted, result);
    }

    [Fact]
    public async Task ReadAllAsync_RequestPostedWhileTheConsumerWaits_ReturnsIt()
    {
        var channel = new ActivationChannel(capacity: 1);
        var reader = channel.ReadAllAsync(TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var next = reader.MoveNextAsync();

        channel.Post(s_first);

        Assert.True(await next);
        Assert.Same(s_first, reader.Current);
    }

    [Fact]
    public void Post_ClosedChannel_SaysThatItIsClosed()
    {
        var channel = new ActivationChannel(capacity: 2);
        channel.Complete();

        var result = channel.Post(s_first);

        Assert.Equal(ActivationPostResult.Closed, result);
    }

    [Fact]
    public void Post_ClosedChannelThatStillHoldsRequests_SaysThatItIsClosed()
    {
        var channel = new ActivationChannel(capacity: 1);
        channel.Post(s_first);
        channel.Complete();

        var result = channel.Post(s_second);

        Assert.Equal(ActivationPostResult.Closed, result);
    }

    [Fact]
    public void ReadAllAsync_SecondConsumer_Throws()
    {
        var channel = new ActivationChannel(capacity: 1);
        _ = channel.ReadAllAsync(TestContext.Current.CancellationToken);

        Assert.Throws<InvalidOperationException>(() => channel.ReadAllAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_CapacityThatIsNotPositive_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActivationChannel(capacity));
    }
}
