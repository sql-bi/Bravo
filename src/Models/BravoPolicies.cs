namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Security.Policies;
    using System;
    using System.Text.Json.Serialization;

    public class BravoPolicies // : IUserSettings
    {
        private static readonly Lazy<BravoPolicies> _instance;

        static BravoPolicies()
        {
            _instance = new Lazy<BravoPolicies>(CreateInstance, isThreadSafe: true);
        }

        public static BravoPolicies Current => _instance.Value;

        private static BravoPolicies CreateInstance()
        {
            var bravoPolicies = new BravoPolicies();
            return bravoPolicies;
        }

        private  BravoPolicies()
        {
            var policyManager = new PolicyManager();
            if (policyManager.PoliciesEnabled)
            {
                {
                    var (policy, value) = policyManager.GetTelemetryEnabledPolicy();
                    TelemetryEnabled = value;
                    TelemetryEnabledPolicy = policy;
                }
                {
                    var (policy, value) = policyManager.GetUpdateChannelPolicy();
                    UpdateChannel = value;
                    UpdateChannelPolicy = policy;
                }
                {
                    var (policy, value) = policyManager.GetUpdateCheckEnabledPolicy();
                    UpdateCheckEnabled = value;
                    UpdateCheckEnabledPolicy = policy;
                }
                {
                    var (policy, value) = policyManager.GetUseSystemBrowserForAuthenticationPolicy();
                    UseSystemBrowserForAuthentication = value;
                    UseSystemBrowserForAuthenticationPolicy = policy;
                }
                {
                    var (policy, value) = policyManager.GetCustomTemplatesEnabledPolicy();
                    CustomTemplatesEnabled = value;
                    CustomTemplatesEnabledPolicy = policy;
                }
                {
                    var (policy, value) = policyManager.GetBuiltInTemplatesEnabledPolicy();
                    BuiltInTemplatesEnabled = value;
                    BuiltInTemplatesEnabledPolicy = policy;
                }
                {
                    var (policy, value) = policyManager.GetCustomTemplatesOrganizationRepositoryPathPolicy();
                    CustomTemplatesOrganizationRepositoryPath = value;
                    CustomTemplatesOrganizationRepositoryPathPolicy = policy;
                }
            }
        }

        [JsonIgnore]
        public bool TelemetryEnabled { get; }

        [JsonPropertyName("telemetryEnabledPolicy")]
        public PolicyStatus TelemetryEnabledPolicy { get; } = PolicyStatus.NotConfigured;

        [JsonIgnore]
        public UpdateChannelType UpdateChannel { get; }

        [JsonPropertyName("updateChannelPolicy")]
        public PolicyStatus UpdateChannelPolicy { get; } = PolicyStatus.NotConfigured;

        [JsonIgnore]
        public bool UpdateCheckEnabled { get; }

        [JsonPropertyName("updateCheckEnabledPolicy")]
        public PolicyStatus UpdateCheckEnabledPolicy { get; } = PolicyStatus.NotConfigured;

        [JsonIgnore]
        public bool UseSystemBrowserForAuthentication { get; }

        [JsonPropertyName("useSystemBrowserForAuthenticationPolicy")]
        public PolicyStatus UseSystemBrowserForAuthenticationPolicy { get; } = PolicyStatus.NotConfigured;

        /// <remarks>This property is not exposed in the <see cref="UserSettings"/> because it is not to be set by the user. It's serialized in <see cref="BravoPolicies"/> for the sole purpose of allowing the UI to read its value</remarks>
        [JsonPropertyName("builtInTemplatesEnabled")]
        public bool BuiltInTemplatesEnabled { get; } // TODO: Add policy to the ADMX template

        [JsonPropertyName("builtInTemplatesEnabledPolicy")]
        public PolicyStatus BuiltInTemplatesEnabledPolicy { get; } = PolicyStatus.NotConfigured;

        /// <remarks>This property is not exposed in the <see cref="UserSettings"/> because it is not to be set by the user. It's serialized in <see cref="BravoPolicies"/> for the sole purpose of allowing the UI to read its value</remarks>
        [JsonPropertyName("customTemplatesEnabled")]
        public bool CustomTemplatesEnabled { get; } // TODO: Add policy to the ADMX template

        [JsonPropertyName("customTemplatesEnabledPolicy")]
        public PolicyStatus CustomTemplatesEnabledPolicy { get; } = PolicyStatus.NotConfigured;

        /// <remarks>This property is not exposed in the <see cref="UserSettings"/> because it is not to be set by the user. It's serialized in <see cref="BravoPolicies"/> for the sole purpose of allowing the UI to read its value</remarks>
        [JsonPropertyName("customTemplatesOrganizationRepositoryPath")]
        public string? CustomTemplatesOrganizationRepositoryPath { get; } // TODO: Add policy to the ADMX template

        [JsonPropertyName("customTemplatesOrganizationRepositoryPathPolicy")]
        public PolicyStatus CustomTemplatesOrganizationRepositoryPathPolicy { get; } = PolicyStatus.NotConfigured;
    }
}
