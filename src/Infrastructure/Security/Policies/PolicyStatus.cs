using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Security.Policies
{
    // The JsonStringEnumConverter is required because the enum is represented as strings on the UI (TypeScript) side,
    // not as integers. Remove this converter once the TypeScript enum is redefined to use integer values.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PolicyStatus
    {
        /// <summary>
        /// No policy has been enforced
        /// </summary>
        NotConfigured = 0,

        /// <summary>
        /// A policy has been applied for the property scope
        /// </summary>
        Forced = 1,
    }
}
