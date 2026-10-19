using System;

namespace Sqlbi.Bravo.Infrastructure.Policies;

public interface IPolicyService
{
    /// <summary>
    /// Gets the policy snapshot initialized once for this service instance.
    /// </summary>
    PolicySnapshot Current { get; }
}

internal sealed class PolicyService : IPolicyService
{
    private readonly IPolicyReader _reader;
    private readonly Lazy<PolicySnapshot> _current;

    public PolicyService(IPolicyReader reader)
    {
        _reader = reader;
        _current = new Lazy<PolicySnapshot>(_reader.Read);
    }

    public PolicySnapshot Current => _current.Value;
}
