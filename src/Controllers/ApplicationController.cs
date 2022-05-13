namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;

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
            options.SaveToUserPreferences();

            _telemetryConfiguration.DisableTelemetry = UserPreferences.Current.TelemetryEnabled == false;

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
            ThemeHelper.ChangeTheme(theme);
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
            if (ProcessHelper.OpenBrowser(address))
                return Ok();

            return Forbid();
        }

        /// <summary>
        /// Launches the Power BI Desktop process after displaying a dialog box that prompts the user to select the PBIX file to be opened
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="403">Status403Forbidden - The path is invalid or not allowed</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpGet]
        [ActionName("PBIDesktopOpenPBIX")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PBIDesktopReport))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult PBIDesktopOpenPBIX(bool waitForStarted, CancellationToken cancellationToken)
        {
            if (WindowDialogHelper.OpenFileDialog(defaultExt: "PBIX", out var path, cancellationToken))
            {
                if (ProcessHelper.OpenShellExecute(path, waitForStarted, out var processId, cancellationToken))
                {
                    var report = PBIDesktopReport.CreateFrom(processId.Value);
                    if (report is null)
                        return NoContent();

                    return Ok(report);
                }

                return Forbid();
            }

            return NoContent();
        }

        /// <summary>
        /// Opens the folder or file path provided
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="403">Status403Forbidden - The path is invalid or not allowed</response>
        [HttpGet]
        [ActionName("FileSystemOpen")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult FileSystemOpen(string path)
        {
            if (ProcessHelper.Open(path))
                return Ok();

            return Forbid();
        }

        /// <summary>
        /// Gets all the <see cref="DiagnosticMessage"/> for the application
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("GetDiagnostics")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DiagnosticMessage>))]
        [ProducesDefaultResponseType]
        public IActionResult GetDiagnostics(bool? all = null)
        {
            var messages = new List<DiagnosticMessage>();
            
            foreach (var message in AppEnvironment.Diagnostics.Values)
            {
                if (all == true || message.LastReadTimestamp is null)
                {
                    message.LastReadTimestamp = DateTime.UtcNow;
                    messages.Add(message);
                }
            }

            return Ok(messages);
        }

        /// <summary>
        /// Gets the current application version for the specified <see cref="UpdateChannelType"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("GetCurrentVersion")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoUpdate))]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetCurrentVersion(UpdateChannelType updateChannel, bool notify, CancellationToken cancellationToken)
        {
            if (UserPreferences.Current.UpdateCheckEnabled == false)
            {
                // TODO: @daniele - remove this 'if' statement when 'UpdateCheckEnabled' is supported.
                return Ok(new BravoUpdate
                {
                    IsNewerVersion = false,
                });
            }

            var bravoUpdate = await CommonHelper.CheckForUpdateAsync(updateChannel, cancellationToken);

            if (bravoUpdate.IsNewerVersion && notify)
                NotificationHelper.NotifyUpdateAvailable(bravoUpdate);

            return Ok(bravoUpdate);
        }

        /// <summary>
        /// Displays a dialog box that prompts the user to enter credentials to authenticate to the HTTP proxy
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpGet]
        [ActionName("UpdateProxyCredentials")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult UpdateProxyCredentials()
        {
            var credentialOptions = new CredentialDialogOptions(caption: "Enter proxy credentials", message: "Enter credentials to authenticate to the HTTP Proxy")
            {
                HwndParent = ProcessHelper.GetCurrentProcessMainWindowHandle(),
            };

            var networkCredential = CredentialDialog.PromptForCredentials(credentialOptions);
            if (networkCredential is not null)
            {
                CredentialManager.WriteCredential(AppEnvironment.CredentialManagerProxyCredentialName, networkCredential.UserName, networkCredential.Password);
                return Ok();
            }

            return NoContent();
        }

        /// <summary>
        /// Removes credentials to authenticate to the HTTP proxy, if any
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("DeleteProxyCredentials")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult DeleteProxyCredentials()
        {
            _ = CredentialManager.DeleteCredential(AppEnvironment.CredentialManagerProxyCredentialName);
            return Ok();
        }
    }
}
