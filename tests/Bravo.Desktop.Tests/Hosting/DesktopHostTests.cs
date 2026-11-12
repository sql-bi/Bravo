using System;
using System.Collections.Generic;
using System.Linq;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Hosting;

// Each test stays on one thread: the ownership is released by the thread that acquired it.
public sealed class DesktopHostTests : IDisposable
{
    private readonly IFatalExceptionHandler _fatalExceptionHandler = Substitute.For<IFatalExceptionHandler>();
    private readonly InstanceOwnership _ownership;
    private readonly IHost _host;

    public DesktopHostTests()
    {
        var id = Guid.NewGuid().ToString("N");
        var names = new InstanceNames($"Bravo.Tests.{id}", $"Bravo.Tests.{id}");
        _ownership = InstanceOwnership.TryAcquire(names, TimeSpan.Zero)
            ?? throw new InvalidOperationException("The instance of the test already has an owner.");

        _host = DesktopHost.Build(_ownership, new SingleInstanceOptions(), _fatalExceptionHandler);
    }

    public void Dispose()
    {
        _host.Dispose();
        _ownership.Dispose();
    }

    [Fact]
    public void Build_Lifetime_IsTheOneOfAWindowAndNotOfAConsole()
    {
        var lifetime = _host.Services.GetRequiredService<IHostLifetime>();

        Assert.IsType<DesktopHostLifetime>(lifetime);
    }

    [Fact]
    public void Build_ShutdownTimeout_IsFiveSeconds()
    {
        var options = _host.Services.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(5), options.ShutdownTimeout);
    }

    [Fact]
    public void Build_Logging_HasNoProviderThatNobodyAdded()
    {
        var providers = _host.Services.GetServices<ILoggerProvider>();

        Assert.Empty(providers);
    }

    [Fact]
    public void Build_Configuration_HasNoSourceThatReadsAFileOrTheEnvironment()
    {
        var configuration = (IConfigurationRoot)_host.Services.GetRequiredService<IConfiguration>();

        var providers = Flatten(configuration).ToList();

        Assert.NotEmpty(providers);
        Assert.All(providers, provider => Assert.IsType<MemoryConfigurationProvider>(provider));
    }

    [Fact]
    public void Build_Environment_IsProductionWhateverTheVariablesOfTheMachineSay()
    {
        var environment = _host.Services.GetRequiredService<IHostEnvironment>();

        Assert.Equal(Environments.Production, environment.EnvironmentName);
        Assert.Equal(AppContext.BaseDirectory, environment.ContentRootPath);
    }

    [Fact]
    public void Build_ObjectsCreatedBeforeTheHost_AreTheOnesTheServicesGet()
    {
        var services = _host.Services;

        Assert.Same(_fatalExceptionHandler, services.GetRequiredService<IFatalExceptionHandler>());
        Assert.Same(_ownership, services.GetRequiredService<InstanceOwnership>());
    }

    // The configuration of the web host is one source of the configuration of the application.
    private static IEnumerable<IConfigurationProvider> Flatten(IConfigurationRoot configuration)
    {
        foreach (var provider in configuration.Providers)
        {
            if (provider is ChainedConfigurationProvider { Configuration: IConfigurationRoot chained })
            {
                foreach (var inner in Flatten(chained))
                {
                    yield return inner;
                }
            }
            else
            {
                yield return provider;
            }
        }
    }
}
