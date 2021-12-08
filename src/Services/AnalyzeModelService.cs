using Bravo.Models;
using Dax.Metadata.Extractor;
using Dax.ViewModel;
using Dax.Vpax.Tools;
using Sqlbi.Bravo.Infrastructure;
using System.IO;
using System.Linq;

namespace Sqlbi.Bravo.Services
{
    internal class AnalyzeModelService : IAnalyzeModelService
    {
        public DatabaseModel GetDatabaseModelFromVpax(Stream stream)
        {
            var vpaxContent = VpaxTools.ImportVpax(stream);
            var databaseModel = GetDatabaseModelFromVpaxMetadataModel(vpaxContent.DaxModel);

            return databaseModel;
        }

        public DatabaseModel GetDatabaseModelFromSSAS(PBIDesktopModel pbidesktop, bool readStatisticsFromData = true, int sampleRows = 0)
        {
            var daxModel = TomExtractor.GetDaxModel(
                pbidesktop.ServerName,
                pbidesktop.DatabaseName,
                AppConstants.ApplicationName,
                AppConstants.ApplicationFileVersion,
                readStatisticsFromData,
                sampleRows
                );

            var databaseModel = GetDatabaseModelFromVpaxMetadataModel(daxModel);

            return databaseModel;
        }

        private DatabaseModel GetDatabaseModelFromVpaxMetadataModel(Dax.Metadata.Model daxModel)
        {
            var vpaModel = new VpaModel(daxModel);

            var databaseSize = vpaModel.Columns.Sum((c) => c.TotalSize);
            var databaseModel = new DatabaseModel
            {
                Info = new DatabaseModelInfo
                {
                    TablesCount = vpaModel.Tables.Count(),
                    ColumnsCount = vpaModel.Columns.Count(),
                    TablesMaxRowsCount = vpaModel.Tables.Max((t) => t.RowsCount),
                    DatabaseSize = databaseSize,
                    ColumnsUnreferencedCount = vpaModel.Columns.Count((t) => t.IsReferenced == false),
                    Columns = vpaModel.Columns.Select((c) =>
                    {
                        var column = new DatabaseModelColumn
                        {
                            Name = c.ColumnName,
                            TableName = c.Table.TableName,
                            Cardinality = c.ColumnCardinality,
                            Size = c.TotalSize,
                            Weight = (double)c.TotalSize / databaseSize,
                            IsReferenced = c.IsReferenced,
                        };
                        return column;
                    })
                },
                Measures = daxModel.Tables.SelectMany((t) => t.Measures).Select((m) =>
                {
                    var measure = new DatabaseModelMeasure
                    {
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
