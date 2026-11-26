using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bravo.Api.Problems;

internal static class ProblemsServiceCollectionExtensions
{
    /// <summary>
    /// Makes every error answer a problem details body with a code, whatever produced it: a route, the
    /// validation, a middleware, an exception. An exception that no use case foresaw goes to the telemetry.
    /// </summary>
    public static IServiceCollection AddApiProblems(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddOptions<ProblemDetailsOptions>().Configure<IOptions<JsonOptions>>(Customize);

        // In this order: the handlers are asked one after the other until one answers.
        services.AddExceptionHandler<BravoExceptionHandler>();
        services.AddExceptionHandler<UnexpectedExceptionTelemetry>();

        return services;
    }

    private static void Customize(ProblemDetailsOptions options, IOptions<JsonOptions> json)
    {
        var namingPolicy = json.Value.SerializerOptions.PropertyNamingPolicy;
        options.CustomizeProblemDetails = context =>
        {
            if (context.ProblemDetails is HttpValidationProblemDetails validation)
            {
                ValidationErrorNames.UseJsonNames(validation, namingPolicy);
            }

            ProblemCodes.Apply(context);
        };
    }
}
