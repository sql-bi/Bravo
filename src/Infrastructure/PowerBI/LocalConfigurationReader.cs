namespace Sqlbi.Bravo.Infrastructure.PowerBI
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure.Extensions;

    internal interface ILocalConfigurationReader
    {
        Uri? GetPowerBIServiceDiscoveryBaseUri();
        Uri? GetPowerBIServiceFixedClusterUri();
    }

    internal sealed class LocalConfigurationReader : ILocalConfigurationReader
    {
        private const string PowerBIDiscoveryUrlValueName = "PowerBIDiscoveryUrl";
        private const string PowerBISubkeyName = @"SOFTWARE\Microsoft\Microsoft Power BI\";
        private const string PowerBIPolicySubkeyName = @"SOFTWARE\Policies\Microsoft\Microsoft Power BI\";

        /// <summary>
        /// Gets the Power BI service discovery base URI from the local machine registry.
        /// <para>
        /// See https://github.com/microsoft/Federal-Business-Applications/tree/main/whitepapers/power-bi-registry-settings
        /// and https://docs.microsoft.com/en-us/power-bi/enterprise/service-govus-overview#sign-in-to-power-bi-for-us-government
        /// </para>
        /// </summary>
        public Uri? GetPowerBIServiceDiscoveryBaseUri()
        {
            var valueName = PowerBIDiscoveryUrlValueName;

            // The value may have been written to either the 64-bit or the 32-bit (WOW6432Node) registry view,
            // depending on the bitness of the Power BI Desktop build that set it. Check both views explicitly so
            // the result does not depend on the bitness of this (Bravo) process.
            var value = GetRegistryString(valueName, RegistryView.Registry64);
            if (value is null)
                value = GetRegistryString(valueName, RegistryView.Registry32);

            return value is null ? null : new Uri(value, UriKind.Absolute);
        }

        public Uri? GetPowerBIServiceFixedClusterUri()
        {
            // `PowerBIServiceUrl` is not a documented registry key, but it is used by the
            // PBI Desktop to override the default service URL for fixed cluster scenarios.

            throw new NotImplementedException();
        }

        private static string? GetRegistryString(string valueName, RegistryView view)
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);

            // Try the policy override first
            var value = key.GetStringValue(PowerBIPolicySubkeyName, valueName);
            if (!string.IsNullOrEmpty(value))
                return value;

            // Then try the standard subkey
            value = key.GetStringValue(PowerBISubkeyName, valueName);
            if (!string.IsNullOrEmpty(value))
                return value;

            return null;
        }
    }
}
