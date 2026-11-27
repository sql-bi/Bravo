using System;
using Bravo.Setup.Telemetry;
using Xunit;

namespace Bravo.Setup.Tests.Telemetry
{
    public sealed class TelemetryDestinationTests
    {
        private const string Key = "00000000-0000-0000-0000-000000000000";

        [Theory]
        [InlineData("InstrumentationKey=" + Key, "https://dc.services.visualstudio.com/v2/track")]
        [InlineData("InstrumentationKey=" + Key + ";IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/", "https://westeurope-5.in.applicationinsights.azure.com/v2/track")]
        [InlineData("InstrumentationKey=" + Key + ";IngestionEndpoint=http://127.0.0.1:8097", "http://127.0.0.1:8097/v2/track")]
        [InlineData(" ingestionendpoint = http://127.0.0.1:8097/ ; instrumentationkey = " + Key + " ;", "http://127.0.0.1:8097/v2/track")]
        [InlineData("InstrumentationKey=" + Key + ";IngestionEndpoint=https://example.com/ingestion;LiveEndpoint=https://example.com/live;ApplicationId=1", "https://example.com/ingestion/v2/track")]
        public void Parse_ConnectionString_IsItsKeyAndTheTrackAddressOfItsEndpoint(string connectionString, string trackAddress)
        {
            var destination = TelemetryDestination.Parse(connectionString);

            Assert.Equal(Key, destination.InstrumentationKey);
            Assert.Equal(new Uri(trackAddress), destination.TrackAddress);
        }

        [Theory]
        [InlineData("")]
        [InlineData("IngestionEndpoint=http://127.0.0.1:8097")]
        [InlineData("InstrumentationKey=")]
        [InlineData("InstrumentationKey=" + Key + ";IngestionEndpoint=127.0.0.1")]
        public void Parse_ConnectionStringThatCannotBeSentTo_Fails(string connectionString)
        {
            Assert.ThrowsAny<FormatException>(() => TelemetryDestination.Parse(connectionString));
        }

        // Every action that reports would fail on a connection string that cannot be read. The values are not
        // printed: the connection string of the releases goes into no output of a test.
        [Fact]
        public void Parse_ConnectionStringOfTheReleases_IsAKeyAndAnAddressOverHttps()
        {
            var destination = TelemetryDestination.Parse(TelemetryDestination.ReleaseConnectionString);

            Assert.True(Guid.TryParse(destination.InstrumentationKey, out _), "The instrumentation key of the releases is not a GUID.");
            Assert.True(destination.TrackAddress.Scheme == Uri.UriSchemeHttps, "The releases do not send over HTTPS.");
        }
    }
}
