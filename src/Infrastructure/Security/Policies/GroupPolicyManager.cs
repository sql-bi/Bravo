namespace Sqlbi.Bravo.Infrastructure.Security.Policies
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.IO;

    internal sealed class GroupPolicyManager
    {
        private const string PolicySubKeyName = @"Software\Policies\SQLBI\Bravo";
        private const string OptionSettingsName = "OptionSettings";

        private readonly Lazy<bool> _enabled;

        public GroupPolicyManager()
        {
            _enabled = new(() => GetGroupPoliciesEnabled());
        }

        public bool Enabled => _enabled.Value;

        #region OptionSettings

        public (PolicyStatus Policy, UpdateChannelType Value) GetPolicyForUpdateChannel()
        {
            var policyValue = GetIntValue(valueName: "UpdateChannel", relativeSubkeyName: OptionSettingsName);

            if (policyValue is null)
            {
                return (PolicyStatus.NotConfigured, Value: UpdateChannelType.Stable);
            }
            else
            {
                var updateChannel = policyValue.TryParseTo<UpdateChannelType>();

                if (updateChannel is null)
                    updateChannel = UpdateChannelType.Stable;

                return (PolicyStatus.Forced, Value: updateChannel.Value);
            }
        }

        public (PolicyStatus Policy, bool Value) GetPolicyForUpdateCheckEnabled()
        {
            var policyValue = GetIntValue(valueName: "UpdateCheckEnabled", relativeSubkeyName: OptionSettingsName);

            if (policyValue.IsPolicyNotConfigured())
            {
                return (PolicyStatus.NotConfigured, Value: true);
            }
            else if (policyValue.IsPolicyEnabled())
            {
                return (PolicyStatus.Forced, Value: true);
            }
            else if (policyValue.IsPolicyDisabled())
            {
                return (PolicyStatus.Forced, Value: false);
            }
            else
            {
                throw new BravoUnexpectedException($"Unexpected { nameof(PolicyStatus) } value ({ policyValue })");
            }
        }

        #endregion

        private static int? GetIntValue(string valueName, string relativeSubkeyName)
        {
            var subkeyName = Path.Combine(PolicySubKeyName, relativeSubkeyName);
            var value = Registry.LocalMachine.GetIntValue(subkeyName, valueName);

            if (value is null)
                value = Registry.CurrentUser.GetIntValue(subkeyName, valueName);

            return value;
        }

        private static bool GetGroupPoliciesEnabled()
        {
            var enabled = Registry.LocalMachine.SubKeyExists(PolicySubKeyName);

            if (enabled == false)
                enabled = Registry.CurrentUser.SubKeyExists(PolicySubKeyName);

            return enabled;
        }
    }

    internal static class GroupPolicyManagerExtensions
    {
        private const int PolicyDisabledValue = 0;
        private const int PolicyEnabledValue = 1;

        public static bool IsPolicyNotConfigured(this int? policyValue)
        {
            if (policyValue is null)
                return true;

            return false;
        }

        public static bool IsPolicyEnabled(this int? policyValue)
        {
            if (policyValue is null)
                return false;

            if (policyValue == PolicyEnabledValue)
                return true;

            return false;
        }

        public static bool IsPolicyDisabled(this int? policyValue)
        {
            if (policyValue is null)
                return false;

            if (policyValue == PolicyDisabledValue)
                return true;

            return false;
        }
    }
}
