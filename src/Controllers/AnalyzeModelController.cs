using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using Sqlbi.Bravo.Infrastructure.Windows;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/[action]")]
    [ApiController]
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
        /// <response code="404">Status404NotFound - PBIDesktop report not found</response>
        [HttpPost]
        [ActionName("GetModelFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabaseFromPBIDesktopReport(PBIDesktopReport report)
        {
            Stream stream;
            try
            {
                stream = _pbidesktopService.ExportVpax(report, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status404NotFound);
            }

            var database = VpaxToolsHelper.GetDatabaseFromVpax(stream);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBICloud dataset
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        /// <response code="404">Status404NotFound - PBICloud dataset not found</response>
        [HttpPost]
        [ActionName("GetModelFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabaseFromPBICloudDataset(PBICloudDataset dataset)
        {
            if (_pbicloudService.IsAuthenticated == false)
                return Unauthorized();

            Stream stream;
            try
            {
                stream = _pbicloudService.ExportVpax(dataset, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status404NotFound);
            }

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
            if (_pbicloudService.IsAuthenticated == false)
                return Unauthorized();

            var onlineWorkspaces = await _pbicloudService.GetWorkspacesAsync();
            var onlineDatasets = await _pbicloudService.GetSharedDatasetsAsync();

            var selectedWorkspaces = onlineWorkspaces.Where((w) => w.CapacitySkuType == WorkspaceCapacitySkuType.Premium);
            // unsupported datasets (not accessible by the XMLA endpoint) https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#unsupported-datasets
            var selectedDatasets = onlineDatasets.Where((d) => !d.Model.IsExcelWorkbook && !d.Model.IsPushDataEnabled);

            var datasets = selectedDatasets.Join(selectedWorkspaces, (d) => d.WorkspaceObjectId, (w) => w.Id, resultSelector: (d, w) => new PBICloudDataset
            {
                WorkspaceId = d.WorkspaceId,
                WorkspaceName = d.WorkspaceName,
                Id = d.Model.Id,
                DisplayName = d.Model.DisplayName,
                Description = d.Model.Description,
                Owner = $"{ d.Model.CreatorUser.GivenName } { d.Model.CreatorUser.FamilyName }",
                Refreshed = d.Model.LastRefreshTime,
                Endorsement = (PBICloudDatasetEndorsement)(d.GalleryItem?.Stage ?? (int)PBICloudDatasetEndorsement.None)
            },
            StringComparer.InvariantCultureIgnoreCase);

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
        public IActionResult GetPBIDesktopReports()
        {
            var reports = _pbidesktopService.GetReports();
            return Ok(reports);
        }

        /// <summary>
        /// Returns a VPAX file stream from an active PBIDesktop report
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - PBIDesktop report not found</response>
        [HttpPost]
        [ActionName("ExportVpaxFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileActionResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult ExportVpaxFromPBIDesktopReport(PBIDesktopReport report)
        {
            Stream stream;
            try
            {
                stream = _pbidesktopService.ExportVpax(report, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status404NotFound);
            }

            var exportResult = ExportVpaxFile(fileName: report.ReportName, stream);
            return Ok(exportResult);
        }

        /// <summary>
        /// Returns a VPAX file stream from a PBICloud dataset
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        /// <response code="404">Status404NotFound - PBICloud dataset not found</response>
        [HttpPost]
        [ActionName("ExportVpaxFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileActionResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult ExportVpaxFromPBICloudDataset(PBICloudDataset dataset)
        {
            if (_pbicloudService.IsAuthenticated == false)
                return Unauthorized();

            Stream stream;
            try
            {
                stream = _pbicloudService.ExportVpax(dataset, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status404NotFound);
            }

            var exportResult = ExportVpaxFile(fileName: dataset.DisplayName, stream);
            return Ok(exportResult);
        }

        private static FileActionResult ExportVpaxFile(string? fileName, Stream stream)
        {
            var dialogOwner = Win32WindowWrapper.CreateFrom(Process.GetCurrentProcess().MainWindowHandle);
            var dialogResult = System.Windows.Forms.DialogResult.None;
            var dialog = new System.Windows.Forms.SaveFileDialog()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Vpax files (*.vpax)|*.vpax|All files (*.*)|*.*",
                Title = "Export file",
                DefaultExt = "vpax",
                FileName = fileName
            };

            var threadStart = new ThreadStart(() => dialogResult = dialog.ShowDialog(dialogOwner));
            var thread = new Thread(threadStart);
            thread.CurrentUICulture = thread.CurrentCulture = CultureInfo.CurrentCulture;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            //var dialog2 = new Bravo.Infrastructure.Windows.SaveFileDialog
            //{
            //    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            //    Filter = "Vpax files (*.vpax)|*.vpax|All files (*.*)|*.*",
            //    Title = "Export file",
            //    DefaultExt = "vpax",
            //    //FileName = fileName
            //};
            //var result = dialog2.ShowDialog(hWnd: Process.GetCurrentProcess().MainWindowHandle);

            var actionResult = new FileActionResult
            {
                Canceled = dialogResult == System.Windows.Forms.DialogResult.Cancel,
                FileName = dialog.FileName
            };

            if (actionResult.Canceled == false)
            {
                using var fileStream = System.IO.File.Create(actionResult.FileName!);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }

            return actionResult;
        }
    }
}
