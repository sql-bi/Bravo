namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using System;
    using System.Diagnostics;

    internal static class TelemetryHelper
    {
        /// <summary>
        /// Default endpoint for Ingestion
        /// Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints.Constants
        /// </summary>
        private const string DefaultIngestionEndpoint = "https://dc.services.visualstudio.com/";

        private static readonly Lazy<TelemetryConfiguration> _telemetryConfiguration = new(Configure(new TelemetryConfiguration()));

        public static TelemetryConfiguration Configure(TelemetryConfiguration configuration)
        {
            //configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use((next) => new AppTelemetryProcessor(next)).Build();
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            configuration.TelemetryInitializers.Add(new AppTelemetryInitializer());
            configuration.InstrumentationKey = AppEnvironment.TelemetryInstrumentationKey;
            configuration.DisableTelemetry = UserPreferences.Current.TelemetryEnabled == false;
#if DEBUG
            configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
#endif
            return configuration;
        }

        public static TelemetryConfiguration TelemetryConfigurationInstance => _telemetryConfiguration.Value;

        public static void TrackException(Exception exception)
        {
            if (exception is AggregateException aex)
                exception = aex.GetBaseException();

            var telemetryClient = new TelemetryClient(TelemetryConfigurationInstance);
            telemetryClient.TrackException(exception);
            telemetryClient.Flush(); // Flush is blocking when using InMemoryChannel, no need for Sleep/Delay
        }

        public static bool IsTelemetryUri(string uriString)
        {
            var requestedUri = new Uri(uriString);
            var telemetryUri = new Uri(uriString: DefaultIngestionEndpoint);

            var result = Uri.Compare(requestedUri, telemetryUri, UriComponents.Scheme | UriComponents.HostAndPort, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase);
            return result == 0;
        }
    }
}