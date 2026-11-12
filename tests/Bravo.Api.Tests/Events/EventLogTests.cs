using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Bravo.Api.Tests.Events;

// The clock never moves unless a test moves it: a wait that ends was ended by what the test did.
public sealed class EventLogTests : IDisposable
{
    private static readonly TimeSpan s_waitLimit = TimeSpan.FromSeconds(30);

    private readonly CancellationTokenSource _hostStopping = new();
    private readonly FakeTimeProvider _time = new();
    private readonly EventLog _log;

    public EventLogTests()
    {
        _log = CreateLog(capacity: 4);
    }

    public void Dispose()
    {
        _hostStopping.Dispose();
    }

    [Fact]
    public async Task ReadAsync_EventsAppendedBefore_ReturnsThemAtOnceNumberedFromOne()
    {
        Append(3);

        var batch = await _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);

        Assert.Equal([1, 2, 3], batch.Events.Select(hostEvent => hostEvent.Number));
        Assert.Equal(3, batch.Next);
    }

    [Fact]
    public async Task ReadAsync_AfterANumber_ReturnsOnlyTheEventsThatFollowIt()
    {
        Append(3);

        var batch = await _log.ReadAsync(after: 2, TestContext.Current.CancellationToken);

        Assert.Equal([3], batch.Events.Select(hostEvent => hostEvent.Number));
        Assert.Equal(3, batch.Next);
    }

    [Fact]
    public async Task ReadAsync_NothingNew_WaitsAndIsCompletedByTheNextEvent()
    {
        Append(1);
        var reading = _log.ReadAsync(after: 1, TestContext.Current.CancellationToken);
        var wasWaiting = !reading.IsCompleted;

        Append(1);

        var batch = await reading;
        Assert.True(wasWaiting);
        Assert.Equal([2], batch.Events.Select(hostEvent => hostEvent.Number));
        Assert.Equal(2, batch.Next);
    }

    [Fact]
    public async Task ReadAsync_NoEventYet_WaitsForTheFirstOne()
    {
        var reading = _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);
        var wasWaiting = !reading.IsCompleted;

        Append(1);

        var batch = await reading;
        Assert.True(wasWaiting);
        Assert.Equal([1], batch.Events.Select(hostEvent => hostEvent.Number));
    }

    [Fact]
    public async Task ReadAsync_SeveralRequestsWaiting_AreAllCompletedByOneEvent()
    {
        var first = _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);
        var second = _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);

        Append(1);

        var batches = await Task.WhenAll(first, second);
        Assert.All(batches, batch => Assert.Single(batch.Events));
    }

    [Fact]
    public async Task ReadAsync_NothingNewWithinTheLimit_ReturnsNoEventAndTheSameNumber()
    {
        Append(2);
        var reading = _log.ReadAsync(after: 2, TestContext.Current.CancellationToken);

        _time.Advance(s_waitLimit);

        var batch = await reading;
        Assert.Empty(batch.Events);
        Assert.Equal(2, batch.Next);
    }

    [Fact]
    public void ReadAsync_NothingNewBeforeTheLimit_KeepsWaiting()
    {
        var reading = _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);

        _time.Advance(s_waitLimit - TimeSpan.FromTicks(1));

        Assert.False(reading.IsCompleted);
    }

    [Fact]
    public async Task ReadAsync_NumberBeyondTheLastEvent_AnswersAtOnceWithTheNumberToGoBackTo()
    {
        Append(2);

        var batch = await _log.ReadAsync(after: 50, TestContext.Current.CancellationToken);

        Assert.Empty(batch.Events);
        Assert.Equal(2, batch.Next);
    }

    [Fact]
    public async Task Append_MoreEventsThanTheLogKeeps_DropsTheOldestAndKeepsTheNumbers()
    {
        Append(6);

        var batch = await _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);

        Assert.Equal([3, 4, 5, 6], batch.Events.Select(hostEvent => hostEvent.Number));
        Assert.Equal(6, batch.Next);
    }

    [Fact]
    public async Task ReadAsync_HostThatStopsWhileARequestWaits_ReturnsNoEvent()
    {
        var reading = _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);

        await _hostStopping.CancelAsync();

        var batch = await reading;
        Assert.Empty(batch.Events);
        Assert.Equal(0, batch.Next);
    }

    [Fact]
    public async Task ReadAsync_HostThatIsAlreadyStopping_DoesNotWait()
    {
        await _hostStopping.CancelAsync();

        var reading = _log.ReadAsync(after: 0, TestContext.Current.CancellationToken);

        Assert.True(reading.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ReadAsync_CallerThatStopsWaiting_IsCanceled()
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var reading = _log.ReadAsync(after: 0, caller.Token);

        await caller.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reading);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_CapacityThatIsNotPositive_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateLog(capacity));
    }

    private EventLog CreateLog(int capacity)
    {
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(_hostStopping.Token);
        var options = new EventLogOptions { Capacity = capacity, WaitLimit = s_waitLimit };

        return new EventLog(options, _time, lifetime);
    }

    private void Append(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _log.Append(number => new HostEvent { Number = number, Kind = HostEventKind.Activation });
        }
    }
}
