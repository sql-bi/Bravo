using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System.Diagnostics;
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
        /// <response code="403">Status403Forbidden - sign-in cancelled by the system because the configured timeout period elapsed before the user completed the sign-in operation</response>
        /// <response code="424">Status424FailedDependency - sign-in failed, for details see the ErrorCode and the class Microsoft.Identity.Client.MsalError</response>
        [HttpGet]
        [ActionName("powerbi/SignIn")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoAccount))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status424FailedDependency)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> PowerBISignIn(string? upn)
        {
            try
            {
                await _pbicloudService.SignInAsync(identifier: upn);
            }
            catch (SignInException ex)
            {
                switch (ex.Problem)
                {
                    case BravoProblem.SignInMsalTimeoutExpired: 
                        return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status403Forbidden);
                    case BravoProblem.SignInMsalExceptionOccurred:
                        return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status424FailedDependency);
                }
                throw;
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
            if (_pbicloudService.IsAuthenticated == false)
            {
                try
                {
                    await _pbicloudService.SignInAsync(silentOnly: true);
                }
                catch (SignInException ex) when (ex.Problem == BravoProblem.SignInMsalExceptionOccurred && ex.ProblemDetail == Microsoft.Identity.Client.MsalError.UserNullError)
                {
                    Debug.Assert(_pbicloudService.IsAuthenticated == false);
                    return Unauthorized();
                }
            }

            var account = _pbicloudService.CurrentAccount;
            return Ok(account);
        }
    }
}
