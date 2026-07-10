namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Serialization
{
    internal static class CloudContractJsonSerializer
    {
        private readonly static JsonSerializerOptions s_options
            = new(JsonSerializerDefaults.Web);

        public static T Deserialize<T>(string json) where T : class
        {
            return JsonSerializer.Deserialize<T>(json, s_options)
                ?? throw new InvalidOperationException($"The JSON content deserialized to a null '{typeof(T)}' instance.");
        }
        public static string Serialize<T>(T value) where T : class
            => JsonSerializer.Serialize(value, s_options);
    }
}
