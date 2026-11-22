using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// A provider whose failures stay inside it: a log that cannot be written never fails the code that writes to it.
/// It passes on the scopes of the factory, which carry the trace, to a provider that writes them.
/// </summary>
internal sealed class GuardedLoggerProvider(ILoggerProvider inner) : ILoggerProvider, ISupportExternalScope
{
    private readonly ILoggerProvider _inner = inner;

    public ILogger CreateLogger(string categoryName)
    {
        return new GuardedLogger(_inner.CreateLogger(categoryName));
    }

    public void Dispose()
    {
        _inner.Dispose();
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        if (_inner is ISupportExternalScope inner)
        {
            inner.SetScopeProvider(scopeProvider);
        }
    }
}
