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
        _googleCalendarMock = new DisabledGoogleCalendarService(new NullLogger<DisabledGoogleCalendarService>());
        _service = new ClassSessionService(_context, _googleCalendarMock);
    }



    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
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
        Assert.NotNull(dbSession);
        Assert.Equal("Present", dbSession.Status);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
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
        Assert.NotNull(dbSession);
        Assert.Equal("Not Marked", dbSession.Status); // Status should remain unchanged
    }

    [Fact(Skip = "24-hour attendance edit window limit is currently disabled in business logic")]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
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
        Assert.NotNull(dbSession);
        Assert.Equal("Not Marked", dbSession.Status);
    }

    [Fact(Skip = "24-hour attendance edit window limit is currently disabled in business logic")]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
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

    [Fact(Skip = "24-hour attendance edit window limit is currently disabled in business logic")]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
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
        Assert.NotNull(dbSession);
        Assert.False(dbSession.IsDeleted);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task UpdateSessionAsync_ShouldSucceed_WhenWithin24Hours()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var module = new TblModule { Name = "Science 101", ModuleCode = "SCI-101" };
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
        Assert.NotNull(dbSession);
        Assert.Equal("Present", dbSession.Status);
        Assert.Equal(now.AddHours(-1), dbSession.StartDatetime);
    }

    [Fact]
    [Trait("Category", "positive")]
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
        Assert.NotNull(dbSession);
        Assert.True(dbSession.IsDeleted);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "boundary")]
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

        var module = new TblModule { Name = "Active Module", ModuleCode = "ACT-101", Semester = semester, IsDeleted = false };
        var deletedModule = new TblModule { Name = "Deleted Module", ModuleCode = "DEL-101", Semester = semester, IsDeleted = true };
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
        var result = await _service.GetSessionsAsync(new GetClassSessionsRequest());

        // Assert
        Assert.True(result.IsSuccess);
        var sessions = result.Data;
        Assert.NotNull(sessions);
        
        // Should only return session1 (active module & active semester)
        Assert.Single(sessions);
        Assert.Equal(session1.Id, sessions.First().Id);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "boundary")]
    public async Task GetSessionsAsync_WhenNoSessionsExist_ShouldReturnSuccessWithEmptyList()
    {
        // Act
        var result = await _service.GetSessionsAsync(new GetClassSessionsRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Pagination.TotalCount);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task GenerateSessionsAsync_WithLectureEndDate_ShouldStopGeneratingAtLectureEndDate()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30),
            LectureEndDate = new DateOnly(2026, 9, 11),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule
        {
            Name = "Software Engineering",
            ModuleCode = "SE101",
            SemesterId = semester.Id,
            IsDeleted = false
        };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Wednesday, // Sep 2, 9, 16, 23, 30
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            IsActive = true,
            IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSessionsAsync(new GenerateSessionsRequest
        {
            SemesterId = semester.Id
        });

        // Assert
        Assert.True(result.IsSuccess);
        var sessions = await _context.TblSessions
            .Where(s => s.SemesterId == semester.Id && !s.IsDeleted)
            .OrderBy(s => s.SessionDate)
            .ToListAsync();

        // Only Sep 2 and Sep 9 should be generated (Sep 16, 23, 30 are after Sep 11)
        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, s => Assert.True(s.SessionDate <= semester.LectureEndDate));
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 2));
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 9));
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task GenerateSessionsAsync_WithTimeBoundSchedule_ShouldGenerateOnlyWithinEffectiveRange()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule
        {
            Name = "Algorithms",
            ModuleCode = "CS-201",
            IsDeleted = false
        };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        // Wednesdays in Sept 2026 are: Sep 2, Sep 9, Sep 16, Sep 23, Sep 30
        // Effective range: Sep 7 to Sep 20 -> Only Sep 9 and Sep 16 should be generated
        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Wednesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            EffectiveStartDate = new DateOnly(2026, 9, 7),
            EffectiveEndDate = new DateOnly(2026, 9, 20),
            IsActive = true,
            IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSessionsAsync(new GenerateSessionsRequest
        {
            SemesterId = semester.Id
        });

        // Assert
        Assert.True(result.IsSuccess);
        var sessions = await _context.TblSessions
            .Where(s => s.SemesterId == semester.Id && !s.IsDeleted)
            .OrderBy(s => s.SessionDate)
            .ToListAsync();

        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 9));
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 16));
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task GenerateSessionsAsync_WithMultipleTimeBoundSchedules_ShouldGenerateNonOverlappingSessions()
    {
        // Arrange (User scenario: Schedule changes after initial weeks)
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule
        {
            Name = "Database Systems",
            ModuleCode = "CS-301",
            IsDeleted = false
        };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        // Schedule 1 (Initial 2 weeks): Wednesdays 9:00 - 11:00 (Sep 2, Sep 9)
        var schedule1 = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Wednesday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            EffectiveStartDate = new DateOnly(2026, 9, 1),
            EffectiveEndDate = new DateOnly(2026, 9, 15),
            IsActive = true,
            IsDeleted = false
        };

        // Schedule 2 (Permanent from week 3): Fridays 14:00 - 16:00 (Sep 18, Sep 25)
        var schedule2 = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Friday,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(16, 0),
            EffectiveStartDate = new DateOnly(2026, 9, 16),
            EffectiveEndDate = new DateOnly(2026, 9, 30),
            IsActive = true,
            IsDeleted = false
        };

        _context.TblRecurringSchedules.AddRange(schedule1, schedule2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSessionsAsync(new GenerateSessionsRequest
        {
            SemesterId = semester.Id
        });

        // Assert
        Assert.True(result.IsSuccess);
        var sessions = await _context.TblSessions
            .Where(s => s.SemesterId == semester.Id && !s.IsDeleted)
            .OrderBy(s => s.SessionDate)
            .ToListAsync();

        Assert.Equal(4, sessions.Count);
        // Schedule 1 sessions
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 2) && s.RecurringScheduleId == schedule1.Id);
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 9) && s.RecurringScheduleId == schedule1.Id);
        // Schedule 2 sessions
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 18) && s.RecurringScheduleId == schedule2.Id);
        Assert.Contains(sessions, s => s.SessionDate == new DateOnly(2026, 9, 25) && s.RecurringScheduleId == schedule2.Id);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task GenerateSessionsAsync_WithoutEffectiveDates_ShouldFallbackToFullSemester()
    {
        // Arrange (Backward compatibility test for existing records with NULL effective dates)
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule
        {
            Name = "Networks",
            ModuleCode = "CS-401",
            IsDeleted = false
        };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var legacySchedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Wednesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            EffectiveStartDate = null, // Legacy null value
            EffectiveEndDate = null,   // Legacy null value
            IsActive = true,
            IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(legacySchedule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSessionsAsync(new GenerateSessionsRequest
        {
            SemesterId = semester.Id
        });

        // Assert
        Assert.True(result.IsSuccess);
        var sessions = await _context.TblSessions
            .Where(s => s.SemesterId == semester.Id && !s.IsDeleted)
            .OrderBy(s => s.SessionDate)
            .ToListAsync();

        // All 5 Wednesdays in Sept 2026 (2, 9, 16, 23, 30)
        Assert.Equal(5, sessions.Count);
    }
}
