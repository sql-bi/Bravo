using Bravo.Core.Policies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api.Policies;

internal static class PoliciesEndpoints
{
    public static void MapPolicies(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/policies", GetPolicies)
            .WithName(nameof(GetPolicies))
            .WithTags("Policies")
            .WithSummary(
                "Reads the policies that an administrator set for the application. They apply over the options of "
                + "the user, which keep the choices of the user, and do not change while the application runs.");
    }

    private static Ok<PolicyDetails> GetPolicies(ApplicationPolicies policies)
    {
        return TypedResults.Ok(PolicyDetails.FromPolicies(policies));
    }
}
