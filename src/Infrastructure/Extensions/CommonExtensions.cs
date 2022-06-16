namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using System;
    using System.Text.Json;

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

        public static TEnum? TryParseTo<TEnum>(this int? value) where TEnum : struct, Enum
        {
            if (value is not null)
            {
                if (Enum.IsDefined(typeof(TEnum), value.Value))
                {
                    var @enum = (TEnum)Enum.ToObject(typeof(TEnum), value.Value);
                    return @enum;
                }
            }

            return null;
        }

        public static T? JsonClone<T>(this T value)
        {
            var json = JsonSerializer.Serialize(value);
            var instance = JsonSerializer.Deserialize<T>(json);
            return instance;
        }
    }
}
