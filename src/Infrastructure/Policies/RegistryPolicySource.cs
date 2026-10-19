using Microsoft.Win32;

namespace Sqlbi.Bravo.Infrastructure.Policies;

/// <summary>
/// Owns a read-only registry key containing raw policy values.
/// </summary>
internal sealed class RegistryPolicySource(RegistryKey? key) : IPolicySource
{
    private readonly RegistryKey? _key = key;

    public int? GetInt(string name)
        => _key?.GetValue(name) is int value ? value : null;

    public string? GetString(string name)
        => _key?.GetValue(name) as string;

    public void Dispose() => _key?.Dispose();
}
