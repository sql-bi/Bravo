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
        private readonly IPBICloudAuthenticationService _pbicloudAuthenticationService;

        public AuthenticationController(IPBICloudAuthenticationService pbicloudAuthenticationService)
        {
            _pbicloudAuthenticationService = pbicloudAuthenticationService;
        }

        [HttpGet]
        [ActionName("powerbi/signIn")]
        public async Task<IActionResult> SignIn()
        {
            var authenticationResult = await _pbicloudAuthenticationService.AcquireTokenAsync(null).ConfigureAwait(false);
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
