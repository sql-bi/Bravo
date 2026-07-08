namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models.Authentication;
    using Sqlbi.Bravo.Services;

    /// <summary>
    /// Authentication controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("auth/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public sealed class AuthenticationController(
        IPBICloudAuthenticationService pbicloudAuthenticationService,
        IPBICloudService pbicloudService,
        IAuthenticationService authenticationService) : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService = authenticationService;
        private readonly IPBICloudAuthenticationService _pbicloudAuthenticationService = pbicloudAuthenticationService;
        private readonly IPBICloudService _pbicloudService = pbicloudService;

        /// <summary>
        /// Returns the list of available PowerBI cloud environments for the specified email account.
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("GetEnvironments")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetEnvironmentsResponse))]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetEnvironmentsAsync(
            [FromQuery] GetEnvironmentsRequest request,
            CancellationToken cancellationToken)
        {
            var environments = await _pbicloudAuthenticationService.GetEnvironmentsAsync(
                request.Email,
                cancellationToken);

            var response = new GetEnvironmentsResponse(environments);
            return Ok(response);
        }

        /// <summary>
        /// Attempts to authenticate and acquire an access token for the account to access the PowerBI cloud services
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("SignIn")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SignInResponse))]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> SignInAsync(
            SignInRequest request,
            CancellationToken cancellationToken)
        {
            await _authenticationService.PBICloudSignInAsync(
                request.Email,
                request.Environment.ToModel(),
                cancellationToken);

            var response = new SignInResponse(_authenticationService.PBICloudAuthentication.Account);
            return Ok(response);
        }

        /// <summary>
        /// Clear the token cache for all the accounts
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("SignOut")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> SignOutAsync(CancellationToken cancellationToken)
        {
            await _authenticationService.PBICloudSignOutAsync(cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Returns the account profile picture as base64 encoded image [data:image/jpeg;base64,...]
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - Current account has no profile picture</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpGet]
        [ActionName("GetUserAvatar")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetUserAvatarAsync(CancellationToken cancellationToken)
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            var avatar = await _pbicloudService.GetAccountAvatarAsync();
            if (avatar is null)
                return NotFound();

            return Ok(avatar);
        }
    }
}
