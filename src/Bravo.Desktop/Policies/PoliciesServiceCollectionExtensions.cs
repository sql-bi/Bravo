using System;
using Bravo.Core.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Desktop.Policies;

internal static class PoliciesServiceCollectionExtensions
{
    /// <summary>
    /// Adds the policies of the application, which were read before the host was built and do not change while it
    /// runs.
    /// </summary>
    public static IServiceCollection AddPolicies(this IServiceCollection services, ApplicationPolicies policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        services.AddSingleton(policies);

        return services;
    }
}
