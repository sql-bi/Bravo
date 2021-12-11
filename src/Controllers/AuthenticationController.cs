using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    [Route("auth/[action]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IPBICloudAuthenticationService _pbicloudAuthenticationService;

        public AuthenticationController(IPBICloudAuthenticationService pbicloudAuthenticationService)
        {
            _pbicloudAuthenticationService = pbicloudAuthenticationService;
        }

        /// <summary>
        /// Attempts to authenticate and acquire an access token for the account to access the PowerBI cloud services
        /// </summary>
        /// <response code="200">Status200OK</response>
        /// <response code="403">Status403Forbidden - sign-in cancelled by the system because the configured timeout period elapsed before the user completed the sign-in operation</response>
        /// <response code="424">Status424FailedDependency - sign-in failed, for details see the ErrorCode and the class Microsoft.Identity.Client.MsalError</response>
        [HttpGet]
        [ActionName("powerbi/SignIn")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoAccount))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(BravoErrorReponse))]
        [ProducesResponseType(StatusCodes.Status424FailedDependency, Type = typeof(BravoErrorReponse))]
        public async Task<IActionResult> PowerBISignIn()
        {
            try
            {
                await _pbicloudAuthenticationService.AcquireTokenAsync(cancelAfter: AppConstants.MSALSignInTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new BravoErrorReponse
                {
                    IsTimeoutElapsed = true
                });
            }
            catch (MsalException ex) // ex.ErrorCode => Microsoft.Identity.Client.MsalError
            {
                return StatusCode(StatusCodes.Status424FailedDependency, new BravoErrorReponse
                {
                    ErrorCode = ex.ErrorCode,
                });
            }

            var account = new BravoAccount
            {
                Identifier = _pbicloudAuthenticationService.CurrentAuthentication!.Account.HomeAccountId.Identifier,
                UserPrincipalName = _pbicloudAuthenticationService.CurrentAuthentication!.Account.Username,
                Username = _pbicloudAuthenticationService.CurrentAuthentication!.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value,
            };

            return Ok(account);
            /*
                        var principal = authenticationResult.ClaimsPrincipal;
                        var properties = new List<string>();

                        if (principal.Identity is not null)
                        {
                            properties.Add($"Identity.Name={principal.Identity.Name ?? string.Empty}");
                            properties.Add($"Identity.AuthenticationType={principal.Identity.AuthenticationType ?? string.Empty}");
                        }

                        foreach (var claim in principal.Claims)
                            properties.Add($"{claim.Type}={claim.Value}");

                        return Ok(properties);
            */
        }

        /// <summary>
        /// Clear the token cache for all the accounts
        /// </summary>
        /// <response code="200">Status200OK</response>
        [HttpGet]
        [ActionName("powerbi/SignOut")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> PowerBISignOut()
        {
            await _pbicloudAuthenticationService.ClearTokenCacheAsync().ConfigureAwait(false);

            return Ok();
        }
    }
}
