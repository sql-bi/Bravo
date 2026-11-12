using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bravo.Api.Problems;

internal static class ProblemsServiceCollectionExtensions
{
    /// <summary>
    /// Makes every error answer a problem details body with a code, whatever produced it: a route, the
    /// validation, a middleware, an exception.
    /// </summary>
    public static IServiceCollection AddApiProblems(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddOptions<ProblemDetailsOptions>().Configure<IOptions<JsonOptions>>(Customize);
        services.AddExceptionHandler<BravoExceptionHandler>();

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
