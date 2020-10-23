using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Net;
using System.Net.Http;

namespace Sqlbi.Bravo.Core.Client.Http
{
    internal abstract class HttpClientMessageHandler : HttpClientHandler
    {
        public HttpClientMessageHandler(IServiceProvider provider)
        {
            var settings = provider.GetRequiredService<IGlobalSettingsProviderService>();
            if (settings.Application.ProxyUseSystem == false)
            {
                UseProxy = true;
                Proxy = new WebProxy(settings.Application.ProxyAddress);
                UseDefaultCredentials = settings.Application.ProxyUser == null;

                if (!UseDefaultCredentials)
                {
                    var username = settings.Application.ProxyUser;
                    var password = settings.Application.ProxyPassword?.ToSecureString();
                    Credentials = new NetworkCredential(username, password);
                }
            }
        }
    }
}
