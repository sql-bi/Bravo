namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Dax.Metadata;
    using Dax.Metadata.Extractor;
    using Dax.ViewModel;
    using Dax.Vpax.Tools;
    using Microsoft.AnalysisServices.AdomdClient;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Models.FormatDax;
    using System.IO;
    using System.Linq;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal static class VpaxToolsHelper
    {
        public static Stream GetVpax(TOM.Database database, bool includeVpaModel = false, bool includeTomDatabase = false, bool readStatisticsFromData = false, int sampleRows = 0)
        {
            var daxModel = GetDaxModel(database);
            var vpaModel = includeVpaModel ? new Dax.ViewVpaExport.Model(daxModel) : null;
            var tomDatabase = includeTomDatabase ? (Microsoft.AnalysisServices.Database)database.Model.Database : null; // TOFIX: VertiPaq-Analyzer NuGet package - change tomModel from 'Microsoft.AnalysisServices.Database' to 'Microsoft.AnalysisServices.Tabular.Database'
            
            var stream = new MemoryStream();
            {
                VpaxTools.ExportVpax(stream, daxModel, vpaModel, tomDatabase);
            }
            return stream;
        }

        public static TabularDatabase GetDatabase(Stream stream)
        {
            VpaxTools.VpaxContent vpaxContent;
            try
            {
                vpaxContent = VpaxTools.ImportVpax(stream);
            }
            catch (FileFormatException)
            {
                throw new BravoException(BravoProblem.VpaxFileContainsCorruptedData);
            }

            var tabularDatabase = GetDatabase(vpaxContent.DaxModel);
            {
                tabularDatabase.Features = AppFeature.All;
                tabularDatabase.Features &= ~AppFeature.AnalyzeModelSynchronize;
                tabularDatabase.Features &= ~AppFeature.AnalyzeModelExportVpax;
                tabularDatabase.Features &= ~AppFeature.FormatDaxSynchronize;
                tabularDatabase.Features &= ~AppFeature.FormatDaxUpdateModel;
                tabularDatabase.Features &= ~AppFeature.ManageDatesAll;
                tabularDatabase.Features &= ~AppFeature.ExportDataAll;
            }
            return tabularDatabase;
        }

        public static TabularDatabase GetDatabase(TOM.Database database)
        {
            var daxModel = GetDaxModel(database);
            var tabularDatabase = GetDatabase(daxModel);

            return tabularDatabase;
        }

        private static TabularDatabase GetDatabase(Model daxModel)
        {
            var vpaModel = new VpaModel(daxModel);
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

            var tabularMeasures = daxModel.Tables.SelectMany((t) => t.Measures).Select((m) =>
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

        private static Model GetDaxModel(TOM.Database database, bool includeStatistics = false, int sampleRows = 0)
        {
            var daxModel = TomExtractor.GetDaxModel(database.Model, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

            using var connection = new AdomdConnection(database.Parent.ConnectionString);
            {
                DmvExtractor.PopulateFromDmv(
                    daxModel,
                    connection,
                    serverName: database.Parent.Name,
                    databaseName: database.Name,
                    extractorApp: AppEnvironment.ApplicationName,
                    extractorVersion: AppEnvironment.ApplicationProductVersion
                    );

                if (includeStatistics)
                {
                    StatExtractor.UpdateStatisticsModel(daxModel, connection, sampleRows);
                }
            }

            return daxModel;
        }
    }
}
