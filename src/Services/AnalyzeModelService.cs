using Bravo.Models;
using Dax.ViewModel;
using Dax.Vpax.Tools;
using System.IO;
using System.Linq;

namespace Sqlbi.Bravo.Services
{
    internal class AnalyzeModelService : IAnalyzeModelService
    {
        public DatabaseModel GetDatabaseModelFromVpax(Stream stream)
        {
            var vpaxContent = VpaxTools.ImportVpax(stream);
            var vpaModel = new VpaModel(vpaxContent.DaxModel);

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
                Measures = vpaxContent.DaxModel.Tables.SelectMany((t) => t.Measures).Select((m) =>
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
