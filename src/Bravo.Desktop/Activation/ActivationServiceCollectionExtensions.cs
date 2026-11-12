using Bravo.Core.Activation;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Desktop.Activation;

internal static class ActivationServiceCollectionExtensions
{
    // More starts than a user makes before the window can serve them: a start beyond these is told to retry.
    private const int Capacity = 16;

    /// <summary>
    /// Adds the queue of the activations and its consumer.
    /// </summary>
    public static IServiceCollection AddActivation(this IServiceCollection services)
    {
        services.AddSingleton<IActivationChannel>(_ => new ActivationChannel(Capacity));
        services.AddSingleton<ActivationPump>();

        return services;
    }
}
