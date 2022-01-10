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
        [HttpGet]
        [ActionName("GetStartupSettings")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StartupSettings))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult GetStartupOptions()
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                //detail: "custom detail"
                instance: HttpContext.Request.Path
                );
            
            //throw new System.Exception("UGO");
            return BadRequest("Property is null");
            return NotFound();
            return Ok(_startupSettings);
        }
    }
}
