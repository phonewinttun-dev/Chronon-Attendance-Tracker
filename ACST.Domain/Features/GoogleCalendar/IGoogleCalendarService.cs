using System;
using System.Threading.Tasks;
using ACST.Shared;

namespace ACST.Domain.Features.GoogleCalendar;

public interface IGoogleCalendarService
{
    Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken);
    Task<Result> UpdateEventStatusAsync(string googleEventId, string status);
    Task<Result> DeleteEventAsync(string googleEventId);

    // Connection Management
    Task<Result<bool>> IsConnectedAsync();
    Task<Result<string>> GetAuthorizationUrlAsync(string redirectUri, string? state);
    Task<Result> ExchangeCodeAndStoreTokenAsync(string code, string redirectUri);
    Task<Result> DisconnectAsync();
}

