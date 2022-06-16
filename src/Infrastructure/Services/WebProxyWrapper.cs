namespace Sqlbi.Bravo.Infrastructure.Services
{
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using System;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// Implements a <see cref="WebProxy"/> wrapper that supports the system proxy on the machine or a single manual configured proxy server
    /// </summary>
    internal sealed class WebProxyWrapper : IWebProxy
    {
        public static readonly WebProxyWrapper Current = new();

        private static readonly object _proxyLock = new();
        private readonly IWebProxy _defaultProxy;
        private IWebProxy? _systemProxy;
        private IWebProxy? _customProxy;
        private IWebProxy? _noProxy;

        private WebProxyWrapper()
        {
            // For .NET Core The initial value of the static property HttpClient.DefaultProxy represents the system proxy on the machine.
            // DO NOT USE WebRequest.GetSystemWebProxy() or WebProxy.GetDefaultProxy() as these legacy methods return a proxy configured with the Internet Explorer settings
            _defaultProxy = HttpClient.DefaultProxy;
        }

        public IWebProxy DefaultSystemProxy => _defaultProxy;

        #region IWebProxy

        public ICredentials? Credentials
        { 
            get
            {
                var webProxy = GetWebProxy();
                return webProxy?.Credentials;
            }
            set
            {
                var webProxy = GetWebProxy();
                if (webProxy is not null)
                    webProxy.Credentials = value;
            }
        }

        public Uri? GetProxy(Uri destination)
        {
            var webProxy = GetWebProxy();
            var proxyUri = webProxy?.GetProxy(destination);

            return proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            var webProxy = GetWebProxy();
            var isBypassed = webProxy.IsBypassed(host);

            return isBypassed;
        }

        #endregion

        private IWebProxy GetWebProxy()
        {
            var proxy = UserPreferences.Current.Proxy;
            if (proxy?.Type == ProxyType.None)
            {
                if (_noProxy is null)
                {
                    lock (_proxyLock)
                    {
                        if (_noProxy is null)
                        {
                            _noProxy = new HttpNoProxy();
                        }
                    }
                }

                return _noProxy;
            }
            else if (proxy?.Type == ProxyType.Custom)
            {
                if (_customProxy is null)
                {
                    lock (_proxyLock)
                    {
                        if (_customProxy is null)
                        {
                            var credentials = proxy.GetCredentials();
                            var bypassList = ProxySettings.GetSafeBypassList(proxy.BypassList, includeLoopback: false);

                            _customProxy = new WebProxy(proxy.Address, proxy.BypassOnLocal, bypassList, credentials);
                        }
                    }
                }

                return _customProxy;
            }
            else
            {
                if (_systemProxy is null)
                {
                    lock (_proxyLock)
                    {
                        if (_systemProxy is null)
                        {   
                            _systemProxy = new HttpSystemProxy(_defaultProxy);

                            if (proxy?.UseDefaultCredentials == false)
                            {
                                var credentials = proxy.GetCredentials();
                                if (credentials is not null)
                                {
                                    _systemProxy.Credentials = credentials;
                                }
                            }
                        }
                    }
                }

                return _systemProxy;
            }
        }
    }

    internal sealed class HttpSystemProxy : IWebProxy
    {
        private readonly IWebProxy _defaultProxy;

        public HttpSystemProxy(IWebProxy defaultProxy)
        {
            _defaultProxy = defaultProxy;
        }

        public ICredentials? Credentials
        { 
            get => _defaultProxy.Credentials;
            set => _defaultProxy.Credentials = value;
        }

        public Uri? GetProxy(Uri destination)
        {
            var proxyUri = _defaultProxy.GetProxy(destination);
            return proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            var isBypassed = _defaultProxy.IsBypassed(host);
            return isBypassed;
        }
    }

    internal sealed class HttpNoProxy : IWebProxy
    {
        public ICredentials? Credentials { get; set; }

        public Uri? GetProxy(Uri destination) => null;

        public bool IsBypassed(Uri host) => true; // always bypassed
    }
}
