using System;

namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// The resource of Application Insights that the events go to: its instrumentation key, and the address that
    /// takes the items.
    /// </summary>
    internal sealed class TelemetryDestination
    {
        // The resource that the application sends to, the one of 1.0.x: a test checks that both are the same. Only
        // the build of the packages sends to it.
        internal const string ReleaseConnectionString = "InstrumentationKey=47a8970c-6293-408a-9cce-5b7b311574d3";

        // Where the SDKs send when the connection string names no endpoint, as the one of 1.0.x.
        private const string DefaultIngestionEndpoint = "https://dc.services.visualstudio.com/";

        /// <param name="instrumentationKey">The key of the resource, which every item carries.</param>
        /// <param name="trackAddress">The address that the items are posted to.</param>
        public TelemetryDestination(string instrumentationKey, Uri trackAddress)
        {
            InstrumentationKey = instrumentationKey;
            TrackAddress = trackAddress;
        }

        public string InstrumentationKey { get; }

        public Uri TrackAddress { get; }

        /// <summary>
        /// Reads the instrumentation key and the ingestion endpoint of a connection string. The other fields are
        /// for the SDKs, which read more of them.
        /// </summary>
        /// <exception cref="FormatException">
        /// The connection string has no instrumentation key, or an endpoint that is not an absolute address.
        /// </exception>
        public static TelemetryDestination Parse(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            string instrumentationKey = null;
            var endpoint = DefaultIngestionEndpoint;
            foreach (var field in connectionString.Split(';'))
            {
                var equals = field.IndexOf('=');
                if (equals < 0)
                {
                    continue;
                }

                var name = field.Substring(0, equals).Trim();
                var value = field.Substring(equals + 1).Trim();
                if (value.Length == 0)
                {
                    continue;
                }

                if (name.Equals("InstrumentationKey", StringComparison.OrdinalIgnoreCase))
                {
                    instrumentationKey = value;
                }
                else if (name.Equals("IngestionEndpoint", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = value;
                }
            }

            if (instrumentationKey == null)
            {
                throw new FormatException("The connection string has no instrumentation key.");
            }

            // The path of the SDKs after the endpoint, which is written with its last slash or without it.
            var trackAddress = new Uri(new Uri(endpoint.TrimEnd('/') + "/", UriKind.Absolute), "v2/track");
            return new TelemetryDestination(instrumentationKey, trackAddress);
        }
    }
}
