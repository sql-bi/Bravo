namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Diagnostics;
    using System.Net.Mime;

    /// <summary>
    /// Application controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class ApplicationController : ControllerBase
    {
        private readonly TelemetryConfiguration _telemetryConfiguration;

        public ApplicationController(IOptions<TelemetryConfiguration> telemetryOptions)
        {
            _telemetryConfiguration = telemetryOptions.Value;
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
            var options = BravoOptions.CreateFromUserPreferences();
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
            UserPreferences.Current.TelemetryEnabled = options.TelemetryEnabled;
            UserPreferences.Current.CustomOptions = options.CustomOptions;
            UserPreferences.Current.Theme = options.Theme;
            UserPreferences.Save();

            _telemetryConfiguration.DisableTelemetry = options.TelemetryEnabled == false;

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
            var windowHandle = ProcessHelper.GetCurrentProcessMainWindowHandle();

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
        /// <response code="403">Status403Forbidden - The address provided is invalid or not allowed</response>
        [HttpGet]
        [ActionName("NavigateTo")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult BrowserNavigateTo(Uri address)
        {
            if (ProcessHelper.OpenInBrowser(address))
            {
                return Ok();
            }

            return Forbid();
        }
    }
}
