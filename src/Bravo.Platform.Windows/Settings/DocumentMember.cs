using System;
using System.Text.Json;

namespace Bravo.Platform.Windows.Settings;

/// <summary>
/// A member of the file of the settings, read as a value of the settings: a member that is missing, of another
/// type or out of range is the default of its setting. An enum is its number, as 1.0.x writes it.
/// </summary>
internal static class DocumentMember
{
    public static bool ToBoolean(JsonElement? member, bool defaultValue)
    {
        return member?.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => defaultValue,
        };
    }

    public static TEnum ToEnum<TEnum>(JsonElement? member, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (member is not { ValueKind: JsonValueKind.Number } number || !number.TryGetInt32(out var value))
        {
            return defaultValue;
        }

        var candidate = (TEnum)Enum.ToObject(typeof(TEnum), value);

        return Enum.IsDefined(candidate) ? candidate : defaultValue;
    }

    /// <summary>
    /// A text that is only blank is no text, as the API reads it.
    /// </summary>
    public static string? ToText(JsonElement? member)
    {
        if (member is not { ValueKind: JsonValueKind.String } text)
        {
            return null;
        }

        var value = text.GetString();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
