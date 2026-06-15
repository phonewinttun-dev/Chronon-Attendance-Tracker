using System;
using System.Threading.Tasks;
using ACST.Domain.DTOs.ClassSession;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Domain.Features.ClassSessions;

[Route("api/[controller]")]
[ApiController]
public class ClassSessionsController : ControllerBase
{
    private readonly IClassSessionService _sessionService;

    public ClassSessionsController(IClassSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? semesterId, [FromQuery] long? moduleId, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, [FromQuery] string? status)
    {
        var result = await _sessionService.GetSessionsAsync(semesterId, moduleId, startDate, endDate, status);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _sessionService.GetSessionByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateSessionsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _sessionService.GenerateSessionsAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateSessionStatusRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _sessionService.UpdateSessionStatusAsync(id, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

// Separate controller for public Magic Link
[Route("api/attendance")]
[ApiController]
public class AttendanceController : ControllerBase
{
    private readonly IClassSessionService _sessionService;

    public AttendanceController(IClassSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet("magic-link/{token}")]
    public async Task<IActionResult> MagicLink(Guid token)
    {
        var result = await _sessionService.MarkAttendanceWithMagicLinkAsync(token);

        string title = result.IsSuccess ? "Success" : "Error";
        string message = result.IsSuccess ? result.Data! : result.Message;
        string color = result.IsSuccess ? "#98d68b" : (result.Message == "Already marked as present." ? "#b4cea9" : "#ffb4ab");
        string bgColor = "#11140f";

        // Beautiful Obsidian-style HTML response
        var html = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>{title}</title>
            <style>
                body {{ background-color: {bgColor}; color: #e1e3db; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }}
                .card {{ background-color: #1d211b; border: 1px solid #3c3e3a; padding: 40px; border-radius: 12px; text-align: center; max-width: 400px; }}
                h2 {{ color: {color}; margin-top: 0; }}
                p {{ color: #c1c9ba; font-size: 16px; }}
            </style>
        </head>
        <body>
            <div class='card'>
                <h2>{title}</h2>
                <p>{message}</p>
            </div>
        </body>
        </html>";

        return Content(html, "text/html");
    }

    [HttpPost]
    public async Task<IActionResult> MarkAttendance([FromBody] DashboardAttendanceRequest request)
    {
        var result = await _sessionService.UpdateSessionStatusAsync(request.SessionId, new UpdateSessionStatusRequest { Status = request.Status });
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public class DashboardAttendanceRequest
{
    public long SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
}
