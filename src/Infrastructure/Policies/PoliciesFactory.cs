namespace Sqlbi.Bravo.Infrastructure.Policies
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;

    /// <summary>
    /// Builds <see cref="Policies"/> instances: parses a single <see cref="IPolicySource"/>,
    /// and composes the effective policy set from LocalMachine + CurrentUser with precedence.
    /// </summary>
    internal static class PoliciesFactory
    {
        private const string OptionSettingsSubKeyName = @"SOFTWARE\Policies\SQLBI\Bravo\OptionSettings";

        public static Policies Create()
        {
            using var machineKey = Registry.LocalMachine.OpenSubKey(OptionSettingsSubKeyName);
            var machinePolicies = FromSource(new RegistryPolicySource(machineKey));

            using var userKey = Registry.CurrentUser.OpenSubKey(OptionSettingsSubKeyName);
            var userPolicies = FromSource(new RegistryPolicySource(userKey));

            return Merge(machinePolicies, userPolicies);
        }

        internal static Policies FromSource(IPolicySource source) => new(
            TelemetryEnabled: source.GetBool("TelemetryEnabled"),
            UpdateChannel: source.GetEnum<UpdateChannelType>("UpdateChannel"),
            UpdateCheckEnabled: source.GetBool("UpdateCheckEnabled"),
            UseSystemBrowserForAuthentication: source.GetBool("UseSystemBrowserForAuthentication"),
            BuiltInTemplatesEnabled: source.GetBool("BuiltInTemplatesEnabled"),
            CustomTemplatesEnabled: source.GetBool("CustomTemplatesEnabled"),
            CustomTemplatesOrganizationRepositoryPath: source.GetString("CustomTemplatesOrganizationRepositoryPath"));

        internal static Policies Merge(Policies machinePolicies, Policies userPolicies)
        {
            // LocalMachine takes precedence over CurrentUser when both are configured
            return new Policies(
                TelemetryEnabled: machinePolicies.TelemetryEnabled ?? userPolicies.TelemetryEnabled,
                UpdateChannel: machinePolicies.UpdateChannel ?? userPolicies.UpdateChannel,
                UpdateCheckEnabled: machinePolicies.UpdateCheckEnabled ?? userPolicies.UpdateCheckEnabled,
                UseSystemBrowserForAuthentication: machinePolicies.UseSystemBrowserForAuthentication ?? userPolicies.UseSystemBrowserForAuthentication,
                BuiltInTemplatesEnabled: machinePolicies.BuiltInTemplatesEnabled ?? userPolicies.BuiltInTemplatesEnabled,
                CustomTemplatesEnabled: machinePolicies.CustomTemplatesEnabled ?? userPolicies.CustomTemplatesEnabled,
                CustomTemplatesOrganizationRepositoryPath: machinePolicies.CustomTemplatesOrganizationRepositoryPath ?? userPolicies.CustomTemplatesOrganizationRepositoryPath);
        }
    }
}
