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
        Assert.True(dbSchedule.IsDeleted);

        var dbPastSession = await _context.TblSessions.FindAsync(pastSession.Id);
        Assert.False(dbPastSession.IsDeleted); // Remains untouched

        var dbFutureSession = await _context.TblSessions.FindAsync(futureSession.Id);
        Assert.True(dbFutureSession.IsDeleted); // Soft-deleted
    }
}
