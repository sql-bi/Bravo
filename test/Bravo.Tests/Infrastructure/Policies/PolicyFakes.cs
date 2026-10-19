using System;
using System.Collections.Generic;
using Sqlbi.Bravo.Infrastructure.Policies;

namespace Bravo.Tests.Infrastructure.Policies;

internal sealed class FakePolicySource : IPolicySource
{
    private readonly Dictionary<string, object> _values = new();
    public int DisposeCount { get; private set; }
    public Exception? ReadFailure { get; set; }

    public void Set(string name, object value) => _values[name] = value;

    public int? GetInt(string name)
    {
        if (ReadFailure is not null)
            throw ReadFailure;

        return _values.TryGetValue(name, out var value) && value is int intValue ? intValue : null;
    }

    public string? GetString(string name)
        => _values.TryGetValue(name, out var value) ? value as string : null;

    public void Dispose() => DisposeCount++;
}

internal sealed class FakePolicySourceFactory(Func<PolicyScope, IPolicySource> open) : IPolicySourceFactory
{
    public IPolicySource Open(PolicyScope scope) => open(scope);
}

internal sealed class FakePolicyReader : IPolicyReader
{
    public int ReadCount { get; private set; }
    public PolicySnapshot Snapshot { get; set; } = PolicySnapshot.NotConfigured;
    public Func<PolicySnapshot>? OnRead { get; set; }

    public PolicySnapshot Read()
    {
        ReadCount++;
        return OnRead?.Invoke() ?? Snapshot;
    }
}
