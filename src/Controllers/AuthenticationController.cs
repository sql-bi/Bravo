using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System.Collections.Generic;
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
        /// <response code="200">Success</response>
        /// <response code="401">Sign-in failed, for details see the MsalErrorCode and the class Microsoft.Identity.Client.MsalError</response>
        [HttpGet]
        [ActionName("powerbi/SignIn")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoAccount))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(BravoErrorReponse))]
        public async Task<IActionResult> PowerBISignIn()
        {
            AuthenticationResult authenticationResult;
            try
            {
                authenticationResult = await _pbicloudAuthenticationService.AcquireTokenAsync().ConfigureAwait(false);
            }
            catch (MsalException ex) // ex.ErrorCode => Microsoft.Identity.Client.MsalError
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new BravoErrorReponse
                {
                    ErrorCode = ex.ErrorCode,
                    // IsCanceled = ex.ErrorCode == MsalError.AuthenticationCanceledError,
                    // IsFailed = ex.ErrorCode == MsalError.AuthenticationFailed
                });
            }

            var account = new BravoAccount
            {
                Identifier = authenticationResult.Account.HomeAccountId.Identifier,
                UserPrincipalName = authenticationResult.Account.Username,
                Username = authenticationResult.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value,
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
        /// <response code="200">Success</response>
        [HttpGet]
        [ActionName("powerbi/SignOut")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> PowerBISignOut()
        {
            await _pbicloudAuthenticationService.ClearTokenCache().ConfigureAwait(false);

            return Ok();
        }
    }
}
