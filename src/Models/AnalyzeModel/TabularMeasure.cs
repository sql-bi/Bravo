namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services.DaxTemplate;
    using Sqlbi.Bravo.Models.FormatDax;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json.Serialization;
    using TOM = Microsoft.AnalysisServices.Tabular;

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

        [JsonPropertyName("displayFolder")]
        public string? DisplayFolder { get; set; }

        [JsonPropertyName("lineBreakStyle")]
        public DaxLineBreakStyle? LineBreakStyle { get; set; }

        [JsonPropertyName("isHidden")]
        public bool? IsHidden { get; set; }

        [JsonPropertyName("isManageDatesTimeIntelligence")]
        public bool? IsManageDatesTimeIntelligence { get; set; }

        internal static TabularMeasure CreateFrom(Dax.Metadata.Measure daxMeasure, string databaseETag, TOM.Model? tomModel = default)
        {
            var (expression, lineBreakStyle) = daxMeasure.MeasureExpression?.Expression.NormalizeDax() ?? (null, DaxLineBreakStyle.None);

            var measure = new TabularMeasure
            {
                ETag = databaseETag,
                Name = daxMeasure.MeasureName.Name,
                TableName = daxMeasure.Table.TableName.Name,
                Expression = expression ?? string.Empty,
                DisplayFolder = daxMeasure.DisplayFolder?.Note,
                LineBreakStyle = lineBreakStyle,
                IsHidden = null,
                IsManageDatesTimeIntelligence = null
            };

            var tomMeasure = tomModel?.Tables?.FindMeasure(measure.TableName, measure.Name);
            if (tomMeasure is not null)
            {
                measure.IsHidden = tomMeasure.IsHidden;
                measure.IsManageDatesTimeIntelligence = tomMeasure.Annotations.Contains(DaxTemplateManager.SqlbiTemplateAnnotation);
            }

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
