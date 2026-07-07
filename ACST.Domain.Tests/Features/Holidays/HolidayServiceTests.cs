using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Holiday;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Holidays;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ACST.Domain.Tests.Features.Holidays;

public class HolidayServiceTests
{
    private readonly AppDbContext _context;
    private readonly FakeGoogleCalendarService _calendarMock;
    private readonly HolidayService _service;

    public HolidayServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _calendarMock = new FakeGoogleCalendarService();
        _service = new HolidayService(_context, _calendarMock);
    }

    [Fact]
    public async Task ImportGoogleCalendarHolidaysAsync_WithNewHolidays_ShouldSaveToDb()
    {
        // Arrange
        var calendarId = "en.mm#holiday@group.v.calendar.google.com";
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);

        _calendarMock.HolidaysToReturn = new List<HolidayDto>
        {
            new HolidayDto { Name = "Independence Day", HolidayDate = new DateOnly(2026, 1, 4) },
            new HolidayDto { Name = "Union Day", HolidayDate = new DateOnly(2026, 2, 12) }
        };

        // Act
        var result = await _service.ImportGoogleCalendarHolidaysAsync(calendarId, startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data);

        var savedHolidays = await _context.TblHolidays.Where(h => !h.IsDeleted).ToListAsync();
        Assert.Equal(2, savedHolidays.Count);
        Assert.Contains(savedHolidays, h => h.Name == "Independence Day" && h.HolidayDate == new DateOnly(2026, 1, 4));
        Assert.Contains(savedHolidays, h => h.Name == "Union Day" && h.HolidayDate == new DateOnly(2026, 2, 12));
    }

    [Fact]
    public async Task ImportGoogleCalendarHolidaysAsync_WithDuplicateHolidays_ShouldSkipDuplicates()
    {
        // Arrange
        var calendarId = "en.mm#holiday@group.v.calendar.google.com";
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);

        // Pre-populate one holiday in database
        _context.TblHolidays.Add(new TblHoliday
        {
            Name = "Independence Day",
            HolidayDate = new DateOnly(2026, 1, 4),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _calendarMock.HolidaysToReturn = new List<HolidayDto>
        {
            new HolidayDto { Name = "Independence Day", HolidayDate = new DateOnly(2026, 1, 4) }, // Duplicate date
            new HolidayDto { Name = "Union Day", HolidayDate = new DateOnly(2026, 2, 12) }        // New
        };

        // Act
        var result = await _service.ImportGoogleCalendarHolidaysAsync(calendarId, startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data); // Only 1 new imported

        var savedHolidays = await _context.TblHolidays.Where(h => !h.IsDeleted).ToListAsync();
        Assert.Equal(2, savedHolidays.Count);
    }

    [Fact]
    public async Task ImportGoogleCalendarHolidaysAsync_CalendarFetchFailure_ShouldReturnFailure()
    {
        // Arrange
        var calendarId = "invalid-calendar";
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);

        _calendarMock.ShouldFail = true;

        // Act
        var result = await _service.ImportGoogleCalendarHolidaysAsync(calendarId, startDate, endDate);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to fetch holidays", result.Message);
    }

    private class FakeGoogleCalendarService : IGoogleCalendarService
    {
        public List<HolidayDto> HolidaysToReturn { get; set; } = new();
        public bool ShouldFail { get; set; }

        public Task<Result<List<HolidayDto>>> FetchHolidaysAsync(string holidayCalendarId, DateTime startUtc, DateTime endUtc)
        {
            if (ShouldFail)
            {
                return Task.FromResult(Result<List<HolidayDto>>.Failure("API Error"));
            }

            return Task.FromResult(Result<List<HolidayDto>>.Success(HolidaysToReturn, "Fetched."));
        }

        public Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken) => throw new NotImplementedException();
        public Task<Result> UpdateEventStatusAsync(string googleEventId, string status) => throw new NotImplementedException();
        public Task<Result> DeleteEventAsync(string googleEventId) => throw new NotImplementedException();
        public Task<Result<bool>> IsConnectedAsync() => throw new NotImplementedException();
        public Task<Result<string>> GetAuthorizationUrlAsync(string redirectUri, string? state) => throw new NotImplementedException();
        public Task<Result> ExchangeCodeAndStoreTokenAsync(string code, string redirectUri) => throw new NotImplementedException();
        public Task<Result> DisconnectAsync() => throw new NotImplementedException();
    }
}
