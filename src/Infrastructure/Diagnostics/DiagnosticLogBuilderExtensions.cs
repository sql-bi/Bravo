using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

internal static class DiagnosticLogBuilderExtensions
{
    public static ILoggingBuilder AddDiagnosticLog(
        this ILoggingBuilder builder,
        DiagnosticLogStore store,
        TimeProvider timeProvider)
    {
        var provider = new DiagnosticLogProvider(store, timeProvider);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(provider));

        return builder;
    }
}
