using System;
using System.Threading.Tasks;
using ACST.Shared;
using Microsoft.Extensions.Logging;

namespace ACST.Domain.Features.GoogleCalendar;

public class DisabledGoogleCalendarService : IGoogleCalendarService
{
    private readonly ILogger<DisabledGoogleCalendarService> _logger;

    public DisabledGoogleCalendarService(ILogger<DisabledGoogleCalendarService> logger)
    {
        _logger = logger;
    }

    #region CreateEvent

    public Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken)
    {
        _logger.LogInformation("DisabledGoogleCalendarService: Google Calendar integration is disabled. Simulated event creation for: {Title}", title);
        
        var dummyEventId = $"mock_google_event_{Guid.NewGuid():N}";
        return Task.FromResult(Result<string>.Success(dummyEventId, "Mock event created successfully."));
    }

    #endregion

    #region UpdateEvent

    public Task<Result> UpdateEventStatusAsync(string googleEventId, string status)
    {
        _logger.LogInformation("DisabledGoogleCalendarService: Google Calendar integration is disabled. Simulated event status update for: {EventId} to {Status}", googleEventId, status);
        return Task.FromResult(Result.Success("Mock event updated successfully."));
    }

    #endregion

    #region DeleteEvent

    public Task<Result> DeleteEventAsync(string googleEventId)
    {
        _logger.LogInformation("DisabledGoogleCalendarService: Google Calendar integration is disabled. Simulated event deletion for: {EventId}", googleEventId);
        return Task.FromResult(Result.Success("Mock event deleted successfully."));
    }

    #endregion

    #region Connection Management

    public Task<Result<bool>> IsConnectedAsync()
    {
        return Task.FromResult(Result<bool>.Success(false, "Google Calendar integration is disabled."));
    }

    public Task<Result<string>> GetAuthorizationUrlAsync(string redirectUri, string? state)
    {
        return Task.FromResult(Result<string>.Failure("Google Calendar integration is disabled."));
    }

    public Task<Result> ExchangeCodeAndStoreTokenAsync(string code, string redirectUri)
    {
        return Task.FromResult(Result.Failure("Google Calendar integration is disabled."));
    }

    public Task<Result> DisconnectAsync()
    {
        return Task.FromResult(Result.Failure("Google Calendar integration is disabled."));
    }

    #endregion
}
