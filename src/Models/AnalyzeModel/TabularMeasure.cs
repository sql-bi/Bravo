namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models.FormatDax;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("'{TableName}'[{Name}]")]
    public class TabularMeasure
    {
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("expression")]
        public string? Expression { get; set; }

        [JsonPropertyName("lineBreakStyle")]
        public DaxLineBreakStyle? LineBreakStyle { get; set; }

        [JsonPropertyName("isHidden")]
        public bool? IsHidden { get; set; }

        internal static TabularMeasure CreateFrom(Dax.Metadata.Measure daxMeasure, string databaseETag)
        {
            var (expression, lineBreakStyle) = daxMeasure.MeasureExpression.Expression.NormalizeDax();

            var measure = new TabularMeasure
            {
                ETag = databaseETag,
                Name = daxMeasure.MeasureName.Name,
                TableName = daxMeasure.Table.TableName.Name,
                Expression = expression,
                LineBreakStyle = lineBreakStyle,
                IsHidden = false //TODO Expose IsHidden property 
            };

            return measure;
        }
    }

    internal static class TabularMeasureExtensions
    {
        public static DaxLineBreakStyle? GetAutoLineBreakStyle(this TabularMeasure[] measures)
        {
            var preferredStyleQuery = measures.GroupBy((measure) => measure.LineBreakStyle)
                .Select((group) => new { LineBreakStyle = group.Key, Count = group.Count() })
                .OrderByDescending((item) => item.Count)
                .FirstOrDefault();

            return preferredStyleQuery?.LineBreakStyle;
        }
    }
}
