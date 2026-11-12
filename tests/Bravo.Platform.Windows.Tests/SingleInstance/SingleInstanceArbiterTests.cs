using System;
using System.Threading.Tasks;
using Bravo.Platform.Windows.SingleInstance;
using Xunit;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

public sealed class SingleInstanceArbiterTests
{
    // Generous: only a test that fails waits for one of them.
    private static readonly SingleInstanceOptions s_options = new()
    {
        ConnectTimeout = TimeSpan.FromSeconds(30),
        ExchangeTimeout = TimeSpan.FromSeconds(30),
        ArbitrationTimeout = TimeSpan.FromSeconds(60),
        RetryInterval = TimeSpan.FromMilliseconds(20),
    };

    private static readonly byte[] s_payload = [1, 2, 3, 4, 5];

    [Fact]
    public async Task Arbitrate_InstanceWithNoOwner_MakesTheProcessTheOwner()
    {
        var outcome = await ArbitrateAsync(TestInstance.CreateNames(), s_options);

        Assert.Equal(new Outcome(IsOwner: true, Failure: null), outcome);
    }

    [Fact]
    public async Task Arbitrate_OwnerThatListens_HandsTheRequestOver()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options, receiver);

        var outcome = await ArbitrateAsync(names, s_options);

        Assert.Equal(new Outcome(IsOwner: false, Failure: null), outcome);
        Assert.Equal([s_payload], receiver.Payloads);
    }

    [Fact]
    public async Task Arbitrate_OwnerThatIsBusyAtFirst_SendsTheRequestAgain()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Busy, HandOverReply.Busy, HandOverReply.Accepted);
        await using var listener = new RunningListener(names, s_options, receiver);

        var outcome = await ArbitrateAsync(names, s_options);

        Assert.Equal(new Outcome(IsOwner: false, Failure: null), outcome);
        Assert.Equal(3, receiver.Payloads.Count);
    }

    [Fact]
    public async Task Arbitrate_OwnerWithoutAPipe_GivesUpAfterTheTimeoutAndIsNotTheOwner()
    {
        var names = TestInstance.CreateNames();
        using var owner = new OwnedInstance(names);

        var outcome = await ArbitrateAsync(names, TestInstance.ShortOptions);

        Assert.Equal(new Outcome(IsOwner: false, HandOverFailure.NotResponding), outcome);
    }

    [Fact]
    public async Task Arbitrate_OwnerThatKeepsSayingItIsShuttingDown_GivesUpAfterTheTimeout()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.ShuttingDown);
        await using var listener = new RunningListener(names, s_options, receiver);
        var options = s_options with { ArbitrationTimeout = TimeSpan.FromMilliseconds(300) };

        var outcome = await ArbitrateAsync(names, options);

        Assert.Equal(new Outcome(IsOwner: false, HandOverFailure.NotResponding), outcome);
    }

    [Fact]
    public async Task Arbitrate_OwnerThatEndsDuringTheArbitration_MakesTheProcessTheOwner()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.ShuttingDown);
        var listener = new RunningListener(names, s_options, receiver);

        // An owner that ends while the arbiter waits for its pipe is noticed when that wait is over: a short
        // one here, so that the test does not last as long as a limit made for failures.
        var options = s_options with { ConnectTimeout = TimeSpan.FromMilliseconds(200) };

        var arbitration = ArbitrateAsync(names, options);
        await receiver.FirstRequest.WaitAsync(s_options.ArbitrationTimeout, TestContext.Current.CancellationToken);
        await listener.DisposeAsync();
        var outcome = await arbitration;

        Assert.Equal(new Outcome(IsOwner: true, Failure: null), outcome);
    }

    [Fact]
    public async Task Arbitrate_OwnerThatRefusesTheRequest_FailsWithoutTryingAgain()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.UnsupportedVersion);
        await using var listener = new RunningListener(names, s_options, receiver);

        var outcome = await ArbitrateAsync(names, s_options);

        Assert.Equal(new Outcome(IsOwner: false, HandOverFailure.Refused), outcome);
        Assert.Single(receiver.Payloads);
    }

    [Fact]
    public async Task Arbitrate_OwnerWhosePipeDeniesAccess_FailsWithAccessDenied()
    {
        var names = TestInstance.CreateNames();
        using var owner = new OwnedInstance(names);
        await using var denied = HandOverClientTests.CreatePipeThatDeniesTheCurrentUser(names);

        var outcome = await ArbitrateAsync(names, s_options);

        Assert.Equal(new Outcome(IsOwner: false, HandOverFailure.AccessDenied), outcome);
    }

    // On one thread from the arbitration to the release, as the entry thread of a process does.
    private static Task<Outcome> ArbitrateAsync(InstanceNames names, SingleInstanceOptions options)
    {
        return Task.Run(() =>
        {
            var arbiter = new SingleInstanceArbiter(names, options, TimeProvider.System);
            var result = arbiter.Arbitrate(s_payload);
            using (result.Ownership)
            {
                return new Outcome(result.IsOwner, result.Failure);
            }
        }, TestContext.Current.CancellationToken);
    }

    private sealed record Outcome(bool IsOwner, HandOverFailure? Failure);
}
