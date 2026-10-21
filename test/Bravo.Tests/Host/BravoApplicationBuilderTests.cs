using Bravo.Tests.Infrastructure.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Host;

public class BravoApplicationBuilderTests
{
    [Fact]
    public void Build_ResolvesTheExistingInstancesWithoutOwningTheirLifetime()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var policies = new PolicyService(new FakePolicyReader());
        var settings = new UserSettings();
        var diagnosticLevel = new DiagnosticLevelSwitch();
        var diagnosticLog = new DiagnosticLogStore();
        var options = TestInstanceOptions.Create();

        Assert.True(SingleInstanceServer.TryStart(options, out var instanceServer));

        using (instanceServer)
        using (var bootstrap = new BootstrapContext(loggerFactory, diagnosticLevel, diagnosticLog, policies, settings, telemetry))
        {
            using (var application = BravoApplication.CreateBuilder(bootstrap, instanceServer).Build())
            {
                Assert.Same(loggerFactory, application.Services.GetRequiredService<ILoggerFactory>());
                Assert.Same(diagnosticLevel, application.Services.GetRequiredService<DiagnosticLevelSwitch>());
                Assert.Same(diagnosticLog, application.Services.GetRequiredService<DiagnosticLogStore>());
                Assert.Same(telemetry, application.Services.GetRequiredService<ITelemetryService>());
                Assert.Same(policies, application.Services.GetRequiredService<IPolicyService>());
                Assert.Same(settings, application.Services.GetRequiredService<IUserSettings>());
                Assert.Same(instanceServer, application.Services.GetRequiredService<SingleInstanceServer>());
                Assert.IsType<BravoApplicationInstance>(application.Services.GetRequiredService<IInstanceEvents>());
                Assert.False(application.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.IsCancellationRequested);
            }

            // The container disposes the services it creates, not the registered instances.
            Assert.Equal(0, telemetry.DisposeCount);
            Assert.Equal(0, loggerFactory.DisposeCount);

            // The pipe stays claimed until Main releases the instance server.
            Assert.False(SingleInstanceServer.TryStart(options, out _));
        }
    }
}
