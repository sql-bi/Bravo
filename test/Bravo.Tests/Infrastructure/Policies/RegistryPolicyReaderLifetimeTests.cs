using System;
using System.Collections.Generic;
using System.IO;
using Sqlbi.Bravo.Infrastructure.Policies;
using Xunit;

namespace Bravo.Tests.Infrastructure.Policies;

public class RegistryPolicyReaderLifetimeTests
{
    [Fact]
    public void Read_CombinesBothScopesAndDisposesTheirSources()
    {
        var computer = new FakePolicySource();
        computer.Set("TelemetryEnabled", 0);
        computer.Set("UpdateCheckEnabled", "invalid");
        var user = new FakePolicySource();
        user.Set("TelemetryEnabled", 1);
        user.Set("UpdateCheckEnabled", 1);
        var scopes = new List<PolicyScope>();
        var factory = new FakePolicySourceFactory(scope =>
        {
            scopes.Add(scope);
            return scope == PolicyScope.Computer ? computer : user;
        });
        var reader = new RegistryPolicyReader(factory);

        var policies = reader.Read();

        Assert.False(policies.TelemetryEnabled);
        Assert.True(policies.UpdateCheckEnabled);
        Assert.Equal([PolicyScope.Computer, PolicyScope.User], scopes);
        Assert.All(new[] { computer, user }, source => Assert.Equal(1, source.DisposeCount));
    }

    [Fact]
    public void Read_UserKeyOpenFailureDisposesComputerSourceAndPropagates()
    {
        var computer = new FakePolicySource();
        var failure = new UnauthorizedAccessException();
        var reader = new RegistryPolicyReader(new FakePolicySourceFactory(scope =>
            scope == PolicyScope.Computer ? computer : throw failure));

        Assert.Same(failure, Assert.Throws<UnauthorizedAccessException>(() => reader.Read()));
        Assert.Equal(1, computer.DisposeCount);
    }

    [Fact]
    public void Read_ValueReadFailureDisposesEveryOpenedSourceAndPropagates()
    {
        var computer = new FakePolicySource();
        var failure = new IOException();
        var user = new FakePolicySource { ReadFailure = failure };
        var reader = new RegistryPolicyReader(new FakePolicySourceFactory(scope =>
            scope == PolicyScope.Computer ? computer : user));

        Assert.Same(failure, Assert.Throws<IOException>(() => reader.Read()));
        Assert.Equal(1, computer.DisposeCount);
        Assert.Equal(1, user.DisposeCount);
    }

    [Fact]
    public void Read_ComputerOpenFailureDoesNotFallBackToUser()
    {
        var scopes = new List<PolicyScope>();
        var reader = new RegistryPolicyReader(new FakePolicySourceFactory(scope =>
        {
            scopes.Add(scope);
            throw new UnauthorizedAccessException();
        }));

        Assert.Throws<UnauthorizedAccessException>(() => reader.Read());
        Assert.Equal([PolicyScope.Computer], scopes);
    }

    [Fact]
    public void Read_MissingKeysAreAnEmptySnapshot()
    {
        var reader = new RegistryPolicyReader(new FakePolicySourceFactory(_ => new RegistryPolicySource(null)));

        Assert.Equal(PolicySnapshot.NotConfigured, reader.Read());
    }
}
