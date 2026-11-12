using System;
using System.Threading.Tasks;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Shell;
using Bravo.Desktop.SingleInstance;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Hosting;

/// <summary>
/// The host of the application: the one place where its services are put together.
/// </summary>
internal static class DesktopHost
{
    public static IHost Build(
        InstanceOwnership ownership,
        SingleInstanceOptions singleInstanceOptions,
        IFatalExceptionHandler fatalExceptionHandler)
    {
        // An empty builder: no configuration file, no environment variable and no logging provider arrives
        // with it. The host has what is added here, wherever the process was started from.
        var settings = new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory };
        var builder = Host.CreateEmptyApplicationBuilder(settings);

        // A service that cannot be created fails the start, not the first use.
        var providerOptions = new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true };
        builder.ConfigureContainer(new DefaultServiceProviderFactory(providerOptions));

        builder.Services.AddDesktopLifetime();
        builder.Services.AddShell(fatalExceptionHandler);
        builder.Services.AddActivation();
        builder.Services.AddSingleInstance(ownership, singleInstanceOptions);

        return builder.Build();
    }

    /// <summary>
    /// Starts the host, runs the main window until it is closed, and stops the host.
    /// </summary>
    /// <param name="host">The host, which the caller disposes.</param>
    /// <param name="firstRequest">What the start of this process asked for.</param>
    public static void Run(IHost host, ActivationRequest firstRequest)
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
