using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;
using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Configuration;

namespace Sqlbi.Bravo.Infrastructure.PowerBI
{
    internal static class ServiceCollectionExtensions
    {
        internal const string PowerBIApiHttpClientName = "PowerBIApi";

        public static IServiceCollection AddPowerBIServices(this IServiceCollection services)
        {
            services.AddHttpClient(PowerBIApiHttpClientName, (client) =>
            {
                client.DefaultRequestHeaders.Accept.Clear(); // No default Accept header required
                client.Timeout = TimeSpan.FromMinutes(3);
            });

            services.AddSingleton<ILocalConfigurationReader, LocalConfigurationReader>();

            services.AddSingleton<ICloudConfigurationService, CloudConfigurationService>();
            services.AddSingleton<ICloudAuthenticationClient, CloudAuthenticationClient>();
            services.AddSingleton<ICloudAuthenticationService, CloudAuthenticationService>();
            services.AddSingleton<ICloudApiClient, CloudApiClient>();

            return services;
        }
    }
}
