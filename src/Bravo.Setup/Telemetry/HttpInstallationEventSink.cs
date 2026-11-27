using System;
using System.Net;
using System.Threading.Tasks;

namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// Posts an event to the ingestion of Application Insights, in one request, through the proxy of the system
    /// with the Windows credentials of who installs. An installation does not wait for the network: the send
    /// returns after the timeout at the latest, and an event that was not sent by then is lost.
    /// </summary>
    internal sealed class HttpInstallationEventSink : IInstallationEventSink
    {
        /// <summary>
        /// How long an installation waits for the event to be sent.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private readonly TelemetryDestination _destination;
        private readonly TimeSpan _timeout;

        public HttpInstallationEventSink(TelemetryDestination destination, TimeSpan timeout)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            _destination = destination;
            _timeout = timeout;
        }

        public void Send(InstallationEvent installationEvent)
        {
#pragma warning disable RS0030 // .NET Framework has no TimeProvider: the one clock of the library (ADR 0029).
            var time = DateTimeOffset.UtcNow;
#pragma warning restore RS0030
            var item = InstallationEventEnvelope.Write(installationEvent, _destination.InstrumentationKey, time, Guid.NewGuid());

            // On a thread of the pool: a script of the proxy of the system runs before the request, and no timeout
            // of the request reaches it. The action ends after the wait, and a send that goes on ends with its
            // process.
            _ = Task.Run(() => Post(item)).Wait(_timeout);
        }

        private void Post(byte[] item)
        {
            try
            {
                // An action gets the protocols of .NET Framework 4.0, which have no TLS 1.2; the default of the
                // system has it.
                if (ServicePointManager.SecurityProtocol != SecurityProtocolType.SystemDefault)
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                }

                var proxy = WebRequest.GetSystemWebProxy();
                proxy.Credentials = CredentialCache.DefaultCredentials;

                var request = (HttpWebRequest)WebRequest.Create(_destination.TrackAddress);
                request.Method = "POST";
                request.ContentType = "application/x-json-stream";
                request.Proxy = proxy;
                request.Timeout = (int)_timeout.TotalMilliseconds;

                // The body goes with the headers, without waiting for an answer to "Expect: 100-continue".
                request.ServicePoint.Expect100Continue = false;

                using (var body = request.GetRequestStream())
                {
                    body.Write(item, 0, item.Length);
                }

                using (request.GetResponse())
                {
                }
            }
            catch (Exception)
            {
                // Not sent, whatever failed, from the proxy to the authentication: the installation goes on, as with
                // 1.0.x.
            }
        }
    }
}
