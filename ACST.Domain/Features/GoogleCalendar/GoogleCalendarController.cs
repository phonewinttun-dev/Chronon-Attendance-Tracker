using System.Threading.Tasks;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Domain.Features.GoogleCalendar;

[Route("api/[controller]")]
[ApiController]
public class GoogleCalendarController : ControllerBase
{
    private readonly IGoogleCalendarService _calendarService;

    public GoogleCalendarController(IGoogleCalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    #region Status Endpoint

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        bool isEnabled = _calendarService is not DisabledGoogleCalendarService;
        return Ok(new { IsEnabled = isEnabled });
    }

    #endregion
}
