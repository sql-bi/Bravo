using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Api.Tests;

internal static class HttpResponseMessageExtensions
{
    /// <summary>
    /// The body as the JSON that travelled, not as the types of the API: a test reads what a client reads.
    /// </summary>
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The member <c>code</c> of a problem details body, or null when the answer is not one.
    /// </summary>
    public static async Task<string?> ReadProblemCodeAsync(this HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/problem+json")
        {
            return null;
        }

        var problem = await response.ReadJsonAsync();
        return problem.GetProperty("code").GetString();
    }
}
