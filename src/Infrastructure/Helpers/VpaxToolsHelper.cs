namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Dax.Metadata.Extractor;
    using Dax.ViewModel;
    using Dax.Vpax.Tools;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.IO;
    using System.Linq;

    internal static class VpaxToolsHelper
    {
        public static Stream ExportVpax(string connectionString, string databaseName, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows)
        {
            try
            {
                var daxModel = TomExtractor.GetDaxModel(connectionString, databaseName, AppEnvironment.ApplicationName, AppEnvironment.ApplicationProductVersion, readStatisticsFromData, sampleRows);
                var tomModel = includeTomModel ? TomExtractor.GetDatabase(connectionString, databaseName) : null;
                var vpaModel = includeVpaModel ? new Dax.ViewVpaExport.Model(daxModel) : null;
                var stream = new MemoryStream();

                VpaxTools.ExportVpax(stream, daxModel, vpaModel, tomModel);

                return stream;
            }
            catch (ArgumentException ex) when (ex.Message == $"The database '{ databaseName }' could not be found. Either it does not exist or you do not have admin rights to it.") // TODO: avoid using the exception message here to filter the error
            {
                throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound);
            }
        }

        public static TabularDatabase GetDatabaseFromVpax(Stream stream)
        {
            var vpaxContent = default(VpaxTools.VpaxContent);
            try
            {
                vpaxContent = VpaxTools.ImportVpax(stream);
            }
            catch (FileFormatException)
            {
                throw new BravoException(BravoProblem.VpaxFileContainsCorruptedData);
            }

            var vpaModel = new VpaModel(vpaxContent.DaxModel);
            var databaseETag = TabularModelHelper.GetDatabaseETag(vpaModel.Model.ModelName.Name, vpaModel.Model.Version, vpaModel.Model.LastUpdate);
            var databaseSize = vpaModel.Columns.Sum((c) => c.TotalSize);

            var tabularColumns = vpaModel.Columns.Select((c) =>
            {
                var column = new TabularColumn
                {
                    Name = c.ColumnName,
                    TableName = c.Table.TableName,
                    Cardinality = c.ColumnCardinality,
                    Size = c.TotalSize,
                    Weight = (double)c.TotalSize / databaseSize,
                    IsReferenced = c.IsReferenced,
                };

                return column;
            }).ToArray();

            var tabularTables = vpaModel.Tables.Select((t) =>
            {
                var table = new TabularTable
                {
                    Name = t.TableName,
                    RowsCount = t.RowsCount,
                    Size = t.TableSize
                };

                return table;
            }).ToArray();

            var tabularMeasures = vpaxContent.DaxModel.Tables.SelectMany((t) => t.Measures).Select((m) =>
            {
                var (expression, lineBreakStyle) = m.MeasureExpression.Expression.NormalizeDax();
                var measure = new TabularMeasure
                {
                    ETag = databaseETag,
                    Name = m.MeasureName.Name,
                    TableName = m.Table.TableName.Name,
                    Expression = expression,
                    LineBreakStyle = lineBreakStyle,
                };

                return measure;
            }).ToArray();

            var tabularDatabase = new TabularDatabase
            {
                Info = new TabularDatabaseInfo
                {
                    ETag = databaseETag,
                    TablesCount = vpaModel.Tables.Count(),
                    ColumnsCount = vpaModel.Columns.Count(),
                    TablesMaxRowsCount = vpaModel.Tables.Any() ? vpaModel.Tables.Max((t) => t.RowsCount) : 0,
                    DatabaseSize = databaseSize,
                    ColumnsUnreferencedCount = vpaModel.Columns.Count((c) => !c.IsReferenced),
                    AutoLineBreakStyle = GetAutoLineBreakStyle(),
                    Columns = tabularColumns,
                    Tables = tabularTables,
                },
                Measures = tabularMeasures
            };

            return tabularDatabase;

            DaxLineBreakStyle? GetAutoLineBreakStyle()
            {
                var preferredStyleQuery = tabularMeasures.GroupBy((measure) => measure.LineBreakStyle)
                    .Select((group) => new { LineBreakStyle = group.Key, Count = group.Count() })
                    .OrderByDescending((item) => item.Count)
                    .FirstOrDefault();

                return preferredStyleQuery?.LineBreakStyle;
            }
        }
    }
}
