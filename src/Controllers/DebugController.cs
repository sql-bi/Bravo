using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sqlbi.Infrastructure.Configuration.Settings;
using System.Net.Mime;

namespace Sqlbi.Bravo.Controllers
{
    [Route("debug/[action]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly StartupSettings _startupSettings;

        public DebugController(IOptions<StartupSettings> startupOptions)
        {
            _startupSettings = startupOptions.Value;
        }

        /// <summary>
        /// Get the application startup settings
        /// </summary>
        /// <response code="200">Status200OK</response>
        [HttpGet]
        [ActionName("GetStartupSettings")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StartupSettings))]
        public IActionResult GetStartupOptions()
        {
            return Ok(_startupSettings);
        }
    }
}
