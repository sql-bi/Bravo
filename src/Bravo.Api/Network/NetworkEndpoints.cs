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
        routes.MapPost("/network/proxy-credentials/prompt", PromptProxyCredentialsAsync)
            .WithName("PromptProxyCredentials")
            .WithTags(Tag)
            .WithSummary(
                "Asks the user for the credentials of the proxy server in a dialog of Windows, and keeps what the "
                + "user enters in the Windows Credential Manager. It answers when the user has closed the dialog.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        routes.MapDelete("/network/proxy-credentials", DeleteProxyCredentials)
            .WithName(nameof(DeleteProxyCredentials))
            .WithTags(Tag)
            .WithSummary("Forgets the credentials of the proxy server that the user entered. With none kept it does nothing.")
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Ok<ProxyCredentialsPromptResult>> PromptProxyCredentialsAsync(
        ProxyCredentialsPrompt prompt,
        CancellationToken cancellationToken)
    {
        var isEntered = await prompt.RunAsync(cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(new ProxyCredentialsPromptResult { Entered = isEntered });
    }

    private static NoContent DeleteProxyCredentials(IProxyCredentialStore store)
    {
        store.Delete();

        return TypedResults.NoContent();
    }
}
