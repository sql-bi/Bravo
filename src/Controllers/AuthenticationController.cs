using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net.Mime;

namespace Sqlbi.Bravo.Controllers
{
    [Route("auth/[action]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public AuthenticationController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// The destination URI where authentication responses (tokens) are returned after successfully authenticating or signing out users.
        /// Reply URI http://localhost/auth/redirect
        /// See docs for "localhost" redirect URI special considerations
        /// <see cref="https://docs.microsoft.com/en-us/azure/active-directory/develop/reply-url#localhost-exceptions"/>
        /// <see cref="https://docs.microsoft.com/en-us/azure/active-directory/develop/reply-url#prefer-127001-over-localhost"/>
        /// </summary>
        [HttpGet]
        [ActionName("redirect")]
        public ContentResult Redirect()
        {
            var path = Path.Combine(_environment.WebRootPath, "auth-redirect.html");
            var content = System.IO.File.ReadAllText(path);

            return Content(content, MediaTypeNames.Text.Html);
        }
    }
}
