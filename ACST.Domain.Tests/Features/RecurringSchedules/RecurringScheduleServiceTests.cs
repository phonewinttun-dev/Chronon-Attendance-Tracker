using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.RecurringSchedule;
using ACST.Domain.Features.ClassSessions;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.RecurringSchedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ACST.Domain.Tests.Features.RecurringSchedules;

public class RecurringScheduleServiceTests
{
    private readonly AppDbContext _context;
    private readonly RecurringScheduleService _service;
    private readonly IGoogleCalendarService _googleCalendarMock;

    public RecurringScheduleServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        _googleCalendarMock = new DisabledGoogleCalendarService(new NullLogger<DisabledGoogleCalendarService>());
        var classSessionService = new ClassSessionService(_context, _googleCalendarMock);
        _service = new RecurringScheduleService(_context, classSessionService, _googleCalendarMock);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task CreateScheduleAsync_ShouldAutoGenerateClassSessions()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Calculus", ModuleCode = "CALC-101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new CreateRecurringScheduleRequest
        {
            DayOfWeek = (short)DayOfWeek.Wednesday, // Wednesdays in Sept 2026: 2, 9, 16, 23, 30 (5 sessions)
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(16, 0)
        };

        // Act
        var result = await _service.CreateScheduleAsync(module.Id, semester.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        var sessions = await _context.TblSessions
            .Where(s => s.RecurringScheduleId == result.Data.Id && !s.IsDeleted)
            .ToListAsync();

        Assert.Equal(5, sessions.Count);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task DeleteScheduleAsync_ShouldCascadeSoftDeleteFutureSessions()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Calculus", ModuleCode = "CALC-101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Wednesday,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(16, 0),
            IsActive = true,
            IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Add a past session (should NOT be deleted)
        var pastSession = new TblSession
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            RecurringScheduleId = schedule.Id,
            SessionDate = new DateOnly(2026, 9, 2),
            StartDatetime = DateTime.UtcNow.AddDays(-1),
            EndDatetime = DateTime.UtcNow.AddDays(-1).AddHours(2),
            Status = "Present",
            IsDeleted = false
        };
        // Add a future session (should be deleted)
        var futureSession = new TblSession
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            RecurringScheduleId = schedule.Id,
            SessionDate = new DateOnly(2026, 9, 30),
            StartDatetime = DateTime.UtcNow.AddDays(5),
            EndDatetime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Status = "Not Marked",
            GoogleEventId = "mock-google-id",
            IsDeleted = false
        };
        _context.TblSessions.AddRange(pastSession, futureSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteScheduleAsync(schedule.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var dbSchedule = await _context.TblRecurringSchedules.FindAsync(schedule.Id);
        Assert.NotNull(dbSchedule);
        Assert.True(dbSchedule.IsDeleted);

        var dbPastSession = await _context.TblSessions.FindAsync(pastSession.Id);
        Assert.NotNull(dbPastSession);
        Assert.False(dbPastSession.IsDeleted); // Remains untouched

        var dbFutureSession = await _context.TblSessions.FindAsync(futureSession.Id);
        Assert.NotNull(dbFutureSession);
        Assert.True(dbFutureSession.IsDeleted); // Soft-deleted
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task CreateScheduleAsync_WithEffectiveDates_ShouldSaveCorrectly()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Calculus II", ModuleCode = "CALC-102" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new CreateRecurringScheduleRequest
        {
            DayOfWeek = (short)DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            EffectiveStartDate = new DateOnly(2026, 9, 1),
            EffectiveEndDate = new DateOnly(2026, 9, 30),
            GenerateSessions = false
        };

        // Act
        var result = await _service.CreateScheduleAsync(module.Id, semester.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Data.EffectiveStartDate);
        Assert.Equal(new DateOnly(2026, 9, 30), result.Data.EffectiveEndDate);

        var dbSchedule = await _context.TblRecurringSchedules.FindAsync(result.Data.Id);
        Assert.NotNull(dbSchedule);
        Assert.Equal(new DateOnly(2026, 9, 1), dbSchedule.EffectiveStartDate);
        Assert.Equal(new DateOnly(2026, 9, 30), dbSchedule.EffectiveEndDate);
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task CreateScheduleAsync_WithInvertedEffectiveDates_ShouldFail()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Physics", ModuleCode = "PHYS-101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new CreateRecurringScheduleRequest
        {
            DayOfWeek = (short)DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            EffectiveStartDate = new DateOnly(2026, 10, 1),
            EffectiveEndDate = new DateOnly(2026, 9, 1), // Inverted
            GenerateSessions = false
        };

        // Act
        var result = await _service.CreateScheduleAsync(module.Id, semester.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Effective start date must be on or before effective end date", result.Message);
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task CreateScheduleAsync_WhenStartDateBeforeSemester_ShouldFail()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Calculus", ModuleCode = "CALC-102" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new CreateRecurringScheduleRequest
        {
            DayOfWeek = (short)DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            EffectiveStartDate = new DateOnly(2026, 8, 15), // Before semester start
            EffectiveEndDate = new DateOnly(2026, 10, 1),
            GenerateSessions = false
        };

        // Act
        var result = await _service.CreateScheduleAsync(module.Id, semester.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Effective start date cannot be before semester start date", result.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task UpdateScheduleAsync_ShouldUpdateScheduleDetails()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Chemistry", ModuleCode = "CHEM-101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            IsActive = true,
            IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateRecurringScheduleRequest
        {
            DayOfWeek = (short)DayOfWeek.Friday,
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(15, 0),
            EffectiveStartDate = new DateOnly(2026, 10, 1),
            EffectiveEndDate = new DateOnly(2026, 11, 30)
        };

        // Act
        var result = await _service.UpdateScheduleAsync(schedule.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal((short)DayOfWeek.Friday, result.Data.DayOfWeek);
        Assert.Equal(new TimeOnly(13, 0), result.Data.StartTime);
        Assert.Equal(new TimeOnly(15, 0), result.Data.EndTime);
        Assert.Equal(new DateOnly(2026, 10, 1), result.Data.EffectiveStartDate);
        Assert.Equal(new DateOnly(2026, 11, 30), result.Data.EffectiveEndDate);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task DeleteScheduleAsync_ShouldPreserveMarkedFutureSessions()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Biology", ModuleCode = "BIO-101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = (short)DayOfWeek.Wednesday,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(16, 0),
            IsActive = true,
            IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Future session already marked as Present (must NOT be deleted)
        var markedFutureSession = new TblSession
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            RecurringScheduleId = schedule.Id,
            SessionDate = new DateOnly(2026, 9, 23),
            StartDatetime = DateTime.UtcNow.AddDays(2),
            EndDatetime = DateTime.UtcNow.AddDays(2).AddHours(2),
            Status = "Present",
            IsDeleted = false
        };
        // Future unmarked session (SHOULD be deleted)
        var unmarkedFutureSession = new TblSession
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            RecurringScheduleId = schedule.Id,
            SessionDate = new DateOnly(2026, 9, 30),
            StartDatetime = DateTime.UtcNow.AddDays(9),
            EndDatetime = DateTime.UtcNow.AddDays(9).AddHours(2),
            Status = "Not Marked",
            IsDeleted = false
        };
        _context.TblSessions.AddRange(markedFutureSession, unmarkedFutureSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteScheduleAsync(schedule.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var dbMarkedSession = await _context.TblSessions.FindAsync(markedFutureSession.Id);
        Assert.NotNull(dbMarkedSession);
        Assert.False(dbMarkedSession.IsDeleted); // Preserved!

        var dbUnmarkedSession = await _context.TblSessions.FindAsync(unmarkedFutureSession.Id);
        Assert.NotNull(dbUnmarkedSession);
        Assert.True(dbUnmarkedSession.IsDeleted); // Deleted!
    }
}
