namespace Sqlbi.Bravo.Infrastructure.Services
{
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using System;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// Implements a <see cref="WebProxy"/> wrapper that supports the system proxy on the machine or a single manual configured proxy server
    /// </summary>
    /// <remarks>
    /// Proxy resolution through the WPAD auto-detect protocol and the Proxy Automatic Configuration (PAC) files is not currently supported
    /// </remarks>
    internal sealed class WebProxyWrapper : IWebProxy
    {
        public static readonly WebProxyWrapper Current = new();

        private static readonly object _proxyLock = new();
        private readonly IWebProxy _systemProxy;
        private IWebProxy? _customProxy;
        private IWebProxy? _noProxy;

        private WebProxyWrapper()
        {
            // For .NET Core The initial value of the static property HttpClient.DefaultProxy represents the system proxy on the machine.
            // DO NOT USE WebRequest.GetSystemWebProxy() or WebProxy.GetDefaultProxy() as these legacy methods return a proxy configured with the Internet Explorer settings
            _systemProxy = new HttpSystemProxy(HttpClient.DefaultProxy);

            WebView2Helper.SetWebView2CmdlineProxyArguments(UserPreferences.Current.Proxy, HttpClient.DefaultProxy);
        }

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
                if (webProxy is not null && webProxy.Credentials != value)
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
                return _systemProxy;
            }
        }
    }

    internal sealed class HttpSystemProxy : IWebProxy
    {
        private readonly IWebProxy _systemProxy;

        public HttpSystemProxy(IWebProxy systemProxy)
        {
            _systemProxy = systemProxy;
        }

        public ICredentials? Credentials
        { 
            get => _systemProxy.Credentials;
            set => _systemProxy.Credentials = value;
        }

        public Uri? GetProxy(Uri destination)
        {
            var proxyUri = _systemProxy.GetProxy(destination);
            return proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            var isBypassed = _systemProxy.IsBypassed(host);
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
