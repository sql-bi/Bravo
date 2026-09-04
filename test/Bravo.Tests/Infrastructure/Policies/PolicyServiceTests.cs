using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sqlbi.Bravo.Infrastructure.Policies;
using Xunit;

namespace Bravo.Tests.Infrastructure.Policies;

public class PolicyServiceTests
{
    [Fact]
    public void Current_ReadsOnceAndKeepsTheSameSnapshot()
    {
        var initial = PolicySnapshot.NotConfigured with { TelemetryEnabled = false };
        var reader = new FakePolicyReader { Snapshot = initial };
        var service = new PolicyService(reader);

        Assert.Equal(0, reader.ReadCount);
        Assert.Same(initial, service.Current);

        reader.Snapshot = PolicySnapshot.NotConfigured with { TelemetryEnabled = true };

        Assert.Same(initial, service.Current);
        Assert.False(service.Current.TelemetryEnabled);
        Assert.Equal(1, reader.ReadCount);
    }

    [Fact]
    public async Task Current_ConcurrentAccessInitializesOnce()
    {
        var reader = new FakePolicyReader();
        var service = new PolicyService(reader);

        var snapshots = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(() => service.Current)))
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.All(snapshots, snapshot => Assert.Same(service.Current, snapshot));
        Assert.Equal(1, reader.ReadCount);
    }

    [Fact]
    public void Current_ReadFailurePropagatesWithoutRetryingOrReturningDefaults()
    {
        var failure = new IOException("Registry unavailable");
        var reader = new FakePolicyReader { OnRead = () => throw failure };
        var service = new PolicyService(reader);

        Assert.Same(failure, Assert.Throws<IOException>(() => service.Current));
        Assert.Same(failure, Assert.Throws<IOException>(() => service.Current));
        Assert.Equal(1, reader.ReadCount);
    }
}
