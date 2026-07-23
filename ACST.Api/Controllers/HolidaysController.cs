using ACST.Api.Middleware;
using ACST.Domain.DTOs.Holiday;
using ACST.Domain.Features.Holidays;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HolidaysController : ControllerBase
{
    private readonly IHolidayService _holidayService;

    public HolidaysController(IHolidayService holidayService)
    {
        _holidayService = holidayService;
    }

    [HttpGet]
    [HasPermission(Permissions.Holidays.View)]
    public async Task<IActionResult> GetAll([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _holidayService.GetAllHolidaysAsync(pageNumber, pageSize);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Holidays.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _holidayService.CreateHolidayAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("import-google")]
    [HasPermission(Permissions.Holidays.Manage)]
    public async Task<IActionResult> ImportGoogleHolidays([FromBody] ImportGoogleHolidaysRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _holidayService.ImportGoogleCalendarHolidaysAsync(request.CalendarId, request.StartDate, request.EndDate);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [HasPermission(Permissions.Holidays.Manage)]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _holidayService.DeleteHolidayAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
