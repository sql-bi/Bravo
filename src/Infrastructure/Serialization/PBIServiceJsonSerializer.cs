namespace Sqlbi.Bravo.Infrastructure.Serialization
{
    internal static class PBIServiceJsonSerializer
    {
        private readonly static JsonSerializerOptions s_options
            = new(JsonSerializerDefaults.Web);

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, s_options)
                ?? throw new InvalidOperationException($"The JSON content deserialized to a null '{typeof(T)}' instance.");
        }
        public static string Serialize<T>(T value)
            => JsonSerializer.Serialize(value, s_options);
    }
}
