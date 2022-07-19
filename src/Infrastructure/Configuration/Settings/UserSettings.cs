namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings
{
    using Sqlbi.Bravo.Infrastructure.Security.Policies;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public interface IUserSettings
    {
        bool TelemetryEnabled { get; set; }

        DiagnosticLevelType DiagnosticLevel { get; set; }

        UpdateChannelType UpdateChannel { get; set; }

        PolicyStatus UpdateChannelPolicy { get; }

        bool UpdateCheckEnabled { get; set; }

        PolicyStatus UpdateCheckEnabledPolicy { get; }

        ThemeType Theme { get; set; }

        ProxySettings? Proxy { get; set; }

        bool UseSystemBrowserForAuthentication { get; set; }

        bool TemplateDevelopmentEnabled { get; set; }

        JsonElement? CustomOptions { get; set; }
    }

    public class UserSettings : IUserSettings
    {
        private UpdateChannelType _updateChannel = UpdateChannelType.Stable;
        private bool _updateCheckEnabled = true;

        public UserSettings()
        {
            if (AppEnvironment.GroupPolicies.Enabled)
            {
                {
                    var (policy, value) = AppEnvironment.GroupPolicies.GetPolicyForUpdateChannel();
                    UpdateChannel = value;
                    UpdateChannelPolicy = policy;
                }
                {
                    var (policy, value) = AppEnvironment.GroupPolicies.GetPolicyForUpdateCheckEnabled();
                    UpdateCheckEnabled = value;
                    UpdateCheckEnabledPolicy = policy;
                }
            }
        }

        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppEnvironment.TelemetryEnabledDefault;

        [JsonPropertyName("diagnosticLevel")]
        public DiagnosticLevelType DiagnosticLevel { get; set; } = DiagnosticLevelType.None;

        [JsonPropertyName("updateChannel")]
        public UpdateChannelType UpdateChannel
        {
            get => _updateChannel;
            set => _updateChannel = GetSetterValue(_updateChannel, value, UpdateChannelPolicy);
        }

        [JsonIgnore]
        public PolicyStatus UpdateChannelPolicy { get; } = PolicyStatus.NotConfigured;

        [JsonPropertyName("updateCheckEnabled")]
        public bool UpdateCheckEnabled 
        { 
            get => _updateCheckEnabled;
            set => _updateCheckEnabled = GetSetterValue(_updateCheckEnabled, value, UpdateCheckEnabledPolicy);
        }

        [JsonIgnore]
        public PolicyStatus UpdateCheckEnabledPolicy { get; } = PolicyStatus.NotConfigured;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        [JsonPropertyName("proxy")]
        public ProxySettings? Proxy { get; set; }

        [JsonPropertyName("useSystemBrowserForAuthentication")]
        public bool UseSystemBrowserForAuthentication { get; set; } = false;

        [JsonPropertyName("templateDevelopmentEnabled")]
        public bool TemplateDevelopmentEnabled { get; set; }

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }

        //[JsonPropertyName("experimental")]
        //public ExperimentalSettings? Experimental { get; set; }

        public bool Validate(bool throwOnError = true)
        {
            try
            {
                Proxy?.Validate(throwOnError);

                return true;
            }
            catch
            {
                if (throwOnError)
                    throw;

                return false;
            }
        }

        internal void AssertUpdateChannelPolicy(UpdateChannelType updateChannel)
        {
            if (UpdateChannelPolicy == PolicyStatus.Forced && UpdateChannel != updateChannel)
            {
                throw new BravoUnexpectedPolicyViolationException(policyName: nameof(UpdateChannelPolicy));
            }
        }

        internal void AssertUpdateCheckEnabledPolicy()
        {
            if (UpdateCheckEnabledPolicy == PolicyStatus.Forced && UpdateCheckEnabled == false)
            {
                throw new BravoUnexpectedPolicyViolationException(policyName: nameof(UpdateCheckEnabledPolicy));
            }
        }

        private T GetSetterValue<T>(T currentValue, T setterValue, PolicyStatus policy)
        {
            if (policy == PolicyStatus.NotConfigured)
            {
                return setterValue;
            }
            else if (policy == PolicyStatus.Forced)
            {
                return currentValue;
            }

            throw new BravoUnexpectedException($"Unexpected { nameof(PolicyStatus) } value ({ policy })");
        }
    }

    public enum ThemeType
    {
        Auto = 0,
        Light = 1,
        Dark = 2
    }

    public enum UpdateChannelType
    {
        /// <summary>
        /// (Default) Stable builds are the best ones to use, they are a result of the code being built in Canary, tested in Dev and bug fixed in Beta
        /// </summary>
        Stable = 0,

        ///// <summary>
        ///// Beta channel is the best build to get if you’re interested in being the first one to know about upcoming features 
        ///// </summary>
        //Beta = 1,

        /// <summary>
        /// Dev build will carry the improvements made to the application and tested by the developers, it’s still not recommended to use it because it can have bugs
        /// </summary>
        Dev = 2,

        ///// <summary>
        ///// Canary build carries features that are released as soon as they’re built and are not tested or used
        ///// </summary>
        //Canary = 3,
    }

    public enum DiagnosticLevelType
    {
        None = 0,
        Basic = 1,
        Verbose = 2
    }
}
