using System;
using System.Threading.Tasks;
using Bravo.Platform.Windows.SingleInstance;
using Xunit;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

public sealed class InstanceOwnershipTests
{
    [Fact]
    public void TryAcquire_InstanceWithNoOwner_ReturnsTheOwnership()
    {
        var names = TestInstance.CreateNames();

        using var ownership = InstanceOwnership.TryAcquire(names, TimeSpan.Zero);

        Assert.NotNull(ownership);
        Assert.Same(names, ownership.Names);
    }

    [Fact]
    public void TryAcquire_InstanceWithAnOwner_ReturnsNull()
    {
        var names = TestInstance.CreateNames();
        using var owner = new OwnedInstance(names);

        using var ownership = InstanceOwnership.TryAcquire(names, TimeSpan.Zero);

        Assert.Null(ownership);
    }

    [Fact]
    public void TryAcquire_OwnerThatReleased_ReturnsTheOwnership()
    {
        var names = TestInstance.CreateNames();
        new OwnedInstance(names).Dispose();

        using var ownership = InstanceOwnership.TryAcquire(names, TimeSpan.Zero);

        Assert.NotNull(ownership);
    }

    [Fact]
    public void TryAcquire_OwnerThatEndedWithoutReleasing_ReturnsTheOwnership()
    {
        var names = TestInstance.CreateNames();
        var gone = new OwnedInstance(names);
        gone.EndWithoutReleasing();

        using var ownership = InstanceOwnership.TryAcquire(names, TimeSpan.Zero);

        Assert.NotNull(ownership);
        GC.KeepAlive(gone);
    }

    [Fact]
    public async Task TryAcquire_OwnerThatReleasesDuringTheWait_ReturnsTheOwnership()
    {
        var names = TestInstance.CreateNames();
        var owner = new OwnedInstance(names);

        var waiting = Task.Run(() =>
        {
            using var ownership = InstanceOwnership.TryAcquire(names, TimeSpan.FromSeconds(30));
            return ownership is not null;
        }, TestContext.Current.CancellationToken);
        owner.Dispose();

        Assert.True(await waiting);
    }

    [Fact]
    public void Dispose_Twice_ReleasesOnce()
    {
        var ownership = InstanceOwnership.TryAcquire(TestInstance.CreateNames(), TimeSpan.Zero);
        Assert.NotNull(ownership);
        ownership.Dispose();

        var exception = Record.Exception(ownership.Dispose);

        Assert.Null(exception);
    }
}
