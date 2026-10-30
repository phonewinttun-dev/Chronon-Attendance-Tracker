using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Search;
using ACST.Domain.Features.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ACST.Domain.Tests.Features.Search;

public class SearchServiceTests
{
    private readonly AppDbContext _context;
    private readonly SearchService _service;

    public SearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new SearchService(_context);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task SearchModuleAsync_WithPartialCode_ShouldFindModule()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 31),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        var module1 = new TblModule
        {
            Name = "Software Engineering II",
            ModuleCode = "SE202",
            TeacherName = "Alice Smith",
            SemesterId = semester.Id,
            IsDeleted = false
        };
        var module2 = new TblModule
        {
            Name = "Algorithms",
            ModuleCode = "CS101",
            TeacherName = "Bob Jones",
            SemesterId = semester.Id,
            IsDeleted = false
        };
        _context.TblModules.AddRange(module1, module2);
        await _context.SaveChangesAsync();

        // Act - user searches "202" in the search box (Name = "202", other fields null)
        var result = await _service.SearchModuleAsync(new SearchDto { Name = "202" }, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("SE202", result.Data[0].ModuleCode);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task SearchModuleAsync_WithSemesterFilter_ReturnsOnlyModulesInSemester()
    {
        // Arrange
        var sem1 = new TblSemester { Name = "Semester 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 5, 1), IsDeleted = false };
        var sem2 = new TblSemester { Name = "Semester 2", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 10, 1), IsDeleted = false };
        _context.TblSemesters.AddRange(sem1, sem2);
        await _context.SaveChangesAsync();

        _context.TblModules.AddRange(
            new TblModule { Name = "Calculus I", ModuleCode = "MTH101", SemesterId = sem1.Id, IsDeleted = false },
            new TblModule { Name = "Calculus II", ModuleCode = "MTH102", SemesterId = sem2.Id, IsDeleted = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchModuleAsync(new SearchDto { Name = "Calculus" }, 1, 10, semesterId: sem1.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Calculus I", result.Data[0].Name);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task SearchSemesterAsync_WithPartialName_ShouldFindSemester()
    {
        // Arrange
        var semester = new TblSemester
        {
            Name = "Spring 2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 5, 31),
            IsDeleted = false
        };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchSemesterAsync(new SearchDto { Name = "spring" }, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Spring 2026", result.Data[0].Name);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "boundary")]
    public async Task SearchSemesterAsync_NoMatches_ReturnsEmptyPagedList()
    {
        // Arrange
        _context.TblSemesters.Add(new TblSemester
        {
            Name = "Spring 2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 5, 31),
            IsDeleted = false
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchSemesterAsync(new SearchDto { Name = "NonExistentSemester" }, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task SearchSessionAsync_ByPartialModuleName_ReturnsMatchingSessions()
    {
        // Arrange
        var semester = new TblSemester { Name = "Fall 2026", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 31), IsDeleted = false };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        var module1 = new TblModule { Name = "Physics 101", ModuleCode = "PHY101", SemesterId = semester.Id, IsDeleted = false };
        var module2 = new TblModule { Name = "Chemistry 101", ModuleCode = "CHM101", SemesterId = semester.Id, IsDeleted = false };
        _context.TblModules.AddRange(module1, module2);
        await _context.SaveChangesAsync();

        _context.TblSessions.AddRange(
            new TblSession { ModuleId = module1.Id, SemesterId = semester.Id, SessionDate = new DateOnly(2026, 9, 10), StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(2), Status = "Not Marked", IsDeleted = false },
            new TblSession { ModuleId = module2.Id, SemesterId = semester.Id, SessionDate = new DateOnly(2026, 9, 11), StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(2), Status = "Not Marked", IsDeleted = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchSessionAsync(new SearchDto { Name = "Physics" }, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Physics 101", result.Data[0].ModuleName);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task SearchSessionAsync_FilterBySemesterIdAndModuleId_ReturnsFilteredSessions()
    {
        // Arrange
        var sem1 = new TblSemester { Name = "Sem 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 5, 1), IsDeleted = false };
        var sem2 = new TblSemester { Name = "Sem 2", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 10, 1), IsDeleted = false };
        _context.TblSemesters.AddRange(sem1, sem2);
        await _context.SaveChangesAsync();

        var mod1 = new TblModule { Name = "Bio 101", ModuleCode = "BIO1", SemesterId = sem1.Id, IsDeleted = false };
        var mod2 = new TblModule { Name = "Bio 201", ModuleCode = "BIO2", SemesterId = sem2.Id, IsDeleted = false };
        _context.TblModules.AddRange(mod1, mod2);
        await _context.SaveChangesAsync();

        _context.TblSessions.AddRange(
            new TblSession { ModuleId = mod1.Id, SemesterId = sem1.Id, SessionDate = new DateOnly(2026, 2, 1), StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(1), Status = "Not Marked", IsDeleted = false },
            new TblSession { ModuleId = mod2.Id, SemesterId = sem2.Id, SessionDate = new DateOnly(2026, 7, 1), StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(1), Status = "Not Marked", IsDeleted = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchSessionAsync(null!, 1, 10, semesterId: sem1.Id, moduleId: mod1.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal(mod1.Id, result.Data[0].ModuleId);
        Assert.Equal(sem1.Id, result.Data[0].SemesterId);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "boundary")]
    public async Task SearchSessionAsync_SoftDeletedSessionsOrModules_ExcludedFromResults()
    {
        // Arrange
        var semester = new TblSemester { Name = "Fall 2026", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 31), IsDeleted = false };
        _context.TblSemesters.Add(semester);
        await _context.SaveChangesAsync();

        var activeModule = new TblModule { Name = "Active Module", ModuleCode = "ACT1", SemesterId = semester.Id, IsDeleted = false };
        var deletedModule = new TblModule { Name = "Deleted Module", ModuleCode = "DEL1", SemesterId = semester.Id, IsDeleted = true };
        _context.TblModules.AddRange(activeModule, deletedModule);
        await _context.SaveChangesAsync();

        _context.TblSessions.AddRange(
            new TblSession { ModuleId = activeModule.Id, SemesterId = semester.Id, SessionDate = new DateOnly(2026, 9, 10), StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(1), Status = "Not Marked", IsDeleted = false },
            new TblSession { ModuleId = activeModule.Id, SemesterId = semester.Id, SessionDate = new DateOnly(2026, 9, 11), StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(1), Status = "Not Marked", IsDeleted = true },
            new TblSession { ModuleId = deletedModule.Id, SemesterId = semester.Id, SessionDate = new DateOnly(2026, 9, 12), StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(1), Status = "Not Marked", IsDeleted = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchSessionAsync(null!, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Active Module", result.Data[0].ModuleName);
    }
}
