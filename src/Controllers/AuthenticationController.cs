namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
    using Sqlbi.Bravo.Models;
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
        ICloudApiClient cloudApiClient,
        IAuthenticationService authenticationService) : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService = authenticationService;
        private readonly ICloudApiClient _cloudApiClient = cloudApiClient;

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
            var environments = await _authenticationService.GetEnvironmentsAsync(
                request.Email,
                cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(AuthenticationController)}.{nameof(GetEnvironmentsAsync)}", JsonSerializer.Serialize(environments));

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
            var session = await _authenticationService.SignInAsync(
                request.Email,
                request.Environment.ToModel(),
                cancellationToken);

            var response = new SignInResponse(session.AuthenticationResult);
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
            await _authenticationService.SignOutAsync(cancellationToken);
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
            var session = await _authenticationService.EnsureSignedInAsync(cancellationToken);
            if (session is null)
                return Unauthorized();

            var avatar = await _cloudApiClient.GetUserPhotoAsync(session, cancellationToken);
            if (avatar is null)
                return NotFound();

            return Ok(avatar);
        }
    }
}
