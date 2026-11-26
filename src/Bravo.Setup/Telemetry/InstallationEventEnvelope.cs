using System;
using System.Buffers;
using System.Text.Json;

namespace Bravo.Setup.Telemetry;

/// <summary>
/// An event as the ingestion of Application Insights takes it: one item of JSON, with the names, the context and
/// the properties that the SDK of 1.0.x sends with it. Nothing names the computer: no role instance, no node name.
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
        ArgumentNullException.ThrowIfNull(installationEvent);
        ArgumentNullException.ThrowIfNull(instrumentationKey);

        var product = installationEvent.Product;
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("name", $"Microsoft.ApplicationInsights.{instrumentationKey.Replace("-", "", StringComparison.Ordinal)}.Event");
            writer.WriteString("time", time.UtcDateTime);
            writer.WriteString("iKey", instrumentationKey);

            writer.WriteStartObject("tags");
            writer.WriteString("ai.user.id", installationEvent.UserId);
            writer.WriteString("ai.session.id", session.ToString());
            writer.WriteString("ai.device.osVersion", installationEvent.OperatingSystem);
            writer.WriteString("ai.application.ver", product.Version);
            writer.WriteEndObject();

            writer.WriteStartObject("data");
            writer.WriteString("baseType", "EventData");
            writer.WriteStartObject("baseData");
            writer.WriteNumber("ver", 2);
            writer.WriteString("name", installationEvent.Name);
            writer.WriteStartObject("properties");
            writer.WriteString("InstallScope", product.InstallScope);
            writer.WriteString("ProductName", product.Name);
            writer.WriteString("Version", product.Version);
            writer.WriteString("Build", product.Build);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }
}
