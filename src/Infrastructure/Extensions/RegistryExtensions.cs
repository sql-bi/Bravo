namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using Microsoft.Win32;

    internal static class RegistryExtensions
    {
        public static bool SubKeyExists(this RegistryKey registryKey, string subkeyName)
        {
            using var registrySubKey = registryKey.OpenSubKey(subkeyName);
            return registrySubKey != null;
        }

        public static bool GetBoolValue(this RegistryKey registryKey, string subkeyName, string valueName)
        {
            var valueInt = GetIntValue(registryKey, subkeyName, valueName);
            return valueInt == 1;
        }

        public static int? GetIntValue(this RegistryKey registryKey, string subkeyName, string valueName)
        {
            var (value, valueKind) = GetRegistryValue(registryKey, subkeyName, valueName);

            if (value is not null && valueKind == RegistryValueKind.DWord)
            {
                var valueInt = (int)value;
                return valueInt;
            }

            return null;
        }

        public static string? GetStringValue(this RegistryKey registryKey, string subkeyName, string valueName)
        {
            var (value, valueKind) = GetRegistryValue(registryKey, subkeyName, valueName);
            
            if (value is not null && valueKind == RegistryValueKind.String)
            {
                var valueString = (string)value;
                return valueString;
            }

            return null;
        }

        private static (object? Value, RegistryValueKind ValueKind) GetRegistryValue(RegistryKey registryKey, string subkeyName, string valueName)
        {
            using var registrySubKey = registryKey.OpenSubKey(subkeyName);

            if (registrySubKey is not null)
            {
                var value = registrySubKey.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (value is not null)
                {
                    var valueKind = registrySubKey.GetValueKind(valueName);
                    return (value, valueKind);
                }
            }

            return (null, RegistryValueKind.None);
        }
    }
}