using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Semester;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Semesters;
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
}
