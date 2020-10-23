using System;

namespace Sqlbi.Bravo.Core.Client.Http
{
    [Flags]
    internal enum DaxFormatterTabularObjectType
    {
        None = 0,

        /// <summary>
        /// Objects of type Microsoft.AnalysisServices.Tabular.Measure in a Tabular model.
        /// </summary>
        Measures = 1,

        /// <summary>
        /// Objects of type Microsoft.AnalysisServices.Tabular.Column in a Tabular model where ColumnType is ColumnType.Calculated.
        /// </summary>
        CalculatedColumns = 2,

        /// <summary>
        /// Objects of type Microsoft.AnalysisServices.Tabular.KPI in a Tabular model.
        /// </summary>
        KPIs = 4,

        /// <summary>
        /// Objects of type Microsoft.AnalysisServices.Tabular.DetailRowsDefinition in a Tabular model.
        /// </summary>
        DetailRowsDefinitions = 8,

        /// <summary>
        /// Objects of type Microsoft.AnalysisServices.Tabular.CalculationItem in a Tabular model.
        /// </summary>
        CalculationItems = 16,

        /// <summary>
        /// All objects in a Tabular model.
        /// </summary>
        All = Measures | CalculatedColumns | KPIs | DetailRowsDefinitions | CalculationItems,
    }
}
