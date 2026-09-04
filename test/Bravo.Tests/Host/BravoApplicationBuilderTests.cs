using Bravo.Tests.Infrastructure.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Host;

public class BravoApplicationBuilderTests
{
    [Fact]
    public void Build_ResolvesTheBootstrapInstancesWithoutOwningTheirLifetime()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var policies = new PolicyService(new FakePolicyReader());
        var settings = new UserSettings();
        using var bootstrap = new BootstrapContext(loggerFactory, policies, settings, telemetry);
        var instance = new FakeInstance();
        var builder = BravoApplication.CreateBuilder(bootstrap);
        builder.Services.Replace(ServiceDescriptor.Singleton<IBravoApplicationInstance>((_) => instance));

        using (var application = builder.Build())
        {
            Assert.Same(loggerFactory, application.Services.GetRequiredService<ILoggerFactory>());
            Assert.Same(telemetry, application.Services.GetRequiredService<ITelemetryService>());
            Assert.Same(policies, application.Services.GetRequiredService<IPolicyService>());
            Assert.Same(settings, application.Services.GetRequiredService<IUserSettings>());
            Assert.Same(instance, application.Services.GetRequiredService<IBravoApplicationInstance>());
            Assert.False(application.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.IsCancellationRequested);
        }

        // The container disposes the services it creates, not the registered bootstrap instances.
        Assert.Equal(1, instance.DisposeCount);
        Assert.Equal(0, telemetry.DisposeCount);
        Assert.Equal(0, loggerFactory.DisposeCount);
    }

    [Fact]
    public void Build_WithoutRunningDoesNotClaimAnInstance()
    {
        var instanceCreations = 0;
        using var bootstrap = new BootstrapContext(new FakeLoggerFactory(), new PolicyService(new FakePolicyReader()), new UserSettings(), new FakeTelemetry());
        var builder = BravoApplication.CreateBuilder(bootstrap);
        builder.Services.Replace(ServiceDescriptor.Singleton<IBravoApplicationInstance>((_) =>
        {
            instanceCreations++;
            return new FakeInstance();
        }));

        using (var application = builder.Build())
        {
            Assert.Equal(0, instanceCreations);
        }

        Assert.Equal(0, instanceCreations);
    }
}