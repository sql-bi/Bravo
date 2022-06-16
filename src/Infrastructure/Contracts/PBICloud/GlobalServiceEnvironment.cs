namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{CloudName}")]
    public class GlobalServiceEnvironment
    {
        [JsonPropertyName("cloudName")]
        public string? CloudName { get; set; }

        [JsonPropertyName("services")]
        public IEnumerable<GlobalServiceEnvironmentService>? Services { get; set; }

        [JsonPropertyName("clients")]
        public IEnumerable<GlobalServiceEnvironmentClient>? Clients { get; set; }
    }
}
