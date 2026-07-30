using Microsoft.Extensions.DependencyInjection;

namespace Sqlbi.Bravo.Infrastructure.Policies;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGroupPolicies(this IServiceCollection services)
    {
        services.AddSingleton<IPolicies>(_ => PoliciesFactory.Create());

        return services;
    }
}
