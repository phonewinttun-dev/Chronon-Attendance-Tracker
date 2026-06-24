using System.Threading.Tasks;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Domain.Features.GoogleCalendar;

[Route("api/google-auth")]
[Route("api/googlecalendar")]
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
    public async Task<IActionResult> GetStatus()
    {
        bool isEnabled = _calendarService is not DisabledGoogleCalendarService;
        bool isConnected = false;

        if (isEnabled)
        {
            var connectedResult = await _calendarService.IsConnectedAsync();
            isConnected = connectedResult.IsSuccess && connectedResult.Data;
        }

        return Ok(new { IsEnabled = isEnabled, IsConnected = isConnected });
    }

    #endregion

    #region Connect Endpoint

    [HttpGet("connect")]
    public async Task<IActionResult> Connect([FromQuery] string? redirectUrl)
    {
        bool isEnabled = _calendarService is not DisabledGoogleCalendarService;
        if (!isEnabled)
        {
            return BadRequest("Google Calendar integration is disabled.");
        }

        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/google-auth/callback";
        var authUrlResult = await _calendarService.GetAuthorizationUrlAsync(redirectUri, redirectUrl);

        if (!authUrlResult.IsSuccess)
        {
            return BadRequest(authUrlResult.Message);
        }

        return Redirect(authUrlResult.Data!);
    }

    #endregion

    #region Callback Endpoint

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? state, [FromQuery] string? error)
    {
        bool isEnabled = _calendarService is not DisabledGoogleCalendarService;
        if (!isEnabled)
        {
            return BadRequest("Google Calendar integration is disabled.");
        }

        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest($"Authorization failed: {error}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("Authorization code is missing.");
        }

        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/google-auth/callback";
        var exchangeResult = await _calendarService.ExchangeCodeAndStoreTokenAsync(code, redirectUri);

        if (!exchangeResult.IsSuccess)
        {
            return BadRequest(exchangeResult.Message);
        }

        if (!string.IsNullOrEmpty(state))
        {
            return Redirect(state!);
        }

        return Content("<html><body style='font-family: sans-serif; text-align: center; padding-top: 50px; background-color: #0f172a; color: #f8fafc;'>" +
                       "<h3>Google Calendar authorized successfully!</h3>" +
                       "<p>You can close this window now.</p></body></html>", "text/html");
    }

    #endregion

    #region Disconnect Endpoint

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        bool isEnabled = _calendarService is not DisabledGoogleCalendarService;
        if (!isEnabled)
        {
            return BadRequest("Google Calendar integration is disabled.");
        }

        var result = await _calendarService.DisconnectAsync();
        if (!result.IsSuccess)
        {
            return BadRequest(result.Message);
        }

        return Ok(new { Message = "Disconnected successfully." });
    }

    #endregion
}
