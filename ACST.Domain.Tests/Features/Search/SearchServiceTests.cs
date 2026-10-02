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
        Assert.Single(result.Data);
        Assert.Equal("SE202", result.Data[0].ModuleCode);
    }

    [Fact]
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
        Assert.Single(result.Data);
        Assert.Equal("Spring 2026", result.Data[0].Name);
    }
}
