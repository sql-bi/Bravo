using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Server;
using Bravo.Desktop.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bravo.Desktop.Tests.Hosting;

public sealed class DesktopHostServiceFailureTests
{
    // The host stops when a service fails, with no error of its own: the process would end as if closed.
    [Fact]
    public async Task ThrowIfAServiceFailed_ServiceThatFailed_ThrowsItsException()
    {
        var exception = new InvalidOperationException("It failed.");
        using var services = await StartAsync(new EndingService(Task.FromException(exception)));

        var thrown = Assert.Throws<InvalidOperationException>(() => DesktopHost.ThrowIfAServiceFailed(services));

        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task ThrowIfAServiceFailed_ServicesThatFailed_ThrowsTheirExceptionsTogether()
    {
        var first = new InvalidOperationException("The first failed.");
        var second = new TimeoutException("The second failed.");
        using var services = await StartAsync(
            new EndingService(Task.FromException(first)),
            new EndingService(Task.CompletedTask),
            new EndingService(Task.FromException(second)));

        var thrown = Assert.Throws<AggregateException>(() => DesktopHost.ThrowIfAServiceFailed(services));

        Assert.Equal<Exception>([first, second], thrown.InnerExceptions);
    }

    [Fact]
    public async Task ThrowIfAServiceFailed_ServicesThatEndedOrWereStopped_DoesNothing()
    {
        using var services = await StartAsync(
            new EndingService(Task.CompletedTask),
            new EndingService(Task.FromCanceled(new CancellationToken(canceled: true))));

        DesktopHost.ThrowIfAServiceFailed(services);
    }

    // The failure stops the host, which cancels the start of the service after it.
    [Fact]
    public void Start_ServiceThatFailsWhileTheHostStarts_ThrowsItsException()
    {
        var exception = new InvalidOperationException("It failed.");
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseLoopbackServer();
        builder.Services.AddHostedService(_ => new EndingService(FailAsync(exception)));
        builder.Services.AddHostedService<SlowStartingService>();
        using var host = builder.Build();

        var thrown = Assert.Throws<InvalidOperationException>(() => DesktopHost.Start(host));

        Assert.Same(exception, thrown);
    }

    private static async Task FailAsync(Exception exception)
    {
        // Off the thread that starts the host, as the work of a service runs.
        await Task.Yield();
        throw exception;
    }

    private static async Task<ServiceProvider> StartAsync(params BackgroundService[] backgroundServices)
    {
        var collection = new ServiceCollection();
        foreach (var service in backgroundServices)
        {
            collection.AddSingleton<IHostedService>(service);
        }

        var services = collection.BuildServiceProvider();
        foreach (var service in services.GetServices<IHostedService>().Cast<BackgroundService>().ToList())
        {
            await service.StartAsync(TestContext.Current.CancellationToken);

            // The work of a service runs on the thread pool: as when the host has stopped, it has ended.
            await Task.WhenAny(service.ExecuteTask!);
        }

        return services;
    }

    private sealed class EndingService(Task end) : BackgroundService
    {
        private readonly Task _end = end;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _end;
        }
    }

    // A start that only a stop of the host ends, as the start of the server of the API can be.
    private sealed class SlowStartingService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
