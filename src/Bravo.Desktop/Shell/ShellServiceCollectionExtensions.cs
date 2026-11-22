using Bravo.Core.Browser;
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
    public static IServiceCollection AddShell(this IServiceCollection services, BuildIdentity identity)
    {
        services.AddSingleton(identity);
        services.AddSingleton<ShellExit>();
        services.AddSingleton<ISystemAppMode, RegistrySystemAppMode>();
        services.AddSingleton<IWindowTitleSettings, RegistryWindowTitleSettings>();
        services.AddSingleton<IMainWindow, MainWindow>();
        services.AddSingleton<MainWindowDispatcher>();
        services.AddSingleton<MainWindowRunner>();
        services.AddSingleton<IBrowserLauncher, DefaultBrowserLauncher>();

        return services;
    }
}
