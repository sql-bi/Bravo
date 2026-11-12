using Bravo.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Desktop.Development;

internal static class DevelopmentServiceCollectionExtensions
{
    /// <summary>
    /// Adds what the command line of a development build asked for. With <see cref="DevelopmentOptions.None"/>,
    /// as in a release build, it adds nothing.
    /// </summary>
    public static IServiceCollection AddDevelopmentOptions(this IServiceCollection services, DevelopmentOptions options)
    {
        if (options.AllowedOrigin is { } origin)
        {
            services.Configure<ApiOptions>(api => api.AllowedOrigins.Add(origin));
        }

        if (options.ConnectionFilePath is not null)
        {
            services.AddSingleton(options);
            services.AddHostedService<ConnectionFileWriter>();
        }

        return services;
    }
}
