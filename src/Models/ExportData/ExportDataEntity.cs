namespace Sqlbi.Bravo.Models.ExportData
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public abstract class ExportDataEntity
    {
        [JsonPropertyName("status")]
        public ExportDataStatus Status { get; set; }

        public void SetRunning() => Status = ExportDataStatus.Running;

        public void SetCompleted() => Status = ExportDataStatus.Completed;

    }

    public class ExportDataJob : ExportDataEntity
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("tables")]
        public HashSet<ExportDataTable> Tables { get; set; } = new();

        public void SetCanceled() => Status = ExportDataStatus.Canceled;

        public void SetFailed() => Status = ExportDataStatus.Failed;

        public static ExportDataJob CreateFrom(ExportDataSettings settings)
        {
            var job = new ExportDataJob
            {
                Path = settings.ExportPath
            };

            return job;
        }
    }

    public class ExportDataTable : ExportDataEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rows")]
        public int Rows { get; set; } = 0;

        [JsonPropertyName("columns")]
        public int Columns { get; set; } = 0;

        public void SetTruncated() => Status = ExportDataStatus.Truncated;
    }

    internal static class ExportDataJobExtensions
    {
        public static ExportDataTable AddNew(this ExportDataJob job, string name)
        {
            var table = new ExportDataTable
            {
                Name = name,
            };

            job.Tables.Add(table);
            table.SetRunning();

            return table;
        }
    }
}
