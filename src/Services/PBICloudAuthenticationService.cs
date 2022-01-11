using Microsoft.AspNetCore.Hosting;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Authentication;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sqlbi.Bravo.Services
{
    public interface IPBICloudAuthenticationService
    {
        AuthenticationResult? Authentication { get; }

        Uri TenantCluster { get; }

        LocalClientSite? CachedUserInfo { get; }

        Task AcquireTokenAsync(TimeSpan cancelAfter, bool silentOnly, string? identifier = null);

        Task ClearTokenCacheAsync();
    }

    internal class PBICloudAuthenticationService : IPBICloudAuthenticationService
    {
        private const string MicrosoftAccountOnlyQueryParameter = "msafed=0";

        private readonly static IPublicClientApplication _application;
        private readonly static PBICloudSettings _pbisettings;

        private readonly IWebHostEnvironment _environment;
        private readonly SemaphoreSlim _tokenSemaphore = new(1);
        private readonly CustomWebViewOptions _customSystemWebViewOptions;

        static PBICloudAuthenticationService()
        {
            _pbisettings = new PBICloudSettings();

            _application = PublicClientApplicationBuilder.Create(_pbisettings.GlobalCloudEnvironment.ClientId)
                .WithAuthority(_pbisettings.GlobalCloudEnvironment.Authority)
                .WithDefaultRedirectUri()
                .Build();

            TokenCacheHelper.EnableSerialization(_application.UserTokenCache);
        }

        public PBICloudAuthenticationService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _customSystemWebViewOptions = new CustomWebViewOptions(_environment.WebRootPath);
        }

        public AuthenticationResult? Authentication { get; private set; }

        public Uri TenantCluster => new (_pbisettings.TenantCluster?.FixedClusterUri ?? throw new BravoUnexpectedException("TenantCluster is null"));

        public LocalClientSite? CachedUserInfo => _pbisettings.LocalClientSites?.Find(url: _pbisettings.GlobalCloudEnvironment.Endpoint, upn: Authentication?.Account.Username);

        /// <summary>
        /// Removes all account information from MSAL's token cache, removes app-only (not OS-wide) and does not affect the browser cookies
        /// </summary>
        public async Task ClearTokenCacheAsync()
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                Authentication = null;

                var accounts = (await _application.GetAccountsAsync().ConfigureAwait(false)).ToArray();

                foreach (var account in accounts)
                    await _application.RemoveAsync(account).ConfigureAwait(false);
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        public async Task AcquireTokenAsync(TimeSpan cancelAfter, bool silentOnly, string? identifier = null)
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(cancelAfter);

                var previousAuthentication = Authentication;
                Authentication = null;

                var currentAuthentication = await InternalAcquireTokenAsync(silentOnly, identifier, cancellationTokenSource.Token).ConfigureAwait(false);
                {
                    await _pbisettings.Refresh(currentAuthentication, previousAuthentication).ConfigureAwait(false);
                }
                Authentication = currentAuthentication;

                //var impersonateTask = System.Security.Principal.WindowsIdentity.RunImpersonatedAsync(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle.InvalidHandle, async () =>
                //{
                //    _authenticationResult = await InternalAcquireTokenAsync(cancellationTokenSource.Token, identifier);
                //});
                //await impersonateTask.ConfigureAwait(false);
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token?tabs=dotnet
        /// </summary>
        private async Task<AuthenticationResult> InternalAcquireTokenAsync(bool silentOnly, string? identifier, CancellationToken cancellationToken)
        {
            // Use account used to signed-in in Windows (WAM). WAM will always get an account in the cache.
            // So if we want to have a chance to select the accounts interactively, we need to force the non-account
            //identifier = PublicClientApplication.OperatingSystemAccount;

            // Use one of the Accounts known by Windows (WAM), if a null account identifier is provided then force WAM to display the dialog with the accounts
            var account = await _application.GetAccountAsync(identifier).ConfigureAwait(false);

            try
            {
                // Try to acquire an access token from the cache, if UI interaction is required, MsalUiRequiredException will be thrown.
                var authenticationResult = await _application.AcquireTokenSilent(_pbisettings.GlobalCloudEnvironment.Scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return authenticationResult;
            }
            catch (MsalUiRequiredException /* murex */)
            {
                if (silentOnly) throw;
                try
                {
                    var builder = _application.AcquireTokenInteractive(_pbisettings.GlobalCloudEnvironment.Scopes)
                        .WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter);

                    //.WithClaims(murex.Claims)
                    //.WithPrompt(Prompt.SelectAccount) // Force a sign-in (Prompt.SelectAccount), as the MSAL web browser might contain cookies for the current user and we don't necessarily want to re-sign-in the same user 

                    if (account is not null)
                        builder.WithAccount(account);
                    else if (identifier is not null)
                        builder.WithLoginHint(identifier);

                    if (_application.IsEmbeddedWebViewAvailable())
                    {
                        Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);
                        Debug.Assert(System.Windows.Forms.Application.MessageLoop == false);

                        var parentWindowHandle = Process.GetCurrentProcess().MainWindowHandle;

                        // *** EmbeddedWebView requiremens ***
                        // Requires VS project OutputType=WinExe and TargetFramework=net5-windows10.0.17763.0
                        // Using 'TargetFramework=net5-windows10.0.17763.0' the framework 'Microsoft.Windows.SDK.NET' is also included as project dependency.
                        // The framework 'Microsoft.Windows.SDK.NET' includes all the WPF(PresentationFramework.dll) and WinForm(System.Windows.Forms.dll) assemblies to the project.

                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: true)
                            .WithParentActivityOrWindow(parentWindowHandle); // used to center embedded wiew on the parent window
                    }
                    else
                    {
                        // If for some reason the EmbeddedWebView is not available than fall back to the SystemWebView
                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: false)
                            .WithSystemWebViewOptions(_customSystemWebViewOptions);
                    }

                    var authenticationResult = await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    return authenticationResult;
                }
                catch (MsalException) // ex.ErrorCode => Microsoft.Identity.Client.MsalError
                {
                    throw;
                }
            }
        }
    }

    internal class PBICloudSettings
    {
        private const string GlobalServiceEnvironmentsDiscoverUrl = "powerbi/globalservice/v201606/environments/discover?client=powerbi-msolap";
        private const string GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl = "spglobalservice/GetOrInsertClusterUrisByTenantlocation";
        private const string CloudEnvironmentGlobalCloudName = "GlobalCloud";

        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly HttpClient _httpClient;

        private static string LocalDataClassicAppCachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Power BI Desktop");

        private static string LocalDataStoreAppCachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Microsoft\\Power BI Desktop Store App");

        public GlobalService GlobalService { get; init; }

        public CloudEnvironment GlobalCloudEnvironment { get; init; }

        public TenantCluster? TenantCluster { get; private set; }

        public LocalClientSites? LocalClientSites { get; private set; }

        static PBICloudSettings()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.BaseAddress = AppConstants.PBICloudApiUri;
        }

        public PBICloudSettings()
        {
            GlobalService = InitializeGlobalService();
            GlobalCloudEnvironment = InitializeGlobalCloudEnvironment();
        }

        public async Task Refresh(AuthenticationResult current, AuthenticationResult? previous)
        {
            // refresh required only if the login account has changed
            if (current.Account.HomeAccountId.Equals(previous?.Account.HomeAccountId) == false)
            {
                await RefreshTenantClusterAsync(current.AccessToken).ConfigureAwait(false);
                //await RefreshLocalClientSitesAsync().ConfigureAwait(false);
            }
        }

        private GlobalService InitializeGlobalService()
        {
            var securityProtocol = ServicePointManager.SecurityProtocol;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using var request = new HttpRequestMessage(HttpMethod.Post, GlobalServiceEnvironmentsDiscoverUrl);
                using var response = _httpClient.Send(request, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();

                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var globalService = JsonSerializer.Deserialize<GlobalService>(json, _jsonOptions) ?? throw new BravoUnexpectedException("PBICloud global service initialization failed");

                return globalService;
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }

        private CloudEnvironment InitializeGlobalCloudEnvironment()
        {
            var environmentName = CloudEnvironmentGlobalCloudName;
            var globalEnvironment = GlobalService.Environments.SingleOrDefault((c) => environmentName.Equals(c.CloudName, StringComparison.OrdinalIgnoreCase)) ?? throw new BravoUnexpectedException($"PBICloud environment not found [{ environmentName }]");
            var authority = globalEnvironment.Services.Single((s) => "aad".Equals(s.Name, StringComparison.OrdinalIgnoreCase));
            var frontend = globalEnvironment.Services.Single((s) => "powerbi-frontend".Equals(s.Name, StringComparison.OrdinalIgnoreCase));
            var backend = globalEnvironment.Services.Single((s) => "powerbi-backend".Equals(s.Name, StringComparison.OrdinalIgnoreCase));
            var gateway = globalEnvironment.Clients.Single((c) => "powerbi-gateway".Equals(c.Name, StringComparison.OrdinalIgnoreCase));

            var cloudEnvironment = new CloudEnvironment
            {
                Name = CloudEnvironmentType.Public,
                Authority = new Uri(authority.Endpoint, UriKind.Absolute),
                ClientId = gateway.AppId,
                Scopes = new string[] { $"{ backend.ResourceId }/.default" },
                Endpoint = new Uri(frontend.Endpoint, UriKind.Absolute),
                //RedirectUri = gateway.RedirectUri,
                //ResourceUri = backend.ResourceId,
            };

            return cloudEnvironment;
        }

        private async Task RefreshTenantClusterAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var request = new HttpRequestMessage(HttpMethod.Put, GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json)
            };

            using var response = _httpClient.Send(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            TenantCluster = JsonSerializer.Deserialize<TenantCluster>(json, _jsonOptions) ?? throw new BravoUnexpectedException("TenantCluster deserialization returned null");
        }

        private async Task RefreshLocalClientSitesAsync()
        {
            const string UserCacheFile = "User.zip";

            var classicAppCacheFile = new FileInfo(fileName: Path.Combine(LocalDataClassicAppCachePath, UserCacheFile));
            var storeAppCacheFile = new FileInfo(fileName: Path.Combine(LocalDataStoreAppCachePath, UserCacheFile));

            if (classicAppCacheFile.Exists && storeAppCacheFile.Exists)
            {
                var lastWritedCacheFile = classicAppCacheFile.LastWriteTime >= storeAppCacheFile.LastWriteTime ? classicAppCacheFile : storeAppCacheFile;
                if ((LocalClientSites = GetLocalClientSites(lastWritedCacheFile.FullName)) is not null)
                    return;
            }

            if (classicAppCacheFile.Exists)
            {
                if ((LocalClientSites = GetLocalClientSites(classicAppCacheFile.FullName)) is not null)
                    return;
            }

            if (storeAppCacheFile.Exists)
            {
                if ((LocalClientSites = GetLocalClientSites(storeAppCacheFile.FullName)) is not null)
                    return;
            }

            LocalClientSites = null;
            await Task.CompletedTask;

            static LocalClientSites? GetLocalClientSites(string archiveFile)
            {
                Version LatestSupportedVersion = new("2.9.0.0");

                using var archive = ZipFile.OpenRead(archiveFile);
                var entry = archive.GetEntry("ClientAccess/ClientAccess.xml");
                if (entry is not null)
                {
                    using var reader = new StreamReader(entry.Open());
                    var document = XDocument.Load(reader);

                    var elements = document.Root?.Descendants("Sites").Descendants("Site");
                    if (elements is not null)
                    {
                        var sites = elements.Select((e) => new LocalClientSite
                        {
                            Url = e.Attribute("Url")?.Value,
                            Version = e.Attribute("Version")?.Value,
                            UserPrincipalName = e.Element("User")?.Value.NullIfEmpty(),
                            DisplayName = e.Element("DisplayName")?.Value.NullIfEmpty(),
                            Avatar = e.Element("Avatar")?.Value.NullIfEmpty(),
                        })
                        .Where((s) => Version.TryParse(s.Version, out var version) && version >= LatestSupportedVersion);

                        var clientSites = new LocalClientSites(sites);
                        if (clientSites.Count > 0)
                            return clientSites;
                    }
                }

                return null;
            }
        }
    }
}
