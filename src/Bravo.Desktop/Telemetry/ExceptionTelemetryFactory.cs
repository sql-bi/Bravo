using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// Builds the item of Application Insights for a chain of exceptions.
/// </summary>
internal static class ExceptionTelemetryFactory
{
    /// <param name="chain">The exceptions, whose texts already lost what is personal or sensitive.</param>
    /// <param name="context">What every item of the run carries.</param>
    /// <param name="traceId">
    /// The trace of the work that failed, which its records in the log also have, or null.
    /// </param>
    public static ExceptionTelemetry Create(
        IReadOnlyList<ExceptionLink> chain,
        TelemetryContext context,
        string? traceId)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(context);

        // Built from its details and not from the exception: the SDK would send the message and the paths of
        // the exception as they are.
        var details = chain.Select(link => new ExceptionDetailsInfo(
            link.Id,
            link.OuterId,
            link.TypeName,
            link.Message,
            link.HasFullStack,
            stack: null,
            link.Frames.Select(frame => new StackFrame(
                frame.Assembly,
                frame.FileName,
                frame.Level,
                frame.Line,
                frame.Method))));
        var item = new ExceptionTelemetry(
            details,
            severityLevel: null,
            problemId: null,
            new Dictionary<string, string>(context.Properties),
            new Dictionary<string, double>());

        item.Context.User.Id = context.UserId;
        item.Context.Session.Id = context.SessionId;
        item.Context.Device.OperatingSystem = context.OperatingSystem;
        item.Context.Component.Version = context.Version;
        item.Context.Operation.Id = traceId;

        // Both, or the SDK sends the name of the computer in the one left empty.
        item.Context.Cloud.RoleInstance = context.RoleInstance;
        item.Context.GetInternalContext().NodeName = context.RoleInstance;

        return item;
    }
}
