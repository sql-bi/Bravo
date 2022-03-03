namespace Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Linq;

    public class RuleScopeConverter : StringEnumConverter
    {
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.ValueType == typeof(string) && objectType == typeof(RuleScope) && reader.Value is string readerValue)
            {
                var types = readerValue.Split(',').ToList();

                // For backwards compatibility with rules created when "Column" existed as a RuleScope:
                if (types.Contains("Column"))
                {
                    types.Remove("Column");
                    types.Add("DataColumn");
                    types.Add("CalculatedColumn");
                    types.Add("CalculatedTableColumn");
                }

                // For backwards compatibility with rules created when "DataSource" existed as a RuleScope:
                if (types.Contains("DataSource"))
                {
                    types.Remove("DataSource");
                    types.Add("ProviderDataSource");
                }

                return types.Select(RuleScopeExtensions.GetScope).Combine();
            }
            
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
        }
    }
}
