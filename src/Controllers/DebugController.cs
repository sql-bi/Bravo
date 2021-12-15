using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sqlbi.Infrastructure;
using System.Net.Mime;

namespace Sqlbi.Bravo.Controllers
{
    [Route("debug/[action]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly AppStartupOptions _startupOptions;

        public DebugController(IOptions<AppStartupOptions> startupOptions)
        {
            _startupOptions = startupOptions.Value;
        }

        /// <summary>
        /// Get the application startup options
        /// </summary>
        /// <response code="200">Status200OK</response>
        [HttpGet]
        [ActionName("GetStartupOptions")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppStartupOptions))]
        public IActionResult GetStartupOptions()
        {
            return Ok(_startupOptions);
        }
    }
}
