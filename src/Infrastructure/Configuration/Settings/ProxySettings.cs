namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Security;
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.Json.Serialization;

    public class ProxySettings
    {
        [JsonPropertyName("type")]
        public ProxyType Type { get; set; } = ProxyType.System;

        /// <summary>
        /// Indicate whether the <see cref="CredentialCache.DefaultCredentials"/> system credentials of the application are sent with requests.
        /// https://docs.microsoft.com/en-us/power-bi/connect-data/desktop-troubleshooting-sign-in#using-default-system-credentials-for-web-proxy
        /// </summary>
        [JsonPropertyName("useDefaultCredentials")]
        public bool UseDefaultCredentials { get; set; } = true;

        /// <summary>
        /// The address of the proxy server.
        /// </summary>
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        /// <summary>
        /// Indicates whether to bypass the proxy server for local addresses. The default value is true.
        /// </summary>
        [JsonPropertyName("bypassOnLocal")]
        public bool BypassOnLocal { get; set; } = true;

        /// <summary>
        /// An array of addresses that do not use the proxy server
        /// </summary>
        [JsonPropertyName("bypassList")]
        public string[]? BypassList { get; set; }

        internal ICredentials? GetCredentials()
        {
            if (UseDefaultCredentials == false)
            {
                if (CredentialManager.TryGetCredential(targetName: AppEnvironment.CredentialManagerProxyCredentialName, out var genericCredential))
                {
                    var credentials = genericCredential.ToNetworkCredential();
                    return credentials;
                }
            }

            return CredentialCache.DefaultCredentials;
        }

        internal static string[] GetSafeBypassList(string[]? bypassList, bool includeLoopback)
        {
            if (bypassList is null)
                bypassList = Array.Empty<string>();

            var safeBypassList = bypassList.ToList();
            _ = safeBypassList.RemoveAll(NetworkHelper.LoopbackProxyBypassRule.EqualsTI); // To prevent traffic to localhost from being sent through a proxy

            if (includeLoopback)
            {
                // Include loopback addresses to avoid routing local WebAPI traffic through a web proxy
                // Applied even though it would not be necessary since the browser applies implicit bypass rules - https://chromium.googlesource.com/chromium/src/+/HEAD/net/docs/proxy.md#implicit-bypass-rules
                //--
                // !!! Keep these rules at the end of the string/list, this is because sorting can matter when using a subtractive rule, as the rules will be evaluated in a left to right order
                // --
                safeBypassList.Add("{0}".FormatInvariant(IPAddress.Loopback));
                safeBypassList.Add("{0}".FormatInvariant(IPAddress.IPv6Loopback)); // IPv6 literals must not be bracketed
            }

            return safeBypassList.ToArray();
        }
    }

    public enum ProxyType
    {
        /// <summary>
        /// Specifies not to use a Proxy, even if the system is otherwise configured to use one. It overrides and ignore any other proxy settings that are provided
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies to try and automatically detect the system proxy configuration. This is the default value
        /// </summary>
        System = 1,

        /// <summary>
        /// Specifies to use a custom proxy configuration and applies all other proxy settings that are provided
        /// </summary>
        Custom = 2,
    }
}
