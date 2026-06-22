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

    [Fact]
    public async Task UpdateSessionStatusAsync_ShouldFail_WhenAfter24Hours()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var session = new TblSession
        {
            StartDatetime = now.AddHours(-25), // Started 25 hours ago
            EndDatetime = now.AddHours(-23),
            Status = "Not Marked",
            ModuleId = 1,
            SemesterId = 1,
            SessionDate = DateOnly.FromDateTime(now.AddDays(-1)),
            RecurringScheduleId = 1
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateSessionStatusAsync(session.Id, new UpdateSessionStatusRequest { Status = "Present" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Attendance status cannot be changed after 24 hours.", result.Message);
        
        var dbSession = await _context.TblSessions.FindAsync(session.Id);
        Assert.Equal("Not Marked", dbSession.Status);
    }

    [Fact]
    public async Task UpdateSessionAsync_ShouldFail_WhenAfter24Hours()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var session = new TblSession
        {
            StartDatetime = now.AddHours(-25),
            EndDatetime = now.AddHours(-23),
            Status = "Not Marked",
            ModuleId = 1,
            SemesterId = 1,
            SessionDate = DateOnly.FromDateTime(now.AddDays(-1)),
            RecurringScheduleId = 1
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        var request = new UpdateClassSessionRequest
        {
            ModuleId = 1,
            SessionDate = DateOnly.FromDateTime(now),
            StartDatetime = now,
            EndDatetime = now.AddHours(1),
            Status = "Present"
        };

        // Act
        var result = await _service.UpdateSessionAsync(session.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Attendance status cannot be changed after 24 hours.", result.Message);
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldFail_WhenAfter24Hours()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var session = new TblSession
        {
            StartDatetime = now.AddHours(-25),
            EndDatetime = now.AddHours(-23),
            Status = "Not Marked",
            ModuleId = 1,
            SemesterId = 1,
            SessionDate = DateOnly.FromDateTime(now.AddDays(-1)),
            RecurringScheduleId = 1
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteSessionAsync(session.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Attendance status cannot be changed after 24 hours.", result.Message);
        
        var dbSession = await _context.TblSessions.FindAsync(session.Id);
        Assert.False(dbSession.IsDeleted);
    }

    [Fact]
    public async Task UpdateSessionAsync_ShouldSucceed_WhenWithin24Hours()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var module = new TblModule { Name = "Science 101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var session = new TblSession
        {
            StartDatetime = now.AddHours(-23), // Started 23 hours ago (within 24 hours)
            EndDatetime = now.AddHours(-21),
            Status = "Not Marked",
            ModuleId = module.Id,
            SemesterId = 1,
            SessionDate = DateOnly.FromDateTime(now.AddDays(-1)),
            RecurringScheduleId = 1
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        var request = new UpdateClassSessionRequest
        {
            ModuleId = module.Id,
            SessionDate = DateOnly.FromDateTime(now),
            StartDatetime = now.AddHours(-1),
            EndDatetime = now.AddHours(1),
            Status = "Present"
        };

        // Act
        var result = await _service.UpdateSessionAsync(session.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        
        var dbSession = await _context.TblSessions.FindAsync(session.Id);
        Assert.Equal("Present", dbSession.Status);
        Assert.Equal(now.AddHours(-1), dbSession.StartDatetime);
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldSucceed_WhenWithin24Hours()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var session = new TblSession
        {
            StartDatetime = now.AddHours(-23),
            EndDatetime = now.AddHours(-21),
            Status = "Not Marked",
            ModuleId = 1,
            SemesterId = 1,
            SessionDate = DateOnly.FromDateTime(now.AddDays(-1)),
            RecurringScheduleId = 1
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteSessionAsync(session.Id);

        // Assert
        Assert.True(result.IsSuccess);
        
        var dbSession = await _context.TblSessions.FindAsync(session.Id);
        Assert.True(dbSession.IsDeleted);
    }

    [Fact]
    public async Task GetSessionsAsync_ShouldIgnoreSessionsOfDeletedModulesOrSemesters()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Active Semester",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            IsDeleted = false
        };
        var deletedSemester = new TblSemester
        {
            Name = "Deleted Semester",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            IsDeleted = true
        };
        _context.TblSemesters.AddRange(semester, deletedSemester);

        var module = new TblModule { Name = "Active Module", Semester = semester, IsDeleted = false };
        var deletedModule = new TblModule { Name = "Deleted Module", Semester = semester, IsDeleted = true };
        _context.TblModules.AddRange(module, deletedModule);
        await _context.SaveChangesAsync();

        var session1 = new TblSession
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            RecurringScheduleId = 1,
            SessionDate = new DateOnly(2026, 1, 5),
            StartDatetime = DateTime.UtcNow,
            EndDatetime = DateTime.UtcNow.AddHours(2),
            Status = "Not Marked",
            IsDeleted = false
        };
        var session2 = new TblSession
        {
            ModuleId = deletedModule.Id,
            SemesterId = semester.Id,
            RecurringScheduleId = 1,
            SessionDate = new DateOnly(2026, 1, 6),
            StartDatetime = DateTime.UtcNow,
            EndDatetime = DateTime.UtcNow.AddHours(2),
            Status = "Not Marked",
            IsDeleted = false
        };
        var session3 = new TblSession
        {
            ModuleId = module.Id,
            SemesterId = deletedSemester.Id,
            RecurringScheduleId = 1,
            SessionDate = new DateOnly(2026, 1, 7),
            StartDatetime = DateTime.UtcNow,
            EndDatetime = DateTime.UtcNow.AddHours(2),
            Status = "Not Marked",
            IsDeleted = false
        };
        _context.TblSessions.AddRange(session1, session2, session3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSessionsAsync(null, null, null, null, null);

        // Assert
        Assert.True(result.IsSuccess);
        var sessions = result.Data;
        Assert.NotNull(sessions);
        
        // Should only return session1 (active module & active semester)
        Assert.Single(sessions);
        Assert.Equal(session1.Id, sessions.First().Id);
    }
}
