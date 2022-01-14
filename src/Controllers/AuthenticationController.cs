using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    [Route("auth/[action]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IPBICloudService _pbicloudService;

        public AuthenticationController(IPBICloudService pbicloudService)
        {
            _pbicloudService = pbicloudService;
        }

        /// <summary>
        /// Attempts to authenticate and acquire an access token for the account to access the PowerBI cloud services
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
        [HttpGet]
        [ActionName("powerbi/SignIn")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoAccount))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> PowerBISignIn(string? upn)
        {
            try
            {
                await _pbicloudService.SignInAsync(userPrincipalName: upn);
            }
            catch (SignInException ex)
            {
                return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status400BadRequest);
            }

            var account = _pbicloudService.CurrentAccount;
            return Ok(account);
        }

        /// <summary>
        /// Clear the token cache for all the accounts
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("powerbi/SignOut")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> PowerBISignOut()
        {
            await _pbicloudService.SignOutAsync();
            return Ok();
        }

        /// <summary>
        /// Returns information about the currently logged in user
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpGet]
        [ActionName("GetUser")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoAccount))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetAccount()
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            var account = _pbicloudService.CurrentAccount;
            return Ok(account);
        }
    }
}
