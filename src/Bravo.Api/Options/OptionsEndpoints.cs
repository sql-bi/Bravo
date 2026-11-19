using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Options;

internal static class OptionsEndpoints
{
    private const string Tag = "Options";

    public static void MapOptions(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/options", GetOptions)
            .WithName(nameof(GetOptions))
            .WithTags(Tag)
            .WithSummary("Reads the options of the user.");

        routes.MapPut("/options", UpdateOptionsAsync)
            .WithName("UpdateOptions")
            .WithTags(Tag)
            .WithSummary(
                "Replaces the options of the user, and returns them as they were kept. When they cannot be kept, "
                + "the ones before stay.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static Ok<UserOptions> GetOptions(IUserSettingsStore store)
    {
        return TypedResults.Ok(UserOptions.FromSettings(store.Current));
    }

    private static async Task<Ok<UserOptions>> UpdateOptionsAsync(
        UserOptions options,
        IUserSettingsStore store,
        CancellationToken cancellationToken)
    {
        await store.ReplaceAsync(options.ToSettings(), cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(UserOptions.FromSettings(store.Current));
    }
}
