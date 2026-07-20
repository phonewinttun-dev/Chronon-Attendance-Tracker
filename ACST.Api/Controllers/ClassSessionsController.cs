using ACST.Domain.DTOs.ClassSession;
using ACST.Domain.Features.ClassSessions;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers;

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
    public async Task<IActionResult> Get([FromQuery] GetClassSessionsRequest request)
    {
        var result = await _sessionService.GetSessionsAsync(request);
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

    [HttpPatch("bulk-status")]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateSessionStatusRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _sessionService.BulkUpdateSessionStatusAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateClassSessionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _sessionService.UpdateSessionAsync(id, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _sessionService.DeleteSessionAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // Public Magic Link Endpoint
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

    // Attendance Update Endpoint
    [HttpPost("attendance")]
    public async Task<IActionResult> MarkAttendance([FromBody] DashboardAttendanceRequest request)
    {
        var result = await _sessionService.UpdateSessionStatusAsync(request.SessionId, new UpdateSessionStatusRequest { Status = request.Status });
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
