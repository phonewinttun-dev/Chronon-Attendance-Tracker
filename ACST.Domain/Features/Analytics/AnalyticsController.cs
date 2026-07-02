using System.Threading.Tasks;
using ACST.Domain.Features.Analytics;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Domain.Features.Analytics;

[Route("api/[controller]")]
[ApiController]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("overall/{semesterId}")]
    public async Task<IActionResult> GetOverall(long semesterId)
    {
        var result = await _analyticsService.GetOverallAnalyticsAsync(semesterId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("modules/{moduleId}/{semesterId}")]
    public async Task<IActionResult> GetByModule(long moduleId, long semesterId)
    {
        var result = await _analyticsService.GetModuleAnalyticsAsync(moduleId, semesterId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("/api/semesters/{id}/dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary(long id)
    {
        var result = await _analyticsService.GetDashboardSummaryAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("/api/semesters/{id}/dashboard/daily-weekly")]
    public async Task<IActionResult> GetDashboardDailyWeekly(long id)
    {
        var result = await _analyticsService.GetDashboardDailyWeeklyAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("/api/semesters/{id}/dashboard/modules")]
    public async Task<IActionResult> GetDashboardModules(long id)
    {
        var result = await _analyticsService.GetDashboardModulesAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
