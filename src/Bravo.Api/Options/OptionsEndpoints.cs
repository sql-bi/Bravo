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

        routes.MapPut("/options", UpdateOptions)
            .WithName(nameof(UpdateOptions))
            .WithTags(Tag)
            .WithSummary("Replaces the options of the user, and returns them as they were kept.")
            .ProducesValidationProblem();
    }

    private static Ok<UserOptions> GetOptions(IUserSettingsStore store)
    {
        return TypedResults.Ok(UserOptions.FromSettings(store.Current));
    }

    private static Ok<UserOptions> UpdateOptions(UserOptions options, IUserSettingsStore store)
    {
        store.Replace(options.ToSettings());

        return TypedResults.Ok(UserOptions.FromSettings(store.Current));
    }
}
