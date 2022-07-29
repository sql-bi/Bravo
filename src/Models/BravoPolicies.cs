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
    }
}
