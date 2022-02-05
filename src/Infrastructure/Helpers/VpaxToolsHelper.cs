namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Dax.Metadata.Extractor;
    using Dax.ViewModel;
    using Dax.Vpax.Tools;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
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
                throw new TOMDatabaseException(BravoProblem.TOMDatabaseDatabaseNotFound);
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

            var databaseModel = new TabularDatabase
            {
                Info = new TabularDatabaseInfo
                {
                    ETag = databaseETag,
                    TablesCount = vpaModel.Tables.Count(),
                    ColumnsCount = vpaModel.Columns.Count(),
                    TablesMaxRowsCount = vpaModel.Tables.Any() ? vpaModel.Tables.Max((t) => t.RowsCount) : 0,
                    DatabaseSize = databaseSize,
                    ColumnsUnreferencedCount = vpaModel.Columns.Count((t) => t.IsReferenced == false),
                    Columns = vpaModel.Columns.Select((c) =>
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
                    }),
                    Tables = vpaModel.Tables.Select((t) =>
                    {
                        var table = new TabularTable
                        {
                            Name = t.TableName,
                            RowsCount = t.RowsCount,
                            Size = t.TableSize
                        };
                        return table;
                    })
                },
                Measures = vpaxContent.DaxModel.Tables.SelectMany((t) => t.Measures).Select((m) =>
                {
                    var measure = new TabularMeasure
                    {
                        ETag = databaseETag,
                        Name = m.MeasureName.Name,
                        TableName = m.Table.TableName.Name,
                        Expression = m.MeasureExpression.Expression
                    };
                    return measure;
                })
            };

            return databaseModel;
        }
    }
}
