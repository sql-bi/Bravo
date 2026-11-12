using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bravo.Api.Problems;

/// <summary>
/// The codes that the API itself gives to an error answer, in the member <c>code</c> of the problem details.
/// A code is a PascalCase name that never changes meaning; an error that a use case foresees has its own.
/// </summary>
internal static class ProblemCodes
{
    public const string MemberName = "code";

    /// <summary>
    /// The request cannot be read: a body that is not what the route expects, a parameter of the wrong type.
    /// </summary>
    public const string InvalidRequest = "InvalidRequest";

    /// <summary>
    /// The request was read, and a value in it is not allowed: the member <c>errors</c> says which.
    /// </summary>
    public const string ValidationFailed = "ValidationFailed";

    /// <summary>
    /// The token of the API is missing or wrong.
    /// </summary>
    public const string Unauthorized = "Unauthorized";

    public const string NotFound = "NotFound";

    public const string MethodNotAllowed = "MethodNotAllowed";

    public const string UnsupportedMediaType = "UnsupportedMediaType";

    /// <summary>
    /// The host failed in a way that nobody foresaw.
    /// </summary>
    public const string Unexpected = "Unexpected";

    /// <summary>
    /// Gives a code to a problem that has none, so that every error answer has one.
    /// </summary>
    public static void Apply(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        if (problem.Extensions.ContainsKey(MemberName))
        {
            return;
        }

        problem.Extensions[MemberName] = Select(problem, context.HttpContext.Response.StatusCode);
    }

    private static string Select(ProblemDetails problem, int responseStatus)
    {
        if (problem is HttpValidationProblemDetails)
        {
            return ValidationFailed;
        }

        var status = problem.Status ?? responseStatus;
        return status switch
        {
            StatusCodes.Status400BadRequest => InvalidRequest,
            StatusCodes.Status401Unauthorized => Unauthorized,
            StatusCodes.Status404NotFound => NotFound,
            StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
            StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
            StatusCodes.Status500InternalServerError => Unexpected,
            _ => string.Create(CultureInfo.InvariantCulture, $"Http{status}"),
        };
    }
}
