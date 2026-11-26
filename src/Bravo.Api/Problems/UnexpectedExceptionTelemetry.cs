using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Telemetry;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Bravo.Api.Problems;

/// <summary>
/// Sends to the telemetry an exception that no handler before it answered, which nothing foresaw, and leaves
/// it to the default, which records it and answers 500.
/// </summary>
internal sealed class UnexpectedExceptionTelemetry(IExceptionTelemetry telemetry) : IExceptionHandler
{
    private readonly IExceptionTelemetry _telemetry = telemetry;

    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _telemetry.Track(exception);

        return ValueTask.FromResult(false);
    }
}
