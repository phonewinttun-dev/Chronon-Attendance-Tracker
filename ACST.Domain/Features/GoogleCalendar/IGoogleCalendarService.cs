using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.Holiday;
using ACST.Shared;

namespace ACST.Domain.Features.GoogleCalendar;

public interface IGoogleCalendarService
{
    Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken);
    Task<Result> UpdateEventStatusAsync(string googleEventId, string status);
    Task<Result> DeleteEventAsync(string googleEventId);
    Task<Result<List<HolidayDto>>> FetchHolidaysAsync(string holidayCalendarId, DateTime startUtc, DateTime endUtc);

    // Connection Management
    Task<Result<bool>> IsConnectedAsync();
    Task<Result<string>> GetAuthorizationUrlAsync(string redirectUri, string? state);
    Task<Result> ExchangeCodeAndStoreTokenAsync(string code, string redirectUri);
    Task<Result> DisconnectAsync();
}


