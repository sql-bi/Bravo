namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using System;

    internal static class EnumExtensions
    {
        public static TEnum? TryParseTo<TEnum>(this Enum? value) where TEnum : struct, Enum
        {
            if (value is not null)
            {
                var valueString = value.ToString();
                var valueEnum = TryParseTo<TEnum>(valueString);

                return valueEnum;
            }

            return null;
        }

        public static TEnum? TryParseTo<TEnum>(this string? value) where TEnum : struct, Enum
        {
            if (value is not null)
            {
                if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var valueEnum))
                {
                    return valueEnum;
                }
            }

            return null;
        }
    }
}
