using System;
using System.Collections.Immutable;
using Bravo.Core.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bravo.Api.Problems;

/// <summary>
/// The member <c>code</c> of the problem details: a PascalCase name that never changes meaning. The codes are
/// a closed list, which the document of the API gives to the user interface: no answer carries another one.
/// </summary>
internal static class ProblemCodes
{
    public const string MemberName = "code";

    /// <summary>
    /// Every code that an error answer can carry: the ones of the API, then the ones of the use cases.
    /// </summary>
    public static ImmutableArray<string> All { get; } =
        [.. Enum.GetNames<ApiProblemCode>(), .. Enum.GetNames<ErrorCode>()];

    /// <summary>
    /// Gives every problem a code of the list. A problem that has none, or one that the list does not have,
    /// gets the code of its status.
    /// </summary>
    public static void Apply(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        if (problem.Extensions.TryGetValue(MemberName, out var code) && code is string name && All.Contains(name))
        {
            return;
        }

        var selected = Select(problem, context.HttpContext.Response.StatusCode);
        problem.Extensions[MemberName] = selected.ToString();
    }

    private static ApiProblemCode Select(ProblemDetails problem, int responseStatus)
    {
        if (problem is HttpValidationProblemDetails)
        {
            return ApiProblemCode.ValidationFailed;
        }

        var status = problem.Status ?? responseStatus;
        return status switch
        {
            StatusCodes.Status400BadRequest => ApiProblemCode.InvalidRequest,
            StatusCodes.Status401Unauthorized => ApiProblemCode.Unauthorized,
            StatusCodes.Status404NotFound => ApiProblemCode.NotFound,
            StatusCodes.Status405MethodNotAllowed => ApiProblemCode.MethodNotAllowed,
            StatusCodes.Status415UnsupportedMediaType => ApiProblemCode.UnsupportedMediaType,
            StatusCodes.Status500InternalServerError => ApiProblemCode.Unexpected,
            _ => ApiProblemCode.HttpError,
        };
    }
}
