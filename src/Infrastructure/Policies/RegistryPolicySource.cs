namespace Sqlbi.Bravo.Infrastructure.Policies
{
    using Microsoft.Win32;

    /// <summary>
    /// Adapter that bridges <see cref="IPolicySource"/> to a real <see cref="RegistryKey"/>.
    /// Intentionally a thin pass-through with no logic of its own.
    /// </summary>
    internal sealed class RegistryPolicySource(RegistryKey? key) : IPolicySource
    {
        private readonly RegistryKey? _key = key;

        public int? GetInt(string name)
            => _key?.GetValue(name) is int value ? value : null;

        public string? GetString(string name)
            => _key?.GetValue(name) as string;
    }
}
