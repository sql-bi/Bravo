using Bravo.Core.Browser;
using Bravo.Desktop.Diagnostics;
using Bravo.Platform.Windows.Browser;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Desktop.Shell;

internal static class ShellServiceCollectionExtensions
{
    /// <summary>
    /// Adds the main window, what runs it, and the default browser, where the links of its page open.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="identity">Read before the host exists. It tells which build the window belongs to.</param>
    /// <param name="fatalExceptionHandler">
    /// Installed before the host exists. The container gets this instance, so that a service can add to what
    /// is done with an exception that escapes.
    /// </param>
    public static IServiceCollection AddShell(
        this IServiceCollection services,
        BuildIdentity identity,
        IFatalExceptionHandler fatalExceptionHandler)
    {
        services.AddSingleton(identity);
        services.AddSingleton(fatalExceptionHandler);
        services.AddSingleton<ShellExit>();
        services.AddSingleton<ISystemAppMode, RegistrySystemAppMode>();
        services.AddSingleton<IWindowTitleSettings, RegistryWindowTitleSettings>();
        services.AddSingleton<IMainWindow, MainWindow>();
        services.AddSingleton<MainWindowRunner>();
        services.AddSingleton<IBrowserLauncher, DefaultBrowserLauncher>();

        return services;
    }
}
