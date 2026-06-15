using System;
using System.Threading.Tasks;
using ACST.Shared;
using Microsoft.Extensions.Logging;

namespace ACST.Domain.Features.GoogleCalendar;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(ILogger<GoogleCalendarService> logger)
    {
        _logger = logger;
    }

    public Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken)
    {
        // Mock implementation since Google Cloud isn't set up yet.
        _logger.LogWarning("Google Calendar integration is currently disabled. Would have created event for: {Title}", title);
        
        // Return a dummy ID so the database can store something, simulating a successful creation.
        var dummyEventId = $"mock_google_event_{Guid.NewGuid():N}";
        
        return Task.FromResult(Result<string>.Success(dummyEventId, "Mock event created successfully."));
    }

    public Task<Result> UpdateEventStatusAsync(string googleEventId, string status)
    {
        _logger.LogWarning("Google Calendar integration is currently disabled. Would have updated event {EventId} to status: {Status}", googleEventId, status);
        return Task.FromResult(Result.Success("Mock event updated successfully."));
    }

    public Task<Result> DeleteEventAsync(string googleEventId)
    {
        _logger.LogWarning("Google Calendar integration is currently disabled. Would have deleted event {EventId}", googleEventId);
        return Task.FromResult(Result.Success("Mock event deleted successfully."));
    }
}
