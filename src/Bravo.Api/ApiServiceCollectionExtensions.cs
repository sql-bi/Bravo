using System;
using Bravo.Api.Events;
using Bravo.Api.Json;
using Bravo.Api.Problems;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Api;

public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services of the API. The server is added apart, with
    /// <see cref="LoopbackServerWebHostBuilderExtensions.UseLoopbackServer"/>, and the use cases that the
    /// routes call are added by the application.
    /// </summary>
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<ApiOptions>();
        services.AddRoutingCore();
        services.AddSingleton<IApiAddressProvider, ServerAddressProvider>();

        services.AddApiJson();
        services.AddApiProblems();
        services.AddApiSecurity();
        services.AddApiEvents();

        // Called here because the validation is generated for the routes of the assembly that calls it.
        services.AddValidation();

        return services;
    }
}
