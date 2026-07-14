namespace Sqlbi.Bravo.Infrastructure.Policies
{
    using Microsoft.Win32;

    /// <summary>
    /// Abstraction over a single raw policy value store (e.g. a registry key), so that
    /// <see cref="Policies"/>' parsing/precedence logic does not depend on <see cref="RegistryKey"/>
    /// directly and can be unit tested against a fake, without touching the real registry.
    /// </summary>
    internal interface IPolicySource
    {
        int? GetInt(string name);
        string? GetString(string name);
    }

    /// <summary>
    /// Typed reading conventions shared by every <see cref="IPolicySource"/>: a policy is a
    /// DWORD (0/1 -> bool, or a defined enum member) or a string. Kept as extensions rather than
    /// interface members so <see cref="IPolicySource"/> itself stays minimal (raw int/string only).
    /// </summary>
    internal static class PolicySourceExtensions
    {
        private const int PolicyDisabledValue = 0;
        private const int PolicyEnabledValue = 1;

        extension(IPolicySource source)
        {
            public bool? GetBool(string name)
            {
                return source.GetInt(name) switch
                {
                    null => null, // Policy not set
                    PolicyDisabledValue => false,
                    PolicyEnabledValue => true,
                    _ => null, // Invalid policy value
                };
            }

            public T? GetEnum<T>(string name) where T : struct, Enum
            {
                if (source.GetInt(name) is { } value && Enum.IsDefined(typeof(T), value))
                    return (T)(object)value;

                return null; // Policy not set or invalid
            }
        }
    }
}
