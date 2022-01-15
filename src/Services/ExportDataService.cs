using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.AnalysisServices.AdomdClient;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Models;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Sqlbi.Bravo.Services
{
    public interface IExportDataService
    {
        void ExportDelimitedTextFile(PBIDesktopReport report, ExportDelimitedTextSettings settings, CancellationToken cancellationToken);

        void ExportDelimitedTextFile(PBICloudDataset dataset, ExportDelimitedTextSettings settings, CancellationToken cancellationToken);
    }

    internal class ExportDataService : IExportDataService
    {
        private static readonly TypeConverterOptions DefaultDelimitedTextTypeConverterOptions = new()
        {
            Formats = new[]
                {
                    string.Format("yyyy-MM-dd HH:mm:ss{0}000", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                }
        };

        private readonly IPBICloudService _pbicloudService;

        public ExportDataService(IPBICloudService pbicloudService)
        {
            _pbicloudService = pbicloudService;
        }

        public void ExportDelimitedTextFile(PBIDesktopReport report, ExportDelimitedTextSettings settings, CancellationToken cancellationToken)
        {
            var (connectionString, databaseName) = report.GetConnectionParameters();
            try
            {
                // TODO: catch exceptions
                ExportDelimitedTextFileImpl(settings, connectionString, databaseName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            // TODO: catch other exceptions
        }

        public void ExportDelimitedTextFile(PBICloudDataset dataset, ExportDelimitedTextSettings settings, CancellationToken cancellationToken)
        {
            var (connectionString, databaseName) = dataset.GetConnectionParameters(_pbicloudService.CurrentAccessToken);
            try
            {
                ExportDelimitedTextFileImpl(settings, connectionString, databaseName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            // TODO: catch other exceptions
        }

        private void ExportDelimitedTextFileImpl(ExportDelimitedTextSettings settings, string connectionString, string databaseName, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(settings.ExportPath);

            var config = new CsvConfiguration(CultureInfo.CurrentCulture);
            {
                config.Delimiter = settings.Delimiter ?? CultureInfo.CurrentUICulture.TextInfo.ListSeparator;
                config.Validate();
            }

            using var connection = new AdomdConnection(connectionString);
            connection.Open();
            connection.ChangeDatabase(databaseName);

            using var command = connection.CreateCommand();
            cancellationToken.Register(() => command.Cancel());

            foreach (var tableName in settings.Tables)
            {
                if (cancellationToken.IsCancellationRequested) 
                    break;

                var fileTableName = tableName.ReplaceInvalidFileNameChars();
                var fileName = Path.ChangeExtension(fileTableName, "csv");
                var path = Path.Combine(settings.ExportPath, fileName);

                Encoding encoding = settings.UnicodeEncoding 
                    ? new UnicodeEncoding() 
                    : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

                using var streamWriter = new StreamWriter(path, append: false, encoding);
                using var csvWriter = new CsvWriter(streamWriter, config);
                
                // TODO: if the SSAS instance supports TOPNSKIP then use that to query batches of rows
                command.CommandText = $"EVALUATE '{ tableName }'";
                command.CommandType = CommandType.Text;

                using var dataReader = command.ExecuteReader(CommandBehavior.SingleResult);
                //using var dataReader = CreateTestDataReader();

                WriteData(csvWriter, dataReader);
            }

            void WriteData(CsvWriter csvWriter, IDataReader dataReader)
            {
                // output dates using ISO 8601 format
                csvWriter.Context.TypeConverterOptionsCache.AddOptions(typeof(DateTime), options: DefaultDelimitedTextTypeConverterOptions);

                // write header
                for (int i = 0; i < dataReader.FieldCount; i++)
                    csvWriter.WriteField(dataReader.GetName(i), shouldQuote: settings.QuoteStringFields ?? false);

                csvWriter.NextRecord();

                // write data
                while (dataReader.Read())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    for (var fieldOrdinal = 0; fieldOrdinal < dataReader.FieldCount; fieldOrdinal++)
                    {
                        var fieldValue = dataReader[fieldOrdinal];

                        if (dataReader.GetFieldType(fieldOrdinal) == typeof(string))
                        {
                            csvWriter.WriteField(dataReader.IsDBNull(fieldOrdinal) ? string.Empty : fieldValue.ToString(), shouldQuote: settings.QuoteStringFields ?? false);
                        }
                        else
                        {
                            csvWriter.WriteField(fieldValue);
                        }
                    }

                    csvWriter.NextRecord();
                }
            }
        }

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
    }
}
