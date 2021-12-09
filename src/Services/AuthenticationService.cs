using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    internal class AuthenticationService : IAuthenticationService
    {
        // TODO: load from IConfiguration.Get<PublicClientApplicationOptions>()
        private static readonly PublicClientApplicationOptions _options = new()
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "f545bd66-7c3f-4729-851a-b7ca3ac9fb6e", // "common"
            ClientId = "4257bc9e-ed10-490f-a73e-5c9ba120bf8f",
        };
        private static readonly string[] _scopes =
        {
            //"api://2daa3311-aaad-4eed-bb63-da2466f0f88f/access_as_user"
        };

        private static readonly IPublicClientApplication _application;

        static AuthenticationService()
        {
            _application = PublicClientApplicationBuilder.Create(_options.ClientId)
                .WithAuthority($"{ _options.Instance }{ _options.TenantId }")
                .WithRedirectUri(_options.RedirectUri)
                //.WithDefaultRedirectUri()
                .Build();

            //TokenCacheHelper.EnableSerialization(_application.UserTokenCache);
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(IAccount account)
        {
            // Use account used to signed-in in Windows (WAM). WAM will always get an account in the cache.
            // So if we want to have a chance to select the accounts interactively, we need to force the non-account
            //account = PublicClientApplication.OperatingSystemAccount;

            // Use one of the Accounts known by Windows (WAM). We force WAM to display the dialog with the accounts
            //account = null;

            AuthenticationResult result;

            try
            {
                // Try to acquire an access token from the cache, if UI interaction is required, MsalUiRequiredException will be thrown.
                result = await _application.AcquireTokenSilent(_scopes, account).ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalUiRequiredException /* murex */)
            {
                try
                {
                    // Force a sign-in (Prompt.SelectAccount), as the MSAL web browser might contain cookies
                    // for the current user and we don't necessarily want to re-sign-in the same user
                    var builder = _application.AcquireTokenInteractive(_scopes)
                        .WithAccount(account)
                        //.WithClaims(murex.Claims)
                        //.WithParentActivityOrWindow(new WindowInteropHelper(Program.HostWindow).Handle) // optional, used to center the browser on the window
                        //.WithParentActivityOrWindow(Program.HostWindow!.WindowHandle) // optional, used to center the browser on the window
                        .WithPrompt(Prompt.SelectAccount);

                    if (!_application.IsEmbeddedWebViewAvailable())
                    {
                        // You app should install the embedded browser WebView2 https://aka.ms/msal-net-webview2
                        // but if for some reason this is not possible, you can fall back to the system browser 
                        // in this case, the redirect uri needs to be set to "http://localhost"
                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: false);
                    }

                    builder = builder.WithSystemWebViewOptions(GetCustomWebViewOptions()); // Using the custom html

                    builder = builder.WithCustomWebUi(new CustomBrowserWebUi()); //Using our custom web ui

                    result = await builder.ExecuteAsync().ConfigureAwait(false);
                }
                catch (MsalException mex)
                {
                    if (mex.ErrorCode == "access_denied")
                    {
                        // The user canceled sign in, take no action.
                    }

                    throw;
                }
            }

            return result;

            static SystemWebViewOptions GetCustomWebViewOptions()
            {
                return new SystemWebViewOptions
                {
                    HtmlMessageSuccess = @"<html style='font-family: sans-serif;'>
                                      <head><title>Authentication Complete</title></head>
                                      <body style='text-align: center;'>
                                          <header>
                                              <h1>Custom Web UI</h1>
                                          </header>
                                          <main style='border: 1px solid lightgrey; margin: auto; width: 600px; padding-bottom: 15px;'>
                                              <h2 style='color: limegreen;'>Authentication complete</h2>
                                              <div>You can return to the application. Feel free to close this browser tab.</div>
                                          </main>
    
                                      </body>
                                    </html>",

                    HtmlMessageError = @"<html style='font-family: sans-serif;'>
                                  <head><title>Authentication Failed</title></head>
                                  <body style='text-align: center;'>
                                      <header>
                                          <h1>Custom Web UI</h1>
                                      </header>
                                      <main style='border: 1px solid lightgrey; margin: auto; width: 600px; padding-bottom: 15px;'>
                                          <h2 style='color: salmon;'>Authentication failed</h2>
                                          <div><b>Error details:</b> error {0} error_description: {1}</div>
                                          <br>
                                          <div>You can return to the application. Feel free to close this browser tab.</div>
                                      </main>
    
                                  </body>
                                </html>"
                };
            }
        }

        public async Task ClearTokenCache()
        {
            var accounts = (await _application.GetAccountsAsync().ConfigureAwait(false)).ToArray();

            // Clears the library cache, does not affect the browser cookies
            while (accounts.Length > 0)
            {
                await _application.RemoveAsync(accounts[0]).ConfigureAwait(false);
                accounts = (await _application.GetAccountsAsync()).ToArray();
            }
        }
    }
}
