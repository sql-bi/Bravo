namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Models.ManageCalendars;
    using Sqlbi.Bravo.Services;
    using System.Net.Mime;

    /// <summary>
    /// ManageCalendars module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("ManageCalendars/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class ManageCalendarsController : ControllerBase
    {
        private readonly IManageCalendarsService _manageCalendarsService;

        public ManageCalendarsController(IManageCalendarsService manageCalendarsService)
        {
            _manageCalendarsService = manageCalendarsService;
        }

        /// <summary>
        /// Gets all calendars and column information for a specific table
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetTableCalendarsForReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TableCalendarInfo))]
        [ProducesDefaultResponseType]
        public IActionResult GetTableCalendars(GetTableCalendarsRequest request)
        {
            var info = _manageCalendarsService.GetTableCalendars(request.Report!, request.TableName ?? "Date");
            return Ok(info);
        }

        /// <summary>
        /// Creates a new calendar on the specified table
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("CreateCalendarForReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult CreateCalendar(CreateCalendarRequest request)
        {
            _manageCalendarsService.CreateCalendar(request.Report!, request.TableName!, request.Calendar!);
            return Ok();
        }

        /// <summary>
        /// Updates an existing calendar
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateCalendarForReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult UpdateCalendar(UpdateCalendarRequest request)
        {
            _manageCalendarsService.UpdateCalendar(request.Report!, request.TableName!, request.CalendarName!, request.Calendar!);
            return Ok();
        }

        /// <summary>
        /// Deletes a calendar from the specified table
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("DeleteCalendarFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult DeleteCalendar(DeleteCalendarRequest request)
        {
            _manageCalendarsService.DeleteCalendar(request.Report!, request.TableName!, request.CalendarName!);
            return Ok();
        }
    }
}
