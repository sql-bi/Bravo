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
        /// http://localhost:5000/auth/redirect
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
