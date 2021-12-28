using Microsoft.AspNetCore.Hosting;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Authentication;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IPBICloudAuthenticationService
    {
        AuthenticationResult? Authentication { get; }

        PBICloudSettings CloudSettings { get; }

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

            _application = PublicClientApplicationBuilder.Create(_pbisettings.CloudEnvironment.ClientId)
                .WithAuthority(_pbisettings.CloudEnvironment.AuthorityUri)
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

        public PBICloudSettings CloudSettings => _pbisettings;

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

                var authentication = await InternalAcquireTokenAsync(silentOnly, identifier, cancellationTokenSource.Token).ConfigureAwait(false);
                {
                    await _pbisettings.RefreshTenantClusterAsync(current: authentication, previous: Authentication);
                }
                Authentication = authentication;

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
                var authenticationResult = await _application.AcquireTokenSilent(_pbisettings.CloudEnvironment.Scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return authenticationResult;
            }
            catch (MsalUiRequiredException /* murex */)
            {
                if (silentOnly) throw;
                try
                {
                    var builder = _application.AcquireTokenInteractive(_pbisettings.CloudEnvironment.Scopes)
                        .WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter);

                    //.WithAccount(account)
                    //.WithClaims(murex.Claims)
                    //.WithPrompt(Prompt.SelectAccount) // Force a sign-in (Prompt.SelectAccount), as the MSAL web browser might contain cookies for the current user and we don't necessarily want to re-sign-in the same user 

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
                            .WithSystemWebViewOptions(_customSystemWebViewOptions); // TODO: configure html files
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

    public class PBICloudSettings
    {
        private const string GlobalServiceEnvironmentsDiscoverUrl = "powerbi/globalservice/v201606/environments/discover?client=powerbi-msolap";
        private const string GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl = "spglobalservice/GetOrInsertClusterUrisByTenantlocation";
        private const string CloudEnvironmentGlobalCloudName = "GlobalCloud";

        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly HttpClient _httpClient;

        public GlobalService GlobalService { get; init; }

        public CloudEnvironment CloudEnvironment { get; init; }

        public TenantCluster? TenantCluster { get; private set; }

        static PBICloudSettings()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.BaseAddress = AppConstants.PBICloudApiUri;
        }

        public PBICloudSettings()
        {
            GlobalService = InitializeGlobalService();
            CloudEnvironment = InitializeCloudEnvironment();
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
                var globalService = JsonSerializer.Deserialize<GlobalService>(json, _jsonOptions) ?? throw new BravoException("PBICloud global service initialization failed");

                return globalService;
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }

        private CloudEnvironment InitializeCloudEnvironment()
        {
            var environmentName = CloudEnvironmentGlobalCloudName;

            var globalEnvironment = GlobalService.Environments.SingleOrDefault((c) => environmentName.Equals(c.CloudName, StringComparison.OrdinalIgnoreCase)) 
                ?? throw new BravoException($"PBICloud environment not found [{ environmentName }]");
            
            var authority = globalEnvironment.Services.Single((s) => "aad".Equals(s.Name, StringComparison.OrdinalIgnoreCase));
            var service = globalEnvironment.Services.Single((s) => "powerbi-backend".Equals(s.Name, StringComparison.OrdinalIgnoreCase));
            var client = globalEnvironment.Clients.Single((c) => "powerbi-gateway".Equals(c.Name, StringComparison.OrdinalIgnoreCase));

            var cloudEnvironment = new CloudEnvironment
            {
                Name = CloudEnvironmentType.Public,
                AuthorityUri = authority.Endpoint,
                RedirectUri = client.RedirectUri,
                ResourceUri = service.ResourceId,
                ClientId = client.AppId,
                Scopes = new string[] { $"{ service.ResourceId }/.default" },
                EndpointUri = service.Endpoint,
            };

            return cloudEnvironment;
        }

        /// <remarks>Do not refresh the tenant cluster every time the token changes, it should only be updated when the login account changes</remarks>
        public async Task<TenantCluster> RefreshTenantClusterAsync(AuthenticationResult? current, AuthenticationResult? previous)
        {
            var previousAccount = previous?.Account.HomeAccountId.Identifier;
            var currentAccount = current?.Account.HomeAccountId.Identifier;
            var accountChanged = previousAccount != currentAccount;

            if (TenantCluster is null || accountChanged)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", current?.AccessToken);

                using var request = new HttpRequestMessage(HttpMethod.Put, GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl)
                {
                    Content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json)
                };

                using var response = _httpClient.Send(request, HttpCompletionOption.ResponseContentRead);                
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                TenantCluster = JsonSerializer.Deserialize<TenantCluster>(json, _jsonOptions) ?? throw new BravoException("PBICloud tenant cluster initialization failed");
            }

            return TenantCluster;
        }
    }
}
