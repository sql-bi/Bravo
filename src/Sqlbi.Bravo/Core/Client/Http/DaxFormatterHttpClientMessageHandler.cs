using System;
using System.Net;
using System.Security.Authentication;

namespace Sqlbi.Bravo.Core.Client.Http
{
    internal class DaxFormatterHttpClientMessageHandler : HttpClientMessageHandler
    {
        public DaxFormatterHttpClientMessageHandler(IServiceProvider provider)
            : base(provider)
        {
            AllowAutoRedirect = false;
            SslProtocols = SslProtocols.Tls12;
            AutomaticDecompression = DecompressionMethods.GZip;
        }
    }
}
