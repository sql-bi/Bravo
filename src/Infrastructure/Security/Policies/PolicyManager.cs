namespace Sqlbi.Bravo.Infrastructure.Security.Policies
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System.IO;

    internal sealed class PolicyManager
    {
        private const string PolicySubKeyName = @"Software\Policies\SQLBI\Bravo";
        private const string OptionSettingsName = "OptionSettings";
        private const int PolicyDisabledValue = 0;
        private const int PolicyEnabledValue = 1;

        public bool PoliciesEnabled => GetPoliciesEnabled();

        public (PolicyStatus Policy, bool Value) GetTelemetryEnabledPolicy() => GetBoolPolicy(valueName: "TelemetryEnabled", relativeSubkeyName: OptionSettingsName);

        public (PolicyStatus Policy, bool Value) GetUseSystemBrowserForAuthenticationPolicy() => GetBoolPolicy(valueName: "UseSystemBrowserForAuthentication", relativeSubkeyName: OptionSettingsName);

        public (PolicyStatus Policy, UpdateChannelType Value) GetUpdateChannelPolicy()
        {
            var policyValue = GetIntValue(valueName: "UpdateChannel", relativeSubkeyName: OptionSettingsName);

            if (IsPolicyNotConfigured(policyValue))
            {
                return (PolicyStatus.NotConfigured, Value: UpdateChannelType.Stable);
            }
            else
            {
                var updateChannel = policyValue.TryParseTo<UpdateChannelType>();

                if (updateChannel is null)
                    updateChannel = UpdateChannelType.Stable;

                return (PolicyStatus.Forced, updateChannel.Value);
            }
        }

        public (PolicyStatus Policy, bool Value) GetUpdateCheckEnabledPolicy() => GetBoolPolicy(valueName: "UpdateCheckEnabled", relativeSubkeyName: OptionSettingsName);

        public (PolicyStatus Policy, bool Value) GetCustomTemplatesEnabledPolicy() => GetBoolPolicy(valueName: "CustomTemplatesEnabled", relativeSubkeyName: OptionSettingsName);

        private static (PolicyStatus Policy, bool Value) GetBoolPolicy(string valueName, string relativeSubkeyName)
        {
            var policyValue = GetIntValue(valueName, relativeSubkeyName);

            if (IsPolicyNotConfigured(policyValue))
            {
                return (PolicyStatus.NotConfigured, Value: true);
            }
            else if (IsPolicyEnabled(policyValue))
            {
                return (PolicyStatus.Forced, Value: true);
            }
            else if (IsPolicyDisabled(policyValue))
            {
                return (PolicyStatus.Forced, Value: false);
            }
            else
            {
                throw new BravoUnexpectedException($"Unexpected {nameof(PolicyStatus)} value ({policyValue})");
            }
        }

        private static int? GetIntValue(string valueName, string relativeSubkeyName)
        {
            var subkeyName = Path.Combine(PolicySubKeyName, relativeSubkeyName);
            var value = Registry.LocalMachine.GetIntValue(subkeyName, valueName);

            if (value is null)
                value = Registry.CurrentUser.GetIntValue(subkeyName, valueName);

            return value;
        }

        private static bool GetPoliciesEnabled()
        {
            var enabled = Registry.LocalMachine.SubKeyExists(PolicySubKeyName);

            if (enabled == false)
                enabled = Registry.CurrentUser.SubKeyExists(PolicySubKeyName);

            return enabled;
        }

        private static bool IsPolicyEnabled(int? policyValue)
        {
            if (policyValue is null)
                return false;

            if (policyValue == PolicyEnabledValue)
                return true;

            return false;
        }

        private static bool IsPolicyDisabled(int? policyValue)
        {
            if (policyValue is null)
                return false;

            if (policyValue == PolicyDisabledValue)
                return true;

            return false;
        }

        private static bool IsPolicyNotConfigured(int? policyValue)
        {
            if (policyValue is null)
                return true;

            return false;
        }
    }
}
