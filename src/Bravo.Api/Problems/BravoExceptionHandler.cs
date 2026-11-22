using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bravo.Api.Problems;

/// <summary>
/// Answers an error that a use case foresaw with its code and its message, and records it with its cause. Any
/// other exception is left to the default, which records it and answers 500, telling nothing of the exception.
/// </summary>
internal sealed partial class BravoExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<BravoExceptionHandler> logger) : IExceptionHandler
{
    // One status for every foreseen error: the request was right, and the application cannot carry it out
    // as things stand. The client chooses what to do by the code.
    public const int StatusCode = StatusCodes.Status409Conflict;

    private readonly IProblemDetailsService _problemDetails = problemDetails;
    private readonly ILogger<BravoExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BravoException known)
        {
            return false;
        }

        // The answer tells the code and the message alone: the cause is in the record, under the same trace.
        LogForeseenError(known, httpContext.Request.Method, httpContext.Request.Path, known.Code);

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
