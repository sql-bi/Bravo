using System;
using System.Collections.Generic;
using System.IO;
using Bravo.Tests.Infrastructure.Policies;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Host;

public class BootstrapContextTests
{
    [Fact]
    public void Dispose_DisposesTheOwnedServicesOnce()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var context = new BootstrapContext(loggerFactory, new PolicyService(new FakePolicyReader()), new UserSettings(), telemetry);

        context.Dispose();
        context.Dispose();

        Assert.Equal(1, telemetry.DisposeCount);
        Assert.Equal(1, loggerFactory.DisposeCount);
    }

    [Theory]
    [InlineData(null, true, true)]
    [InlineData(null, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    public void Create_LoadsPoliciesOnceBeforeCreatingTelemetry(bool? policy, bool preference, bool expected)
    {
        var loggerFactory = new FakeLoggerFactory();
        var snapshot = PolicySnapshot.NotConfigured with { TelemetryEnabled = policy };
        var reader = new FakePolicyReader { Snapshot = snapshot };
        var policyService = new PolicyService(reader);
        var settings = new UserSettings { TelemetryEnabled = preference };
        var policyFactory = new FakePolicyFactory(() => policyService);
        var settingsFactory = new FakeSettingsFactory(() => settings);
        var telemetryFactory = new FakeTelemetryFactory();
        var factory = new BootstrapContextFactory(loggerFactory, policyFactory, settingsFactory, telemetryFactory);

        Assert.Equal(0, reader.ReadCount);
        Assert.Equal(0, policyFactory.CreateCount);
        Assert.Equal(0, settingsFactory.CreateCount);
        Assert.Empty(telemetryFactory.EnabledValues);

        using var context = factory.Create();
        reader.Snapshot = PolicySnapshot.NotConfigured with { TelemetryEnabled = !expected };

        Assert.Equal([expected], telemetryFactory.EnabledValues);
        Assert.Equal(expected, context.Telemetry.TelemetryEnabled);
        Assert.Same(loggerFactory, context.LoggerFactory);
        Assert.Same(policyService, context.PolicyService);
        Assert.Same(snapshot, context.PolicyService.Current);
        Assert.Same(settings, context.Settings);
        Assert.Equal(1, reader.ReadCount);
        Assert.Equal(1, policyFactory.CreateCount);
        Assert.Equal(1, settingsFactory.CreateCount);
    }

    [Fact]
    public void Create_ReadFailurePropagatesBeforeTelemetryIsCreated()
    {
        var failure = new IOException("Registry unavailable");
        var reader = new FakePolicyReader { OnRead = () => throw failure };
        using var loggerFactory = new FakeLoggerFactory();
        var policyFactory = new FakePolicyFactory(() => new PolicyService(reader));
        var settingsFactory = new FakeSettingsFactory(() => new UserSettings());
        var telemetryFactory = new FakeTelemetryFactory();
        var factory = new BootstrapContextFactory(loggerFactory, policyFactory, settingsFactory, telemetryFactory);

        Assert.Same(failure, Assert.Throws<IOException>(() => factory.Create()));
        Assert.Equal(0, settingsFactory.CreateCount);
        Assert.Empty(telemetryFactory.EnabledValues);
        Assert.Equal(0, loggerFactory.DisposeCount);
    }

    [Fact]
    public void Create_PolicyCreationFailurePropagatesBeforeCreatingOtherServices()
    {
        var failure = new InvalidOperationException("Policy service unavailable");
        using var loggerFactory = new FakeLoggerFactory();
        var policyFactory = new FakePolicyFactory(() => throw failure);
        var settingsFactory = new FakeSettingsFactory(() => new UserSettings());
        var telemetryFactory = new FakeTelemetryFactory();
        var factory = new BootstrapContextFactory(loggerFactory, policyFactory, settingsFactory, telemetryFactory);

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => factory.Create()));
        Assert.Equal(0, settingsFactory.CreateCount);
        Assert.Empty(telemetryFactory.EnabledValues);
        Assert.Equal(0, loggerFactory.DisposeCount);
    }

    [Fact]
    public void Create_SettingsCreationFailurePropagatesBeforeCreatingTelemetry()
    {
        var failure = new IOException("Settings unavailable");
        using var loggerFactory = new FakeLoggerFactory();
        var policyFactory = new FakePolicyFactory(() => new PolicyService(new FakePolicyReader()));
        var settingsFactory = new FakeSettingsFactory(() => throw failure);
        var telemetryFactory = new FakeTelemetryFactory();
        var factory = new BootstrapContextFactory(loggerFactory, policyFactory, settingsFactory, telemetryFactory);

        Assert.Same(failure, Assert.Throws<IOException>(() => factory.Create()));
        Assert.Empty(telemetryFactory.EnabledValues);
        Assert.Equal(0, loggerFactory.DisposeCount);
    }

    [Fact]
    public void Create_TelemetryFailureUsesDisabledServiceAndLogsWarning()
    {
        var failure = new InvalidOperationException("Telemetry unavailable");
        var loggerFactory = new FakeLoggerFactory();
        var policyFactory = new FakePolicyFactory(() => new PolicyService(new FakePolicyReader()));
        var settingsFactory = new FakeSettingsFactory(() => new UserSettings());
        var telemetryFactory = new FakeTelemetryFactory { Failure = failure };
        var factory = new BootstrapContextFactory(loggerFactory, policyFactory, settingsFactory, telemetryFactory);

        using var context = factory.Create();

        Assert.IsType<NullTelemetryService>(context.Telemetry);
        Assert.False(context.Telemetry.TelemetryEnabled);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    private sealed class FakePolicyFactory(Func<IPolicyService> create) : PolicyServiceFactory
    {
        public int CreateCount { get; private set; }

        public IPolicyService Create()
        {
            CreateCount++;
            return create();
        }
    }

    private sealed class FakeSettingsFactory(Func<IUserSettings> create) : IUserSettingsServiceFactory
    {
        public int CreateCount { get; private set; }

        public IUserSettings Create()
        {
            CreateCount++;
            return create();
        }
    }

    private sealed class FakeTelemetryFactory : ITelemetryServiceFactory
    {
        public List<bool> EnabledValues { get; } = [];
        public Exception? Failure { get; init; }

        public ITelemetryService Create(bool enabled)
        {
            EnabledValues.Add(enabled);
            if (Failure is not null)
                throw Failure;

            return new FakeTelemetry { TelemetryEnabled = enabled };
        }
    }
}
