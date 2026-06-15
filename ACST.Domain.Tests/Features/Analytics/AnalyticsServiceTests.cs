using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Analytics;
using ACST.Domain.Features.Analytics;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ACST.Domain.Tests.Features.Analytics;

public class AnalyticsServiceTests
{
    private readonly AppDbContext _context;
    private readonly AnalyticsService _service;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        _service = new AnalyticsService(_context);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateReconciliationAndBreakdownsCorrectly()
    {
        // Arrange
        var semester = new TblSemester
        {
            Id = 1,
            Name = "Test Semester",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Id = 1, Name = "Module 1" };
        _context.TblModules.Add(module);

        var schedule = new TblRecurringSchedule { Id = 1, ModuleId = 1, SemesterId = 1 };
        _context.TblRecurringSchedules.Add(schedule);

        // Add 5 sessions:
        // 1. Jan 5 (Monday) - Present
        // 2. Jan 12 (Monday) - Absent
        // 3. Jan 19 (Monday) - Late
        // 4. Jan 26 (Monday) - Cancelled (excluded)
        // 5. Jan 15 (Thursday) - Holiday (excluded)
        var sessions = new[]
        {
            new TblSession
            {
                Id = 1, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 5), StartDatetime = new DateTime(2026, 1, 5, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 5, 10, 30, 0),
                Status = "Present"
            },
            new TblSession
            {
                Id = 2, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 12), StartDatetime = new DateTime(2026, 1, 12, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 12, 10, 30, 0),
                Status = "Absent"
            },
            new TblSession
            {
                Id = 3, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 19), StartDatetime = new DateTime(2026, 1, 19, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 19, 10, 30, 0),
                Status = "Late"
            },
            new TblSession
            {
                Id = 4, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 26), StartDatetime = new DateTime(2026, 1, 26, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 26, 10, 30, 0),
                Status = "Cancelled"
            },
            new TblSession
            {
                Id = 5, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 15), StartDatetime = new DateTime(2026, 1, 15, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 15, 10, 30, 0),
                Status = "Holiday"
            }
        };

        _context.TblSessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardSummaryAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        var data = result.Data;
        Assert.NotNull(data);

        Assert.Equal("Test Semester", data.SemesterName);
        Assert.Equal(new DateOnly(2026, 1, 1), data.StartDate);
        Assert.Equal(new DateOnly(2026, 1, 31), data.EndDate);

        // Reconciliation
        Assert.Equal(5, data.TotalSessions);
        Assert.Equal(1, data.PresentSessions);
        Assert.Equal(1, data.AbsentSessions);
        Assert.Equal(1, data.LateSessions);
        Assert.Equal(1, data.CancelledSessions);
        Assert.Equal(1, data.HolidaySessions);
        Assert.Equal(3, data.ValidSessions); // 5 - (1 cancelled + 1 holiday)
        
        // Present (1) / Valid (3) * 100 = 33.33%
        Assert.Equal(33.33, data.SemesterHealthRate);
        Assert.Equal(33.33, data.CalculatedRate);

        // Daily breakdown
        // Mondays: 4 sessions (Jan 5 - Present, Jan 12 - Absent, Jan 19 - Late, Jan 26 - Cancelled)
        // Valid Monday sessions = 3 (Total 4 - Cancelled 1)
        // Present Monday sessions = 1
        // Monday Rate = 1/3 * 100 = 33.33%
        var mondayStats = data.DailyAttendance.FirstOrDefault(d => d.DayOfWeek == "Monday");
        Assert.NotNull(mondayStats);
        Assert.Equal(4, mondayStats.TotalSessions);
        Assert.Equal(1, mondayStats.Present);
        Assert.Equal(1, mondayStats.Absent);
        Assert.Equal(1, mondayStats.Late);
        Assert.Equal(1, mondayStats.Cancelled);
        Assert.Equal(3, mondayStats.ValidSessions);
        Assert.Equal(33.33, mondayStats.AttendanceRate);

        // Thursdays: 1 session (Jan 15 - Holiday)
        // Valid Thursday sessions = 0 (Total 1 - Holiday 1)
        // Thursday Rate = 0%
        var thursdayStats = data.DailyAttendance.FirstOrDefault(d => d.DayOfWeek == "Thursday");
        Assert.NotNull(thursdayStats);
        Assert.Equal(1, thursdayStats.TotalSessions);
        Assert.Equal(0, thursdayStats.Present);
        Assert.Equal(1, thursdayStats.Holiday);
        Assert.Equal(0, thursdayStats.ValidSessions);
        Assert.Equal(0, thursdayStats.AttendanceRate);

        // Weekly breakdown (calendar weeks starting Monday)
        // Sessions:
        // Jan 5 (Mon) -> Week starting Jan 5 (Mon)
        // Jan 12 (Mon) -> Week starting Jan 12 (Mon)
        // Jan 15 (Thu) -> Week starting Jan 12 (Mon)
        // Jan 19 (Mon) -> Week starting Jan 19 (Mon)
        // Jan 26 (Mon) -> Week starting Jan 26 (Mon)
        
        Assert.Equal(4, data.WeeklyAttendance.Count);

        // Week 1 (Jan 5 - Jan 11): Jan 5 session (Present) -> 100%
        var w1 = data.WeeklyAttendance.FirstOrDefault(w => w.WeekStartDate == new DateOnly(2026, 1, 5));
        Assert.NotNull(w1);
        Assert.Equal(1, w1.TotalSessions);
        Assert.Equal(1, w1.Present);
        Assert.Equal(1, w1.ValidSessions);
        Assert.Equal(100.0, w1.AttendanceRate);

        // Week 2 (Jan 12 - Jan 18): Jan 12 (Absent), Jan 15 (Holiday) -> Valid = 1, Present = 0 -> 0%
        var w2 = data.WeeklyAttendance.FirstOrDefault(w => w.WeekStartDate == new DateOnly(2026, 1, 12));
        Assert.NotNull(w2);
        Assert.Equal(2, w2.TotalSessions);
        Assert.Equal(0, w2.Present);
        Assert.Equal(1, w2.Holiday);
        Assert.Equal(1, w2.ValidSessions);
        Assert.Equal(0.0, w2.AttendanceRate);

        // Monthly breakdown
        Assert.Single(data.MonthlyAttendance);
        var m1 = data.MonthlyAttendance.First();
        Assert.Equal("January 2026", m1.MonthName);
        Assert.Equal(5, m1.TotalSessions);
        Assert.Equal(3, m1.ValidSessions);
        Assert.Equal(33.33, m1.AttendanceRate);
    }
}
