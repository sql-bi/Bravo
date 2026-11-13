using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bravo.Api.Problems;

/// <summary>
/// Answers an error that a use case foresaw with its code and its message. Any other exception is left to
/// the default, which answers 500 and tells nothing of the exception.
/// </summary>
internal sealed class BravoExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    // One status for every foreseen error: the request was right, and the application cannot carry it out
    // as things stand. The client chooses what to do by the code.
    public const int StatusCode = StatusCodes.Status409Conflict;

    private readonly IProblemDetailsService _problemDetails = problemDetails;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BravoException known)
        {
            return false;
        }

        var problem = new ProblemDetails { Status = StatusCode, Detail = known.Message };
        problem.Extensions[ProblemCodes.MemberName] = known.Code.ToString();
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = known,
        };

        httpContext.Response.StatusCode = StatusCode;

        // False when the client accepts no JSON: the status alone is the answer then.
        _ = await _problemDetails.TryWriteAsync(context).ConfigureAwait(false);

        return true;
    }
}
