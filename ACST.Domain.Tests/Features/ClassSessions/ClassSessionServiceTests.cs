using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.ClassSession;
using ACST.Domain.Features.ClassSessions;
using ACST.Domain.Features.GoogleCalendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ACST.Domain.Tests.Features.ClassSessions;

public class ClassSessionServiceTests
{
    private readonly AppDbContext _context;
    private readonly ClassSessionService _service;
    private readonly IGoogleCalendarService _googleCalendarMock;

    public ClassSessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        
        // Use real mock or dummy
        _googleCalendarMock = new GoogleCalendarService(new NullLogger<GoogleCalendarService>());
        _service = new ClassSessionService(_context, _googleCalendarMock);
    }

    [Fact]
    public async Task GenerateSessionsAsync_ShouldCreateSessions_RespectingHolidaysAndDayOfWeek()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Test Semester",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Math 101" };
        _context.TblModules.Add(module);
        
        await _context.SaveChangesAsync();

        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            IsActive = true
        };
        _context.TblRecurringSchedules.Add(schedule);

        var holiday = new TblHoliday
        {
            Name = "Special Holiday",
            HolidayDate = new DateOnly(2026, 1, 12) // This is a Monday
        };
        _context.TblHolidays.Add(holiday);
        
        await _context.SaveChangesAsync();

        // Act
        var request = new GenerateSessionsRequest { SemesterId = semester.Id };
        var result = await _service.GenerateSessionsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        
        var sessions = await _context.TblSessions.ToListAsync();
        
        // January 2026 has 4 Mondays: 5th, 12th, 19th, 26th
        Assert.Equal(4, sessions.Count);
        
        var holidaySession = sessions.FirstOrDefault(s => s.SessionDate == new DateOnly(2026, 1, 12));
        Assert.NotNull(holidaySession);
        Assert.Equal("Holiday", holidaySession.Status);

        var normalSession = sessions.FirstOrDefault(s => s.SessionDate == new DateOnly(2026, 1, 5));
        Assert.NotNull(normalSession);
        Assert.Equal("Not Marked", normalSession.Status);
    }

    [Fact]
    public async Task MarkAttendanceWithMagicLinkAsync_ValidWindow_ShouldMarkPresent()
    {
        // Arrange
        var token = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var session = new TblSession
        {
            MagicLinkToken = token,
            StartDatetime = now.AddMinutes(-10), // Class started 10 mins ago
            EndDatetime = now.AddMinutes(50),
            Status = "Not Marked",
            ModuleId = 1,
            SemesterId = 1,
            SessionDate = DateOnly.FromDateTime(now),
            RecurringScheduleId = 1
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAttendanceWithMagicLinkAsync(token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Attendance successfully marked.", result.Data);
        
        var dbSession = await _context.TblSessions.FindAsync(session.Id);
        Assert.Equal("Present", dbSession.Status);
    }

    [Fact]
    public async Task MarkAttendanceWithMagicLinkAsync_OutsideWindow_ShouldFail()
    {
        // Arrange
        var token = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var session = new TblSession
        {
            MagicLinkToken = token,
            StartDatetime = now.AddHours(-2), // Class ended 1 hour ago (window is +1 hr)
            EndDatetime = now.AddHours(-1),
            Status = "Not Marked",
            ModuleId = 1,
            SemesterId = 1,
            SessionDate = DateOnly.FromDateTime(now),
            RecurringScheduleId = 1
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAttendanceWithMagicLinkAsync(token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Outside of attendance window.", result.Message);
        
        var dbSession = await _context.TblSessions.FindAsync(session.Id);
        Assert.Equal("Not Marked", dbSession.Status); // Status should remain unchanged
    }
}
