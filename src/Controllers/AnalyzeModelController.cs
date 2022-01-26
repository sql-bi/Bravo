using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    /// <summary>
    /// Analyze model controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class AnalyzeModelController : ControllerBase
    {
        private readonly IPBIDesktopService _pbidesktopService;
        private readonly IPBICloudService _pbicloudService;

        public AnalyzeModelController(IPBIDesktopService pbidesktopService, IPBICloudService pbicloudService)
        {
            _pbidesktopService = pbidesktopService;
            _pbicloudService = pbicloudService;
        }

        /// <summary>
        /// Returns a database model from the VPAX file stream
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetModelFromVpax")]
        [Consumes(MediaTypeNames.Application.Octet)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabaseFromVpax()
        {
            var database = VpaxToolsHelper.GetDatabaseFromVpax(stream: Request.Body);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBIDesktop instance
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetModelFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabaseFromPBIDesktopReport(PBIDesktopReport report)
        {
            var stream = _pbidesktopService.ExportVpax(report, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            var database = VpaxToolsHelper.GetDatabaseFromVpax(stream);

            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBICloud dataset
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("GetModelFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetDatabaseFromPBICloudDataset(PBICloudDataset dataset)
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            var stream = _pbicloudService.ExportVpax(dataset, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            var database = VpaxToolsHelper.GetDatabaseFromVpax(stream);

            return Ok(database);
        }

        /// <summary>
        /// Returns a list of all PBICloud datasets
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpGet]
        [ActionName("ListDatasets")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBICloudDataset>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetPBICloudDatasets()
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            var datasets = await _pbicloudService.GetDatasetsAsync();
            return Ok(datasets);
        }

        /// <summary>
        /// Returns a list of all open PBIDesktop reports
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("ListReports")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBIDesktopReport>))]
        [ProducesDefaultResponseType]
        public IActionResult GetPBIDesktopReports(CancellationToken cancellationToken)
        {
            var reports = _pbidesktopService.GetReports(cancellationToken);
            return Ok(reports);
        }

        /// <summary>
        /// Returns a list of all open PBIDesktop reports as fast as possible, providing only process information and without attempting to establish a database connection
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("QueryReports")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBIDesktopReport>))]
        [ProducesDefaultResponseType]
        public IActionResult QueryPBIDesktopReports(CancellationToken cancellationToken)
        {
            var reports = _pbidesktopService.QueryReports(cancellationToken);
            return Ok(reports);
        }

        /// <summary>
        /// Returns a VPAX file stream from an active PBIDesktop report
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("ExportVpaxFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportResult))]
        [ProducesDefaultResponseType]
        public IActionResult ExportVpaxFromPBIDesktopReport(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            var (canceled, path) = WindowDialogHelper.SaveFileDialog(fileName: report.ReportName, defaultExt: "VPAX", cancellationToken);
            if (!canceled)
            {
                using var stream = _pbidesktopService.ExportVpax(report, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);

                if (!cancellationToken.IsCancellationRequested)
                {
                    using var fileStream = System.IO.File.Create(path!);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
            }

            return Ok(new ExportResult
            {
                Canceled = canceled,
                Path = path
            });
        }

        /// <summary>
        /// Returns a VPAX file stream from a PBICloud dataset
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("ExportVpaxFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ExportVpaxFromPBICloudDataset(PBICloudDataset dataset, CancellationToken cancellationToken)
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            var (canceled, path) = WindowDialogHelper.SaveFileDialog(fileName: dataset.DisplayName, defaultExt: "VPAX", cancellationToken);
            if (!canceled)
            {
                using var stream = _pbicloudService.ExportVpax(dataset, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);

                if (!cancellationToken.IsCancellationRequested)
                {
                    using var fileStream = System.IO.File.Create(path!);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
            }

            return Ok(new ExportResult
            {
                Canceled = canceled,
                Path = path
            });
        }
    }
}
