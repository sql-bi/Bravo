using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Desktop.Diagnostics;

internal static class DiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Makes the host write to the log that the start opened before it: one log with the same rules from the
    /// start of the process to its end, which the host does not close.
    /// </summary>
    public static IServiceCollection AddHostLog(this IServiceCollection services, HostLog log)
    {
        services.Replace(ServiceDescriptor.Singleton(log.Factory));
        services.AddSingleton(log.Buffer);

        return services;
    }
}
