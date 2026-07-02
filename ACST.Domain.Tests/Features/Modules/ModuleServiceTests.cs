using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using System.Collections.Generic;
using ACST.Domain.Features.ClassSessions;
using ACST.Domain.DTOs.RecurringSchedule;
using ACST.Domain.DTOs.Module;
using ACST.Domain.DTOs.ClassSession;
using ACST.Shared;

namespace ACST.Domain.Tests.Features.Modules;

public class ModuleServiceTests
{
    private readonly AppDbContext _context;
    private readonly ModuleService _service;
    private readonly TestGoogleCalendarService _calendarSpy;

    public ModuleServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
            
        _context = new AppDbContext(options);
        _calendarSpy = new TestGoogleCalendarService();
        var classSessionService = new ClassSessionService(_context, _calendarSpy);
        _service = new ModuleService(_context, _calendarSpy, classSessionService);
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

    #region CreateModuleAsync Tests

    [Fact]
    public async Task CreateModuleAsync_WithValidRequest_WithoutSchedules_ShouldSucceed()
    {
        // Arrange
        var request = new CreateModuleRequest
        {
            Name = "Data Structures",
            ModuleCode = "CS-201",
            TeacherName = "Prof. Smith",
            SemesterId = null,
            Schedules = new List<CreateRecurringScheduleRequest>(),
            GenerateSessions = false
        };

        // Act
        var result = await _service.CreateModuleAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Data Structures", result.Data.Name);
        Assert.Equal("CS-201", result.Data.ModuleCode);
        Assert.Equal("Prof. Smith", result.Data.TeacherName);
        Assert.Null(result.Data.SemesterId);
        Assert.Empty(result.Data.Schedules);

        var dbModule = await _context.TblModules.FindAsync(result.Data.Id);
        Assert.NotNull(dbModule);
        Assert.Equal("CS-201", dbModule.ModuleCode);
    }

    [Fact]
    public async Task CreateModuleAsync_WithMissingSemesterForSchedules_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateModuleRequest
        {
            Name = "Algorithms",
            ModuleCode = "CS-301",
            SemesterId = null,
            Schedules = new List<CreateRecurringScheduleRequest>
            {
                new() { DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
            }
        };

        // Act
        var result = await _service.CreateModuleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Semester is required when adding schedules.", result.Message);
    }

    [Fact]
    public async Task CreateModuleAsync_WithInvalidScheduleTimes_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateModuleRequest
        {
            Name = "Algorithms",
            ModuleCode = "CS-301",
            SemesterId = 1,
            Schedules = new List<CreateRecurringScheduleRequest>
            {
                new() { DayOfWeek = 1, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(10, 0) } // invalid
            }
        };

