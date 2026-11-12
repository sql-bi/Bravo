using Bravo.Core.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Application;

internal static class ApplicationEndpoints
{
    public static void MapApplication(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/application", GetApplication)
            .WithName(nameof(GetApplication))
            .WithTags("Application")
            .WithSummary("Reads what the application is: its version, the culture of the user and the kind of its build.");
    }

    private static Ok<ApplicationDetails> GetApplication(ApplicationInfo application)
    {
        return TypedResults.Ok(ApplicationDetails.FromInfo(application));
    }
}
