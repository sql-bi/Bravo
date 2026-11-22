using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Records the exception of a task that failed and that nothing awaited. The process goes on: the runtime
/// raises the exception after a collection of the memory, long after the work that failed, and on the thread
/// of the finalizers, where nothing may wait.
/// </summary>
internal sealed partial class UnobservedTaskExceptionLog : IDisposable
{
    private readonly ILogger _logger;

    internal UnobservedTaskExceptionLog(ILogger logger)
    {
        _logger = logger;
    }

    /// <returns>What stops the records: dispose it before the log.</returns>
    public static UnobservedTaskExceptionLog Attach(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var log = new UnobservedTaskExceptionLog(loggerFactory.CreateLogger<UnobservedTaskExceptionLog>());
        TaskScheduler.UnobservedTaskException += log.OnUnobservedTaskException;
        return log;
    }

    public void Dispose()
    {
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    internal void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogUnobservedTaskException(_logger, e.Exception);
    }
}
