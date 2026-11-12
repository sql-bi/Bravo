using Bravo.Core.Activation;
using Bravo.Platform.Windows.SingleInstance;
using NSubstitute;
using Xunit;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

public sealed class ActivationHandOverReceiverTests
{
    [Theory]
    [InlineData(ActivationPostResult.Accepted, HandOverReply.Accepted)]
    [InlineData(ActivationPostResult.Full, HandOverReply.Busy)]
    [InlineData(ActivationPostResult.Closed, HandOverReply.ShuttingDown)]
    public void Receive_Request_AnswersWhatTheChannelDidWithIt(ActivationPostResult posted, HandOverReply expected)
    {
        var channel = Substitute.For<IActivationChannel>();
        channel.Post(Arg.Any<ActivationRequest>()).Returns(posted);
        var receiver = new ActivationHandOverReceiver(channel);

        var reply = receiver.Receive(ActivationPayload.Encode(ActivationRequest.Empty));

        Assert.Equal(expected, reply);
    }

    [Fact]
    public void Receive_Request_PostsWhatTheSecondProcessSent()
    {
        var channel = Substitute.For<IActivationChannel>();
        var receiver = new ActivationHandOverReceiver(channel);
        var request = new ActivationRequest { ServerName = "localhost:12345", DatabaseName = "db" };

        receiver.Receive(ActivationPayload.Encode(request));

        channel.Received(1).Post(Arg.Is<ActivationRequest>(posted =>
            posted.ServerName == "localhost:12345" && posted.DatabaseName == "db"));
    }

    [Fact]
    public void Receive_PayloadThatIsNotARequest_RefusesItAndPostsNothing()
    {
        var channel = Substitute.For<IActivationChannel>();
        var receiver = new ActivationHandOverReceiver(channel);

        var reply = receiver.Receive("not json"u8.ToArray());

        Assert.Equal(HandOverReply.InvalidRequest, reply);
        channel.DidNotReceive().Post(Arg.Any<ActivationRequest>());
    }
}
