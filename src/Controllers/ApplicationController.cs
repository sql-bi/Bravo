using Bravo.Infrastructure.Windows.Interop;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Configuration.Options;
using Sqlbi.Bravo.Models;
using Sqlbi.Infrastructure.Configuration.Settings;
using System;
using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json;

namespace Sqlbi.Bravo.Controllers
{
    /// <summary>
    /// Application controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class ApplicationController : ControllerBase
    {
        private readonly IWritableOptions<UserSettings> _userOptions;
        private readonly TelemetryConfiguration _telemetryConfiguration;

        public ApplicationController(IWritableOptions<UserSettings> userOptions, IOptions<TelemetryConfiguration> telemetryOptions)
        {
            _userOptions = userOptions;
            _telemetryConfiguration = telemetryOptions.Value;
            
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
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("GetOptions")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoOptions))]
        [ProducesDefaultResponseType]
        public IActionResult GetOptions()
        {
            var options = BravoOptions.CreateFrom(_userOptions.Value);
            return Ok(options);
        }

        /// <summary>
        /// Update the application options
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateOptions")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult UpdateOptions(BravoOptions options)
        {
            var customOptionsAsString = JsonSerializer.Serialize(options.CustomOptions, AppConstants.DefaultJsonOptions);

            _userOptions.Update((o) =>
            {
                o.TelemetryEnabled = options.TelemetryEnabled;
                o.Theme = options.Theme;
                o.CustomOptions = customOptionsAsString;
            });

            _telemetryConfiguration.DisableTelemetry = !options.TelemetryEnabled;

            return Ok();
        }

        /// <summary>
        /// Change the current window theme
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("ChangeTheme")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult ChangeTheme(ThemeType theme)
        {
            var windowHandle = Process.GetCurrentProcess().MainWindowHandle;

            switch (theme)
            {
                case ThemeType.Light:
                case ThemeType.Dark:
                    Uxtheme.ChangeTheme(windowHandle, useDark: theme == ThemeType.Dark);
                    break;
                case ThemeType.Auto:
                    Uxtheme.ChangeTheme(windowHandle, useDark: Uxtheme.IsSystemUsingDarkMode());
                    break;
            }

            return Ok();
        }

        /// <summary>
        /// Opens the provided URL using the system's default browser
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("NavigateTo")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult BrowserNavigateTo(Uri address)
        {
            if (address.IsAbsoluteUri && (Uri.UriSchemeHttps.Equals(address.Scheme) || Uri.UriSchemeHttp.Equals(address.Scheme)))
            {
                _ = Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = address.OriginalString
                });
            }

            return Ok();
        }
    }
}
