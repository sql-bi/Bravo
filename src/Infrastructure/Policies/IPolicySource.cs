using System;

namespace Sqlbi.Bravo.Infrastructure.Policies;

internal interface IPolicySource : IDisposable
{
    int? GetInt(string name);
    string? GetString(string name);
}

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

        /// <remarks>
        /// TEnum must have int as its underlying type.
        /// </remarks>
        public TEnum? GetEnum<TEnum>(string name) where TEnum : struct, Enum
        {
            if (source.GetInt(name) is { } value && Enum.IsDefined(typeof(TEnum), value))
                return (TEnum)(object)value;

            return null; // Policy not set or invalid
        }
    }
}
