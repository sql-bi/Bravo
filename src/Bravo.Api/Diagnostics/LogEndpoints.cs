using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Diagnostics;

internal static class LogEndpoints
{
    public static void MapLog(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/log", GetLog)
            .WithName(nameof(GetLog))
            .WithTags("Log")
            .WithSummary("Reads the records of the log of the host after a number, among the last ones that it keeps.")
            .ProducesValidationProblem();
    }

    private static Ok<LogBatch> GetLog(
        LogBuffer log,
        [Description("The number of the last record already read. Without it, the answer has every record still kept.")]
        [Range(0, int.MaxValue)]
        int after = 0)
    {
        return TypedResults.Ok(log.Read(after));
    }
}
