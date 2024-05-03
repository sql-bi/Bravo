namespace Sqlbi.Bravo.Controllers;

using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
using Sqlbi.Bravo.Models;
using System.Net.Mime;

/// <summary>
/// ModelDiagram module controller
/// </summary>
/// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
[Route("api/[action]")]
[ApiController]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
public class ModelDiagramController : ControllerBase
{
    private readonly IPBIDesktopService _pbidesktopService;

    public ModelDiagramController(IPBIDesktopService pbidesktopService)
    {
        _pbidesktopService = pbidesktopService;
    }

    [HttpPost]
    [ActionName("GetReportDiagram")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBICloudDataset>))]
    [ProducesDefaultResponseType]
    public IActionResult GetDiagram(PBIDesktopReport report)
    {
        var json = _pbidesktopService.GetDiagram(report);
        return Ok(json);
    }
}
