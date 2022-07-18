namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ManageDates;
    using Sqlbi.Bravo.Services;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Threading;

    /// <summary>
    /// ManageDates module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("ManageDates/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class ManageDatesController : ControllerBase
    {
        private readonly IManageDatesService _manageDatesService;

        public ManageDatesController(IManageDatesService manageDatesService)
        {
            _manageDatesService = manageDatesService;
        }

        /// <summary>
        /// Gets all the available <see cref="CustomPackage"/> from the user's and organization's package repositories
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetCustomPackages")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CustomPackage>))]
        [ProducesDefaultResponseType]
        public IActionResult GetCustomPackages(CancellationToken cancellationToken)
        {
            var customPackages = _manageDatesService.GetCustomPackages(cancellationToken);
            return Ok(customPackages);
        }

        /// <summary>
        /// Gets all the available <see cref="DateConfiguration"/> from the embedded templates
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetConfigurationsForReport")] // TODO: raname GetConfigurations
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DateConfiguration>))]
        [ProducesDefaultResponseType]
        public IActionResult GetConfigurations(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            var configurations = _manageDatesService.GetConfigurations(report, cancellationToken);
            return Ok(configurations);
        }

        /// <summary>
        /// Validate the <see cref="DateConfiguration"/> provided against the tabular model
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("ValidateConfigurationForReport")] // TODO: raname ValidateConfiguration
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DateConfiguration))]
        [ProducesDefaultResponseType]
        public IActionResult ValidateConfiguration(ValidatePBIDesktopReportConfigurationRequest request, CancellationToken cancellationToken)
        {
            var configuration = _manageDatesService.ValidateConfiguration(request.Report!, request.Configuration!, cancellationToken);
            return Ok(configuration);
        }

        /// <summary>
        /// Applies the provided <see cref="DateConfiguration"/> without commit changes and returns a preview of changes to objects and data
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetPreviewChangesFromReport")] // TODO: rename to GetConfigurationPreviewChanges
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dax.Template.Model.ModelChanges))]
        [ProducesDefaultResponseType]
        public IActionResult GetPreviewChanges(PreviewChangesFromPBIDesktopReportRequest request, CancellationToken cancellationToken)
        {
            var modelChanges = _manageDatesService.GetPreviewChanges(request.Report!, request.Settings!, cancellationToken);
            return Ok(modelChanges);
        }

        /// <summary>
        /// Applies the provided <see cref="CustomPackage"/> without commit changes and returns a preview of changes to objects and data
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetCustomPackagePreviewChanges")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dax.Template.Model.ModelChanges))]
        [ProducesDefaultResponseType]
        public IActionResult GetPreviewChanges(CustomPackagePreviewChangesRequest request, CancellationToken cancellationToken)
        {
            var modelChanges = _manageDatesService.GetPreviewChanges(request.Report!, request.Settings!, cancellationToken);
            return Ok(modelChanges);
        }

        /// <summary>
        /// Update the <see cref="PBIDesktopReport"/> by appliying the provided <see cref="DateConfiguration"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateReport")] // TODO: rename to ApplyConfiguration
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult Apply(UpdatePBIDesktopReportRequest request, CancellationToken cancellationToken)
        {
            _manageDatesService.ApplyTemplate(request.Report!, request.Configuration!, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Update the <see cref="PBIDesktopReport"/> by appliying the provided <see cref="CustomPackage"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("ApplyCustomPackage")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult Apply(CustomPackageApplyRequest request, CancellationToken cancellationToken)
        {
            _manageDatesService.ApplyTemplate(request.Report!, request.CustomPackage!, cancellationToken);
            return Ok();
        }
    }
}
