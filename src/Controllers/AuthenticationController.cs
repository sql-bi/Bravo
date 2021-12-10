using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Services;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    [Route("auth/[action]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IWebHostEnvironment environment, IAuthenticationService authenticationService)
        {
            _environment = environment;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// The destination URI where authentication responses (tokens) are returned after successfully authenticating or signing out users
        /// </summary>
        /// <remarks>The reply URI is http://localhost/auth/redirect </remarks>
        /// <response code="200">Success</response>
        [HttpGet]
        [ActionName("redirect")]
        [Produces(MediaTypeNames.Text.Html)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public IActionResult Redirect()
        {
            // See docs for "localhost" redirect URI special considerations
            // - https://docs.microsoft.com/en-us/azure/active-directory/develop/reply-url#localhost-exceptions
            // - https://docs.microsoft.com/en-us/azure/active-directory/develop/reply-url#prefer-127001-over-localhost

            var path = Path.Combine(_environment.WebRootPath, "auth-redirect.html");
            var content = System.IO.File.ReadAllText(path);

            return Content(content, MediaTypeNames.Text.Html);
        }

        [HttpGet]
        [ActionName("signIn")]
        public async Task<IActionResult> SignIn()
        {
            var authenticationResult = await _authenticationService.AcquireTokenAsync(null).ConfigureAwait(false);
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
        }
    }
}
