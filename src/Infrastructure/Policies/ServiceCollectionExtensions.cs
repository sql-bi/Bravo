namespace Sqlbi.Bravo.Infrastructure.Policies
{
    using Microsoft.Extensions.DependencyInjection;

    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGroupPolicies(this IServiceCollection services)
        {
            services.AddSingleton<IPolicies>(_ => PoliciesFactory.Create());

            return services;
        }
    }
}
