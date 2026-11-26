using System;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Telemetry;
using Microsoft.AspNetCore.Http;

namespace Bravo.Api.Problems;

/// <summary>
/// Sends to the telemetry an exception that escapes the handler of the exceptions, which cannot answer once the
/// answer has begun: the server records it and ends the connection. The handler sends every other one
/// (<see cref="UnexpectedExceptionTelemetry"/>). A request that the client gave up is not a defect.
/// </summary>
internal sealed class UnansweredExceptionTelemetry(RequestDelegate next, IExceptionTelemetry telemetry)
{
    private readonly RequestDelegate _next = next;
    private readonly IExceptionTelemetry _telemetry = telemetry;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsUnanswered(context, exception))
        {
            _telemetry.Track(exception);
            throw;
        }
    }

    private static bool IsUnanswered(HttpContext context, Exception exception)
    {
        return context.Response.HasStarted
            && !context.RequestAborted.IsCancellationRequested
            && exception is not BravoException;
    }
}
