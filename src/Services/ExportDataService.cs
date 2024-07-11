namespace Sqlbi.Bravo.Services
{
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;
    using LargeXlsx;
    using Microsoft.AnalysisServices.AdomdClient;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.ExportData;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ExportData;
    using System;
    using System.Data;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    public interface IExportDataService
    {
        ExportDataJob ExportDelimitedTextFile(PBIDesktopReport report, ExportDelimitedTextSettings settings, string path, CancellationToken cancellationToken);

        ExportDataJob ExportDelimitedTextFile(PBICloudDataset dataset, ExportDelimitedTextSettings settings, string path, string accessToken, CancellationToken cancellationToken);

        ExportDataJob ExportExcelFile(PBIDesktopReport report, ExportExcelSettings settings, string path, bool useZip64, CancellationToken cancellationToken);

        ExportDataJob ExportExcelFile(PBICloudDataset dataset, ExportExcelSettings settings, string path, string accessToken, bool useZip64, CancellationToken cancellationToken);

        ExportDataJob? QueryExportJob(PBIDesktopReport report);

        ExportDataJob? QueryExportJob(PBICloudDataset dataset);
    }

    internal class ExportDataService : IExportDataService
    {
        private const int BatchSize = 10_000;
        private const int ExcelMaxRows = 1_000_000; // Excel cannot exceed the limit of 1,048,576 rows and 16,384 columns

        private static readonly TypeConverterOptions _defaultDelimitedTextTypeConverterOptions = new()
        {
            Formats = new[]
            {
                "yyyy-MM-dd HH:mm:ss.fff" // We force the '.' as the preferred separator between the time element and its fraction - see https://github.com/sql-bi/Bravo/issues/549
            }
        };

        private readonly ExportDataJobMap<PBICloudDataset> _datasetJobs = new();
        private readonly ExportDataJobMap<PBIDesktopReport> _reportJobs = new();

        public ExportDataJob ExportDelimitedTextFile(PBIDesktopReport report, ExportDelimitedTextSettings settings, string path, CancellationToken cancellationToken)
        {
            settings.ExportPath = path;

            var job = _reportJobs.AddNew(report, settings);
            try
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(report);

                ExportDelimitedTextFileImpl(job, settings, connection, cancellationToken);
                job.SetCompleted();
            }
            catch (OperationCanceledException)
            {
                job.SetCanceled();
            }
            catch (Exception ex)
            {
                job.SetFailed();
                throw new BravoException(BravoProblem.ExportDataFileError, ex.Message, ex);
            }
            finally
            {
                _reportJobs.Remove(report);
            }

            return job;
        }

        public ExportDataJob ExportDelimitedTextFile(PBICloudDataset dataset, ExportDelimitedTextSettings settings, string path, string accessToken, CancellationToken cancellationToken)
        {
            settings.ExportPath = path;

            var job = _datasetJobs.AddNew(dataset, settings);
            try
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(dataset, accessToken);

                ExportDelimitedTextFileImpl(job, settings, connection, cancellationToken);
                job.SetCompleted();
            }
            catch (OperationCanceledException)
            {
                job.SetCanceled();
            }
            catch (Exception ex)
            {
                job.SetFailed();
                throw new BravoException(BravoProblem.ExportDataFileError, ex.Message, ex);
            }
            finally
            {
                _datasetJobs.Remove(dataset);
            }

            return job;
        }

        public ExportDataJob ExportExcelFile(PBIDesktopReport report, ExportExcelSettings settings, string path, bool useZip64, CancellationToken cancellationToken)
        {
            settings.ExportPath = path;

            var job = _reportJobs.AddNew(report, settings);
            try
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(report);

                ExportExcelFileImpl(job, settings, connection, useZip64, cancellationToken);
                job.SetCompleted();
            }
            catch (OperationCanceledException)
            {
                job.SetCanceled();
            }
            catch (Exception ex)
            {
                job.SetFailed();
                throw new BravoException(BravoProblem.ExportDataFileError, ex.Message, ex);
            }
            finally
            {
                _reportJobs.Remove(report);
            }

            return job;
        }

        public ExportDataJob ExportExcelFile(PBICloudDataset dataset, ExportExcelSettings settings, string path, string accessToken, bool useZip64, CancellationToken cancellationToken)
        {
            settings.ExportPath = path;

            var job = _datasetJobs.AddNew(dataset, settings);
            try
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(dataset, accessToken);

                ExportExcelFileImpl(job, settings, connection, useZip64, cancellationToken);
                job.SetCompleted();
            }
            catch (OperationCanceledException)
            {
                job.SetCanceled();
            }
            catch (Exception ex)
            {
                job.SetFailed();
                throw new BravoException(BravoProblem.ExportDataFileError, ex.Message, ex);
            }
            finally
            {
                _datasetJobs.Remove(dataset);
            }

            return job;
        }

        public ExportDataJob? QueryExportJob(PBIDesktopReport report)
        {
            _reportJobs.TryGet(report, out var job);

            return job;
        }

        public ExportDataJob? QueryExportJob(PBICloudDataset dataset)
        {
            _datasetJobs.TryGet(dataset, out var job);

            return job;
        }

        private static void ExportDelimitedTextFileImpl(ExportDataJob job, ExportDelimitedTextSettings settings, AdomdConnectionWrapper connection, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(settings.ExportPath);

            var config = new CsvConfiguration(CultureInfo.CurrentCulture);
            {
                config.Delimiter = settings.Delimiter.NullIfEmpty() ?? CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                config.Validate();
            }

            using var command = connection.CreateAdomdCommand();
            using var _ = cancellationToken.Register(() => command.Cancel());

            foreach (var tableName in settings.Tables)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var table = job.AddNew(tableName);
                var fileTableName = tableName.ReplaceInvalidFileNameChars();
                var fileName = Path.ChangeExtension(fileTableName, "csv");
                var path = Path.Combine(settings.ExportPath, fileName);

                Encoding encoding = settings.UnicodeEncoding
                    ? new UnicodeEncoding()
                    : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

                using var streamWriter = new StreamWriter(path, append: false, encoding);
                using var csvWriter = new CsvWriter(streamWriter, config);

                Export(command, table, csvWriter, settings.QuoteStringFields, rowBatchMode: connection.IsDaxFunctionTopNSkipSupported, cancellationToken);
            }

            static void Export(AdomdCommand command, ExportDataTable table, CsvWriter writer, bool quoteStringFields, bool rowBatchMode, CancellationToken cancellationToken)
            {
                var tableName = TabularModelHelper.GetDaxTableName(table.Name);

                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{nameof(ExportDataService)}.{nameof(ExportDelimitedTextFileImpl)}.{nameof(Export)}", content: $"Export table {tableName} in {(rowBatchMode ? "row batch" : "full")} mode");

                if (rowBatchMode)
                {
                    var batchCount = 0;
                    do
                    {
                        // Sort order based on RowNumber column. Order changes when the table is refreshed.
                        command.CommandText = $"EVALUATE TOPNSKIP({BatchSize}, {batchCount++ * BatchSize}, {tableName})";
                        using var dataReader = command.ExecuteReader(CommandBehavior.SingleResult);

                        if (batchCount == 1) WriteHeader(table, writer, dataReader, quoteStringFields);
                        var batchRows = WriteData(table, writer, dataReader, quoteStringFields, cancellationToken);
                        if (batchRows == 0)
                            break;
                    }
                    while (true);
                }
                else
                {
                    command.CommandText = $"EVALUATE {tableName}";
                    using var dataReader = command.ExecuteReader(CommandBehavior.SingleResult);

                    WriteHeader(table, writer, dataReader, quoteStringFields);
                    WriteData(table, writer, dataReader, quoteStringFields, cancellationToken);
                }

                table.SetCompleted();
            }

            static void WriteHeader(ExportDataTable table, CsvWriter writer, IDataReader reader, bool quoteStringFields)
            {
                // output dates using ISO 8601 format
                writer.Context.TypeConverterOptionsCache.AddOptions(typeof(DateTime), options: _defaultDelimitedTextTypeConverterOptions);
                table.Columns = reader.FieldCount;

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = GetDaxColumnName(reader, i);

                    if (quoteStringFields)
                        writer.WriteField(columnName, shouldQuote: true);
                    else
                        writer.WriteField(columnName); // use default ConfigurationFunctions.ShouldQuote()
                }

                writer.NextRecord();
            }

            static int WriteData(ExportDataTable table, CsvWriter writer, IDataReader reader, bool quoteStringFields, CancellationToken cancellationToken)
            {
                var rows = 0;
                while (reader.Read())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var field = reader[i];

                        if (reader.GetFieldType(i) == typeof(string))
                        {
                            var stringField = reader.IsDBNull(i) ? string.Empty : field.ToString();

                            if (quoteStringFields)
                                writer.WriteField(stringField, shouldQuote: true);
                            else
                                writer.WriteField(field); // use default ConfigurationFunctions.ShouldQuote()
                        }
                        else
                        {
                            writer.WriteField(field);
                        }
                    }

                    rows++;
                    table.Rows++;
                    writer.NextRecord();

                    if (table.Rows % 1_000 == 0)
                        cancellationToken.ThrowIfCancellationRequested();
                }
                return rows;
            }
        }

        private static void ExportExcelFileImpl(ExportDataJob job, ExportExcelSettings settings, AdomdConnectionWrapper connection, bool useZip64, CancellationToken cancellationToken)
        {
            var xlsxFile = new FileInfo(settings.ExportPath);

            BravoUnexpectedException.ThrowIfNull(xlsxFile.Directory);
            Directory.CreateDirectory(xlsxFile.Directory.FullName);

            using var command = connection.CreateAdomdCommand();
            using var _ = cancellationToken.Register(() => command.Cancel());

            if (useZip64 && AppEnvironment.IsDiagnosticLevelVerbose)
            {
                // Zip64 is an experimental feature in Bravo that must be explicitly enabled by the user and is not enabled by default because it may create a file that Excel or LibreOffice reports as corrupt.
                // See https://github.com/salvois/LargeXlsx/issues/3#issuecomment-867675374 and https://github.com/salvois/LargeXlsx/issues/5#issuecomment-981072044
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{nameof(ExportDataService)}.{nameof(ExportExcelFileImpl)}", content: $"Experimental feature: useZip64 is enabled");
            }

            using var fileStream = new FileStream(xlsxFile.FullName, FileMode.Create, FileAccess.Write);
            using var xlsxWriter = new XlsxWriter(fileStream, useZip64: useZip64);

            foreach (var (tableName, tableIndex) in settings.Tables.WithIndex())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var table = job.AddNew(tableName);
                var worksheetName = GetWorksheetName(tableName, tableIndex);
                
                xlsxWriter.BeginWorksheet(worksheetName, splitRow: 1);
                Export(command, table, xlsxWriter, rowBatchMode: connection.IsDaxFunctionTopNSkipSupported, cancellationToken);

                if (table.Rows > 0 && table.Columns > 0)
                    xlsxWriter.SetAutoFilter(fromRow: 1, fromColumn: 1, rowCount: table.Rows, columnCount: table.Columns);
            }

            WriteSummary(job, settings, xlsxWriter);

            static string GetWorksheetName(string tableName, int tableIndex)
            {
                const char ApostropheChar = '\'';
                const int WorksheetNameMaxLength = 31;
                /* const */ var WorksheetNameForbiddenChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };

                var worksheetName = tableName;
                var appendSuffix = false;
                var suffix = $"#{ tableIndex }";

                // Worksheet name cannot be left blank
                if (worksheetName == string.Empty)
                {
                    worksheetName = "Sheet";
                    appendSuffix = true;
                }

                // Worksheet cannot be named 'history' because it's a reserved name
                if (worksheetName.EqualsI("history"))
                    appendSuffix = true;

                // Apostrophe cannot be used at the beginning or end of the worksheet name
                if (worksheetName.StartsWith(ApostropheChar) || worksheetName.EndsWith(ApostropheChar))
                {
                    worksheetName = worksheetName.TrimStart(ApostropheChar).TrimEnd(ApostropheChar);
                    appendSuffix = true;
                }

                // Worksheet name does not contain forbidden characters
                if (worksheetName.IndexOfAny(WorksheetNameForbiddenChars) != -1)
                {
                    foreach (var forbiddenChar in WorksheetNameForbiddenChars)
                        worksheetName = worksheetName.Replace(forbiddenChar, '_');

                    appendSuffix = true;
                }

                // Worksheet name cannot exceed 31 characters
                if (worksheetName.Length > WorksheetNameMaxLength)
                {
                    worksheetName = worksheetName[..(WorksheetNameMaxLength - suffix.Length)];
                    appendSuffix = true;
                }

                if (appendSuffix)
                {
                    worksheetName += suffix;
                }

                return worksheetName;
            }

            static void Export(AdomdCommand command, ExportDataTable table, XlsxWriter writer, bool rowBatchMode, CancellationToken cancellationToken)
            {
                var tableName = TabularModelHelper.GetDaxTableName(table.Name);

                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{nameof(ExportDataService)}.{nameof(ExportExcelFileImpl)}.{nameof(Export)}", content: $"Export table {tableName} in {(rowBatchMode ? "row batch" : "full")} mode");

                if (rowBatchMode)
                {
                    var batchCount = 0;
                    do
                    {
                        // Sort order based on RowNumber column. Order changes when the table is refreshed.
                        command.CommandText = $"EVALUATE TOPNSKIP({BatchSize}, {batchCount++ * BatchSize}, {tableName})";
                        using var reader = command.ExecuteReader(CommandBehavior.SingleResult);

                        if (batchCount == 1) WriteHeader(table, writer, reader);
                        var batchRows = WriteData(table, writer, reader, cancellationToken);
                        if (batchRows == 0 || table.Status == ExportDataStatus.Truncated)
                            break;
                    }
                    while (true);
                }
                else
                {
                    command.CommandText = $"EVALUATE {tableName}";
                    using var reader = command.ExecuteReader(CommandBehavior.SingleResult);

                    WriteHeader(table, writer, reader);
                    WriteData(table, writer, reader, cancellationToken);
                }

                if (table.Status == ExportDataStatus.Running)
                    table.SetCompleted();
            }

            static void WriteHeader(ExportDataTable table, XlsxWriter writer, IDataReader reader)
            {
                // TODO: improve the column format(XlsxStyle) by using the TOM.Column.FormatString property (see DaxStudio.UI.Utils.XlsxHelper.GetStyle)

                var headerStyle = new XlsxStyle(
                    font: new XlsxFont(XlsxFont.Default.Name, XlsxFont.Default.Size, Color.White, bold: true),
                    fill: new XlsxFill(Color.FromArgb(0, 0x45, 0x86)),
                    border: XlsxStyle.Default.Border,
                    numberFormat: XlsxStyle.Default.NumberFormat,
                    alignment: XlsxAlignment.Default);

                writer.SetDefaultStyle(headerStyle).BeginRow();
                table.Columns = reader.FieldCount;

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = GetDaxColumnName(reader, i);
                    writer.Write(columnName);
                }

                // restore default style
                writer.SetDefaultStyle(XlsxStyle.Default);
            }

            static int WriteData(ExportDataTable table, XlsxWriter writer, IDataReader reader, CancellationToken cancellationToken)
            {
                var dateTimeStyle = XlsxStyle.Default.With(new XlsxNumberFormat($"yyyy-mm-dd hh:mm:ss"));
                var rows = 0;

                while (reader.Read())
                {
                    writer.BeginRow();

                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader[i];

                        switch (value)
                        {
                            case null:
                                writer.Write();
                                break;
                            case int @int:
                                writer.Write(@int);
                                break;
                            case double @double when !double.IsNaN(@double) && !double.IsPositiveInfinity(@double) && !double.IsNegativeInfinity(@double):
                                writer.Write(@double);
                                break;
                            case decimal @decimal:
                                writer.Write(@decimal);
                                break;
                            case DateTime dateTime:
                                writer.Write(dateTime, dateTimeStyle);
                                break;
                            case string @string:
                                writer.Write(@string);
                                break;
                            case bool @bool:
                                writer.Write(@bool.ToString());
                                break;
                            case long @long when @long >= int.MinValue && @long <= int.MaxValue:
                                writer.Write(Convert.ToInt32(@long));
                                break;
                            default:
                                writer.Write(value.ToString());
                                break;
                        }
                    }

                    rows++;

                    if (++table.Rows >= ExcelMaxRows)
                    { 
                        table.SetTruncated();
                        return rows;
                    }

                    if (table.Rows % 1_000 == 0)
                        cancellationToken.ThrowIfCancellationRequested();
                }

                return rows;
            }

            static void WriteSummary(ExportDataJob job, ExportExcelSettings settings, XlsxWriter writer)
            {
                if (!settings.CreateExportSummary)
                    return;

                var headerStyle = new XlsxStyle(
                    font: new XlsxFont(XlsxFont.Default.Name, XlsxFont.Default.Size, Color.White, bold: true),
                    fill: new XlsxFill(Color.FromArgb(0, 0x45, 0x86)),
                    border: XlsxStyle.Default.Border,
                    numberFormat: XlsxStyle.Default.NumberFormat,
                    alignment: XlsxAlignment.Default);
                var infoStyle = XlsxStyle.Default.With(font: new XlsxFont(XlsxFont.Default.Name, XlsxFont.Default.Size, XlsxFont.Default.Color, bold: true));
                var warningStyle = XlsxStyle.Default.With(fill: new XlsxFill(Color.FromArgb(0xff, 0xff, 0x88))).With(border: XlsxBorder.Around(around: new XlsxBorder.Line(Color.DeepPink, XlsxBorder.Style.Dashed)));

                writer.BeginWorksheet("Bravo Export Summary");
                writer.BeginRow().Write($"Exported with { AppEnvironment.ApplicationMainWindowTitle }", style: infoStyle);
                writer.BeginRow().Write($"Version { AppEnvironment.ApplicationProductVersion } (build { AppEnvironment.ApplicationFileVersion })", style: infoStyle);
                writer.SkipRows(1);
                writer.SetDefaultStyle(headerStyle).BeginRow().Write("Worksheet").Write("Table").Write("Rows").Write("Status");
                writer.SetDefaultStyle(XlsxStyle.Default);

                foreach (var (tableName, tableIndex) in settings.Tables.WithIndex())
                {
                    var table = job.Tables.Single((t) => t.Name.Equals(tableName));
                    var statusStyle = table.Status == ExportDataStatus.Truncated ? warningStyle : XlsxStyle.Default;
                    var worksheetName = GetWorksheetName(tableName, tableIndex);

                    writer.BeginRow();
                    writer.Write(worksheetName).Write(tableName).Write(table.Rows).Write(table.Status.ToString(), statusStyle);
                }

                writer.SetAutoFilter(fromRow: 4, fromColumn: 1, rowCount: writer.CurrentRowNumber, columnCount: 4);
            }
        }

        private static string? GetDaxColumnName(IDataReader reader, int fieldIndex)
        {
            var fullyQualifiedName = reader.GetName(fieldIndex);
            if (fullyQualifiedName is not null)
            {
                var columnName = fullyQualifiedName.GetDaxName();
                return columnName;
            }

            return null;
        }

        /*
                private static IDataReader CreateTestData()
                {
                    const int Columns = 100;
                    const int Rows = 10_000;

                    var table = new DataTable();

                    for (int c = 0; c < Columns; c++)
                    {
                        table.Columns.Add(new DataColumn($"col{ c }", typeof(string)));
                    }

                    for (int r = 0; r < Rows; r++)
                    {
                        var row = table.NewRow();
                        {
                            for (var c = 0; c < Columns; c++)
                            {
                                row[c] = (r == c) ? null : $"Sample text [{ c },{ r }]";
                            }
                        }
                        table.Rows.Add(row);
                    }

                    return table.CreateDataReader();
                }
        */
    }
}