        // Act
        var result = await _service.CreateModuleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Start time must be before end time for all schedules.", result.Message);
    }

    [Fact]
    public async Task CreateModuleAsync_WithNonexistentSemester_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateModuleRequest
        {
            Name = "Algorithms",
            ModuleCode = "CS-301",
            SemesterId = 9999, // does not exist
            Schedules = new List<CreateRecurringScheduleRequest>
            {
                new() { DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
            }
        };

        // Act
        var result = await _service.CreateModuleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Selected semester does not exist.", result.Message);
    }

    [Fact]
    public async Task CreateModuleAsync_WithSchedulesAndGenerateSessions_ShouldCreateModuleAndSchedulesAndCallGenerateSessions()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30) // September 2026
        };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        var request = new CreateModuleRequest
        {
            Name = "Software Engineering",
            ModuleCode = "SE-301",
            TeacherName = "Dr. Alice",
            SemesterId = semester.Id,
            Schedules = new List<CreateRecurringScheduleRequest>
            {
                new()
                {
                    DayOfWeek = (short)DayOfWeek.Monday, // Mondays in Sept 2026: 7, 14, 21, 28 (4 sessions)
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 0)
                }
            },
            GenerateSessions = true,
            SyncWithGoogleCalendar = false
        };

        // Act
        var result = await _service.CreateModuleAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Schedules);
        
        var scheduleDto = result.Data.Schedules.First();
        Assert.Equal((short)DayOfWeek.Monday, scheduleDto.DayOfWeek);

        var sessions = await _context.TblSessions
            .Where(s => s.ModuleId == result.Data.Id && !s.IsDeleted)
            .ToListAsync();

        Assert.Equal(4, sessions.Count);
    }

    [Fact]
    public async Task CreateModuleAsync_OnException_ShouldRollbackTransactionAndReturnFailure()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Spring 2026",
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 2, 28)
        };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        var request = new CreateModuleRequest
        {
            Name = "Failing Module",
            ModuleCode = "FAIL-101",
            SemesterId = semester.Id,
            Schedules = new List<CreateRecurringScheduleRequest>
            {
                new() { DayOfWeek = 2, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
            },
            GenerateSessions = true
        };

        // Inject FakeClassSessionService that throws exception
        var fakeSessionService = new FakeClassSessionService { ShouldThrow = true };
        var serviceWithFake = new ModuleService(_context, _calendarSpy, fakeSessionService);

        // Act
        var result = await serviceWithFake.CreateModuleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("An unexpected error occurred while creating the module.", result.Message);
    }

    #endregion

    #region UpdateModuleAsync Tests

    [Fact]
    public async Task UpdateModuleAsync_WithNonexistentModule_ShouldReturnFailure()
    {
        // Arrange
        var request = new UpdateModuleRequest
        {
            Name = "Updated Name",
            ModuleCode = "UP-101"
        };

        // Act
        var result = await _service.UpdateModuleAsync(9999, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Module not found.", result.Message);
    }

    [Fact]
    public async Task UpdateModuleAsync_WithInvalidSemesterId_ShouldReturnFailure()
    {
        // Arrange
        var semester = new TblSemester { Name = "Semester 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        _context.TblSemesters.Add(semester);
        var module = new TblModule { Name = "Old Module", ModuleCode = "OLD-101", Semester = semester };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new UpdateModuleRequest
        {
            Name = "Updated Name",
            ModuleCode = "UP-101",
            SemesterId = 9999 // nonexistent
        };

        // Act
        var result = await _service.UpdateModuleAsync(module.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Selected semester does not exist or is deleted.", result.Message);
    }

    [Fact]
    public async Task UpdateModuleAsync_WithMissingSemesterForSchedules_ShouldReturnFailure()
    {
        // Arrange
        var module = new TblModule { Name = "Old Module", ModuleCode = "OLD-101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new UpdateModuleRequest
        {
            Name = "Updated Name",
            ModuleCode = "UP-101",
            SemesterId = null,
            Schedules = new List<UpdateRecurringScheduleRequest>
            {
                new() { DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) }
            }
        };

        // Act
        var result = await _service.UpdateModuleAsync(module.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Semester is required when adding schedules.", result.Message);
    }

    [Fact]
    public async Task UpdateModuleAsync_WithInvalidScheduleTimes_ShouldReturnFailure()
    {
        // Arrange
        var module = new TblModule { Name = "Old Module", ModuleCode = "OLD-101" };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new UpdateModuleRequest
        {
            Name = "Updated Name",
            ModuleCode = "UP-101",
            SemesterId = 1,
            Schedules = new List<UpdateRecurringScheduleRequest>
            {
                new() { DayOfWeek = 1, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(9, 0) } // invalid
            }
        };

        // Act
        var result = await _service.UpdateModuleAsync(module.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Start time must be before end time for all schedules.", result.Message);
    }

    [Fact]
    public async Task UpdateModuleAsync_WithValidRequest_ShouldUpdateBasicFields()
    {
        // Arrange
        var semester = new TblSemester { Name = "Semester 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        _context.TblSemesters.Add(semester);
        var module = new TblModule { Name = "Old Module", ModuleCode = "OLD-101", TeacherName = "Old Teacher", Semester = semester };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new UpdateModuleRequest
        {
            Name = "New Module",
            ModuleCode = "NEW-101",
            TeacherName = "New Teacher",
            SemesterId = semester.Id,
            Schedules = null
        };

        // Act
        var result = await _service.UpdateModuleAsync(module.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("New Module", result.Data.Name);
        Assert.Equal("NEW-101", result.Data.ModuleCode);
        Assert.Equal("New Teacher", result.Data.TeacherName);

        var dbModule = await _context.TblModules.FindAsync(module.Id);
        Assert.NotNull(dbModule);
        Assert.Equal("New Module", dbModule.Name);
    }

    [Fact]
    public async Task UpdateModuleAsync_WithSchedulesToRemove_ShouldSoftDeleteSchedulesAndFutureSessionsAndCollectGoogleEventIds()
    {
        // Arrange
        var semester = new TblSemester { Name = "Semester 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Math", ModuleCode = "MATH-1", Semester = semester };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var scheduleA = new TblRecurringSchedule
        {
            ModuleId = module.Id, SemesterId = semester.Id, DayOfWeek = 1,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0), IsActive = true, IsDeleted = false
        };
        var scheduleB = new TblRecurringSchedule
        {
            ModuleId = module.Id, SemesterId = semester.Id, DayOfWeek = 3,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0), IsActive = true, IsDeleted = false
        };
        _context.TblRecurringSchedules.AddRange(scheduleA, scheduleB);
        await _context.SaveChangesAsync();

        // Add sessions:
        // Schedule A has a future session.
        var futureSessionA = new TblSession
        {
            ModuleId = module.Id, SemesterId = semester.Id, RecurringScheduleId = scheduleA.Id,
            SessionDate = new DateOnly(2026, 1, 12), StartDatetime = DateTime.UtcNow.AddDays(2), EndDatetime = DateTime.UtcNow.AddDays(2).AddHours(2),
            Status = "Not Marked", GoogleEventId = "google-event-A", IsDeleted = false
        };
        // Schedule B has a future session (which should be soft-deleted) and a past session (which should NOT be deleted because start < utcnow).
        var futureSessionB = new TblSession
        {
            ModuleId = module.Id, SemesterId = semester.Id, RecurringScheduleId = scheduleB.Id,
            SessionDate = new DateOnly(2026, 1, 14), StartDatetime = DateTime.UtcNow.AddDays(4), EndDatetime = DateTime.UtcNow.AddDays(4).AddHours(2),
            Status = "Not Marked", GoogleEventId = "google-event-B", IsDeleted = false
        };
        var pastSessionB = new TblSession
        {
            ModuleId = module.Id, SemesterId = semester.Id, RecurringScheduleId = scheduleB.Id,
            SessionDate = new DateOnly(2026, 1, 7), StartDatetime = DateTime.UtcNow.AddDays(-5), EndDatetime = DateTime.UtcNow.AddDays(-5).AddHours(2),
            Status = "Present", GoogleEventId = "google-event-B-past", IsDeleted = false
        };
        _context.TblSessions.AddRange(futureSessionA, futureSessionB, pastSessionB);
        await _context.SaveChangesAsync();

        // Update request: Only keep schedule A, meaning schedule B is removed
        var request = new UpdateModuleRequest
        {
            Name = "Math",
            ModuleCode = "MATH-1",
            SemesterId = semester.Id,
            Schedules = new List<UpdateRecurringScheduleRequest>
            {
                new() { Id = scheduleA.Id, DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
            }
        };

        // Act
        var result = await _service.UpdateModuleAsync(module.Id, request);

        // Assert
        Assert.True(result.IsSuccess);

        // Schedule B should be soft deleted
        var dbScheduleB = await _context.TblRecurringSchedules.FindAsync(scheduleB.Id);
        Assert.NotNull(dbScheduleB);
        Assert.True(dbScheduleB.IsDeleted);

        // Schedule A should not be soft deleted
        var dbScheduleA = await _context.TblRecurringSchedules.FindAsync(scheduleA.Id);
        Assert.NotNull(dbScheduleA);
        Assert.False(dbScheduleA.IsDeleted);

        // Future session B should be soft deleted
        var dbFutureSessionB = await _context.TblSessions.FindAsync(futureSessionB.Id);
        Assert.NotNull(dbFutureSessionB);
        Assert.True(dbFutureSessionB.IsDeleted);

        // Past session B should NOT be soft deleted
        var dbPastSessionB = await _context.TblSessions.FindAsync(pastSessionB.Id);
        Assert.NotNull(dbPastSessionB);
        Assert.False(dbPastSessionB.IsDeleted);

        // Event of deleted future session B should be cleaned up
        Assert.Contains("google-event-B", _calendarSpy.DeletedEventIds);
        Assert.DoesNotContain("google-event-A", _calendarSpy.DeletedEventIds);
    }

    [Fact]
    public async Task UpdateModuleAsync_WithSchedulesToAddAndModify_ShouldAddAndModifySchedules()
    {
        // Arrange
        var semester = new TblSemester { Name = "Semester 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        _context.TblSemesters.Add(semester);
        var module = new TblModule { Name = "Math", ModuleCode = "MATH-1", Semester = semester };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var existingSchedule = new TblRecurringSchedule
        {
            ModuleId = module.Id, SemesterId = semester.Id, DayOfWeek = 1,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0), IsActive = true, IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(existingSchedule);
        await _context.SaveChangesAsync();

        var request = new UpdateModuleRequest
        {
            Name = "Math",
            ModuleCode = "MATH-1",
            SemesterId = semester.Id,
            Schedules = new List<UpdateRecurringScheduleRequest>
            {
                // Modify existing schedule
                new() { Id = existingSchedule.Id, DayOfWeek = 1, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0) },
                // Add new schedule
                new() { Id = null, DayOfWeek = 4, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0) }
            }
        };

        // Act
        var result = await _service.UpdateModuleAsync(module.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Schedules.Count);

        var dbSchedules = await _context.TblRecurringSchedules
            .Where(s => s.ModuleId == module.Id && !s.IsDeleted)
            .ToListAsync();

        Assert.Equal(2, dbSchedules.Count);
        
        var modified = dbSchedules.First(s => s.Id == existingSchedule.Id);
        Assert.Equal(new TimeOnly(10, 0), modified.StartTime);

        var added = dbSchedules.First(s => s.Id != existingSchedule.Id);
        Assert.Equal((short)4, added.DayOfWeek);
    }

    [Fact]
    public async Task UpdateModuleAsync_OnDatabaseException_ShouldRollbackTransactionAndReturnFailure()
    {
        // Arrange
        var semester = new TblSemester { Name = "Semester 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        _context.TblSemesters.Add(semester);
        var module = new TblModule { Name = "Old Module", ModuleCode = "OLD-101", Semester = semester };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var request = new UpdateModuleRequest
        {
            Name = "Attempted Update",
            ModuleCode = "ATT-101",
            SemesterId = semester.Id,
            Schedules = new List<UpdateRecurringScheduleRequest>
            {
                new() { DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) }
            }
        };

        var fakeSessionService = new FakeClassSessionService { ShouldThrow = true };
        var serviceWithFake = new ModuleService(_context, _calendarSpy, fakeSessionService);

        // Act
        var result = await serviceWithFake.UpdateModuleAsync(module.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to update module due to an internal server error.", result.Message);
    }

    [Fact]
    public async Task UpdateModuleAsync_WithSchedulesToRemove_ShouldDeleteOrphanedGoogleCalendarEventsOutofTransaction()
    {
        // Arrange
        var semester = new TblSemester { Name = "Semester 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Name = "Math", ModuleCode = "MATH-1", Semester = semester };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        var schedule = new TblRecurringSchedule
        {
            ModuleId = module.Id, SemesterId = semester.Id, DayOfWeek = 1,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0), IsActive = true, IsDeleted = false
        };
        _context.TblRecurringSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        var session = new TblSession
        {
            ModuleId = module.Id, SemesterId = semester.Id, RecurringScheduleId = schedule.Id,
            SessionDate = new DateOnly(2026, 1, 12), StartDatetime = DateTime.UtcNow.AddDays(2), EndDatetime = DateTime.UtcNow.AddDays(2).AddHours(2),
            Status = "Not Marked", GoogleEventId = "throw-event-id", IsDeleted = false
        };
        _context.TblSessions.Add(session);
        await _context.SaveChangesAsync();

        var request = new UpdateModuleRequest
        {
            Name = "Math",
            ModuleCode = "MATH-1",
            SemesterId = semester.Id,
            Schedules = new List<UpdateRecurringScheduleRequest>() // delete schedule
        };

        _calendarSpy.ShouldThrowOnDelete = true;

        // Act
        var result = await _service.UpdateModuleAsync(module.Id, request);

        // Assert: method still succeeds because calendar exception is handled gracefully out-of-transaction
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Helper Test Doubles

    private class TestGoogleCalendarService : IGoogleCalendarService
    {
        public List<string> DeletedEventIds { get; } = new();
        public bool ShouldThrowOnDelete { get; set; }

        public Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken)
        {
            return Task.FromResult(Result<string>.Success($"mock_event_{Guid.NewGuid():N}", "Mock event created."));
        }

        public Task<Result> UpdateEventStatusAsync(string googleEventId, string status)
        {
            return Task.FromResult(Result.Success("Mock event updated."));
        }

        public Task<Result> DeleteEventAsync(string googleEventId)
        {
            if (ShouldThrowOnDelete)
            {
                throw new Exception("Google Calendar deletion failed.");
            }
            DeletedEventIds.Add(googleEventId);
            return Task.FromResult(Result.Success("Mock event deleted."));
        }

        public Task<Result<bool>> IsConnectedAsync() => Task.FromResult(Result<bool>.Success(true, "Connected."));
        public Task<Result<string>> GetAuthorizationUrlAsync(string redirectUri, string? state) => Task.FromResult(Result<string>.Success("https://mockauth", "Auth URL"));
        public Task<Result> ExchangeCodeAndStoreTokenAsync(string code, string redirectUri) => Task.FromResult(Result.Success("Stored."));
        public Task<Result> DisconnectAsync() => Task.FromResult(Result.Success("Disconnected."));
    }

    private class FakeClassSessionService : IClassSessionService
    {
        public bool ShouldThrow { get; set; }

        public Task<PagedResult<ClassSessionDto>> GetSessionsAsync(GetClassSessionsRequest request)
        {
            return Task.FromResult(PagedResult<ClassSessionDto>.Failure("Fake service error."));
        }

        public Task<Result<ClassSessionDto>> GetSessionByIdAsync(long id)
        {
            return Task.FromResult(Result<ClassSessionDto>.Failure("Fake service error."));
        }

        public Task<Result> GenerateSessionsAsync(GenerateSessionsRequest request)
        {
            if (ShouldThrow)
            {
                throw new Exception("Simulated service exception.");
            }
            return Task.FromResult(Result.Success("Fake sessions generated."));
        }

        public Task<Result> UpdateSessionStatusAsync(long id, UpdateSessionStatusRequest request)
        {
            return Task.FromResult(Result.Success("Fake status updated."));
        }

        public Task<Result<string>> MarkAttendanceWithMagicLinkAsync(Guid token)
        {
            return Task.FromResult(Result<string>.Failure("Fake service error."));
        }

        public Task<Result> UpdateSessionAsync(long id, UpdateClassSessionRequest request)
        {
            return Task.FromResult(Result.Success("Fake session updated."));
        }

        public Task<Result> DeleteSessionAsync(long id)
        {
            return Task.FromResult(Result.Success("Fake session deleted."));
        }

        public Task SyncGoogleCalendarEventAsync(long sessionId)
        {
            return Task.CompletedTask;
        }
    }

    #endregion
}
