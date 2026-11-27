using System;
using System.Globalization;
using System.IO;

namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// An event as the ingestion of Application Insights takes it: one item of JSON, with the names, the context and
    /// the properties that the SDK of 1.0.x sends with it. Nothing names the computer: no role instance, no node
    /// name.
    /// </summary>
    internal static class InstallationEventEnvelope
    {
        /// <param name="installationEvent">The event.</param>
        /// <param name="instrumentationKey">The key of the resource.</param>
        /// <param name="time">When the event happened.</param>
        /// <param name="session">The session of the event: 1.0.x gives each event one of its own.</param>
        /// <returns>The item, in UTF-8.</returns>
        public static byte[] Write(InstallationEvent installationEvent, string instrumentationKey, DateTimeOffset time, Guid session)
        {
            if (installationEvent == null)
            {
                throw new ArgumentNullException(nameof(installationEvent));
            }

            if (instrumentationKey == null)
            {
                throw new ArgumentNullException(nameof(instrumentationKey));
            }

            var product = installationEvent.Product;
            using (var item = new MemoryStream())
            {
                using (var writer = Json.CreateWriter(item, indent: false))
                {
                    Json.WriteStartObject(writer);
                    Json.WriteString(writer, "name", $"Microsoft.ApplicationInsights.{instrumentationKey.Replace("-", "")}.Event");
                    Json.WriteString(writer, "time", time.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));
                    Json.WriteString(writer, "iKey", instrumentationKey);

                    Json.WriteStartObject(writer, "tags");
                    Json.WriteString(writer, "ai.user.id", installationEvent.UserId);
                    Json.WriteString(writer, "ai.session.id", session.ToString());
                    Json.WriteString(writer, "ai.device.osVersion", installationEvent.OperatingSystem);
                    Json.WriteString(writer, "ai.application.ver", product.Version);
                    writer.WriteEndElement();

                    Json.WriteStartObject(writer, "data");
                    Json.WriteString(writer, "baseType", "EventData");
                    Json.WriteStartObject(writer, "baseData");
                    Json.WriteNumber(writer, "ver", 2);
                    Json.WriteString(writer, "name", installationEvent.Name);
                    Json.WriteStartObject(writer, "properties");
                    Json.WriteString(writer, "InstallScope", product.InstallScope);
                    Json.WriteString(writer, "ProductName", product.Name);
                    Json.WriteString(writer, "Version", product.Version);
                    Json.WriteString(writer, "Build", product.Build);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }

                return item.ToArray();
            }
        }
    }
}
