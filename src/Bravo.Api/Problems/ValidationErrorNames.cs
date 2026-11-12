using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Bravo.Api.Problems;

/// <summary>
/// The validation names a value as the code does, <c>Proxy.Address</c>. A client knows it as it travels,
/// <c>proxy.address</c>: the member <c>errors</c> of a problem uses the names of the JSON.
/// </summary>
internal static class ValidationErrorNames
{
    public static void UseJsonNames(HttpValidationProblemDetails problem, JsonNamingPolicy? namingPolicy)
    {
        if (namingPolicy is null)
        {
            return;
        }

        var errors = problem.Errors.ToArray();
        problem.Errors.Clear();
        foreach (var (name, messages) in errors)
        {
            problem.Errors[ToJsonName(name, namingPolicy)] = messages;
        }
    }

    // A name is a path: each part between dots is the name of a member, with the index of an item after it.
    private static string ToJsonName(string name, JsonNamingPolicy namingPolicy)
    {
        return string.Join('.', name.Split('.').Select(namingPolicy.ConvertName));
    }
}
