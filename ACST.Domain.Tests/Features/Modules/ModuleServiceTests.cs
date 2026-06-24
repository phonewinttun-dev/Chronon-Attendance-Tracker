using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ACST.Domain.Tests.Features.Modules;

public class ModuleServiceTests
{
    private readonly AppDbContext _context;
    private readonly ModuleService _service;
    private readonly IGoogleCalendarService _googleCalendarMock;

    public ModuleServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        _googleCalendarMock = new GoogleCalendarService(new NullLogger<GoogleCalendarService>());
        _service = new ModuleService(_context, _googleCalendarMock);
    }

    [Fact]
    public async Task DeleteModuleAsync_ShouldCascadeSoftDeleteSchedulesAndSessions()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Test Semester",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Math 101", ModuleCode = "MATH-101", Semester = semester };
        _context.TblModules.Add(module);
        
        await _context.SaveChangesAsync();

        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            DayOfWeek = 1,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            IsActive = true,
            IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(schedule);

        var session = new TblSession
        {
            ModuleId = module.Id,
            SemesterId = semester.Id,
            RecurringSchedule = schedule,
            SessionDate = new DateOnly(2026, 1, 5),
            StartDatetime = DateTime.UtcNow,
            EndDatetime = DateTime.UtcNow.AddHours(2),
            Status = "Not Marked",
            GoogleEventId = "test-event-id",
            IsDeleted = false
        };
        _context.TblSessions.Add(session);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteModuleAsync(module.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var dbModule = await _context.TblModules.FindAsync(module.Id);
        Assert.NotNull(dbModule);
        Assert.True(dbModule.IsDeleted);

        var dbSchedule = await _context.TblRecurringSchedules.FindAsync(schedule.Id);
        Assert.NotNull(dbSchedule);
        Assert.True(dbSchedule.IsDeleted);

        var dbSession = await _context.TblSessions.FindAsync(session.Id);
        Assert.NotNull(dbSession);
        Assert.True(dbSession.IsDeleted);
    }
}
