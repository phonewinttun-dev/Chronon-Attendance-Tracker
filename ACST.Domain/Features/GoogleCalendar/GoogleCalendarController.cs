using System;
using System.Threading.Tasks;
using ACST.Domain.Features.Holidays;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ACST.Domain.Features.GoogleCalendar;

[Route("api/google-auth")]
[Route("api/googlecalendar")]
[ApiController]
public class GoogleCalendarController : ControllerBase
{
    private readonly IGoogleCalendarService _calendarService;
    private readonly IHolidayService _holidayService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarController> _logger;

    public GoogleCalendarController(
        IGoogleCalendarService calendarService,
        IHolidayService holidayService,
        IConfiguration configuration,
        ILogger<GoogleCalendarController> logger)
    {
        _calendarService = calendarService;
        _holidayService = holidayService;
        _configuration = configuration;
        _logger = logger;
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

        // Auto-import default holidays for the current year
        try
        {
            var holidayCalendarId = _configuration["GoogleCalendar:HolidayCalendarId"] ?? "en.mm#holiday@group.v.calendar.google.com";
            var currentYear = DateTime.UtcNow.Year;
            var startDate = new DateOnly(currentYear, 1, 1);
            var endDate = new DateOnly(currentYear, 12, 31);
            
            _logger.LogInformation("Auto-importing holidays for {Year} from calendar {CalendarId} after successful OAuth connection.", currentYear, holidayCalendarId);
            await _holidayService.ImportGoogleCalendarHolidaysAsync(holidayCalendarId, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to automatically import holidays on Google Calendar connection callback.");
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
