namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings
{
    using Sqlbi.Bravo.Infrastructure.Security.Policies;
    using Sqlbi.Bravo.Models;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public interface IUserSettings
    {
        bool TelemetryEnabled { get; set; }

        DiagnosticLevelType DiagnosticLevel { get; set; }

        UpdateChannelType UpdateChannel { get; set; }

        bool UpdateCheckEnabled { get; set; }

        ThemeType Theme { get; set; }

        ProxySettings? Proxy { get; set; }

        bool UseSystemBrowserForAuthentication { get; set; }

        JsonElement? CustomOptions { get; set; }
    }

    public class UserSettings : IUserSettings
    {
        public const bool DefaultTelemetryEnabled = true;
        public const DiagnosticLevelType DefaultDiagnosticLevel = DiagnosticLevelType.None;
        public const UpdateChannelType DefaultUpdateChannel = UpdateChannelType.Stable;
        public const bool DefaultUpdateCheckEnabled = true;
        public const ThemeType DefaultTheme = ThemeType.Auto;
        public const bool DefaultUseSystemBrowserForAuthentication = false;

        private bool _telemetryEnabled = DefaultTelemetryEnabled;
        private UpdateChannelType _updateChannel = DefaultUpdateChannel;
        private bool _updateCheckEnabled = DefaultUpdateCheckEnabled;
        private bool _useSystemBrowserForAuthentication = DefaultUseSystemBrowserForAuthentication;

        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled
        {
            get => _telemetryEnabled;
            set => _telemetryEnabled = GetSetterValue(value, BravoPolicies.Current.TelemetryEnabledPolicy, BravoPolicies.Current.TelemetryEnabled);
        }

        [JsonPropertyName("diagnosticLevel")]
        public DiagnosticLevelType DiagnosticLevel { get; set; } = DefaultDiagnosticLevel;

        [JsonPropertyName("updateChannel")]
        public UpdateChannelType UpdateChannel
        {
            get => _updateChannel;
            set => _updateChannel = GetSetterValue(value, BravoPolicies.Current.UpdateChannelPolicy, BravoPolicies.Current.UpdateChannel);
        }

        [JsonPropertyName("updateCheckEnabled")]
        public bool UpdateCheckEnabled
        { 
            get => _updateCheckEnabled;
            set => _updateCheckEnabled = GetSetterValue(value, BravoPolicies.Current.UpdateCheckEnabledPolicy, BravoPolicies.Current.UpdateCheckEnabled);
        }

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = DefaultTheme;

        [JsonPropertyName("proxy")]
        public ProxySettings? Proxy { get; set; }

        [JsonPropertyName("useSystemBrowserForAuthentication")]
        public bool UseSystemBrowserForAuthentication
        {
            get => _useSystemBrowserForAuthentication;
            set => _useSystemBrowserForAuthentication = GetSetterValue(value, BravoPolicies.Current.UseSystemBrowserForAuthenticationPolicy, BravoPolicies.Current.UseSystemBrowserForAuthentication);
        }

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }

        //[JsonPropertyName("experimental")]
        //public ExperimentalSettings? Experimental { get; set; }

        private T GetSetterValue<T>(T setterValue, PolicyStatus policyStatus, T policyValue)
        {
            if (policyStatus == PolicyStatus.Forced)
            {
                return policyValue;
            }
            else if (policyStatus == PolicyStatus.NotConfigured)
            {
                return setterValue;
            }

            throw new BravoUnexpectedException($"Unexpected { nameof(PolicyStatus) } value ({ policyStatus })");
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
