using System;
using System.Threading;
using System.Threading.Tasks;
using ACST.Shared;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ACST.Domain.Features.GoogleCalendar;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly string _calendarId;
    private CalendarService? _calendarService;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _calendarId = configuration["GoogleCalendar:CalendarId"] ?? "primary";
    }

    #region Helper Methods

    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        if (_calendarService != null)
        {
            return _calendarService;
        }

        var section = _configuration.GetSection("GoogleCalendar");
        var clientId = section["ClientId"] ?? throw new InvalidOperationException("GoogleCalendar:ClientId configuration is missing.");
        var clientSecret = section["ClientSecret"] ?? throw new InvalidOperationException("GoogleCalendar:ClientSecret configuration is missing.");

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            new[] { CalendarService.Scope.Calendar },
            "user",
            CancellationToken.None,
            new FileDataStore("Google.Apis.Auth")
        );

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = section["ApplicationName"] ?? "Chronon Attendance Tracker"
        });

        return _calendarService;
    }

    #endregion

    #region CreateEvent

    public async Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken)
    {
        try
        {
            var service = await GetCalendarServiceAsync();

            var ev = new Event
            {
                Summary = title,
                Description = description,
                Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(startUtc, TimeSpan.Zero)
                },
                End = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(endUtc, TimeSpan.Zero)
                }
            };

            var request = service.Events.Insert(ev, _calendarId);
            var createdEvent = await request.ExecuteAsync();

            return Result<string>.Success(createdEvent.Id, "Google Calendar event created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar event for: {Title}", title);
            return Result<string>.Failure($"Failed to create Google Calendar event: {ex.Message}");
        }
    }

    #endregion

    #region UpdateEvent

    public async Task<Result> UpdateEventStatusAsync(string googleEventId, string status)
    {
        try
        {
            var service = await GetCalendarServiceAsync();
            var ev = await service.Events.Get(_calendarId, googleEventId).ExecuteAsync();

            string googleStatus = status.ToLowerInvariant() switch
            {
                "cancelled" => "cancelled",
                "confirmed" => "confirmed",
                "present" => "confirmed",
                "absent" => "confirmed",
                "late" => "confirmed",
                _ => "confirmed"
            };

            ev.Status = googleStatus;

            await service.Events.Update(ev, _calendarId, googleEventId).ExecuteAsync();
            return Result.Success("Google Calendar event status updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Google Calendar event status for event ID: {EventId}", googleEventId);
            return Result.Failure($"Failed to update Google Calendar event status: {ex.Message}");
        }
    }

    #endregion

    #region DeleteEvent

    public async Task<Result> DeleteEventAsync(string googleEventId)
    {
        try
        {
            var service = await GetCalendarServiceAsync();
            await service.Events.Delete(_calendarId, googleEventId).ExecuteAsync();
            return Result.Success("Google Calendar event deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Google Calendar event for event ID: {EventId}", googleEventId);
            return Result.Failure($"Failed to delete Google Calendar event: {ex.Message}");
        }
    }

    #endregion
}
