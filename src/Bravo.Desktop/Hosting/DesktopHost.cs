using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Bravo.Api;
using Bravo.Api.Server;
using Bravo.Core.Activation;
using Bravo.Core.Network;
using Bravo.Core.Policies;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Development;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Network;
using Bravo.Desktop.Policies;
using Bravo.Desktop.Settings;
using Bravo.Desktop.Shell;
using Bravo.Desktop.SingleInstance;
using Bravo.Desktop.Telemetry;
using Bravo.Desktop.Updates;
using Bravo.Desktop.WebView;
using Bravo.Platform.Windows.Network;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Hosting;

/// <summary>
/// The host of the application: the one place where its services are put together.
/// </summary>
internal static class DesktopHost
{
    /// <param name="identity">The build that was started.</param>
    /// <param name="dataFolder">Where the host keeps the data of the user.</param>
    /// <param name="development">What the command line of a development build asked its host for.</param>
    /// <param name="policies">What an administrator set for the application, read at the start.</param>
    /// <param name="ownership">The ownership of the instance, which the caller releases after the host.</param>
    /// <param name="singleInstanceOptions">The limits that the arbitration of this process used.</param>
    /// <param name="log">The log of the process, which the caller closes after the host.</param>
    /// <param name="session">The run, as the telemetry names it.</param>
    public static WebApplication Build(
        BuildIdentity identity,
        DataFolder dataFolder,
        DevelopmentOptions development,
        ApplicationPolicies policies,
        InstanceOwnership ownership,
        SingleInstanceOptions singleInstanceOptions,
        HostLog log,
        TelemetrySession session)
    {
        // An empty builder: no configuration file, no environment variable, no logging provider and no
        // server arrives with it. The host has what is added here, wherever the process was started from.
        var options = new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory };
        var builder = WebApplication.CreateEmptyBuilder(options);
        builder.Services.AddHostLog(log);

        // The first service of the background: it stops after the others, whose exceptions it sends.
        builder.Services.AddTelemetry(identity, dataFolder, session, development.TelemetryEndpoint);

        // A service that cannot be created fails the start, not the first use.
        builder.Host.UseDefaultServiceProvider(provider =>
        {
            provider.ValidateOnBuild = true;
            provider.ValidateScopes = true;
        });

        builder.WebHost.UseLoopbackServer();

        builder.Services.AddDesktopLifetime();
        builder.Services.AddApplicationInfo(identity);
        builder.Services.AddInstallation();
        builder.Services.AddShell(identity);
        builder.Services.AddWebView(identity, dataFolder);
        builder.Services.AddDevelopmentOptions(development);
        builder.Services.AddPolicies(policies);
        builder.Services.AddActivation();
        builder.Services.AddSingleInstance(ownership, singleInstanceOptions);
        builder.Services.AddUserSettings(dataFolder);
        builder.Services.AddUpdates(identity, development.UpdateFeedAddress);
        builder.Services.AddNetwork(identity);
        builder.Services.AddApi();

        var app = builder.Build();
        app.UseApi();

        return app;
    }

    /// <summary>
    /// Starts the host, runs the main window until it is closed, and stops the host.
    /// </summary>
    /// <param name="host">The host, which the caller disposes.</param>
    /// <param name="firstRequest">What the start of this process asked for.</param>
    /// <returns>The exit code of the process.</returns>
    public static int Run(WebApplication host, ActivationRequest firstRequest)
    {
        // The first start is an activation like the others, and the first of the queue.
        _ = host.Services.GetRequiredService<IActivationChannel>().Post(firstRequest);

        // Before any request: a client that the host does not create takes the default proxy of the process at
        // its first one. Not in Build, which the tests call in a process that they share.
        var proxy = host.Services.GetRequiredService<ProxySelection>();
        proxy.ApplyToProcess(host.Services.GetRequiredService<ProxyCredentials>());

        Start(host);
        try
        {
            var window = host.Services.GetRequiredService<IMainWindow>();
            host.Services.GetRequiredService<MainWindowRunner>().Run(window);
        }
        finally
        {
            // On the thread pool: what the services await must not resume on this thread, which has no
            // message loop any longer.
            Task.Run(() => host.StopAsync()).GetAwaiter().GetResult();
        }

        ThrowIfAServiceFailed(host.Services);

        return host.Services.GetRequiredService<ShellExit>().Code;
    }

    /// <summary>
    /// Starts the host. A service of the background that fails meanwhile stops the host, which cancels the start
    /// of the services after it: the exception of the service is thrown, not that cancellation.
    /// </summary>
    internal static void Start(WebApplication host)
    {
        try
        {
            host.Start();
        }
        catch (OperationCanceledException)
        {
            ThrowIfAServiceFailed(host.Services);
            throw;
        }
    }

    /// <summary>
    /// Throws the exception of a service of the background that failed, or, when several failed, an
    /// <see cref="AggregateException"/> of theirs, as .NET 11 does. The host stopped when the first failed, and
    /// the window closed: the process ends as it does for any exception that nothing caught, with a report.
    /// </summary>
    internal static void ThrowIfAServiceFailed(IServiceProvider services)
    {
        var failures = services.GetServices<IHostedService>()
            .OfType<BackgroundService>()
            .Select(service => service.ExecuteTask)
            .OfType<Task>()
            .Where(task => task.IsFaulted)
            .SelectMany(task => task.Exception!.InnerExceptions)
            .ToList();

        if (failures.Count == 1)
        {
            // The exception itself, with its stack, and not the aggregate of the task.
            ExceptionDispatchInfo.Throw(failures[0]);
        }

        if (failures.Count > 1)
        {
            throw new AggregateException("Services of the background failed.", failures);
        }
    }
}
