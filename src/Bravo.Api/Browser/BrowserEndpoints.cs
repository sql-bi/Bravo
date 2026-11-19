using Bravo.Core.Browser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Browser;

internal static class BrowserEndpoints
{
    public static void MapBrowser(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/browser/open", OpenInBrowser)
            .WithName(nameof(OpenInBrowser))
            .WithTags("Browser")
            .WithSummary("Opens a page in the default browser of the user: only a page on a site that the application trusts.")
            .ProducesValidationProblem();
    }

    private static NoContent OpenInBrowser(OpenInBrowserRequest request, IBrowserLauncher browserLauncher)
    {
        browserLauncher.Open(request.Address);

        return TypedResults.NoContent();
    }
}
