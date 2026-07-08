using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.Infrastructure.Services.PowerBI;

namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        //public static void AddSingletonIfNotRegistered<TService, TImplementation>(this IServiceCollection services)
        //    where TService : class
        //    where TImplementation : class, TService
        //{
        //    if (!services.Any(sd => sd.ServiceType == typeof(TService)))
        //    {
        //        services.AddSingleton<TService, TImplementation>();
        //    }
        //}

        public static IServiceCollection AddPBICloudServices(this IServiceCollection services)
        {
            services.AddSingleton<IPBILocalConfigurationReader, PBILocalConfigurationReader>();
            services.AddSingleton<IPBICloudConfigurationService, PBICloudConfigurationService>();
            services.AddSingleton<IPBICloudAuthenticationService, PBICloudAuthenticationService>();
            services.AddSingleton<IPBICloudService, PBICloudService>();

            return services;
        }
    }
}
