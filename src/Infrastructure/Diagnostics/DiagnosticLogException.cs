using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// One exception of the chain kept with a <see cref="DiagnosticLogEntry"/>.
/// </summary>
/// <param name="Details">
/// The public properties the exception type adds to <see cref="System.Exception"/>, such as an error code or a status
/// code, which the type usually writes in its <see cref="object.ToString"/> override.
/// </param>
public sealed record DiagnosticLogException(
    string Type,
    string Message,
    string? StackTrace,
    IReadOnlyDictionary<string, string?>? Details)
{
    public static IReadOnlyList<DiagnosticLogException>? CreateChain(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        var chain = new List<DiagnosticLogException>();
        var pending = new Stack<Exception>();
        pending.Push(exception);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            chain.Add(Create(current));

            // An aggregate keeps every inner exception, not only the first one that InnerException returns.
            if (current is AggregateException aggregate)
            {
                for (var index = aggregate.InnerExceptions.Count - 1; index >= 0; index--)
                {
                    pending.Push(aggregate.InnerExceptions[index]);
                }
            }
            else if (current.InnerException is not null)
            {
                pending.Push(current.InnerException);
            }
        }

        return chain;
    }

    private static DiagnosticLogException Create(Exception exception)
    {
        var type = exception.GetType();
        var typeName = type.FullName ?? type.Name;

        return new DiagnosticLogException(typeName, exception.Message, exception.StackTrace, ReadDetails(exception));
    }

    private static Dictionary<string, string?>? ReadDetails(Exception exception)
    {
        var details = new Dictionary<string, string?>();

        foreach (var property in exception.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!IsDetail(property))
            {
                continue;
            }

            try
            {
                var value = property.GetValue(exception);
                if (value is not null)
                {
                    details[property.Name] = value.ToString();
                }
            }
            catch (Exception)
            {
                // A property that fails to read is left out; the rest of the record is still useful.
            }
        }

        if (details.Count == 0)
        {
            return null;
        }

        return details;
    }

    private static bool IsDetail(PropertyInfo property)
    {
        if (property.DeclaringType == typeof(Exception) || property.GetIndexParameters().Length > 0)
        {
            return false;
        }

        // Inner exceptions are already entries of the chain.
        var propertyType = property.PropertyType;
        return !typeof(Exception).IsAssignableFrom(propertyType) && !typeof(IEnumerable<Exception>).IsAssignableFrom(propertyType);
    }
}
