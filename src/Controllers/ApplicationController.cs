using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure.Configuration.Options;
using Sqlbi.Bravo.Models;
using Sqlbi.Infrastructure;
using System.Net.Mime;
using System.Text.Json;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    public class ApplicationController : ControllerBase
    {
        //private readonly BravoOptions _settings;
        private readonly IWritableOptions<AppOptions> _options;

        public ApplicationController(IWritableOptions<AppOptions> options)
        {
            _options = options;
            try
            {
                //_settings = options.Value;
            }
            catch /* (OptionsValidationException ex) */
            {
                throw;

                // TODO: logging
                //foreach (var failure in ex.Failures)
                //{
                //    _logger.LogError(failure);
                //}
            }
        }

        /// <summary>
        /// Get the application options
        /// </summary>
        /// <response code="200">Status200OK</response>
        [HttpGet]
        [ActionName("GetOptions")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoOptions))]
        public IActionResult GetOptions()
        {
            JsonElement? customOptionsAsJsonElement = _options.Value.CustomOptions is not null
                ? JsonSerializer.Deserialize<JsonElement>(_options.Value.CustomOptions)
                : null;

            var options = new BravoOptions
            {
                TelemetryEnabled = _options.Value.TelemetryEnabled,
                CustomOptions = customOptionsAsJsonElement
            };

            return Ok(options);
        }

        /// <summary>
        /// Update the application options
        /// </summary>
        /// <response code="200">Status200OK</response>
        [HttpPost]
        [ActionName("UpdateOptions")]
        [Consumes(MediaTypeNames.Application.Json)]
        public IActionResult UpdateOptions(BravoOptions options)
        {
            var customOptionsAsString = JsonSerializer.Serialize(options.CustomOptions);

            _options.Update((o) =>
            {
                o.TelemetryEnabled = options.TelemetryEnabled;
                o.CustomOptions = customOptionsAsString;
            });

            return Ok();
        }
    }
}
