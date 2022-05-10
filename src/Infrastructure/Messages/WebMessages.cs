namespace Sqlbi.Bravo.Infrastructure.Messages
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Models;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    internal interface IWebMessage
    {
        /// <summary>
        /// Message type identifier
        /// </summary>
        WebMessageType MessageType { get; }
    }

    internal class UnknownWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.Unknown;

        [JsonPropertyName("message")]
        public JsonElement? Message { get; set; }

        [JsonPropertyName("exception")]
        public JsonElement? Exception { get; set; }

        [JsonIgnore]
        public string AsString => JsonSerializer.Serialize(this, AppEnvironment.DefaultJsonOptions);

        public static UnknownWebMessage CreateFrom(JsonElement message)
        {
            var webMessage = new UnknownWebMessage
            {
                Message = message,
                Exception = null,
            };

            return webMessage;
        }

        public static UnknownWebMessage CreateFrom(Exception exception)
        {
            if (exception is AggregateException aggregateException)
                exception = aggregateException.GetBaseException();

            var exceptionObject = new
            {
                Message = exception.Message,
                Details = exception.ToString(),
            };

            var exceptionObjectString = JsonSerializer.Serialize(exceptionObject, AppEnvironment.DefaultJsonOptions);
            var exceptionObjectJson = JsonSerializer.Deserialize<JsonElement>(exceptionObjectString);

            var webMessage = new UnknownWebMessage
            {
                Message = null,
                Exception = exceptionObjectJson,
            };

            return webMessage;
        }
    }

    internal class PBIDesktopReportOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.ReportOpen;

        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [JsonIgnore]
        public string AsString => JsonSerializer.Serialize(this, AppEnvironment.DefaultJsonOptions);

        public static PBIDesktopReportOpenWebMessage CreateFrom(PBIDesktopReport report)
        {
            var webMessage = new PBIDesktopReportOpenWebMessage
            {
                Report = report,
            };

            return webMessage;
        }
    }

    internal class PBICloudDatasetOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.DatasetOpen;

        [JsonPropertyName("dataset")]
        public PBICloudDataset? Dataset { get; set; }

        [JsonIgnore]
        public string AsString => JsonSerializer.Serialize(this, AppEnvironment.DefaultJsonOptions);
    }

    internal class VpaxFileOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.VpaxOpen;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("blob")]
        public byte[]? Content { get; set; }

        [JsonPropertyName("lastModified")]
        public long? LastModified { get; set; }

        [JsonIgnore]
        public string AsString => JsonSerializer.Serialize(this, AppEnvironment.DefaultJsonOptions);
    }
}
