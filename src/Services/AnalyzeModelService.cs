using Dax.ViewModel;
using Dax.Vpax.Tools;
using Sqlbi.Bravo.Models;
using System.IO;
using System.Linq;

namespace Sqlbi.Bravo.Services
{
    internal class AnalyzeModelService : IAnalyzeModelService
    {
        public TabularDatabase GetDatabaseFromVpax(Stream vpax)
        {
            var vpaxContent = VpaxTools.ImportVpax(stream: vpax);
            var vpaModel = new VpaModel(vpaxContent.DaxModel);

            var databaseSize = vpaModel.Columns.Sum((c) => c.TotalSize);
            var databaseModel = new TabularDatabase
            {
                Info = new TabularDatabaseInfo
                {
                    TablesCount = vpaModel.Tables.Count(),
                    ColumnsCount = vpaModel.Columns.Count(),
                    TablesMaxRowsCount = vpaModel.Tables.Max((t) => t.RowsCount),
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
                    })
                },
                Measures = vpaxContent.DaxModel.Tables.SelectMany((t) => t.Measures).Select((m) =>
                {
                    var measure = new TabularMeasure
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
