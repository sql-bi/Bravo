using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Updates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Releases;

internal static class ReleasesEndpoints
{
    public static void MapReleases(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/releases/latest", GetLatestReleaseAsync)
            .WithName("GetLatestRelease")
            .WithTags("Releases")
            .WithSummary(
                "Reads the latest release from the update feed: whether it is newer than the application, and the "
                + "address of its file for this copy. The host also tells a newer release with the beep of the system.")
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Ok<LatestRelease>> GetLatestReleaseAsync(
        UpdateCheck updateCheck,
        CancellationToken cancellationToken)
    {
        var result = await updateCheck.CheckAsync(cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(LatestRelease.FromResult(result));
    }
}
