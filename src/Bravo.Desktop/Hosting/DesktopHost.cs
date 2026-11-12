using System;
using System.Threading.Tasks;
using Bravo.Api;
using Bravo.Api.Server;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Settings;
using Bravo.Desktop.Shell;
using Bravo.Desktop.SingleInstance;
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
    public static WebApplication Build(
        BuildIdentity identity,
        InstanceOwnership ownership,
        SingleInstanceOptions singleInstanceOptions,
        IFatalExceptionHandler fatalExceptionHandler)
    {
        // An empty builder: no configuration file, no environment variable, no logging provider and no
        // server arrives with it. The host has what is added here, wherever the process was started from.
        var options = new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory };
        var builder = WebApplication.CreateEmptyBuilder(options);

        // A service that cannot be created fails the start, not the first use.
        builder.Host.UseDefaultServiceProvider(provider =>
        {
            provider.ValidateOnBuild = true;
            provider.ValidateScopes = true;
        });

        builder.WebHost.UseLoopbackServer();

        builder.Services.AddDesktopLifetime();
        builder.Services.AddShell(identity, fatalExceptionHandler);
        builder.Services.AddActivation();
        builder.Services.AddSingleInstance(ownership, singleInstanceOptions);
        builder.Services.AddUserSettings();
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
    public static void Run(WebApplication host, ActivationRequest firstRequest)
    {
        // The first start is an activation like the others, and the first of the queue.
        _ = host.Services.GetRequiredService<IActivationChannel>().Post(firstRequest);

        host.Start();
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
    }
}
