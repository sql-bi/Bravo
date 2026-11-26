using Bravo.Core.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Telemetry;

internal static class TelemetryEndpoints
{
    public static void MapTelemetry(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/telemetry", GetTelemetry)
            .WithName(nameof(GetTelemetry))
            .WithTags("Telemetry")
            .WithSummary(
                "Reads where the page sends its telemetry and what its items carry. Whether it sends it is the "
                + "option of the user, or the policy of an administrator.");
    }

    private static Ok<TelemetryDetails> GetTelemetry(PageTelemetry telemetry)
    {
        return TypedResults.Ok(TelemetryDetails.FromPageTelemetry(telemetry));
    }
}
