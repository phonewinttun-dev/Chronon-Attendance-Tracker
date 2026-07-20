using ACST.Domain.DTOs.RecurringSchedule;
using ACST.Domain.Features.RecurringSchedules;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers;

[Route("api/modules/{moduleId}/recurring-schedules")]
[ApiController]
public class RecurringSchedulesController : ControllerBase
{
    private readonly IRecurringScheduleService _scheduleService;

    public RecurringSchedulesController(IRecurringScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByModule(long moduleId)
    {
        var result = await _scheduleService.GetSchedulesByModuleAsync(moduleId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{semesterId}")]
    public async Task<IActionResult> Create(long moduleId, long semesterId, [FromBody] CreateRecurringScheduleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _scheduleService.CreateScheduleAsync(moduleId, semesterId, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("~/api/recurring-schedules/{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _scheduleService.DeleteScheduleAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
