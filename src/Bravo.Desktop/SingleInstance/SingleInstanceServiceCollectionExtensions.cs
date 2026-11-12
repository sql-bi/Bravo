using System;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Desktop.SingleInstance;

internal static class SingleInstanceServiceCollectionExtensions
{
    /// <summary>
    /// Adds what the owner of the instance needs to take the requests of the processes started after it.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="ownership">
    /// Acquired before the host exists. The container uses this instance and does not dispose it: the entry
    /// point releases the ownership, after the host.
    /// </param>
    /// <param name="options">The limits that the arbitration of this process used.</param>
    public static IServiceCollection AddSingleInstance(
        this IServiceCollection services,
        InstanceOwnership ownership,
        SingleInstanceOptions options)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(ownership);
        services.AddSingleton(options);
        services.AddSingleton<IHandOverReceiver, ActivationHandOverReceiver>();
        services.AddSingleton<HandOverListener>();
        services.AddHostedService<HandOverListenerService>();

        return services;
    }
}
