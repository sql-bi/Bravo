namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class GlobalService
    {
        [JsonPropertyName("environments")]
        public IEnumerable<GlobalServiceEnvironment>? Environments { get; set; }
    }
}
