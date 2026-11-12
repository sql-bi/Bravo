using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Events;

internal static class EventsEndpoints
{
    public static void MapEvents(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/events", GetEventsAsync)
            .WithName("GetEvents")
            .WithTags("Events")
            .WithSummary("Reads the events of the host after a number, waiting for the next one when there is none.")
            .ProducesValidationProblem();
    }

    private static async Task<Ok<EventBatch>> GetEventsAsync(
        EventLog log,
        CancellationToken cancellationToken,
        [Description("The number of the last event already read. Without it, the answer has every event still kept.")]
        [Range(0, int.MaxValue)]
        int after = 0)
    {
        var batch = await log.ReadAsync(after, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(batch);
    }
}
