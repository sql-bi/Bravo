using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Network;

internal static class NetworkEndpoints
{
    private const string Tag = "Network";

    public static void MapNetwork(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/network/test", TestConnectionAsync)
            .WithName("TestConnection")
            .WithTags(Tag)
            .WithSummary(
                "Asks each site that the application needs for an answer, with the proxy settings as they are kept "
                + "now, and tells for each one the proxy server that the request goes through and, when the site "
                + "did not answer, why. A site that does not answer is a result, not an error.");
    }

    private static async Task<Ok<ConnectionTestResult>> TestConnectionAsync(
        ConnectionTest connectionTest,
        CancellationToken cancellationToken)
    {
        var connections = await connectionTest.RunAsync(cancellationToken).ConfigureAwait(false);
        var result = new ConnectionTestResult { Sites = [.. connections.Select(SiteTestResult.FromConnection)] };

        return TypedResults.Ok(result);
    }
}
