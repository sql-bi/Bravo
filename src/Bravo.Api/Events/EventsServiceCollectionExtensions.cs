using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Api.Events;

internal static class EventsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the log of the events and what publishes to it.
    /// </summary>
    public static IServiceCollection AddApiEvents(this IServiceCollection services)
    {
        services.TryAddSingleton(new EventLogOptions());
        services.AddSingleton<EventLog>();
        services.AddSingleton<IHostEventPublisher, HostEventPublisher>();

        return services;
    }
}
