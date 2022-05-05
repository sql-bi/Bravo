namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class UserSettings
    {
        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppEnvironment.TelemetryEnabledDefault;

        [JsonPropertyName("diagnosticLevel")]
        public DiagnosticLevelType DiagnosticLevel { get; set; } = DiagnosticLevelType.None;

        [JsonPropertyName("updateChannel")]
        public UpdateChannelType UpdateChannel { get; set; } = UpdateChannelType.Stable;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        [JsonPropertyName("proxy")]
        public ProxySettings? Proxy { get; set; }

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }
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
