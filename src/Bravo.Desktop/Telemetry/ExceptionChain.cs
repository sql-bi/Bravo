using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bravo.Desktop.Diagnostics;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// An exception with the ones it holds, inner and aggregated, within the limits of Application Insights.
/// </summary>
internal static class ExceptionChain
{
    // The limits of the SDK of Application Insights, which the ingestion expects.
    internal const int MaxExceptions = 10;

    internal const int MaxStackLength = 32_768;

    /// <summary>
    /// The exception, then the ones that it holds, depth first, at most <see cref="MaxExceptions"/>. An aggregate
    /// of one exception is that exception. A message loses the text marked as sensitive and the personal names,
    /// and the path of a source file the personal names.
    /// </summary>
    public static IReadOnlyList<ExceptionLink> Create(Exception exception, PersonalNames personalNames)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(personalNames);

        // As 1.0.x: the exception that failed, not the aggregate of a task that holds it alone.
        if (exception is AggregateException aggregate)
        {
            exception = aggregate.GetBaseException();
        }

        var links = new List<ExceptionLink>();
        Add(exception, outerId: 0, links, personalNames);

        return [.. links];
    }

    private static void Add(Exception exception, int outerId, List<ExceptionLink> links, PersonalNames personalNames)
    {
        if (links.Count == MaxExceptions)
        {
            return;
        }

        var id = links.Count + 1;
        var (frames, hasFullStack) = GetFrames(exception, personalNames);
        var message = personalNames.RemoveFrom(SensitiveTags.Redact(exception.Message));
        links.Add(new ExceptionLink(id, outerId, exception.GetType().FullName!, message, hasFullStack, frames));

        if (exception is AggregateException aggregate)
        {
            foreach (var aggregated in aggregate.InnerExceptions)
            {
                Add(aggregated, id, links, personalNames);
            }
        }
        else if (exception.InnerException is { } innerException)
        {
            Add(innerException, id, links, personalNames);
        }
    }

    // The frames of both ends, as the SDK keeps them when a stack is too long: where the exception was thrown,
    // and where the work that threw it began.
    private static (IReadOnlyList<ExceptionFrame> Frames, bool HasFullStack) GetFrames(
        Exception exception,
        PersonalNames personalNames)
    {
        var frames = new StackTrace(exception, fNeedFileInfo: true).GetFrames();
        var kept = new List<ExceptionFrame>(frames.Length);
        var length = 0;
        for (var step = 0; step < frames.Length; step++)
        {
            var level = step % 2 == 0 ? frames.Length - 1 - (step / 2) : step / 2;
            var frame = ToFrame(frames[level], level, personalNames);
            length += frame.Length;
            if (length > MaxStackLength)
            {
                return (kept, false);
            }

            kept.Insert(kept.Count / 2, frame);
        }

        return (kept, true);
    }

    private static ExceptionFrame ToFrame(StackFrame frame, int level, PersonalNames personalNames)
    {
        var method = frame.GetMethod();
        var methodName = method is null
            ? "unknown"
            : method.DeclaringType is { } type ? $"{type.FullName}.{method.Name}" : method.Name;
        var assembly = method?.Module.Assembly.FullName ?? "unknown";
        var fileName = frame.GetFileName() is { } path ? personalNames.RemoveFrom(path) : null;

        return new ExceptionFrame(level, methodName, assembly, fileName, frame.GetFileLineNumber());
    }
}
