using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

// Generated at compile time: the crashing process serializes without reflection or code emitted at run time.
// An incomplete report fails to load instead of loading with null members that a reader trusts.
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true,
    RespectRequiredConstructorParameters = true,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(CrashReport))]
internal sealed partial class CrashReportJsonContext : JsonSerializerContext
{
}
