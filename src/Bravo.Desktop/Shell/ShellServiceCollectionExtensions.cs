using Bravo.Desktop.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Desktop.Shell;

internal static class ShellServiceCollectionExtensions
{
    /// <summary>
    /// Adds the main window and what runs it.
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
        services.AddSingleton<IMainWindow, MainWindow>();
        services.AddSingleton<MainWindowRunner>();

        return services;
    }
}
