using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

public sealed class HandOverListenerTests
{
    // Generous: only a test that fails waits for one of them.
    private static readonly SingleInstanceOptions s_options = new()
    {
        ConnectTimeout = TimeSpan.FromSeconds(30),
        ExchangeTimeout = TimeSpan.FromSeconds(30),
    };

    private static readonly byte[] s_payload = [1, 2, 3, 4, 5];

    [Fact]
    public async Task RunAsync_Request_GivesThePayloadToTheReceiverAndReturnsItsAnswer()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options, receiver);

        var result = await CreateClient(names).SendAsync(s_payload);

        Assert.True(result.IsAccepted);
        Assert.Equal([s_payload], receiver.Payloads);
    }

    [Theory]
    [InlineData(HandOverReply.Busy)]
    [InlineData(HandOverReply.ShuttingDown)]
    [InlineData(HandOverReply.InvalidRequest)]
    public async Task RunAsync_ReceiverThatRefuses_ReturnsTheRefusal(HandOverReply refusal)
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(refusal);
        await using var listener = new RunningListener(names, s_options, receiver);

        var result = await CreateClient(names).SendAsync(s_payload);

        Assert.Equal(new HandOverResult(HandOverStatus.Answered, refusal), result);
    }

    [Fact]
    public async Task RunAsync_RequestsOneAfterTheOther_AnswersEachOne()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Busy, HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options, receiver);
        var client = CreateClient(names);

        var first = await client.SendAsync(s_payload);
        var second = await client.SendAsync(s_payload);

        Assert.Equal(HandOverReply.Busy, first.Reply);
        Assert.Equal(HandOverReply.Accepted, second.Reply);
    }

    [Fact]
    public async Task RunAsync_PayloadBeyondTheLimit_RefusesItWithoutReadingIt()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options with { MaxPayloadBytes = 16 }, receiver);

        var result = await CreateClient(names).SendAsync(new byte[100_000]);

        Assert.Equal(new HandOverResult(HandOverStatus.Answered, HandOverReply.PayloadTooLarge), result);
        Assert.Empty(receiver.Payloads);
    }

    [Fact]
    public async Task RunAsync_RequestOfAnotherProtocolVersion_RefusesIt()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options, receiver);

        var result = await CreateClient(names).SendAsync(HandOverFrame.Version + 1, s_payload);

        Assert.Equal(new HandOverResult(HandOverStatus.Answered, HandOverReply.UnsupportedVersion), result);
        Assert.Empty(receiver.Payloads);
    }

    [Fact]
    public async Task RunAsync_RequestWithNoPayload_RefusesIt()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options, receiver);

        var result = await CreateClient(names).SendAsync(ReadOnlyMemory<byte>.Empty);

        Assert.Equal(new HandOverResult(HandOverStatus.Answered, HandOverReply.InvalidRequest), result);
    }

    [Fact]
    public async Task RunAsync_ClientThatSendsNothing_LeavesThePipeToTheNextOneAfterTheTimeout()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        var options = s_options with { ExchangeTimeout = TimeSpan.FromMilliseconds(300) };
        await using var listener = new RunningListener(names, options, receiver);
        await using var silent = await ConnectAsync(names);

        var result = await CreateClient(names).SendAsync(s_payload);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public async Task RunAsync_ClientThatLeavesInTheMiddleOfARequest_LeavesThePipeToTheNextOne()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options, receiver);
        var hasty = await ConnectAsync(names);
        await hasty.WriteAsync(new byte[] { 1, 0, 0 }, TestContext.Current.CancellationToken);
        await hasty.DisposeAsync();

        var result = await CreateClient(names).SendAsync(s_payload);

        Assert.True(result.IsAccepted);
        Assert.Equal([s_payload], receiver.Payloads);
    }

    [Fact]
    public async Task RunAsync_PipeNameAlreadyTaken_EndsAndLogsIt()
    {
        var names = TestInstance.CreateNames();
        await using var taken = new NamedPipeServerStream(names.PipeName, PipeDirection.InOut, 1);
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        var logger = new RecordingLogger<HandOverListener>();
        await using var listener = new RunningListener(names, s_options, receiver, logger);

        await listener.Completion.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal((LogLevel.Warning, 1), (entry.Level, entry.Id.Id));
    }

    [Fact]
    public async Task RunAsync_RequestsThatComplete_LogNothing()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        var logger = new RecordingLogger<HandOverListener>();
        var listener = new RunningListener(names, s_options, receiver, logger);
        var client = CreateClient(names);

        // The second request is answered only after the listener has closed the first exchange.
        await client.SendAsync(s_payload);
        await client.SendAsync(s_payload);
        await listener.DisposeAsync();

        Assert.Empty(logger.Entries);
    }

    private static HandOverClient CreateClient(InstanceNames names)
    {
        return new HandOverClient(names, s_options, TimeProvider.System);
    }

    private static async Task<NamedPipeClientStream> ConnectAsync(InstanceNames names)
    {
        var pipe = new NamedPipeClientStream(".", names.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(s_options.ConnectTimeout, TestContext.Current.CancellationToken);
        return pipe;
    }
}
