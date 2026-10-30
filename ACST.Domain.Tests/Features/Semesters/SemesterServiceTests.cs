using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Semester;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Semesters;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ACST.Domain.Tests.Features.Semesters;

public class SemesterServiceTests
{
    private readonly AppDbContext _context;
    private readonly SemesterService _service;

    public SemesterServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var googleCalendarMock = new DisabledGoogleCalendarService(new NullLogger<DisabledGoogleCalendarService>());
        _service = new SemesterService(_context, googleCalendarMock);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task CreateSemesterAsync_ValidDateRange_SucceedsAndPersists()
    {
        // Arrange
        var request = new CreateSemesterRequest
        {
            Name = "Spring 2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 5, 31),
            LectureEndDate = new DateOnly(2026, 5, 15)
        };

        // Act
        var result = await _service.CreateSemesterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Spring 2026", result.Data.Name);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Data.StartDate);
        Assert.Equal(new DateOnly(2026, 5, 31), result.Data.EndDate);
        Assert.Equal(new DateOnly(2026, 5, 15), result.Data.LectureEndDate);

        var saved = await _context.TblSemesters.FindAsync(result.Data.Id);
        Assert.NotNull(saved);
        Assert.False(saved.IsDeleted);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task CreateSemesterAsync_StartDateAfterEndDate_ReturnsFailure()
    {
        // Arrange
        var request = new CreateSemesterRequest
        {
            Name = "Invalid Semester",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 1, 1) // Start > End
        };

        // Act
        var result = await _service.CreateSemesterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Start date cannot be after end date.", result.Message);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task CreateSemesterAsync_LectureEndDateOutOfRange_ReturnsFailure()
    {
        // Arrange: LectureEndDate is after Semester EndDate
        var request = new CreateSemesterRequest
        {
            Name = "Invalid Lecture End",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 5, 31),
            LectureEndDate = new DateOnly(2026, 6, 15) // Outside range
        };

        // Act
        var result = await _service.CreateSemesterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Lecture end date must fall between semester start date and end date.", result.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task GetSemesterByIdAsync_ExistingId_ReturnsSemesterDto()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Summer 2026",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 8, 31),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSemesterByIdAsync(semester.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Summer 2026", result.Data.Name);
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task GetSemesterByIdAsync_NonExistentOrDeletedId_ReturnsFailure()
    {
        // Arrange
        var deleted = new TblSemester
        {
            Name = "Deleted Semester",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 5, 31),
            IsDeleted = true
        };
        _context.TblSemesters.Add(deleted);
        await _context.SaveChangesAsync();

        // Act
        var notFoundResult = await _service.GetSemesterByIdAsync(999);
        var deletedResult = await _service.GetSemesterByIdAsync(deleted.Id);

        // Assert
        Assert.False(notFoundResult.IsSuccess);
        Assert.Equal("Semester not found.", notFoundResult.Message);

        Assert.False(deletedResult.IsSuccess);
        Assert.Equal("Semester not found.", deletedResult.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task GetAllSemestersAsync_ValidPagination_ReturnsPagedResultsInDescendingOrder()
    {
        // Arrange
        _context.TblSemesters.AddRange(
            new TblSemester { Name = "Semester 2024", StartDate = new DateOnly(2024, 1, 1), EndDate = new DateOnly(2024, 5, 1), IsDeleted = false },
            new TblSemester { Name = "Semester 2025", StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2025, 5, 1), IsDeleted = false },
            new TblSemester { Name = "Deleted 2026", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 5, 1), IsDeleted = true }
        );
        await _context.SaveChangesAsync();

        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetAllSemestersAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        // Descending order by StartDate
        Assert.Equal("Semester 2025", result.Data[0].Name);
        Assert.Equal("Semester 2024", result.Data[1].Name);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task GetAllSemestersAsync_NullRequest_ReturnsFailure()
    {
        // Act
        var result = await _service.GetAllSemestersAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Pagination request cannot be null.", result.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task UpdateSemesterAsync_SettingLectureEndDate_SoftDeletesOutOfRangeSessions()
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
        await _context.SaveChangesAsync();

        var session1 = new TblSession
        {
            SemesterId = semester.Id,
            SessionDate = new DateOnly(2026, 9, 5),
            StartDatetime = DateTime.UtcNow,
            EndDatetime = DateTime.UtcNow.AddHours(2),
            Status = "Not Marked",
            IsDeleted = false
        };
        var session2 = new TblSession
        {
            SemesterId = semester.Id,
            SessionDate = new DateOnly(2026, 9, 10),
            StartDatetime = DateTime.UtcNow,
            EndDatetime = DateTime.UtcNow.AddHours(2),
            Status = "Not Marked",
            IsDeleted = false
        };
        var sessionOutOfRange = new TblSession
        {
            SemesterId = semester.Id,
            SessionDate = new DateOnly(2026, 9, 20),
            StartDatetime = DateTime.UtcNow,
            EndDatetime = DateTime.UtcNow.AddHours(2),
            Status = "Not Marked",
            IsDeleted = false
        };
        _context.TblSessions.AddRange(session1, session2, sessionOutOfRange);
        await _context.SaveChangesAsync();

        // Act - set lecture end date to Sep 11
        var updateRequest = new UpdateSemesterRequest
        {
            Name = "Fall 2026 Updated",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30),
            LectureEndDate = new DateOnly(2026, 9, 11)
        };
        var result = await _service.UpdateSemesterAsync(semester.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(new DateOnly(2026, 9, 11), result.Data.LectureEndDate);

        var s1 = await _context.TblSessions.FindAsync(session1.Id);
        var s2 = await _context.TblSessions.FindAsync(session2.Id);
        var sOut = await _context.TblSessions.FindAsync(sessionOutOfRange.Id);

        Assert.NotNull(s1);
        Assert.False(s1.IsDeleted);

        Assert.NotNull(s2);
        Assert.False(s2.IsDeleted);

        Assert.NotNull(sOut);
        Assert.True(sOut.IsDeleted); // Out of range session must be soft-deleted!
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task DeleteSemesterAsync_ExistingSemester_SoftDeletesSemesterAndCascadeSoftDeletesModules()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "To Delete",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 5, 31),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        var module = new TblModule
        {
            Name = "Module 1",
            ModuleCode = "M-1",
            SemesterId = semester.Id,
            IsDeleted = false
        };
        _context.TblModules.Add(module);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteSemesterAsync(semester.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var deletedSemester = await _context.TblSemesters.FindAsync(semester.Id);
        Assert.NotNull(deletedSemester);
        Assert.True(deletedSemester.IsDeleted);

        var deletedModule = await _context.TblModules.FindAsync(module.Id);
        Assert.NotNull(deletedModule);
        Assert.True(deletedModule.IsDeleted);
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task DeleteSemesterAsync_NonExistentSemester_ReturnsFailure()
    {
        // Act
        var result = await _service.DeleteSemesterAsync(777);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Semester not found.", result.Message);
    }
}
